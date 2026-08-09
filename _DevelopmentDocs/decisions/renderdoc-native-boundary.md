# Native RenderDoc Boundary Decision

Status: `PM-RDOC-001` ADR accepted. The isolated `PM-RDOC-002` source bridge is implemented and validated without public/managed wiring; `PM-RDOC-003` and real Unity `.rdc` validation are pending. This decision and the bridge evidence are not a support or release claim.

## Context And Scope

PerfMeter keeps the `PM-CAP-001` capture coordinator and the `PM-CAP-002` bundle released in `2026.8.6-2`, plus the generic `PM-EXT-001/002` artifact and lease contracts released in `2026.8.9-1`. The Unity `ExternalGPUProfiler` backend and its broader matrix remain a separate compatibility/fallback path; this decision does not change its identity or completion semantics.

The native RenderDoc slice is limited to in-process capture control and artifact provenance. Replay, counters, and analysis remain an out-of-process `PM-RDA` concern. No RenderDoc replay DLL or replay controller is loaded in the Unity process.

## Initial Matrix And Rollout

The initial native matrix has exactly these independent smoke rows:

| Host | Unity target | Graphics API |
| --- | --- | --- |
| Windows x64 | Unity Editor | D3D11 |
| Windows x64 | Unity Editor | D3D12 |
| Windows x64 | Unity Editor | Vulkan |

Each row requires its own attached real RenderDoc smoke before any future support statement. Development Player, Linux x64, IL2CPP, mobile, and macOS are deferred and are not claimed. Linux can be considered only after the Windows path is stable.

Rollout is deliberately staged: `PM-RDOC-001` is this accepted ADR; `PM-RDOC-002` is the completed isolated Windows bridge and test slice with no public capture wiring; `PM-RDOC-003` adds managed selection, provenance, quota/privacy behavior, and the real smoke gates. `PM-RDA` remains a separate later rollout.

## Already-Loaded Native Boundary

The Windows bridge may resolve only a module that is already loaded through the supported external/Unity RenderDoc flow:

```text
GetModuleHandleW(L"renderdoc.dll")
GetProcAddress(module, "RENDERDOC_GetAPI")
```

If the module or export is absent, the bridge reports that condition. It must never call `LoadLibrary`, accept a DLL path from MCP or settings, bundle or install RenderDoc, elevate privileges, inject or self-inject, or otherwise attach a production player. The RenderDoc installation remains user-owned. The optional Windows x64 Editor plugin and managed adapter have no core dependency; no RenderDoc binary or vendor SDK is shipped.

`PM-RDOC-002` pins the public RenderDoc app header to upstream commit [`7db2264afa00a5313154022f8c4ae0628a641300`](https://github.com/baldurk/renderdoc/commit/7db2264afa00a5313154022f8c4ae0628a641300), verifies SHA-256 at configure time, and retains its MIT license notice. Windows x64 Editor plugin import settings remain a `PM-RDOC-003` gate because this isolated slice ships source but no DLL.

## Coordinator And Frame Boundary

Native capture reuses the existing capture coordinator and lease resources; it does not introduce an independent overlap policy. The current synchronous `IPerfMeterCaptureBackend.TryBegin/TryEnd` contract cannot represent deferred frame hooks or artifact finalization. `PM-RDOC-002` is not auto-wired to public capture. `PM-RDOC-003` must add a narrow internal asynchronous control/artifact-observer seam with generation-bound phases for preflight, begin scheduled/executed, end scheduled/executed, awaiting artifact, finalizing artifact, and terminal completion. The existing generic backend remains a synchronous adapter, while public enums, results, MCP IDs, bundle schemas, and timeline schemas stay compatible.

The capture lease remains held from storage preflight until the native operation reaches an artifact terminal state and its immutable capture/bundle/generation association is frozen. A later request cannot supersede an operation waiting for an artifact. Domain reload or runtime stop marks a nonterminal operation `LostSession`, never resumes authority from path evidence alone, and performs only marker-owned cleanup that can be proven safe.

The MVP accepts the request on the Unity main thread, begins before the intended captured render, and schedules end through `WaitForEndOfFrame`; the recorded boundary mode is `managed_end_of_frame`. This requires `PM-RDOC-003` to replace the current native-path `Update`-time synchronous end call. A later render-plugin callback, if adopted, may perform only short RenderDoc app-API operations. Filesystem preflight, polling, copying, hashing, replay, and other blocking work never run on the Unity main or render-frame path.

The initial bridge uses RenderDoc's `(NULL, NULL)` active-context target and records `target_mode = wildcard_active_context`; it does not claim a specific Editor window or graphics-device handle. More than one matching artifact, an unexpected context, or a multi-window result that cannot be disambiguated is non-authoritative and fails the native association gate. A future explicit device/window target requires a new versioned bridge capability and real matrix evidence.

## Fixed Bridge ABI V1

The bridge owns a separate fixed C ABI, rather than exporting RenderDoc structs directly. Its public functions are suffixed `V1`, including `SggRd_GetCapabilitiesV1`, `SggRd_BeginCaptureV1`, `SggRd_EndCaptureV1`, `SggRd_DiscardCaptureV1`, `SggRd_TryGetNewArtifactV1`, and `SggRd_SetCaptureCommentsV1`.

- Every public struct starts with `struct_size`; all scalar fields use fixed-width C types such as `uint32_t` and `uint64_t`.
- Exports use `extern "C"`, explicit symbol visibility, and `__cdecl`. Windows x64 layout uses 8-byte packing and a matching managed `StructLayout`; ABI tests baseline size, alignment, and every field offset.
- A caller sets `struct_size`; the bridge rejects a value below the V1 minimum, writes only the known prefix, and ignores a larger forward-compatible tail. Boolean values are `uint32_t`, not C++ `bool`.
- UTF-8 input/output uses explicit byte lengths and bounded, caller-owned buffers. Inputs reject invalid UTF-8, embedded NUL, and inconsistent null/length pairs. The bridge caps title input at 256 UTF-8 bytes, comments input at 1024 UTF-8 bytes, and path output at 32768 UTF-8 bytes. Output includes the required byte count including terminator; insufficient capacity returns `SGG_RD_BUFFER_TOO_SMALL` without truncation.
- No C++ exception crosses the C boundary. Mutating app-API calls are serialized for one owner operation; concurrent begin/end/discard/artifact enumeration is rejected. ABI layout, struct sizes/offsets, and enum values are baselined independently.
- `SggRdResult` has these stable numeric values:

  | Name | Value |
  | --- | ---: |
  | `SGG_RD_OK` | 0 |
  | `SGG_RD_NOT_LOADED` | 1 |
  | `SGG_RD_EXPORT_MISSING` | 2 |
  | `SGG_RD_API_NEGOTIATION_FAILED` | 3 |
  | `SGG_RD_ALREADY_CAPTURING` | 4 |
  | `SGG_RD_NOT_CAPTURING` | 5 |
  | `SGG_RD_CAPTURE_FAILED` | 6 |
  | `SGG_RD_CAPTURE_NOT_OBSERVED` | 7 |
  | `SGG_RD_BUFFER_TOO_SMALL` | 8 |
  | `SGG_RD_UNSUPPORTED_PLATFORM` | 9 |
  | `SGG_RD_INVALID_ARGUMENT` | 10 |
  | `SGG_RD_INTERNAL_ERROR` | 11 |

App API negotiation tries `eRENDERDOC_API_Version_1_7_0`, then `eRENDERDOC_API_Version_1_6_0`, then `eRENDERDOC_API_Version_1_4_0`. API `1.4` is the minimum because safe native cancellation requires `DiscardFrameCapture`; an API below `1.4` is unsupported. The bridge reports the actual negotiated major/minor/patch and feature flags, including discard/title/comments support; managed code must not infer function availability from a version string. Capture title is capability-conditional and used only with API `1.6+`; API `1.4` reports it unsupported and the bridge never calls the missing function pointer. The negotiated API is not `ToolVersion`, and an unknown RenderDoc build remains unknown.

## Token And Artifact Provenance

The begin token contains a cryptographically generated 64-bit request nonce, `count_before`, and start time. Storage preflight derives the project-local root `Temp/PerfMeter/RenderDoc/<nonce-hex>` without using raw `capture_id` as a path segment, canonicalizes it below the project root, rejects reparse points, checks free space, and writes an ownership marker before begin. The raw capture ID is display metadata only.

Before begin, the adapter reads and bounds the previous global RenderDoc capture-path template, sets the unique nonce-root template, and records whether restore is required. End, discard, begin failure, and cleanup all restore the previous template in a `finally`-equivalent path. Empty/NULL "most recent capture" comments are forbidden: comments can be written only to the exactly selected token-bound path.

After end, the bridge polls capture count with a bounded non-tight-loop policy and filters new indices by `count_before`, request time window, and canonical path under the unique nonce root. Zero candidates is not observed; more than one candidate is ambiguous and cannot be authenticated. Hotkey or foreign captures outside the nonce root are ignored. The selected file is opened once, its Windows volume/file identity and timestamps are recorded, and stable-size, source SHA-256, and any Copy/Embed operation use that same file identity/handle before a final identity check. Replacement, deletion, growth, sharing failure, or identity change fails finalization. The nonce-root binding, exact single candidate, bridge index/timestamp, and file identity/hash are all required; `GetCapture`, a path, extension, timestamp, or hash alone never authenticates association.

Native cancellation uses `DiscardFrameCapture` and the same template-restore/owned-cleanup path. End/discard uncertainty leaves the operation and lease in explicit cleanup or lost-session state; it never silently invokes generic end or adopts a later file.

Backend selection is additive and explicit: `GenericUnity` remains the compatibility default, `NativeRequired` never falls back, and `NativePreferred` may fall back only for a pre-begin `NOT_LOADED`, `EXPORT_MISSING`, `API_NEGOTIATION_FAILED`, or `UNSUPPORTED_PLATFORM` result. Requested tool alone does not select native mode. There is no fallback for lease/overlap, invalid argument, internal error, permission/policy failure, or after any begin attempt returned success or uncertainty.

Bridge result, coordinator result, lease reason, native operation phase, and artifact status remain separate failure dimensions. If capture end succeeds but no artifact reaches terminal observation, the public capture can complete only after the bounded artifact wait ends; the artifact is then `Failed`/non-authoritative with a warning. Authority is never silently inferred.

## Existing Envelope And Authority

An unverified file that is stable and hashed maps to `AssociationState.Unverified` (`PerfMeterExternalArtifactAssociationState.Unverified`), `FinalizationState.Finalized` (`PerfMeterExternalArtifactFinalizationState.Finalized`), and `AuthorityState.Observed` (`PerfMeterExternalArtifactAuthorityState.Observed`); it is never authoritative. A RenderDoc-authenticated result may set `AuthorityState.Authenticated` only when all of these are true: `AssociationState.BridgeAuthenticated` (`PerfMeterExternalArtifactAssociationState.BridgeAuthenticated`), `FinalizationState.Finalized`, `ContainsGpuCaptureData = PerfMeterExternalArtifactContentState.Present`, and a valid source SHA-256. Copy and Embed additionally require a valid post-copy SHA-256. `ToolAuthenticated` is reserved for a stronger tool-provided association.

The existing `IsAuthoritative` predicate is necessary but not sufficient for the native path. `PM-RDOC-003` must enforce the additional RenderDoc preconditions above before publishing authority, without changing the existing public envelope enums, results, MCP IDs, or schemas.

The current caller-supplied exporter path always creates a legacy `Unverified`/`Observed` embedded snapshot and cannot consume native authority. `PM-RDOC-003` must add a separate internal, generation-bound source descriptor carrying the already validated generic snapshot and RenderDoc provenance; it must not reinterpret the legacy caller path. The outer compatibility state maps no artifact to `Unavailable`, an observed/non-authoritative file to `FileObserved`, and only a fully authenticated native snapshot to `Authoritative`. `require_authoritative_external_artifact` may succeed only through this native descriptor path.

The versioned `external-artifact.json` output adds a RenderDoc-specific provenance object containing bridge ABI, negotiated app API, boundary mode, target mode, request nonce, `count_before`, capture index/timestamps, and source file identity. These are additive provenance fields, not substitutions for `ToolVersion`; schema/unknown-field fixtures are required before wiring.

## Privacy, Storage, And Ownership

An `.rdc` is sensitive GPU-capture data. The default is `MetadataOnly` plus `DoNotShare`; an export request must explicitly move to `ReviewBeforeShare`. There is no automatic upload. Public and MCP surfaces expose only relative project-local metadata, never an absolute source path. `Copy` and `Embed` are explicit opt-in modes.

Native source and copied/embed storage quotas are separate from the current generic 64 MiB/default bundle quota. The numeric source/copy quota, retention limit, free-space threshold, and owned cleanup/retry policy are intentionally gated decisions for `PM-RDOC-003`; they must be decided and tested before enabling the backend. `PM-RDOC-002` alone cannot expose production capture. Cleanup may delete only marker- or token-owned paths; unknown or external captures are never deleted.

## Capability Vocabulary

Future capability snapshots may add fields for static eligibility, module loaded, API negotiated, capture ready, and provenance available. Existing `renderdoc_supported` remains compatibility/static eligibility only; a requested tool is not verified identity. `PM-RDOC-003` must expose the explicit backend-selection mode and the exact pre-begin fallback reason without changing the default behavior of existing API/MCP calls.

## Validation Gates And Non-Goals

The isolated bridge requires fake-table, missing-module, missing-export, API `1.4`/`1.6`/`1.7` negotiation, mandatory discard, capture-count, zero/multiple/foreign candidates, path-buffer, invalid UTF-8, template restoration, concurrency rejection, and C/C++ ABI/result baselines. `PM-RDOC-003` managed/filesystem tests must cover traversal/reparse rejection, unique nonce roots, wildcard/multi-window ambiguity, file replacement/growth/deletion, lease retention through artifact wait, cancel, domain reload, scene load, runtime stop, and successful/uncertain begin. Privacy/storage tests cover selection defaults, metadata/copy/embed transitions, quota/free-space/retention, share confirmation, and marker-owned cleanup. Compile/tests must cover the optional assembly boundary before managed wiring.

Each initial Windows matrix row then needs a manual attached `.rdc` smoke that opens in the matching RenderDoc build, contains the intended GPU frame, preserves comments and, for API `1.6+`, the capture title, binds the expected path/index/timestamp/file identity and hashes, rejects overlap/foreign hotkeys, and records main/render-thread stall against a numeric budget approved before `PM-RDOC-003` enablement.

The original research intake was static. The later `PM-RDOC-002` validation compiled the C ABI and DLL with MSVC, passed the fake table suite, and launched a non-capturing production-resolver probe through portable RenderDoc v1.46 at the pinned commit; the probe observed the already-loaded module/export and negotiated app API `1.7.0`. It did not run a Unity frame capture, create/open an `.rdc`, or validate any D3D11/D3D12/Vulkan matrix row, so no support or release readiness is claimed. Replay, counters, analyzer protocol, and expensive analysis stay out-of-process under `PM-RDA`; no replay DLL/controller belongs in Unity.

Related decisions: [`capture-coordinator.md`](capture-coordinator.md), [`capture-bundles.md`](capture-bundles.md), and [`roadmap.md`](../backlog/roadmap.md). Research intake: `C:\Work\Unity\perfmeter-temp\00-index.md` and `C:\Work\Unity\perfmeter-temp\02-renderdoc-native-recommendations.md`.
