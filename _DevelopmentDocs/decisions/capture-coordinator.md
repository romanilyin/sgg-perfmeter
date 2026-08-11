# Capture Coordinator Decision

Status: implemented for `PM-CAP-001`; released `2026.8.6-2`.

## Decision

PerfMeter owns one frame-driven external GPU capture coordinator. A request advances on the Unity main thread through `PreRoll`, `Capturing`, `PostRoll`, and `Completed`. `Canceled`, `Unavailable`, and `Error` are explicit terminal states. A repeated active capture ID is idempotent; a different active ID is rejected without replacing the active request.

Only `Capturing` owns the existing alert capture scope and invokes the backend. Pre-roll and post-roll remain normal performance samples. Runtime stop/disable performs best-effort backend and alert-scope cleanup before resetting coordinator instrumentation.

## Backend Contract

The `GenericUnity` production backend wraps `UnityEngine.Experimental.Rendering.ExternalGPUProfiler` behind `UNITY_EDITOR || DEVELOPMENT_BUILD`. It requires an already attached external profiler and evaluates an explicit requested tool against this matrix:

| Tool | Platform | Graphics API |
| --- | --- | --- |
| RenderDoc | Windows or Linux desktop | Direct3D 11, Direct3D 12, or Vulkan |
| PIX | Windows desktop | Direct3D 12 |

Unity does not expose attached-tool identity or authoritative artifact paths through this generic API. Callers therefore select `RenderDoc` or `Pix` explicitly. Generic `Completed` confirms only the guarded begin/end lifecycle. The optional `NativePreferred`/`NativeRequired` RenderDoc path is generation-bound and separately limited to Windows x64 Unity Editor D3D11/D3D12/Vulkan.

Automated tests use the internal backend/scope seams. Real RenderDoc/PIX attachment and artifact confirmation are release-candidate gates.

## Public Contract

- `PerformanceMeter.RequestCapture(PerfMeterCaptureOptions)` returns a typed start/idempotency/overlap/capability/failure result.
- `PerformanceMeter.GetCaptureStatus()` is safe before runtime startup and reports requested/completed frame counts plus warnings.
- `PerformanceMeter.CancelCapture(...)` validates ownership by capture ID and retries failed cleanup when possible.
- `SGG.PerfMeter.Capture.Coordinator` and `SGG.PerfMeter.Capture.State` expose internal Profiler instrumentation without changing session schemas.

## Implemented By PM-CAP-002

- Bundle manifests, atomic artifact export, correlated samples/context/screenshots, truthful provenance, path validation, and MCP capture control are defined in [`capture-bundles.md`](capture-bundles.md).
- Native RenderDoc control/provenance is implemented for the `2026.8.11-1` candidate; native PIX remains separate future work.
