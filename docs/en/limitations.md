# Limitations

SGG PerfMeter is designed as a low-overhead runtime diagnostics layer, not a deep capture replacement for Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.

## Platform And Pipeline Scope

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline is unsupported and not planned.
- HDRP overdraw and heatmap are unsupported. HDRP projects still get FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup, and MCP diagnostics.
- Unity `2022.3` through `6000.3` may import for compile-safety, but runtime behavior and support target Unity `6000.4+`.

## Timing Availability

- GPU timing can be unavailable, delayed, or unreliable depending on platform and graphics API.
- `CollectionFrame` is the Unity frame where PerfMeter collected the snapshot, not necessarily the exact hardware frame represented by `FrameTimingManager`.
- Android should prefer Vulkan when GPU frame timing matters.
- OpenGL/OpenGLES should be treated as degraded mode for GPU timing and overdraw instrumentation.

## Counter Availability

Profiler counters vary by platform, Unity version, render pipeline settings, and graphics API. Use `AvailableCounters`, `UnavailableCounters`, and warnings instead of assuming every counter exists everywhere.

## External GPU Capture

- `GenericUnity` uses Unity's experimental `ExternalGPUProfiler`; it does not ship, inject, launch, or identify RenderDoc or PIX. It is available only in the Editor or Development Builds when the requested profiler is already attached.
- The generic requested-tool matrix remains RenderDoc on Windows/Linux desktop with Direct3D 11, Direct3D 12, or Vulkan, and PIX on Windows desktop with Direct3D 12. Generic completion cannot authenticate tool or artifact identity.
- The optional native RenderDoc path is supported only in the Windows x64 Unity Editor with Direct3D 11, Direct3D 12, or Vulkan. Development Player, Linux native, IL2CPP, mobile, and macOS native paths are unsupported.
- The UPM package remains binary-free. Its separately published pinned bridge resolves only an already-loaded `renderdoc.dll`; neither the package nor bridge installs, loads, launches, injects, or bundles RenderDoc/replay binaries.
- One capture request can be active at a time. Heavy capture is always explicit and opt-in.
- Generic `Completed` confirms only Unity's wrapper lifecycle. Native status additionally exposes backend kind and generation-bound phase; authority requires exactly selected, finalized, bridge-authenticated `.rdc` evidence with stable identity and hashes.
- Native PIX circular timing capture is unavailable. Microsoft's documented Windows timing API supports forward capture but ignores circular-storage, memory-limit, and discard controls; PerfMeter does not replace the requested pre-alert ring with forward capture without a documented storage bound or with private PIX integration.
- Native MetadataOnly defaults to `DoNotShare`; Copy/Embed data is sensitive, separately quota-managed, marker-owned, and `ReviewBeforeShare`. Caller-supplied artifacts always remain observed and non-authoritative.
- Real attached RenderDoc validation covers the initial native D3D11/D3D12/Vulkan Editor rows; broader platforms and players remain release gates rather than inferred support.

## RenderDoc Command Annotations

- Command annotations are a separate optional integration from the broader `ExternalGPUProfiler` capture matrix. The initial transport is Windows x64 Editor/D3D12 only and requires an already-loaded RenderDoc App API `1.7` module plus an active capture.
- The UPM package remains binary-free. Annotations require a separately installed Editor bridge artifact with the additive annotation exports; the currently published `2026.8.11-1` capture bridge reports `BridgeTooOld` for annotations. Neither package nor bridge ships, loads, injects, or installs RenderDoc.
- Batches are bounded to 32 entries, keys to 127 bytes, strings to 255 UTF-8 bytes, and the native pool to 64 pending packets. Exhaustion and unavailable states are explicit no-ops.
- V1 scopes are non-nested and must be disposed. They clear their own keys, but cannot restore annotation state written independently by another library.
- API-object/resource annotations, D3D11, Vulkan, Development Player, IL2CPP, Linux, mobile, and Metal are not supported by this initial transport. Each requires a separate real-capture gate.
- Real D3D12 `.rdc` smokes passed on Unity `6000.4.12f1` and `6000.5.6f1` with the pinned RenderDoc v1.46 build: the annotated red clear was bracketed by set/delete calls and the neighboring blue clear followed deletion. A clean external package consumer remains a release gate.

## Overdraw Cost And Support

Numerical overdraw and visual heatmap are diagnostic modes. They add rendering work and should be used in bounded windows, not left on as steady-state gameplay UI.

Numerical overdraw requires URP and:

- `PerfMeterRenderGraphFeature` installed into the active URP renderer;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- supported graphics API;
- async GPU readback support.

Unsupported targets, including HDRP, report `OverdrawState.Unsupported` with warnings.

## Overlay Cost

The overlay is allocation-conscious and throttled, but changed numeric values and graph labels can still materialize managed strings at the refresh interval. It has two UI Toolkit backend paths: an owned `UIDocument` host on Unity `6000.4` and an owned `PanelRenderer` host on Unity `6000.5+`. The host preserves foreign UI panel settings and children and rebuilds only the PerfMeter-owned container. Numeric values use stable reserved slots and a numeric monospace role; `FpsOnly` uses a deterministic bounded two-row fallback when one row does not fit, while cards and bars wrap at narrow logical widths. These bounds do not promise every arbitrary resolution or scale, so heavy visual diagnostics, graph modes, and the resulting layout should be validated on target devices.

## Validation Status

Current validation includes automated EditMode coverage, HDRP smoke validation in Unity `6000.4.10f1`, and previous Android S23 Vulkan/GLES smoke validation. Broader player-build and device coverage is still useful before treating data as release-signoff evidence.

## Optional Memory Snapshots: Limits And Privacy

- The feature is unavailable without `com.unity.memoryprofiler` `1.1.0+` on Unity `6000.4+`; the core package does not install or require that dependency.
- Manual capture is the only default. System-memory threshold and bounded leak-growth triggers are opt-in, and every request is subject to single-flight/overlap, cooldown, minimum-free-space, backend, and capture-flag guards.
- Owned `.snap` staging is under `Temp/PerfMeter/MemorySnapshots` and is limited to 512 MiB. Memory-only evidence is exported below `Temp/PerfMeter/CaptureBundles`; bundle retention has a total 2 GiB quota. A successful export is one-shot and removes the staging source, subject to explicit cleanup warnings.
- Memory snapshots can contain sensitive process memory. Protect and review them before sharing. The bundle records `contains_sensitive_memory`, backend/flag provenance, `memory-snapshot.json`, and SHA-256 metadata; it does not create an external GPU artifact.
- OS-locked deletion and portable managed reparse-point race protections are best-effort. Unsafe or unowned paths are rejected, and cleanup failures remain visible as warnings rather than being silently ignored.
- Evidence includes memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, optional compilation with `com.unity.memoryprofiler@1.1.12`, and Unity `6000.4.12f1` full EditMode `182/182` plus full PlayMode `14/14`. This is not a release-player or device-behavior claim.

## Graphics Diagnostics And Graphics-State Collection Limits

- Shader GPU-program and graphics-pipeline markers are dynamic `ProfilerRecorder` capabilities. Unity, platform, graphics API, and catalog refresh state can change availability. Use `Unavailable`, `AvailableNoSample`, and `AvailableSampled` plus provenance; do not infer availability from a numeric zero.
- Marker values retain their discovered `Unit` and `DataType` and are raw recorder values. They are not universally shader or PSO counts, and PerfMeter does not convert them to a common unit. Exact/alias resolution, resolved recorder names, resolved/sampled component counts, and catalog revision are part of the capability metadata.
- The optional `SGG.PerfMeter.GraphicsStateCollection` assembly targets Unity `6000.4+`. It uses `UnityEngine.Experimental.Rendering.GraphicsStateCollection` on Unity `6000.4` and `UnityEngine.Rendering.GraphicsStateCollection` on Unity `6000.5+`; older Unity versions are not supported for this integration.
- A trace requires an active PerfMeter session. Trace frames are finalized after end-of-frame in normal Play Mode and with a next-frame fallback in batch mode. Correlated session samples are subject to the session's warm-up, interval, and maximum-sample settings.
- Only one graphics-state flight is admitted, including preparation, trace finalization, prewarm, and cleanup. Active external GPU capture, memory snapshot, and alert-capture work also cause overlap rejection. `IsBusy`/`is_busy` covers those flights and persisted cleanup; `HasPendingCleanup`/`has_pending_cleanup` specifically reports an owned artifact awaiting retry. Matching cancellation is best effort; cleanup failures remain visible and can delay the next request.
- `StopSession()` cancels an active trace, so an active session is required throughout the trace. Failed owned-artifact deletion creates an adjacent `.delete-pending` sidecar marker; it is restored after domain reload and retried. The warning and busy state remain visible until both artifact cleanup and marker removal succeed.
- Prewarm accepts only an owned project-relative artifact, runs synchronously, preserves the artifact, and may report incomplete progressive warmup. The Unity backend does not support cache-miss tracing; requesting it returns `Unavailable`, and no cache-miss evidence is exposed.
- Owned `.graphicsstate` artifacts are stored below `Temp/PerfMeter/GraphicsStateCollections`, must be regular non-empty files, and are limited to 64 MiB. Trace length is limited to 600 frames and progressive prewarm count to 1,000,000 states. Minimum-free-disk and project-local path guards apply.
- Final evidence is Unity `6000.4.12f1` compile passed; targeted GSC EditMode `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208`, and full post-fix PlayMode `16/16`. An isolated Unity `6000.5.6f1` optional consumer compile also passed. Full Unity `6000.5` tests, release-player behavior, and target-device behavior remain release gates and are not claimed here.

## Render Integration Context Limits

- The public `PerfMeterRenderIntegrationSnapshot` is integration-neutral, but it is an observation contract, not a deep Render Graph or Custom Pass capture. Reads do not start runtime collection; before the first observation the supported current pipeline may be `Available` with `NotObserved`, and a pipeline/configuration change marks the previous observation stale with `ObservationMatchesCurrentPipeline: false` and an explicit age/warning.
- URP uses public current-frame `UniversalRenderingData.renderingMode` and reports the PerfMeter passes actually scheduled for that frame. HDRP reports the actual PerfMeter `CustomPass`, while effective rendering mode remains unavailable.
- Private/internal Render Graph pass/resource reflection was removed. The legacy facade keeps `registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count`, and `aliased_resource_count` at `-1` because no stable public API exposes them.
- GRD activity uses the public `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` result and is global runtime state, not proof that a particular camera or renderer used GRD. URP Forward+ is a current-frame observation; HDRP rendering-mode/Forward+ availability remains `Unknown`.
- GRD effectiveness uses aggregate BRG draw-call/instance recorder aliases with exact capability provenance. These counters can include other `BatchRendererGroup` users and therefore do not prove per-renderer GRD participation; unavailable or not-yet-sampled values serialize as `null`.
- VRS reports authoritative `SystemInfo`/`ShadingRateInfo` hardware support. Configuration and activity remain `Unknown` unless a future typed adapter proves them; this snapshot does not claim VRS activity.
- Unity exposes no stable public RenderGraph/CustomPass viewer or pass-target API, so PerfMeter adds no Editor navigation and does not promise it.
- Capture context schema v1 preserves `render` and adds `render_integration`; session JSON/CSV schemas are unchanged. External capture context is frozen on the first `Capturing` sample, not continuously replaced by later reads.
- PM-REN-001 final evidence is Unity `6000.4.12f1` main compile passed; targeted `PerformanceMeterApiTests` `53/53`, `PerfMeterCaptureBundleTests` `15/15`, and `PerformanceMeterPlayModeSmokeTests` `12/12`; final full EditMode `215/215` and full PlayMode `16/16` passed. Focused review P1/P2 resolved. The isolated compile matrix passed for Unity `6000.4.12f1` URP `17.4` and HDRP `17.4`, and Unity `6000.5.6f1` URP `17.5` and HDRP `17.5`. Release-player/device validation remains pending; no release claim is made.
- PM-GRD-001 final evidence is Unity `6000.4.12f1` compile passed; targeted API `58/58`, capture-bundle `15/15`, and PlayMode smoke `12/12`; full EditMode `220/220` and PlayMode `16/16` passed. Focused review P1/P2 resolved, and the Unity `6000.4`/`6000.5` URP `17.4`/`17.5` and HDRP `17.4`/`17.5` compile matrix passed. Release-player/device behavior remains pending.
