# SGG PerfMeter Status

## Current Readiness

- Package identity is `com.sungeargames.perfmeter` / `SGG PerfMeter`; current private release candidate version is `2026.5.18-1`.
- Runtime API, collection modes, device/environment snapshot with monitor names, camera snapshot for reproducible captures, session recorder with bounded samples, warm-up seconds, reset/scene-scope summaries, worst-frame metadata, JSON/CSV export, rule/alert engine with MCP alert commands, JSON settings for zero-code setup, Package Manager samples, the `Presets` tab with overlay presets/modules, metrics collection, UI Toolkit overlay with modes, module filtering, allocation-conscious text field refresh, stacked CPU/GPU graphs, colored legend labels, and min/max text history, URP Render Graph marker feature, Editor setup/runtime tabs, opt-in numerical overdraw measurement, and visual overdraw heatmap are present.
- EditMode API/classifier tests and PlayMode runtime smoke tests are present; classifier mixed-load edge cases, overdraw stale-readback safety, and heatmap toggles are covered. Android S23 Vulkan/GLES smoke validation has passed; broader player-build validation is still pending.
- The package is prepared as a private/internal release candidate for Unity 6000.4 / URP 17 validation; public release remains deferred.

## Public Runtime API

- `PerformanceMeter.EnsureRunning()` starts the singleton runtime when possible.
- `PerformanceMeter.Stop()` stops metric collection and overlay runtime objects.
- `PerformanceMeter.GetStatus()` / `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)` return agent-readable status.
- `PerformanceMeter.GetLatestMetrics()` / `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)` return immutable metric snapshots with FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing, and overdraw values.
- `PerformanceMeter.GetDeviceInfo()` / `PerformanceMeter.TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)` return an immutable device/environment snapshot with Unity/platform/CPU/GPU/screen/display/monitor info without starting the runtime.
- `PerformanceMeter.GetCameraSnapshot(...)` / `PerformanceMeter.TryGetCameraSnapshot(...)` return an immutable camera snapshot with transform/projection/clip/pixel rect/target display/URP camera settings without starting the runtime.
- `PerformanceMeter.GetSettings()` returns the zero-code JSON settings snapshot or safe defaults when the JSON file is missing.
- `PerformanceMeter.SetCollectionMode(...)` and `PerformanceMeter.CollectionMode` control `Stopped`, `Background`, `Overlay`, and `OverdrawDiagnostic`; `PerfMeterStatusSnapshot.CollectionMode` mirrors the active mode for MCP/agents.
- `PerformanceMeter.StartSession(...)`, `PerformanceMeter.StopSession()`, `PerformanceMeter.ResetStats()`, `PerformanceMeter.GetSessionSummary()`, `PerformanceMeter.GetSessionSamples()`, `PerformanceMeter.ExportSessionJson(path)`, `PerformanceMeter.ExportSessionCsv(path)`, and `PerformanceMeter.IsSessionRecording` control session recording with `WarmupFrames`, `WarmupSeconds`, `SampleIntervalSeconds`, `MaxSamples`, reset-on-scene-load/scene-ignore options, dropped-sample count, whole-run/current-scene summaries, worst-frame metadata, metadata from settings/device/camera snapshots, and JSON/CSV export.
- `PerformanceMeter.GetLatestAlerts()`, `PerformanceMeter.ClearAlerts()`, and `PerformanceMeter.AlertFired` expose rule alerts; the status snapshot includes active/fired counts and latest alert summary.
- `CollectionFrame` identifies the Unity frame where the snapshot was collected; `FrameTimingManager` values can be delayed relative to that frame.
- `PerfMeterBottleneck.PresentLimited` separates present/VSync/frame pacing waits from balanced frames and CPU/GPU-bound frames.
- `PerformanceMeter.SetOverlayVisible(bool visible)`, `PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset preset)`, `PerformanceMeter.SetOverlayModules(PerfMeterOverlayModule modules)`, `PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)`, `PerformanceMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerformanceMeter.IsOverlayVisible`, `PerformanceMeter.OverlayCorner`, `PerformanceMeter.OverlayMode`, `PerformanceMeter.OverlayPreset`, `PerformanceMeter.OverlayModules`, and `PerformanceMeter.TargetFps` control the runtime overlay, module filtering, and target line.
- `PerformanceMeter.RequestOverdrawMeasurement(int frameCount = 60)` / `PerformanceMeter.CancelOverdrawMeasurement()` control the bounded numerical overdraw measurement.
- `PerformanceMeter.SetOverdrawHeatmapVisible(bool visible)` and `PerformanceMeter.IsOverdrawHeatmapVisible` control the visual overdraw heatmap.
- Editor setup actions for agents: `PerfMeterSetupActions.GetStatusReport()`, `PerfMeterSetupActions.EnableFrameTimingStats()`, `PerfMeterSetupActions.InstallRendererFeatures()`, `PerfMeterSetupActions.CreateDefaultSettings()`, `PerfMeterSetupActions.SaveSettings(...)`, `PerfMeterSetupActions.ApplySettingsToRuntime()`, and `PerfMeterSetupActions.RunRecommendedSetup()`.

## Setup

- Open `SGG/Perfmeter/Setup` to inspect the project, list active/discovered URP renderer assets, install `PerfMeterRenderGraphFeature` into all or selected editable renderers, configure collection mode and the `Presets` tab, and copy bootstrap code if needed.
- The `Presets` tab creates and edits `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`; this is project-owned JSON, not a `ScriptableObject`. With `enabled=true` and `autoStart=true`, runtime auto-start applies collection mode and overlay settings without handwritten bootstrap code. The active preset and module toggles are saved into JSON and applied to the runtime overlay.
- For headless/agent setup, call `SGG.PerfMeter.Editor.Setup.PerfMeterSetupActions.RunRecommendedSetup()` from an Editor context.
- Package samples cover bootstrap/zero-code settings, runtime workflows, editor setup automation, MCP command examples, session export, alerts, overdraw/heatmap, and camera snapshot replay.
- Add `PerfMeterRenderGraphFeature` to the active URP renderer asset when Render Graph markers, numerical overdraw measurement, or visual overdraw heatmap are needed; the setup window does this automatically for discovered URP renderer assets.
- Enable Player Settings -> Rendering -> Frame Timing Stats before relying on `FrameTimingManager` in builds.
- Prefer Vulkan on Android when GPU frame timing matters; OpenGL ES timing may be unavailable or unreliable.
- System monitor names are read from `Screen.GetDisplayLayout(List<DisplayInfo>)`; platforms without display layout support fall back to `Screen.currentResolution`.
- Call `PerformanceMeter.EnsureRunning()` from gameplay/bootstrap code, then query snapshots from agents, diagnostics, or tests.

## Known Limitations

- Local `-runTests` works when launched without `-quit`; Unity writes XML results and exits after the run.
- Overdraw numeric measurement uses replacement shader rendering, atomic fragment counting, and `AsyncGPUReadback`; visual heatmap uses a separate additive replacement shader and an extra scene redraw while visible.
- Overdraw measurement defaults to Game cameras only and can be restricted by camera-name filter in `PerfMeterRenderGraphFeature` settings.
- Overdraw measurement gates unsupported targets with `OverdrawState.Unsupported`; fragment UAV/storage buffer behavior still needs device validation on limited backends.
- Render Graph pass/aliasing/merge analytics are not implemented.
- MCP session commands `perfmeter.session.start/stop/summary/export`, runtime reset command `perfmeter.runtime.reset_stats`, and mode command `perfmeter.runtime.mode.set` are implemented; export paths are restricted to the project directory and existing files are not overwritten.
- MCP alert commands `perfmeter.alerts.latest/clear` are implemented; output includes alerts, counters, status fields, and Editor state.
- Editor warning alerts have a separate JSON-tunable cooldown and do not write warnings every frame.
- Session start accepts warm-up seconds plus scene-load reset/ignore overrides; summary/export output includes whole-run/current-scene and worst-frame data.
- The empty overlay marker pass is opt-in diagnostic mode; self-overhead subtraction is still pending.
- Overlay text refresh now uses stable field labels, cached enum strings, reusable numeric formatting buffers, and dirty value-label assignment; changed numeric values and graph legend labels can still materialize managed strings at the throttled refresh interval.
- Broader manual device validation is still useful beyond the current Android S23 Vulkan/GLES smoke coverage.

## Release Readiness

- Root release plan: `docs/release-2026.5.18-1.md`.
- Release notes draft: `docs/release-notes-2026.5.18-1.md`.
- Release process: `docs/release-process.md`.
- Local/manual gates: Unity compile, EditMode tests, PlayMode tests, `git diff --check`, and optional Android Vulkan/GLES smoke builds.
- The GitHub release workflow is manual-only (`workflow_dispatch`) and does not publish packages.
