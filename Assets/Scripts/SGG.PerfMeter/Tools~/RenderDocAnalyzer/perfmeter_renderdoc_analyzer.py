import datetime
import hashlib
import json
import math
import os
import struct
import sys
import time


ANALYZER_VERSION = "1.0.0"
REQUEST_SCHEMA = "sgg.perfmeter.renderdoc-analysis-request"
RESULT_SCHEMA = "sgg.perfmeter.renderdoc-analysis"
ERROR_SCHEMA = "sgg.perfmeter.renderdoc-analysis-error"
WORKSPACE_SCHEMA = "sgg.perfmeter.renderdoc-analyzer-workspace"
SCHEMA_VERSION = 1
MAX_DOCUMENT_BYTES = 64 * 1024 * 1024
MAX_CAPTURE_BYTES = 512 * 1024 * 1024
MAX_COUNTERS = 4096
CORE_COUNTER_IDS = tuple(range(1, 16))
MARKER_FILE = ".sgg-perfmeter-renderdoc-analyzer"
REQUEST_FILE = "request.json"
RESPONSE_FILE = "response.json"
STAGE_FILE = "stage.txt"


class AnalyzerFailure(Exception):
    def __init__(self, code, stage, message, retryable=False):
        Exception.__init__(self, message)
        self.code = code
        self.stage = stage
        self.message = message
        self.retryable = retryable


def utc_milliseconds():
    return int(time.time() * 1000.0)


def format_utc(milliseconds):
    value = datetime.datetime.fromtimestamp(milliseconds / 1000.0, datetime.timezone.utc)
    return value.strftime("%Y-%m-%dT%H:%M:%S.") + ("%03dZ" % (milliseconds % 1000))


def set_stage(stage):
    temporary = STAGE_FILE + ".tmp"
    with open(temporary, "xb") as stream:
        stream.write(stage.encode("ascii"))
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, STAGE_FILE)


def bounded_text(value, maximum, allow_empty=True):
    text = str(value) if value is not None else ""
    cleaned = "".join(character if ord(character) >= 32 or character in "\r\n\t" else " " for character in text)
    if not allow_empty and not cleaned:
        raise AnalyzerFailure("invalid_renderdoc_data", "serialize", "RenderDoc returned empty required text.")
    return cleaned[:maximum]


def enum_name(value):
    return bounded_text(str(value).split(".")[-1], 128, False)


def read_bounded_json(path):
    if os.path.islink(path) or not os.path.isfile(path):
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request file is invalid.")
    size = os.path.getsize(path)
    if size <= 0 or size > MAX_DOCUMENT_BYTES:
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request size is invalid.")
    with open(path, "rb") as stream:
        payload = stream.read(MAX_DOCUMENT_BYTES + 1)
    if len(payload) != size or len(payload) > MAX_DOCUMENT_BYTES:
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request changed while being read.")
    try:
        return json.loads(payload.decode("utf-8"))
    except Exception:
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request is not valid UTF-8 JSON.")


def canonical_uint(value, maximum):
    if not isinstance(value, str) or not value or (len(value) > 1 and value[0] == "0") or any(character not in "0123456789" for character in value):
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request contains an invalid unsigned value.")
    parsed = int(value)
    if parsed > maximum:
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request contains an out-of-range unsigned value.")
    return parsed


def require_token(value, maximum=128):
    allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.:/"
    if not isinstance(value, str) or not value or len(value) > maximum or any(character not in allowed for character in value):
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request contains an invalid token.")
    return value


def validate_request(request):
    if not isinstance(request, dict) or request.get("schema") != REQUEST_SCHEMA or type(request.get("schema_version")) is not int or request.get("schema_version") != SCHEMA_VERSION:
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request schema is unsupported.")
    require_token(request.get("request_id"))
    capture = request.get("capture")
    selection = request.get("counter_selection")
    options = request.get("options")
    if not isinstance(capture, dict) or not isinstance(selection, dict) or not isinstance(options, dict):
        raise AnalyzerFailure("invalid_request", "request", "The analyzer request is incomplete.")
    for key in ("artifact_id", "capture_id", "bundle_id"):
        require_token(capture.get(key))
    relative_path = capture.get("path")
    if not isinstance(relative_path, str) or not relative_path or len(relative_path) > 1024 or any(ord(character) < 32 for character in relative_path) or not relative_path.lower().endswith(".rdc") or "\\" in relative_path or ":" in relative_path:
        raise AnalyzerFailure("invalid_capture_path", "request", "The capture path is not a safe project-relative RDC path.")
    segments = relative_path.split("/")
    if any(not segment or segment in (".", "..") for segment in segments):
        raise AnalyzerFailure("invalid_capture_path", "request", "The capture path is not a safe project-relative RDC path.")
    expected_hash = capture.get("sha256")
    if not isinstance(expected_hash, str) or len(expected_hash) != 64 or any(character not in "0123456789abcdef" for character in expected_hash):
        raise AnalyzerFailure("invalid_capture_hash", "request", "The expected capture hash is invalid.")
    source_hash = capture.get("source_file_identity_sha256")
    if not isinstance(source_hash, str) or len(source_hash) != 64 or any(character not in "0123456789abcdef" for character in source_hash):
        raise AnalyzerFailure("invalid_capture_hash", "request", "The source capture hash is invalid.")
    capture_api = capture.get("capture_app_api")
    if not isinstance(capture_api, str) or not capture_api or len(capture_api) > 64 or any(ord(character) < 32 and character not in "\r\n\t" for character in capture_api):
        raise AnalyzerFailure("invalid_capture_authority", "request", "The capture API provenance is invalid.")
    capture_size = canonical_uint(capture.get("size_bytes"), MAX_CAPTURE_BYTES)
    if capture_size == 0:
        raise AnalyzerFailure("invalid_capture_size", "request", "The expected capture size is invalid.")
    if capture.get("association_state") != "bridge_authenticated" or capture.get("finalization_state") != "finalized" or capture.get("authority_state") != "authenticated" or capture.get("content_state") != "present":
        raise AnalyzerFailure("invalid_capture_authority", "request", "The capture is not authenticated and finalized.")
    require_token(selection.get("mode"), 64)
    packs = selection.get("packs")
    explicit = selection.get("explicit_counter_ids")
    if not isinstance(packs, list) or not isinstance(explicit, list) or len(packs) > 64 or len(explicit) > MAX_COUNTERS:
        raise AnalyzerFailure("invalid_counter_selection", "request", "The counter selection is invalid.")
    for value in packs + explicit:
        require_token(value)
    mode = selection.get("mode")
    if len(set(packs)) != len(packs) or len(set(explicit)) != len(explicit) or not (
        (mode == "none" and not packs and not explicit)
        or (mode == "semantic_pack" and packs and not explicit)
        or (mode == "explicit" and not packs and explicit)
        or (mode == "semantic_pack_and_explicit" and packs and explicit)
    ):
        raise AnalyzerFailure("invalid_counter_selection", "request", "The counter selection is invalid.")
    timeout = options.get("timeout_seconds")
    max_actions = options.get("max_actions")
    max_results = options.get("max_counter_results")
    max_output = options.get("max_output_bytes")
    if not isinstance(options.get("include_action_tree"), bool) or not isinstance(options.get("include_raw_counter_descriptions"), bool):
        raise AnalyzerFailure("invalid_options", "request", "The analyzer options are invalid.")
    if type(timeout) is not int or timeout < 1 or timeout > 1800 or type(max_actions) is not int or max_actions < 1 or max_actions > 1000000 or type(max_results) is not int or max_results < 1 or max_results > 1000000 or type(max_output) is not int or max_output < 1024 or max_output > MAX_DOCUMENT_BYTES:
        raise AnalyzerFailure("invalid_options", "request", "The analyzer options are outside supported bounds.")
    return capture_size


def validate_workspace():
    workspace = os.path.realpath(os.getcwd())
    nonce = os.path.basename(workspace)
    analyzer_root = os.path.dirname(workspace)
    perfmeter_root = os.path.dirname(analyzer_root)
    temp_root = os.path.dirname(perfmeter_root)
    project_root = os.path.dirname(temp_root)
    if len(nonce) != 32 or any(character not in "0123456789abcdef" for character in nonce):
        raise AnalyzerFailure("invalid_workspace", "workspace", "The analyzer workspace nonce is invalid.")
    if os.path.basename(analyzer_root) != "RenderDocAnalyzer" or os.path.basename(perfmeter_root) != "PerfMeter" or os.path.basename(temp_root) != "Temp":
        raise AnalyzerFailure("invalid_workspace", "workspace", "The analyzer workspace is outside its fixed project location.")
    marker_path = os.path.join(workspace, MARKER_FILE)
    if os.path.islink(marker_path) or not os.path.isfile(marker_path) or os.path.getsize(marker_path) > 512:
        raise AnalyzerFailure("invalid_workspace", "workspace", "The analyzer workspace marker is invalid.")
    with open(marker_path, "rt", encoding="utf-8") as stream:
        marker = stream.read(513)
    expected = "schema=%s\nversion=1\nnonce=%s\n" % (WORKSPACE_SCHEMA, nonce)
    if marker != expected:
        raise AnalyzerFailure("invalid_workspace", "workspace", "The analyzer workspace marker does not match its directory.")
    return workspace, project_root


def resolve_capture(project_root, relative_path):
    capture_path = os.path.abspath(os.path.join(project_root, *relative_path.split("/")))
    try:
        contained = os.path.normcase(os.path.commonpath((project_root, capture_path))) == os.path.normcase(project_root)
    except Exception:
        contained = False
    current = project_root
    for segment in relative_path.split("/"):
        current = os.path.join(current, segment)
        if os.path.islink(current):
            raise AnalyzerFailure("capture_unavailable", "hash", "The authenticated capture is unavailable.")
    if not contained or os.path.normcase(os.path.realpath(capture_path)) != os.path.normcase(capture_path) or not os.path.isfile(capture_path):
        raise AnalyzerFailure("capture_unavailable", "hash", "The authenticated capture is unavailable.")
    return capture_path


def hash_capture(capture_path, expected_size):
    digest = hashlib.sha256()
    total = 0
    with open(capture_path, "rb") as stream:
        while True:
            block = stream.read(1024 * 1024)
            if not block:
                break
            total += len(block)
            if total > expected_size or total > MAX_CAPTURE_BYTES:
                raise AnalyzerFailure("capture_size_mismatch", "hash", "The capture size does not match authenticated evidence.")
            digest.update(block)
    if total != expected_size:
        raise AnalyzerFailure("capture_size_mismatch", "hash", "The capture size does not match authenticated evidence.")
    return digest.hexdigest()


def renderdoc_identity(rd):
    version = bounded_text(rd.GetVersionString(), 128, False)
    commit = ""
    if hasattr(rd, "GetCommitHash"):
        commit = bounded_text(rd.GetCommitHash(), 96)
    build = version + ("+" + commit if commit else "")
    return bounded_text(build, 256, False), bounded_text(version.lstrip("vV"), 128, False)


def action_flags(rd, flags):
    values = []
    for name in dir(rd.ActionFlags):
        if name.startswith("_") or name in ("NoFlags", "All"):
            continue
        try:
            flag = getattr(rd.ActionFlags, name)
            if int(flag) != 0 and flags & flag:
                token = require_token(name, 128)
                if token not in values:
                    values.append(token)
        except Exception:
            continue
    values.sort()
    return values[:128]


def read_actions(rd, controller, maximum, output_budget):
    structured = controller.GetStructuredFile()
    roots = list(controller.GetRootActions())
    stack = [(action, 0) for action in reversed(roots)]
    actions = []
    event_ids = set()
    used_bytes = 0
    while stack:
        action, parent = stack.pop()
        event_id = int(action.eventId)
        if event_id <= 0 or event_id > 0xffffffff or event_id in event_ids:
            raise AnalyzerFailure("invalid_action_tree", "actions", "RenderDoc returned an invalid action tree.")
        if len(actions) >= maximum:
            raise AnalyzerFailure("action_limit_exceeded", "actions", "The capture action count exceeds the request limit.")
        item = {
            "event_id": event_id,
            "parent_event_id": parent,
            "name": bounded_text(action.GetName(structured), 1024),
            "flags": action_flags(rd, action.flags),
        }
        used_bytes += len(json.dumps(item, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        if used_bytes > output_budget:
            raise AnalyzerFailure("output_too_large", "actions", "The action tree exceeds the response byte limit.")
        actions.append(item)
        event_ids.add(event_id)
        for child in reversed(list(action.children)):
            stack.append((child, event_id))
    return actions, event_ids, used_bytes


def parse_explicit_counter(value):
    prefix = "renderdoc:"
    if not value.startswith(prefix):
        raise AnalyzerFailure("unsupported_counter_selection", "counters", "This analyzer only accepts numeric RenderDoc counter IDs.")
    native = value[len(prefix):]
    if not native or any(character not in "0123456789" for character in native) or (len(native) > 1 and native[0] == "0"):
        raise AnalyzerFailure("unsupported_counter_selection", "counters", "This analyzer only accepts numeric RenderDoc counter IDs.")
    parsed = int(native)
    if parsed < 1 or parsed > 0xffffffff:
        raise AnalyzerFailure("unsupported_counter_selection", "counters", "A requested RenderDoc counter ID is outside the supported range.")
    return parsed


def requested_counter_ids(selection):
    mode = selection["mode"]
    packs = selection["packs"]
    explicit = selection["explicit_counter_ids"]
    if mode not in ("none", "semantic_pack", "explicit", "semantic_pack_and_explicit"):
        raise AnalyzerFailure("unsupported_counter_selection", "counters", "The requested counter selection mode is unsupported.")
    if any(pack != "core" for pack in packs):
        raise AnalyzerFailure("unsupported_counter_selection", "counters", "This analyzer revision only supports the core semantic pack.")
    selected = set(CORE_COUNTER_IDS if "core" in packs else ())
    for value in explicit:
        selected.add(parse_explicit_counter(value))
    return selected


def describe_counter(controller, native_id, requested, include_description):
    try:
        description = controller.DescribeCounter(native_id)
        name = bounded_text(description.name, 512, False)
        result_type = enum_name(description.resultType)
        width = int(description.resultByteWidth)
        unit = enum_name(description.unit)
        if width < 1 or width > 32:
            raise ValueError()
        serializable = (result_type == "Float" and width in (4, 8)) or (result_type in ("UInt", "SInt") and width in (1, 2, 4, 8))
        return {
            "id": "renderdoc:%d" % native_id,
            "native_id": native_id,
            "name": name,
            "description": bounded_text(description.description, 4096) if include_description else "",
            "unit": unit,
            "result_type": result_type,
            "result_byte_width": width,
            "aggregation": "sum" if native_id in CORE_COUNTER_IDS else "non_aggregatable",
            "availability": "not_requested" if not requested else ("available" if serializable else "unsupported"),
            "reason": "not selected" if not requested else ("" if serializable else "unsupported result representation"),
            "requested": requested,
            "fetched": False,
            "pass_index": -1,
            "provenance": "renderdoc",
        }
    except Exception:
        return synthetic_counter(native_id, requested, "counter description unavailable")


def synthetic_counter(native_id, requested, reason):
    return {
        "id": "renderdoc:%d" % native_id,
        "native_id": native_id,
        "name": "RenderDoc counter %d" % native_id,
        "description": "",
        "unit": "Unknown",
        "result_type": "Unknown",
        "result_byte_width": 1,
        "aggregation": "non_aggregatable",
        "availability": "unsupported" if requested else "not_requested",
        "reason": reason if requested else "not selected",
        "requested": requested,
        "fetched": False,
        "pass_index": -1,
        "provenance": "renderdoc",
    }


def raw_counter_value(result, metadata):
    result_type = metadata["result_type"]
    width = metadata["result_byte_width"]
    if result_type == "Float":
        value = float(result.value.f if width == 4 else result.value.d)
        if not math.isfinite(value):
            raise ValueError()
        text = format(value, ".9g" if width == 4 else ".17g")
    elif result_type == "UInt":
        value = int(result.value.u32 if width <= 4 else result.value.u64)
        value &= (1 << (width * 8)) - 1
        text = str(value)
    elif result_type == "SInt":
        bits = width * 8
        value = int(result.value.u32 if width <= 4 else result.value.u64) & ((1 << bits) - 1)
        if value & (1 << (bits - 1)):
            value -= 1 << bits
        text = str(value)
    else:
        raise ValueError()
    return {"result_type": result_type, "byte_width": width, "encoding": "decimal", "value": text}


def read_counters(controller, selection, include_description, max_results, output_budget, valid_event_ids):
    available = sorted(set(int(counter) for counter in controller.EnumerateCounters()))
    if len(available) > MAX_COUNTERS:
        raise AnalyzerFailure("counter_limit_exceeded", "counters", "The available counter catalog exceeds the analyzer limit.")
    requested = requested_counter_ids(selection)
    catalog_ids = sorted(set(available) | requested)
    if len(catalog_ids) > MAX_COUNTERS:
        raise AnalyzerFailure("counter_limit_exceeded", "counters", "The requested counter catalog exceeds the analyzer limit.")
    available_set = set(available)
    catalog = []
    by_native = {}
    for native_id in catalog_ids:
        metadata = describe_counter(controller, native_id, native_id in requested, include_description) if native_id in available_set else synthetic_counter(native_id, True, "counter unavailable on this replay")
        catalog.append(metadata)
        by_native[native_id] = metadata
    selected = [native_id for native_id in available if native_id in requested and by_native[native_id]["availability"] == "available"]
    pass_count = 1 if selected else 0
    raw_results = []
    fetch_failed = False
    if selected:
        try:
            raw_results = list(controller.FetchCounters(selected))
            for native_id in selected:
                metadata = by_native[native_id]
                metadata["fetched"] = True
                metadata["pass_index"] = 0
        except Exception:
            fetch_failed = True
            for native_id in selected:
                metadata = by_native[native_id]
                metadata["availability"] = "fetch_failed"
                metadata["reason"] = "counter batch failed"
                metadata["pass_index"] = 0
    results = []
    failed_results = 0
    used_bytes = sum(len(json.dumps(item, ensure_ascii=False, separators=(",", ":")).encode("utf-8")) for item in catalog)
    for raw in raw_results:
        native_id = int(raw.counter)
        metadata = by_native.get(native_id)
        event_id = int(raw.eventId)
        if metadata is None or event_id <= 0 or event_id > 0xffffffff or (valid_event_ids is not None and event_id not in valid_event_ids):
            raise AnalyzerFailure("invalid_counter_result", "counters", "RenderDoc returned a counter result outside the analyzed action tree.")
        item = {"event_id": event_id, "counter_id": metadata["id"], "availability": "available", "reason": ""}
        try:
            item["raw_value"] = raw_counter_value(raw, metadata)
        except Exception:
            item["availability"] = "fetch_failed"
            item["reason"] = "counter value could not be represented"
            failed_results += 1
        used_bytes += len(json.dumps(item, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        if len(results) >= max_results:
            raise AnalyzerFailure("counter_result_limit_exceeded", "counters", "The counter result count exceeds the request limit.")
        if used_bytes > output_budget:
            raise AnalyzerFailure("output_too_large", "counters", "Counter data exceeds the response byte limit.")
        results.append(item)
    failed_counters = sum(1 for item in catalog if item["availability"] == "fetch_failed")
    warnings = ["One or more counter batches failed."] if fetch_failed else []
    summary = {
        "requested_counter_count": sum(1 for item in catalog if item["requested"]),
        "described_counter_count": len(catalog),
        "fetched_counter_count": sum(1 for item in catalog if item["fetched"]),
        "unsupported_counter_count": sum(1 for item in catalog if item["availability"] == "unsupported"),
        "failed_counter_count": failed_counters,
        "failed_result_count": failed_results,
        "replay_pass_count": pass_count,
    }
    return catalog, results, summary, warnings, used_bytes


def replay_host():
    system = "windows" if sys.platform.startswith("win") else ("linux" if sys.platform.startswith("linux") else "unsupported")
    return "%s-%s" % (system, "x64" if struct.calcsize("P") == 8 else "x86")


def gpu_and_driver(rd, capture, api_properties):
    gpu = ""
    driver = ""
    try:
        devices = list(capture.GetAvailableGPUs())
        matching = [device for device in devices if device.vendor == api_properties.vendor]
        if len(matching) == 1:
            gpu = bounded_text(matching[0].name, 512)
            driver = bounded_text(matching[0].driver, 512)
    except Exception:
        pass
    try:
        information = rd.GetDriverInformation(api_properties.localRenderer)
        version = bounded_text(information.version, 512)
        if version:
            driver = version
    except Exception:
        pass
    return gpu, driver


def write_response(workspace, document, maximum_bytes):
    payload = json.dumps(document, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    if len(payload) > maximum_bytes or len(payload) > MAX_DOCUMENT_BYTES:
        raise AnalyzerFailure("output_too_large", "serialize", "The analyzer response exceeds the request byte limit.")
    temporary = os.path.join(workspace, RESPONSE_FILE + ".tmp")
    final = os.path.join(workspace, RESPONSE_FILE)
    with open(temporary, "xb") as stream:
        stream.write(payload)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, final)


def analyzer_provenance(started_ms, renderdoc_build, replay_api, graphics_api, gpu, driver):
    completed_ms = max(started_ms, utc_milliseconds())
    return {
        "analyzer_version": ANALYZER_VERSION,
        "renderdoc_build": renderdoc_build,
        "replay_api": replay_api,
        "graphics_api": graphics_api,
        "gpu": gpu,
        "driver": driver,
        "replay_host": replay_host(),
        "started_utc": format_utc(started_ms),
        "completed_utc": format_utc(completed_ms),
        "duration_milliseconds": str(completed_ms - started_ms),
    }


def success_document(request, observed_hash, started_ms, identity, api, gpu, driver, actions, catalog, results, summary, warnings):
    capture = request["capture"]
    provenance = analyzer_provenance(started_ms, identity[0], identity[1], api, gpu, driver)
    summary["duration_milliseconds"] = provenance["duration_milliseconds"]
    failed = summary["failed_counter_count"] + summary["failed_result_count"]
    return {
        "schema": RESULT_SCHEMA,
        "schema_version": SCHEMA_VERSION,
        "request_id": request["request_id"],
        "status": "completed_with_errors" if failed else "completed",
        "capture": {
            "artifact_id": capture["artifact_id"],
            "capture_id": capture["capture_id"],
            "bundle_id": capture["bundle_id"],
            "expected_sha256": capture["sha256"],
            "observed_sha256": observed_hash,
            "hash_verified": True,
            "size_bytes": capture["size_bytes"],
            "association_state": capture["association_state"],
            "finalization_state": capture["finalization_state"],
            "authority_state": capture["authority_state"],
            "content_state": capture["content_state"],
            "source_file_identity_sha256": capture["source_file_identity_sha256"],
            "capture_app_api": capture["capture_app_api"],
        },
        "analyzer": provenance,
        "actions_complete": True,
        "action_total_count": len(actions),
        "actions": actions,
        "counter_catalog_complete": True,
        "counter_total_count": len(catalog),
        "counter_catalog": catalog,
        "results_complete": True,
        "result_total_count": len(results),
        "results": results,
        "summary": summary,
        "warnings": warnings,
        "diagnostics": [],
    }


def error_document(request, expected_hash, observed_hash, hash_verified, started_ms, identity, failure):
    request_id = request.get("request_id", "invalid-request") if isinstance(request, dict) else "invalid-request"
    return {
        "schema": ERROR_SCHEMA,
        "schema_version": SCHEMA_VERSION,
        "request_id": request_id,
        "status": "failed",
        "capture": {"expected_sha256": expected_hash, "observed_sha256": observed_hash, "hash_verified": hash_verified},
        "analyzer": analyzer_provenance(started_ms, identity[0], identity[1], "", "", ""),
        "error": {"code": failure.code, "stage": failure.stage, "message": failure.message, "retryable": failure.retryable},
        "warnings": [],
        "diagnostics": [],
    }


def analyze(rd, request, capture_path, expected_size, started_ms, identity, observed_hash):
    capture_file = None
    controller = None
    try:
        set_stage("open_capture")
        capture_file = rd.OpenCaptureFile()
        opened = capture_file.OpenFile(capture_path, "", None)
        if opened != rd.ResultCode.Succeeded:
            raise AnalyzerFailure("capture_open_failed", "open", "RenderDoc could not open the authenticated capture.")
        if not capture_file.LocalReplaySupport():
            raise AnalyzerFailure("local_replay_unsupported", "open", "The capture cannot be replayed on this host.")
        opened, controller = capture_file.OpenCapture(rd.ReplayOptions(), None)
        if opened != rd.ResultCode.Succeeded or controller is None:
            raise AnalyzerFailure("replay_open_failed", "open", "RenderDoc could not initialize local replay.")
        api_properties = controller.GetAPIProperties()
        graphics_api = enum_name(api_properties.pipelineType)
        gpu, driver = gpu_and_driver(rd, capture_file, api_properties)
        output_budget = request["options"]["max_output_bytes"] - 4096
        set_stage("actions")
        if request["options"]["include_action_tree"]:
            actions, event_ids, action_bytes = read_actions(rd, controller, request["options"]["max_actions"], output_budget)
        else:
            actions, event_ids, action_bytes = [], None, 0
        set_stage("counters")
        catalog, results, summary, warnings, _ = read_counters(
            controller,
            request["counter_selection"],
            request["options"]["include_raw_counter_descriptions"],
            request["options"]["max_counter_results"],
            output_budget - action_bytes,
            event_ids,
        )
        set_stage("verify_capture")
        post_hash = hash_capture(capture_path, expected_size)
        if post_hash != observed_hash:
            raise AnalyzerFailure("capture_changed", "hash", "The capture changed while it was being analyzed.")
        return success_document(request, observed_hash, started_ms, identity, graphics_api, gpu, driver, actions, catalog, results, summary, warnings)
    finally:
        if controller is not None:
            try:
                controller.Shutdown()
            except Exception:
                pass
        if capture_file is not None:
            try:
                capture_file.Shutdown()
            except Exception:
                pass


def main():
    started_ms = utc_milliseconds()
    request = {}
    workspace = ""
    expected_hash = "0" * 64
    observed_hash = ""
    hash_verified = False
    identity = ("", "")
    maximum_output = MAX_DOCUMENT_BYTES
    try:
        workspace, project_root = validate_workspace()
        set_stage("workspace")
        request = read_bounded_json(os.path.join(workspace, REQUEST_FILE))
        expected_size = validate_request(request)
        expected_hash = request["capture"]["sha256"]
        maximum_output = request["options"]["max_output_bytes"]
        capture_path = resolve_capture(project_root, request["capture"]["path"])
        set_stage("hash_capture")
        observed_hash = hash_capture(capture_path, expected_size)
        if observed_hash != expected_hash:
            raise AnalyzerFailure("capture_hash_mismatch", "hash", "The capture hash does not match authenticated evidence.")
        hash_verified = True
        try:
            import renderdoc as rd
        except Exception:
            raise AnalyzerFailure("renderdoc_import_failed", "startup", "The configured qrenderdoc build did not expose its Python replay API.")
        identity = renderdoc_identity(rd)
        document = analyze(rd, request, capture_path, expected_size, started_ms, identity, observed_hash)
        set_stage("write_response")
        write_response(workspace, document, maximum_output)
        return 0
    except AnalyzerFailure as failure:
        if not workspace:
            return 2
        try:
            write_response(workspace, error_document(request, expected_hash, observed_hash, hash_verified, started_ms, identity, failure), maximum_output)
            return 0
        except Exception:
            return 2
    except Exception:
        if not workspace:
            return 2
        failure = AnalyzerFailure("internal_error", "internal", "The analyzer failed without a safe diagnostic.")
        try:
            write_response(workspace, error_document(request, expected_hash, observed_hash, hash_verified, started_ms, identity, failure), maximum_output)
            return 0
        except Exception:
            return 2


raise SystemExit(main())
