# RenderDoc GPU Command Annotations Decision

Status: `PM-RDANN-001/002` implemented on the feature branch. Windows x64 Editor/D3D12 real-capture acceptance passed on Unity `6000.4.12f1` and `6000.5.6f1`; a newly built separate bridge artifact and the first clean external consumer remain release gates. This document is not yet a published package support claim.

## Scope

PerfMeter exposes a public RenderDoc-neutral managed API that lets a Unity project attach bounded typed semantic state to GPU commands. RenderDoc-specific discovery, App API negotiation, Unity native-plugin lifecycle, command-target resolution, packet ownership, and function calls remain inside the optional `SGG.PerfMeter.RenderDoc` integration.

The package never ships, loads, injects, or installs `renderdoc.dll`. The bridge resolves only an already loaded module and requires RenderDoc App API `1.7`. GPU markers remain the navigation/timing structure; annotations add state to the draw or dispatch and do not replace markers.

## Public Contract And Schema V1

`PerfMeterGpuAnnotations` provides capabilities, the cheap `ShouldRecord` gate, ambient context publication, and command-buffer scopes. `PerfMeterRenderGraphGpuAnnotations` provides direct adapters for `RasterCommandBuffer`, `ComputeCommandBuffer`, and `UnsafeCommandBuffer`; a safe raster or compute pass is never converted into an unsafe pass for annotations.

`PerfMeterGpuAnnotationBatch` is reusable and bounded to 32 entries. Keys are case-sensitive ASCII paths, at most 127 UTF-8 bytes, using letters, digits, `_`, `-`, and `.` with no leading, trailing, or repeated dot. String values are strict UTF-8 and at most 255 bytes. Supported values are empty, bool, signed/unsigned 32-bit and 64-bit integers, float, double, string, and numeric/bool vectors with widths 1–4. Raw API-object/native resource handles are deliberately absent from v1.

Every recorded scope includes `SGG.Annotation.SchemaVersion = 1`. Canonical keys are:

| Key | Meaning |
| --- | --- |
| `SGG.Module` | Stable package or module identifier. |
| `SGG.RenderGraph.Pass` | Stable machine pass identifier, not a display label. |
| `SGG.Camera.StableId` | Stable or explicitly session-local camera identity. |
| `SGG.Asset.Material` | Baked/runtime-safe material identity when available. |
| `SGG.StableObjectId` | Stable domain object identity. |

Consumers extend the schema below `SGG.<Domain>.*`. `Object.GetInstanceID()` must not be presented as cross-run stable. Runtime code must not query `AssetDatabase`; asset identity must be baked or supplied by the owner.

## Scope And Context Semantics

Ambient context is an immutable snapshot published by `ownerId` and monotonically increasing nonzero generation. A late clear succeeds only for the exact active generation. Different owners may not publish the same key. Local pass values override ambient values. Publishing or clearing ambient state records no GPU command and performs no native call.

`BeginScope` records a native set packet before the annotated draw/dispatch and pre-creates a matching end packet that clears every key owned by that scope. `Dispose` records the end event into the same command stream. Scopes are intentionally non-nested in schema/ABI v1; consumers must use one lexical scope per logical pass and must dispose it. A missing provider, old bridge, unloaded RenderDoc, inactive capture, unsupported backend, invalid data, or exhausted packet pool is an explicit capability state or a safe no-op, not per-frame log spam.

## Additive Native ABI V1

The existing capture ABI remains binary compatible. Annotation transport is additive through four independently resolved exports:

- `SggRd_GetAnnotationCapabilitiesV1`;
- `SggRd_GetAnnotationEventV1`;
- `SggRd_CreateAnnotationPacketV1`;
- `SggRd_ReleaseAnnotationPacketV1`.

The annotation capabilities struct is 88 bytes and the fixed entry struct is 440 bytes under 8-byte packing. Each entry contains inline key/string storage and four 64-bit value lanes. Native and managed ABI tests fix sizes, offsets, enum values, and result mapping. Missing exports map to `BridgeTooOld` without affecting the capture ABI or the rest of PerfMeter.

The bridge owns a fixed pool of 64 immutable packets. Managed code synchronously copies entries into a free native packet and passes only an opaque slot-and-generation handle to `IssuePluginEventAndData`; the render-thread callback claims the exact generation, consumes it, and scrubs it exactly once. Cancellation before enqueue returns the packet. Stale handles cannot claim a reused slot. Invalid, duplicate-consumed, wrong-event, reset, and exhausted-pool paths are bounded and counted. No managed pointer is retained across the asynchronous Unity callback.

## Unity D3D12 Transport

The first implementation matrix row is Windows x64 Unity Editor with D3D12. The optional preloaded Editor plugin registers `UnityPluginLoad`/`UnityPluginUnload`, reserves one rendering event ID, subscribes to graphics-device initialize/reset/shutdown events, and uses `IUnityGraphicsD3D12v7`. At callback time it obtains the current `ID3D12Device` and `ID3D12GraphicsCommandList` from `CommandRecordingState`, then calls RenderDoc `SetCommandAnnotation` only while a capture is active.

The callback does no filesystem work, hashing, managed callback, dynamic logging, or heap-heavy formatting. Device reset/unload drops only still-allocated packets; an executing packet owns its completion. D3D11, Vulkan, Development Player, Linux, IL2CPP, mobile, Metal, and object/resource annotations are not enabled by this decision.

## Packaging And Availability

The UPM package remains binary-free. `sgg_renderdoc_bridge.dll` is built from the audited source in `Native~/RenderDocBridge`, the pinned RenderDoc header commit `7db2264afa00a5313154022f8c4ae0628a641300`, and Unity PluginAPI headers from a supported Unity editor installation. A release publishes it as a separately verified Windows x86_64 artifact that FTUE installs project-locally as an Editor-only plugin with all Player targets disabled. RenderDoc itself is never included.

The managed RenderDoc provider registers additively in the Editor. Without an installed binary, capabilities report `BridgeUnavailable`; with the currently published `2026.8.11-1` capture-only bridge, annotation capabilities report `BridgeTooOld`. Core runtime APIs and the older capture bridge contract continue to work in both cases. A new artifact must be published and pinned before annotations become an installable supported feature.

## Validation And Remaining Gates

Automated acceptance includes strict key/UTF-8/value validation, all scalar/vector mappings, ambient generation and owner collision, set/end ordering, failed recording cleanup, fixed C/C++/managed ABI layouts, fake App API 1.7 calls, wrong-event drop, native rejection, packet exhaustion, and no-provider/no-RenderDoc degraded behavior.

The pre-rebase native Release build passed CTest `1/1` and all `16/16` fake-table cases. The managed annotation suite passed `10/10`; the final Unity `6000.4.12f1` D3D12 EditMode suite passed `433/433`. These counts are historical evidence from the original feature base and must be refreshed after rebasing onto the current `origin/main`. The same legacy settings test intentionally fails under `-nographics` because overdraw requires `AsyncGPUReadback`, so the authoritative full run used D3D12.

The D3D12 real-tool gate was run on both Unity `6000.4.12f1` and `6000.5.6f1` with portable RenderDoc v1.46 at commit `7db2264a`, then repeated after the generation-safe packet-handle hardening. The reusable manual smoke recorded four set calls, a red `ClearRenderTargetView`, four exact-key deletes, and a neighboring blue clear. Native counters required two executed packets, eight calls, and zero errors. Converting each final `.rdc` to RenderDoc XML confirmed `SGG.Annotation.SchemaVersion`, `SGG.Module`, `SGG.RenderGraph.Pass`, and `SGG.PerfMeter.Smoke.Sequence` before the red clear and their deletion before the blue clear. The final captures were 6,175,383 bytes (SHA-256 `EF9336C335547BF63CB9EBCFA4C2BCF4159D43412DC24AAEDA1E220730EB2D0C`) and 3,846,009 bytes (SHA-256 `F375ED17058E1C49B7AC4E88B33831976E2BBD3374E90B55DA729D819B12073B`) respectively; these local sensitive artifacts are not committed.

The first clean external package consumer must still prove optional dependency behavior and ambient/local merge before a release/support statement. Vulkan, object annotations, Player, and D3D11 remain independent later gates.

Related decisions: [`renderdoc-native-boundary.md`](renderdoc-native-boundary.md), [`renderdoc-storage-policy.md`](renderdoc-storage-policy.md), and [`roadmap.md`](../backlog/roadmap.md).
