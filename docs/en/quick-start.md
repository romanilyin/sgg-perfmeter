# Quick Start

This path gets a visible overlay running without writing code.

## 1. Open The Setup Window

In Unity, open:

```text
SGG/Perfmeter/Setup
```

## 2. Run Recommended Setup

Use the setup window to:

- enable Frame Timing Stats;
- install `PerfMeterRenderGraphFeature` into editable active URP renderers;
- create default project-owned JSON settings;
- configure overlay visibility, corner, target FPS, visual preset, and collection mode.

The zero-code settings file is saved to:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

When `enabled` and `autoStart` are true, PerfMeter starts from this JSON at runtime.

## 3. Enter Play Mode

The default overlay should appear in the selected corner. If it does not:

- confirm the JSON settings file exists on the Resources path;
- confirm the overlay is visible in the setup window;
- confirm runtime collection mode is `Overlay`;
- confirm the active URP renderer has `PerfMeterRenderGraphFeature` when testing Render Graph diagnostics or overdraw.

## Optional Manual Bootstrap

Use code when you want explicit startup control:

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
    {
        PerformanceMeter.EnsureRunning();
        PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
        PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
        PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
        PerformanceMeter.SetOverlayVisible(true);
    }
}
```

## First Useful Reads

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
