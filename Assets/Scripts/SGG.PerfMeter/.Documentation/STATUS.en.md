# SGG PerfMeter Status

## Current Readiness

- Package identity is `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Runtime API, metrics collection, UI Toolkit overlay with modes, stacked CPU/GPU graphs, colored legend labels, and min/max text history, URP Render Graph marker feature, Editor setup/runtime tabs, and opt-in numerical overdraw measurement are present.
- The package is ready for internal Unity 6000.4 / URP 17 validation, not for public release.

## Public Runtime API

- `PerfMeter.EnsureRunning()` starts the singleton runtime when possible.
- `PerfMeter.Stop()` stops metric collection and overlay runtime objects.
- `PerfMeter.GetStatus()` / `PerfMeter.TryGetStatus(out PerfMeterStatusSnapshot status)` return agent-readable status.
- `PerfMeter.GetLatestMetrics()` / `PerfMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)` return immutable metric snapshots with FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing, and overdraw values.
- `CollectionFrame` identifies the Unity frame where the snapshot was collected; `FrameTimingManager` values can be delayed relative to that frame.
- `PerfMeterBottleneck.PresentLimited` separates present/VSync/frame pacing waits from balanced frames and CPU/GPU-bound frames.
- `PerfMeter.SetOverlayVisible(bool visible)`, `PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerfMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerfMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerfMeter.IsOverlayVisible`, `PerfMeter.OverlayCorner`, `PerfMeter.OverlayMode`, and `PerfMeter.TargetFps` control the runtime overlay and target line.
- `PerfMeter.RequestOverdrawMeasurement(int frameCount = 60)` / `PerfMeter.CancelOverdrawMeasurement()` control the bounded numerical overdraw measurement.
- Editor setup actions for agents: `PerfMeterSetupActions.GetStatusReport()`, `PerfMeterSetupActions.EnableFrameTimingStats()`, `PerfMeterSetupActions.InstallRendererFeatures()`, `PerfMeterSetupActions.RunRecommendedSetup()`.

## Setup

- Open `SGG/Perfmeter/Setup` to inspect the project, install `PerfMeterRenderGraphFeature` into all or selected URP renderer assets, and copy bootstrap code.
- For headless/agent setup, call `SGG.PerfMeter.Editor.Setup.PerfMeterSetupActions.RunRecommendedSetup()` from an Editor context.
- Add `PerfMeterRenderGraphFeature` to the active URP renderer asset when Render Graph markers or numerical overdraw measurement are needed; the setup window does this automatically for discovered URP renderer assets.
- Enable Player Settings -> Rendering -> Frame Timing Stats before relying on `FrameTimingManager` in builds.
- Prefer Vulkan on Android when GPU frame timing matters; OpenGL ES timing may be unavailable or unreliable.
- Call `PerfMeter.EnsureRunning()` from gameplay/bootstrap code, then query snapshots from agents, diagnostics, or tests.

## Known Limitations

- `-runTests` batchmode execution is a known local verification issue; compile batchmode is the current reliable check.
- Overdraw numeric measurement uses replacement shader rendering, atomic fragment counting, and `AsyncGPUReadback`; visual heatmap output is not implemented yet.
- Overdraw measurement gates unsupported targets with `OverdrawState.Unsupported`; fragment UAV/storage buffer behavior still needs device validation on limited backends.
- Render Graph pass/aliasing/merge analytics are not implemented.
- Full zero-allocation overlay refresh is not implemented yet; the current overlay throttles text rebuilds and managed string assignment to the refresh interval.
- Manual device validation is still pending, especially Android Vulkan/GLES GPU timing behavior.
