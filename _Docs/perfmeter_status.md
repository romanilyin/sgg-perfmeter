# SGG PerfMeter Status

## Package State

- Package metadata is `com.sungeargames.perfmeter` / `SGG PerfMeter`; no `sky` or `weather` package identity remnants were found under `Assets/Scripts/SGG.PerfMeter/`.
- User-facing docs are maintained in English and Russian under `Assets/Scripts/SGG.PerfMeter/.Documentation/`.
- Public runtime API entry point is `SGG.PerfMeter.PerformanceMeter`; snapshots use `CollectionFrame` and MCP JSON uses `collection_frame` to avoid implying exact `FrameTimingManager` sample-frame identity.
- Runtime now includes opt-in numerical overdraw measurement through a hidden replacement shader, fragment-stage atomic counter buffer, and `AsyncGPUReadback`.
- Unsupported overdraw targets are gated through `OverdrawState.Unsupported` before scheduling Render Graph work.
- Runtime snapshots expose FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing, and overdraw values through API, MCP JSON, and overlay output.
- `DrawCalls`, `Batches`, BRG draw calls, and BRG instances are aggregates of Unity 6000 component `ProfilerRecorder` counters, not single recorder names.
- Runtime overlay corner placement is configurable through `PerformanceMeter.SetOverlayCorner(...)`; display mode is configurable through `PerformanceMeter.SetOverlayMode(...)` with `FpsOnly`, `TextCompact`, `Graphs`, and `Full`; target line budget is configurable through `PerformanceMeter.SetTargetFps(...)` for 15/30/60/90/120/144/240 FPS; default placement is `TopRight`/`Full`/`60 FPS`, with stacked CPU graph layout, softer colored graph legends, fixed-width graph numbers, placeholder formatting for unavailable samples/counters, text min/max history, and generated UI Toolkit theme/text settings for reliable runtime text rendering.
- Editor setup window now has `Setup` and `Runtime` tabs under `SGG/Perfmeter/Setup`: setup discovers active URP renderers from Graphics/Quality settings first, falls back to renderer assets under `Assets`, marks renderer assets under `Packages` as not editable, installs into all or selected editable renderers, and exposes Play Mode runtime controls for target FPS, overlay mode/corner/visibility, and short overdraw capture.
- `PerfMeterRenderGraphFeature` no longer enqueues an empty overlay marker pass by default; enable `Record Overlay Marker Pass` only for diagnostic/self-overhead measurement. Active overdraw requests still enqueue the needed Render Graph pass.
- Android player smoke validation helpers now exist outside the package: `PerfMeterAndroidBuild.BuildDevelopmentApk` builds a Development APK after recommended setup, accepts `-perfMeterAndroidGraphics vulkan|gles3` and `-perfMeterAndroidApk <path>` overrides, and `PerfMeterAndroidSmokeBootstrap` runs only in Android players to emit `SGG_PERFMETER_SMOKE` logcat markers.
- Tests include EditMode API/classifier coverage and PlayMode runtime smoke coverage for overlay lifecycle, snapshot updates, and overdraw terminal/degraded states.
- Bottleneck classification now uses main-thread work time after present wait, returns `Unknown` for significant present wait without GPU timing when CPU work is below budget, and picks the dominant CPU/GPU overshoot for mixed overloads.
- Overdraw measurement defaults to Game cameras only, supports an optional camera-name filter, and ignores stale `AsyncGPUReadback` callbacks from older measurement sessions.

## Verification Notes

- Use Unity batchmode compile and `-runTests` without `-quit` as the reliable local verification path. The project version remains `6000.4.5f1`; current Android validation used installed Unity `6000.4.7f1` because that editor has Android Build Support.
- Latest package hardening verification passed with `Logs/opencode-hardening-compile.log`, `Logs/opencode-hardening-editmode-results.xml`, and `Logs/opencode-hardening-playmode-results.xml`.
- Latest Unity `6000.4.7f1` checks passed with `Logs/opencode-6000.4.7-compile-2.log`, `Logs/opencode-6000.4.7-editmode-results.xml`, `Logs/opencode-6000.4.7-playmode-results.xml`, and `Logs/opencode-android-smoke-compile-6000.4.7.log`.
- Android Development APK build passed with `Logs/opencode-android-s23-build-terminal.log`; required external NDK is `C:/Work/SDK/AndroidSDK/ndk/27.2.12479018`.
- Galaxy S23 validation passed on `SM-S911B` / Android 16 API 36 / Adreno 740 / Vulkan: logcat emitted `SGG_PERFMETER_SMOKE bootstrapped`, `frame_timing=Available`, `gpu_available=True`, and `overdraw_state=Completed` with empty `last_error`; no `AndroidRuntime` fatal output appeared in the filtered dump.
- Galaxy S23 OpenGLES3 fallback validation passed with `Logs/opencode-android-s23-gles-build.log`: player started on OpenGL ES 3.2, smoke markers reported `graphics=OpenGLES3`, `frame_timing=Available`, `gpu_available=True`, and `overdraw_state=Unsupported` with the expected warning about fragment UAV/storage-buffer instrumentation requiring a modern backend; no `AndroidRuntime` fatal output appeared in the filtered dump.
- Recent hardening compile logs: `Logs/opencode-iter2-setup-ui-compile.log`, `Logs/opencode-iter3-bottleneck-compile.log`, `Logs/opencode-iter4-collection-frame-compile.log`, `Logs/opencode-iter5-overdraw-unsupported-compile-2.log`, `Logs/opencode-setup-discovery-compile.log`, and `Logs/opencode-marker-optin-compile.log`.
- Do not add `-quit` to Unity `-runTests` commands; Unity writes XML results and exits after the run.
- Broader target-device validation is still useful for older GPUs/OS versions and platform-specific ProfilerRecorder counter availability.

## Handoff Notes

- Next meaningful implementation work should focus on overdraw heatmap output, broader device matrix coverage, or Render Graph analytics.
- `ProjectSettings/ProjectSettings.asset` currently has `enableFrameTimingStats: 1`; keep it enabled before depending on `FrameTimingManager` in builds.
