# SGG PerfMeter

**Lightweight runtime performance diagnostics and agent-readable profiling for Unity 6 URP+HDRP (FPS meter).**

Package name: `com.sungeargames.perfmeter`

SGG PerfMeter detects frame bottlenecks, compares performance changes, captures reproducible sessions, and exposes structured profiling data to tools and AI agents. It combines FrameTimingManager timings, ProfilerRecorder counters, bottleneck classification, UI Toolkit overlay, URP overdraw diagnostics, session export, alerts, custom metrics, device/camera snapshots, URP Render Graph diagnostics, HDRP Custom Pass diagnostics, and MCP command metadata.

The main user documentation lives in the repository-level GitHub docs:

- English: https://github.com/romanilyin/sgg-perfmeter/blob/main/README.md
- Russian: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/ru/README.md
- German: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/de/README.md
- Spanish: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/es/README.md
- French: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/fr/README.md
- Italian: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/it/README.md
- Japanese: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/ja/README.md
- Korean: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/ko/README.md
- Brazilian Portuguese: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/pt-br/README.md
- Simplified Chinese: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/zh-cn/README.md
- Quick Start: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/en/quick-start.md
- API: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/en/api.md
- Comparison: https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/en/comparison.md

## Requirements

- Unity `6000.4+` for supported runtime usage.
- URP `17.4+` with Render Graph path or HDRP `17.4+` with the package HDRP Custom Pass integration.
- Frame Timing Stats enabled for reliable frame timing in builds.
- Vulkan preferred on Android when GPU frame timing matters.

Unity `2022.3` through `6000.3` may be import-safe for compile checks, but runtime overlay, render integration, overdraw passes, and support expectations target Unity `6000.4+` with URP `17.4+` or HDRP `17.4+`. HDRP overdraw and heatmap are unsupported; core runtime diagnostics remain available.

## Install

Install from the public npm registry with a Unity Package Manager scoped registry:

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.7.19-1"
  }
}
```

You can also install this folder as a Git UPM package with the path:

```text
Assets/Scripts/SGG.PerfMeter
```

Example `Packages/manifest.json` entry:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.7.19-1"
  }
}
```

The current npm registry package version is `2026.7.19-1`.

## Quick Start

1. Open `SGG/Perfmeter/Setup`.
2. Enable Frame Timing Stats.
3. Install `PerfMeterRenderGraphFeature` into editable active URP renderer assets, or use HDRP where the package Custom Pass is registered at runtime.
4. Save JSON settings from the `Presets` tab for zero-code setup, or copy the generated initialization snippet.
5. Enter Play Mode and verify the overlay.

Minimal runtime API:

```csharp
using SGG.PerfMeter;

PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
PerformanceMeter.SetOverlayVisible(true);

PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
```

## Samples

Import package samples from Package Manager or copy them from `Samples~` while developing from this repository.

- `Bootstrap and Zero-Code Settings`: minimal bootstrap and Resources JSON settings.
- `Runtime Workflows`: overlay switching, session export, alerts, overdraw/heatmap, and camera replay.
- `Editor and MCP Automation`: setup actions and MCP command examples.

## License

This package is licensed under `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.

The authoritative Russian license text is `LICENSE.ru.md`; English convenience text is `LICENSE.md`. Keep `NOTICE.md` and `NOTICE.ru.md` with package distributions.

Brand usage policy translations live on GitHub under `https://github.com/romanilyin/sgg-perfmeter/blob/main/docs/<lang>/brand.md`.
