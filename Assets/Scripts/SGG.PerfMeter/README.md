# SGG PerfMeter

SGG PerfMeter is a low-overhead runtime performance meter and agent-readable profiling API for Unity 6000.4+ URP projects.

Package version: `2026.5.18-1`.

This package is in private release-candidate validation. The repository remains private until the public switch is explicitly approved.

## Documentation

- English: `.Documentation/README.en.md`
- Russian: `.Documentation/README.ru.md`
- English status: `.Documentation/STATUS.en.md`
- Russian status: `.Documentation/STATUS.ru.md`

Root repository release docs, outside this package path:

- `docs/release-2026.5.18-1.md`
- `docs/release-notes-2026.5.18-1.md`
- `docs/release-process.md`

## Quick Start

1. Install this folder as a Git UPM package using `?path=/Assets/Scripts/SGG.PerfMeter`.
2. Open `SGG/Perfmeter/Setup`.
3. Enable `Frame Timing Stats`.
4. Install `PerfMeterRenderGraphFeature` into the active URP renderer.
5. Save JSON settings from the `Presets` tab for zero-code setup, or add the generated initialization snippet to project-owned runtime code.

Minimal runtime API:

```csharp
using SGG.PerfMeter;

PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
PerformanceMeter.SetOverlayVisible(true);

PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
```

## Samples

Import package samples from Package Manager or copy them from `Samples~` when developing from this repository.

- `Bootstrap and Zero-Code Settings`: minimal bootstrap component plus a `Resources/SGG.PerfMeter/perfmeter-settings.json` example with overlay/rule/session/overdraw tunables.
- `Runtime Workflows`: overlay preset switching, bounded session export, alert callbacks, overdraw/heatmap controls, and camera snapshot replay.
- `Editor and MCP Automation`: setup menu actions and MCP command examples for agent-driven profiling runs.

## License

This package is licensed under `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.

The authoritative Russian license text is `LICENSE.ru.md`; English convenience text is `LICENSE.md`. Keep `NOTICE.md` and `NOTICE.ru.md` with package distributions.
