# Troubleshooting

Use this checklist when PerfMeter does not show expected data.

## Overlay Does Not Appear

- Open `SGG/Perfmeter/Setup` and confirm overlay visibility is enabled.
- Confirm collection mode is `Overlay`, not `Background` or `Stopped`.
- If using zero-code setup, confirm the settings file exists at `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- If using manual bootstrap, confirm `PerformanceMeter.EnsureRunning()` is called after scene load.
- Enter Play Mode; Edit Mode API calls are safe but do not create a runtime overlay.

## Frame Timing Or GPU Timing Is Missing

- Enable Player Settings -> Rendering -> Frame Timing Stats.
- Prefer Vulkan on Android when GPU frame timing matters.
- Treat OpenGL/OpenGLES as degraded mode for GPU timing.
- Check `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters`, and `Warning` before assuming a counter exists.

## Overdraw Measurement Does Not Progress

- In URP, install `PerfMeterRenderGraphFeature` into the active URP renderer.
- In HDRP, overdraw and heatmap are unsupported by design; use core diagnostics instead.
- Confirm the active camera uses the renderer that contains the feature.
- Confirm the target backend supports fragment UAV/storage buffers, compute shaders, and async GPU readback.
- Use `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` for a bounded measurement window.
- If the target is unsupported, PerfMeter reports `OverdrawState.Unsupported` instead of scheduling the pass.

## Session Export Fails

- Export to a project-local path.
- Do not overwrite an existing export unless your workflow explicitly removes it first.
- Keep `MaxSamples` bounded for long runs.
- Use warm-up frames/seconds to avoid startup spikes in summaries.

## Alerts Are Too Noisy

- Adjust thresholds and consecutive-frame windows in JSON settings.
- Increase Editor warning cooldowns.
- Disable Editor warning logs when callbacks or structured logs are enough.

## Data Looks Different Between Devices

This is expected. GPU timings, profiler counters, display information, async readback, and overdraw support vary by graphics API, platform, Unity version, and device. Use device snapshots and warnings in exported sessions to explain differences.
