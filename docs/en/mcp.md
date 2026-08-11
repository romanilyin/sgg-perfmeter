# MCP And Agent Automation

SGG PerfMeter exposes command metadata for Unity MCP/editor-agent workflows under the package path:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

The goal is structured JSON output for agents instead of screenshot parsing, overlay text parsing, or Unity Console scraping.

## Command Groups

| Command | Purpose |
| --- | --- |
| `perfmeter.setup.status` | Read setup status. |
| `perfmeter.setup.run` | Run recommended setup actions. |
| `perfmeter.compatibility.status` | Read separate import, core runtime, and active render-integration compatibility. |
| `perfmeter.runtime.status` | Read runtime status. |
| `perfmeter.runtime.ensure` | Start runtime if needed. |
| `perfmeter.runtime.stop` | Stop runtime. |
| `perfmeter.runtime.reset_stats` | Reset rolling stats, alert counters, and active session counters. |
| `perfmeter.runtime.mode.set` | Switch `Stopped`, `Background`, `Overlay`, or `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Read latest metrics, including custom metrics. |
| `perfmeter.profiler.capabilities` | Read cached Profiler metric capabilities and resolution provenance without starting runtime or discovery. |
| `perfmeter.profiler.lease.capabilities` | Read process-local profiler lease resources and reload semantics. |
| `perfmeter.profiler.lease.status` | Read current or matching process-local profiler lease state. |
| `perfmeter.alerts.latest` | Read active alerts, counters, and Editor warning state. |
| `perfmeter.alerts.clear` | Clear active alerts, counters, and cooldown state. |
| `perfmeter.alerts.capture.begin` | Begin a bounded external-capture classification scope. |
| `perfmeter.alerts.capture.end` | End the matching external-capture classification scope. |
| `perfmeter.device.info` | Read device, graphics, display, monitor, pipeline, and Unity environment info. |
| `perfmeter.camera.snapshot` | Read camera transform/projection and URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Read latest observed PerfMeter render integration diagnostics for URP Render Graph or HDRP Custom Pass. |
| `perfmeter.render.snapshot` | Read the neutral render integration snapshot, including freshness, camera/pass context, GRD/VRS context, and the legacy Render Graph facade. |
| `perfmeter.overlay.set` | Show/hide overlay and set preset, modules, corner, mode, and target FPS. |
| `perfmeter.overdraw.start` | Start bounded overdraw measurement. |
| `perfmeter.overdraw.cancel` | Cancel active overdraw measurement. |
| `perfmeter.overdraw.heatmap.set` | Show or hide visual overdraw heatmap. |
| `perfmeter.session.start` | Start bounded session recording. |
| `perfmeter.session.stop` | Stop recording and return summary. |
| `perfmeter.session.summary` | Read current session summary. |
| `perfmeter.session.export` | Export current session to project-local JSON or CSV. |
| `perfmeter.capture.request` | Request a bounded external GPU capture and correlated bundle; optional `backend_mode` is `GenericUnity`, `NativePreferred`, or `NativeRequired`. |
| `perfmeter.capture.status` | Read capture and bundle state. |
| `perfmeter.capture.cancel` | Cancel the matching active capture. |
| `perfmeter.capture.export` | Atomically export a ready bundle under the project-local bundle root. |
| `perfmeter.capture.export.request` | Queue a single-flight bundle export and return its export ID and progress. |
| `perfmeter.capture.export.status` | Read export phase, progress, cancellation, retry, and artifact authority. |
| `perfmeter.capture.export.cancel` | Request cancellation of the matching active export. |
| `perfmeter.capture.capabilities` | Read bundle schema, quota, retention, screenshot, and provenance capabilities. |

Prefer `perfmeter.capture.export.request`, then poll `perfmeter.capture.export.status` and optionally call `perfmeter.capture.export.cancel`. The legacy `perfmeter.capture.export` command blocks for compatibility. Export responses include the generic `external_artifact` envelope with association, authority, finalization, content, privacy/share policy, size, and source/post-copy hashes. The read-only lease commands expose process-local conflict state without acquiring a lease.

Runtime ensure/stop/mode, overlay, overdraw, and session mutation responses include a `mutation` object with `operation`, boolean `success`, `result`, `reason`, `requested`, and `effective`. `Rejected`, `Unavailable`, and `Unsupported` are not reported as success. Normalized requests remain successful but expose both values.

`perfmeter.compatibility.status` is read-only and does not start runtime. It reports `import_compatible`, `core_runtime_compatible`, and `render_integration_compatible` independently, with current/floor versions and a reason for each result. `perfmeter.setup.status` includes the same structured `compatibility` object while retaining its existing human-readable `status_report`; setup/configuration readiness remains separate.

## Runtime Self-Overhead Payload

`perfmeter.runtime.status` includes the additive `self_overhead` object; this is not a separate command. Top-level keys are `state`, `cpu_timing_available`, `gpu_timing_availability`, and `has_budget_violation`.

Component objects are `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration`, and `hdrp_render_integration`. Each contains `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state`, and `allocation_budget_state`.

Values describe fixed 120-frame CPU callback windows with per-invocation averages. GPU attribution is `Unavailable`; inactive render integration is `Unsupported`, and supported components without calls are `NotMeasured`. Session JSON/CSV schemas are unchanged, and existing CPU/GPU metrics are not adjusted.

`perfmeter.metrics.latest` includes additive `diagnostics` while retaining the top-level instantaneous `bottleneck` and raw metrics. The diagnostics object publishes the stable bottleneck, availability/freshness/provenance, confidence/coverage, typed flags, verification steps, evidence age/counts, and raw warning. `perfmeter.platform.telemetry` publishes bounded-cache metadata: `last_attempt_time_seconds`, `last_success_time_seconds`, `sample_age_seconds`, `freshness`, `last_attempt_result`, and `forced_at_capture_boundary`.

## Typical Profiling Run

```text
perfmeter.profiler.capabilities {}
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Use `OverdrawDiagnostic` only for bounded URP diagnostic windows because numerical overdraw and heatmap rendering add extra GPU work. HDRP reports overdraw and heatmap as unsupported while the rest of the diagnostics stay available.

## Memory Snapshot Commands

| Command | Purpose and main inputs |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Request one manual snapshot with `capture_id`, optional capture-flag booleans, `minimum_free_disk_mb`, and `cooldown_seconds`. |
| `perfmeter.memory.snapshot.status` | Read snapshot and correlated bundle state without starting the runtime or exposing the temporary source path. |
| `perfmeter.memory.snapshot.capabilities` | Read backend provenance, supported flags, the 512 MiB snapshot limit, and the owned temporary root. |
| `perfmeter.memory.snapshot.triggers.configure` | Explicitly enable/disable system-memory threshold and bounded leak-growth triggers, their frame window, flags, free-space guard, and cooldown. |

The request and trigger-configuration commands require Play Mode. Automation is disabled by default. A typical sequence is:

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Poll status until the bundle is export-ready, then use the existing `perfmeter.capture.export` command. A memory-only bundle uses `requested_tool: MemoryProfiler`, includes `memory-snapshot.json` and manifest provenance, and has no external GPU artifact. The successful export is one-shot and removes the owned staging source.

`perfmeter.alerts.latest` reports the alert-history interval and reset reason, classified lifecycle/steady-state/capture counters, and the latest fired alert. PerfMeter does not infer captures from slow frames; wrap an external capture with matching `perfmeter.alerts.capture.begin/end` calls when capture attribution is required.

For a correlated capture, use `perfmeter.capture.request`, poll `perfmeter.capture.status` to a terminal bundle state, then call `perfmeter.capture.export`. The request accepts `backend_mode`; status reports `requested_backend_mode`, `effective_backend_kind`, `native_phase`, result code, and fallback reason. Generic/caller-supplied `.rdc`/`.wpix` data remains observed because Unity cannot authenticate association. The optional Windows x64 Editor native RenderDoc path can publish a generation-bound authenticated artifact and satisfy `require_authoritative_external_artifact`. MCP intentionally does not expose native storage-mode or authority selection; use the C# API for explicit MetadataOnly/Copy/Embed policy.

## Graphics Diagnostics And State-Collection Commands

The following six commands expose the PM-GFX-001 surface:

| Command | Purpose and main inputs |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Read the latest shader GPU-program and graphics-pipeline marker values, dynamic capability provenance, catalog revision, and graphics API context. No inputs. |
| `perfmeter.graphics.state_collection.request` | Start a bounded trace. Requires Play Mode and an active PerfMeter session; `capture_id` is required, `trace_frames` is 1–600 (default 60), and `minimum_free_disk_mb` defaults to 1024. |
| `perfmeter.graphics.state_collection.status` | Read availability, state, progress, backend identity, counts, `is_busy`, `has_pending_cleanup`, warnings, and the owned project-relative artifact path. No inputs. |
| `perfmeter.graphics.state_collection.capabilities` | Read backend provenance, trace/prewarm support, cache-miss and parallel-PSO support, session requirement, 600-frame limit, 64 MiB limit, and owned artifact root. No inputs. |
| `perfmeter.graphics.state_collection.cancel` | Cancel the matching active or preparing trace and clean its pending artifact. Requires `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Load and synchronously prewarm one owned project-relative artifact in Play Mode. `relative_path` is required; `max_state_count` is 0–1,000,000 and defaults to 0. |

`perfmeter.graphics.diagnostics` returns `shader_gpu_program_creation_value` and `graphics_pipeline_creation_value` plus each capability's `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count`, and `sampled_component_count`. `perfmeter.metrics.latest` and session exports expose the same marker metadata. Values retain the discovered recorder unit and are not universally shader or PSO counts; use `sample_state` instead of interpreting zero as unavailable.

The graphics-state status response includes `result`, `availability`, `state`, `capture_id`, requested/completed trace frames, backend ID/version, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup`, and `warning`. `is_busy` remains true during preparation, tracing, ending, prewarm, cleanup, or persisted cleanup work; `has_pending_cleanup` identifies an owned artifact waiting for retry. A failed deletion is persisted with an owned `.delete-pending` sidecar and restored/retried after domain reload. `StopSession` cancels an active trace, so the session must remain active through completion. A trace reaches its terminal state after the requested frames are ticked at end-of-frame; batch mode uses a next-frame fallback. Samples admitted by an active session carry `graphics_state_trace_id` equal to `capture_id`.

Typical trace and prewarm sequence:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

Only one graphics-state flight is admitted. A repeated active ID returns `AlreadyActive`; a different overlapping trace/prewarm returns `RejectedOverlap`. Cancellation matches the active/preparing ID only. The Unity backend reports `supports_cache_miss_tracing: false`; cache-miss evidence is unsupported, and the MCP prewarm schema does not expose a cache-miss option. Artifacts are owned below `Temp/PerfMeter/GraphicsStateCollections` and are limited to 64 MiB.

## Render Integration Snapshot

`perfmeter.render.snapshot {}` is a read-only command with no inputs. It does not start the runtime. The response uses `schema_version: 1` and returns `render_integration` with the current pipeline/source, observation frame and age, `observation_matches_current_pipeline`, observed camera identity, integration/pass/injection metadata, scheduled PerfMeter pass count, effective rendering mode where available, nested `gpu_resident_drawer` and `variable_rate_shading` context, and `legacy_render_graph`.

`gpu_resident_drawer` includes project/compute support, public global activity plus `activity_source`, URP Forward+/clustered compatibility, `degraded_reason`, and nested BRG `effectiveness`. Effectiveness values are `null` unless their capability is `AvailableSampled`; recorder names, exact/alias resolution, and component counts preserve provenance. `scope: "brg_aggregate"` means these counters do not prove per-renderer GRD use.

The command is the MCP equivalent of `PerformanceMeter.GetRenderIntegrationSnapshot()` and `TryGetRenderIntegrationSnapshot(...)`. A stale observation is reported with an explicit non-match and warning rather than presented as current. `perfmeter.rendergraph.snapshot` remains available for the legacy facade. The command does not add Editor navigation: stable Unity APIs do not expose RenderGraph/CustomPass viewer or pass-target information.
