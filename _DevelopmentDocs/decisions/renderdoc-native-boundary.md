# Native RenderDoc Boundary Decision

Status: `PM-RDOC-001..003` implemented for the `2026.8.11-1` release candidate. The initial Windows x64 Unity Editor D3D11/D3D12/Vulkan rows passed production-wired real `.rdc` validation. `PM-RDANN-001/002` is an additive feature-branch extension to the same separately distributed bridge; it is not part of the published `2026.8.11-1` artifact. Broader platforms/players and replay analysis remain deferred.

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

Each row passed its own attached real RenderDoc smoke with authenticated artifact, title/comments, replay XML/thumbnail, and stall evidence. Development Player, Linux x64, IL2CPP, mobile, and macOS are deferred and are not claimed.

The staged rollout is complete through `PM-RDOC-003`: the accepted ADR, fixed Windows bridge, managed selection/provenance/storage, production registration, real smoke gates, and separately verified optional bridge distribution form the `2026.8.11-1` candidate. `PM-RDA` remains a separate later rollout.

## Already-Loaded Native Boundary

The Windows bridge may resolve only a module that is already loaded through the supported external/Unity RenderDoc flow:

```text
GetModuleHandleW(L"renderdoc.dll")
GetProcAddress(module, "RENDERDOC_GetAPI")
```

If the module or export is absent, the bridge reports that condition. It must never call `LoadLibrary`, accept a DLL path from MCP or settings, bundle or install RenderDoc, elevate privileges, inject or self-inject, or otherwise attach a production player. The RenderDoc installation remains user-owned. The optional Windows x64 Editor plugin and managed adapter have no core dependency; no RenderDoc binary or vendor SDK is shipped.

`PM-RDOC-002` pins the public RenderDoc app header to upstream commit [`7db2264afa00a5313154022f8c4ae0628a641300`](https://github.com/baldurk/renderdoc/commit/7db2264afa00a5313154022f8c4ae0628a641300), verifies SHA-256 at configure time, and retains its MIT license notice. The binary-free UPM package pins the separately published bridge bytes; FTUE installs them only as a Windows x64 Editor-only plugin with every player target disabled.

The annotation extension keeps that distribution boundary. A new bridge artifact adds Unity native-plugin load/unload exports and the annotation ABI; no DLL is embedded in the UPM package. Until that artifact is published and pinned by FTUE, an installed `2026.8.11-1` bridge remains valid for capture and is reported as `BridgeTooOld` only for annotation calls.

## Coordinator And Frame Boundary

Native capture reuses the existing capture coordinator and lease resources; it does not introduce an independent overlap policy. `PM-RDOC-003` adds the internal asynchronous control/artifact-observer seam with generation-bound preflight, begin, end, artifact wait/finalization, and terminal phases. The generic backend remains the compatibility default; existing results, MCP IDs, bundle schemas, and timeline schemas remain compatible.

The capture lease remains held from storage preflight until the native operation reaches an artifact terminal state and its immutable capture/bundle/generation association is frozen. A later request cannot supersede an operation waiting for an artifact. Domain reload or runtime stop marks a nonterminal operation `LostSession`, never resumes authority from path evidence alone, and performs only marker-owned cleanup that can be proven safe.

The implemented path accepts the request on the Unity main thread, begins before the intended captured render, and schedules end through `WaitForEndOfFrame`; the recorded boundary mode is `managed_end_of_frame`. Filesystem preflight, polling, copying, hashing, and retention run on the task worker, not the main or render-frame path.

The initial bridge uses RenderDoc's `(NULL, NULL)` active-context target and records `target_mode = wildcard_active_context`; it does not claim a specific Editor window or graphics-device handle. More than one matching artifact, an unexpected context, or a multi-window result that cannot be disambiguated is non-authoritative and fails the native association gate. A future explicit device/window target requires a new versioned bridge capability and real matrix evidence.

## Fixed Bridge ABI V1

The bridge owns a separate fixed C ABI, rather than exporting RenderDoc structs directly. Its capture functions are suffixed `V1`, including `SggRd_GetCapabilitiesV1`, `SggRd_BeginCaptureV1`, `SggRd_EndCaptureV1`, `SggRd_DiscardCaptureV1`, `SggRd_TryGetNewArtifactV1`, and `SggRd_SetCaptureCommentsV1`. The annotation extension adds `SggRd_GetAnnotationCapabilitiesV1`, `SggRd_GetAnnotationEventV1`, `SggRd_CreateAnnotationPacketV1`, and `SggRd_ReleaseAnnotationPacketV1` without changing the capture ABI. The separately built plugin also exports `UnityPluginLoad` and `UnityPluginUnload` for Unity graphics-device lifecycle registration.

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
  | `SGG_RD_ANNOTATIONS_UNAVAILABLE` | 12 |
  | `SGG_RD_CAPTURE_INACTIVE` | 13 |
  | `SGG_RD_BACKEND_UNSUPPORTED` | 14 |
  | `SGG_RD_PACKET_POOL_EXHAUSTED` | 15 |
  | `SGG_RD_ANNOTATION_REJECTED` | 16 |

Annotation packets use a fixed pool of 64 slots and opaque slot/generation handles. Managed data is copied synchronously before `IssuePluginEventAndData`; the render-thread callback claims and scrubs the exact generation once. The initial callback transport is Windows x64 Editor/D3D12 only and resolves `IUnityGraphicsD3D12v7` command-recording state. Missing annotation exports degrade to `BridgeTooOld` without weakening capture behavior.

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

The existing `IsAuthoritative` predicate is necessary but not sufficient for the native path. The native descriptor enforces the additional RenderDoc preconditions above before publishing authority without changing existing public envelope enums, results, MCP IDs, or schemas.

The caller-supplied exporter path always creates a legacy `Unverified`/`Observed` snapshot and cannot consume native authority. The separate internal generation-bound source descriptor carries validated RenderDoc provenance without reinterpreting that legacy path. Only a fully authenticated native snapshot maps to `Authoritative` and may satisfy `require_authoritative_external_artifact`.

The versioned `external-artifact.json` output adds a RenderDoc-specific provenance object containing bridge ABI, negotiated app API, boundary mode, target mode, request nonce, `count_before`, capture index/timestamps, and an opaque SHA-256 binding for the source file identity. Raw Windows volume/file IDs stay internal under the storage/privacy policy. These are additive provenance fields, not substitutions for `ToolVersion`; schema/unknown-field fixtures are required before wiring.

## Privacy, Storage, And Ownership

An `.rdc` is sensitive GPU-capture data. The default is `MetadataOnly` plus `DoNotShare`; an export request must explicitly move to `ReviewBeforeShare`. There is no automatic upload. Public and MCP surfaces expose only relative project-local metadata, never an absolute source path. `Copy` and `Embed` are explicit opt-in modes.

Native source and Copy/Embed storage quotas are separate from the generic 64 MiB/default bundle quota. The implemented [`PM-RDOC-003A` policy](renderdoc-storage-policy.md) fixes the initial per-file, aggregate, retention, free-space, polling, cleanup/retry, privacy, and stall values. Cleanup may delete only marker- or token-owned paths; unknown or external captures are never deleted.

## Capability Vocabulary

Capability snapshots expose static eligibility, module/API readiness, backend-selection mode, native phase, result code, and exact pre-begin fallback reason. Existing `renderdoc_supported` remains compatibility/static eligibility only; a requested tool is not verified identity.

## Validation Gates And Non-Goals

Automated coverage includes fake-table, missing module/export, API negotiation, discard, capture count, candidate ambiguity, path/UTF-8/template/concurrency cases, fixed ABI baselines, traversal/reparse and replacement races, nonce ownership, lease retention, cancel/reload/scene/runtime teardown, MetadataOnly/Copy/Embed, quota/free-space/retention, privacy, and cleanup.

Annotation coverage additionally fixes native/managed struct layouts and result values, validates the generation-safe packet pool and wrong-event/reset/exhaustion paths, and exercises public batch/context/scope behavior. Real annotation acceptance is a separate D3D12 `.rdc` gate and does not expand the capture matrix to D3D11 or Vulkan.

Each initial Windows matrix row passed a manual attached `.rdc` smoke in the matching RenderDoc build with intended GPU-frame content, live title, persisted comments, bound path/index/timestamp/file identity and hashes, overlap/foreign-hotkey rejection, and the fixed `PM-RDOC-003A` main/render-thread stall budget.

The initial isolated `PM-RDOC-002` probe was later superseded by production-wired D3D11/D3D12/Vulkan validation under portable RenderDoc v1.46 at the pinned commit and negotiated app API `1.7.0`. Replay, counters, analyzer protocol, and expensive analysis stay out-of-process under `PM-RDA`; no replay DLL/controller belongs in Unity.

Related decisions: [`renderdoc-gpu-annotations.md`](renderdoc-gpu-annotations.md), [`renderdoc-storage-policy.md`](renderdoc-storage-policy.md), [`capture-coordinator.md`](capture-coordinator.md), [`capture-bundles.md`](capture-bundles.md), and [`roadmap.md`](../backlog/roadmap.md). Research intake: `C:\Work\Unity\perfmeter-temp\00-index.md` and `C:\Work\Unity\perfmeter-temp\02-renderdoc-native-recommendations.md`.
