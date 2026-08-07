# Graphics State Collection Decision

Status: implemented for `PM-GFX-001`; released `2026.8.7-1`.

## Context

PerfMeter needs a bounded way to investigate shader and graphics-pipeline stutter without turning the core package into a graphics-state or native-capture dependency. Unity exposes `GraphicsStateCollection` through different namespaces across Unity 6000.4 and 6000.5+, and the resulting artifact can be large and project-local. Marker availability and marker units also vary by Unity version, platform, and active runtime catalog, so the public data must preserve provenance instead of presenting every value as a count.

## Decision

The core public API exposes `GetGraphicsDiagnostics()`, `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)`, and `CancelGraphicsStateTrace(string captureId)`. A custom integration implements `IPerfMeterGraphicsStateCollectionBackend`; the separate `SGG.PerfMeter.GraphicsStateCollection` assembly supplies the Unity backend only for Unity `6000.4+`.

The backend aliases `UnityEngine.Experimental.Rendering.GraphicsStateCollection` on Unity `6000.4` and `UnityEngine.Rendering.GraphicsStateCollection` on Unity `6000.5+`. It reports backend identity/version, trace/prewarm support, cache-miss support, parallel-PSO capability, the 600-frame trace limit, and the 64 MiB artifact limit without making the core assembly depend on one namespace.

Shader GPU-program creation uses the exact `Shader.CreateGPUProgram` recorder name and declared aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram`, and `Shader.DynamicLoadGPUProgram`. Graphics-pipeline creation uses the exact `CreatePSO.Job` name. Discovery happens at runtime startup and explicit catalog refresh/reconfigure. Capability snapshots retain exact/alias resolution, resolved recorder names, category, discovered unit, data type, resolved component count, sampled component count, and catalog revision. Metric values remain raw `ProfilerRecorder` values; consumers must use the capability metadata and sample state rather than assuming shader or PSO counts or converting units.

A trace is admitted only while a PerfMeter session is recording and while the external-capture, memory-snapshot, and alert-capture domains are idle. The session must remain recording throughout the trace: `StopSession()` cancels an active trace. The coordinator owns one graphics-state flight at a time, including preparation, tracing, ending, prewarm, and pending cleanup. The requested trace frame count is advanced after `WaitForEndOfFrame`; batch mode uses a next-frame `yield null` fallback. Session samples admitted during the active trace carry the trace `capture_id` as `graphics_state_trace_id`; session settings therefore control correlated sample density, not the number of trace frames.

Trace output is created and validated as an owned non-empty regular `.graphicsstate` file below `Temp/PerfMeter/GraphicsStateCollections`; absolute, traversing, outside, reparse-point, and oversized paths are rejected. A new trace cleans the previous owned artifact when possible. Repeating the active capture ID returns `AlreadyActive`, a different active request returns `RejectedOverlap`, and cancellation applies only to the matching active/preparing ID, cancels the backend, and cleans the pending artifact. `IsBusy`/`is_busy` covers active flights and pending cleanup, while `HasPendingCleanup`/`has_pending_cleanup` identifies an owned artifact awaiting retry. Cleanup failures remain visible and block replacement until retry succeeds.

When owned deletion fails, the storage writes an adjacent `.delete-pending` sidecar marker. A new coordinator restores that marker after domain reload, reports the cleanup warning and busy state, and retries cleanup before accepting another graphics-state operation. The marker is removed only after the owned artifact is deleted.

Prewarm accepts only an owned project-relative artifact and runs Unity `WarmUp` or `WarmUpProgressively` synchronously through a completed `JobHandle`. It preserves the input artifact, reports completed warmup count and `IsWarmedUp`, and can complete with an explicit incomplete warning. Unity's backend reports `SupportsCacheMissTracing = false`; requesting `traceCacheMisses` returns `Unavailable` before backend prewarm, and no cache-miss evidence is exposed. The MCP prewarm command consequently has no cache-miss input.

## Validation

Unity `6000.4.12f1` compile passed; targeted GSC EditMode `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208`, and full post-fix PlayMode `16/16` passed. An isolated Unity `6000.5.6f1` optional consumer compile also passed. Full Unity `6000.5` tests, release-player behavior, and target-device behavior are not claimed by this record.

## Release Gate

Release requires the Unity `6000.4` experimental and `6000.5+` rendering namespace/assembly matrix, full Unity `6000.5` tests, release-player behavior, and target-device validation. The final Unity `6000.4.12f1` evidence validates post-fix compile, targeted/full EditMode and PlayMode behavior, dynamic marker/provenance and unit checks, active-session correlation, end-of-frame and batch lifecycle, overlap/cancel/prewarm cleanup, and the 64 MiB owned-artifact limit, but does not substitute for those release gates.
