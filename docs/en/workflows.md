# Workflows

## Runtime Overlay

Use the overlay when you need immediate in-game visibility.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

The overlay uses UI Toolkit and does not intercept gameplay input. It supports FPS-only, compact text, graph, full diagnostics, metric bars, visual themes, module filters, CPU/GPU graphs, CPU core widgets, and limited custom metric rows.

## Background Collection

Use background mode for tests, device runs, or agent workflows where visible UI is not needed.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording And Export

Use sessions for repeatable profiling windows.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session exports include timing, FPS lows, spikes, bottleneck counts, render counters, memory counters, overdraw state, warning/counter availability, scene summaries, worst frames, device metadata, camera metadata, settings metadata, and custom metrics.

## Alerts

Rules can report budget violations, low FPS, unavailable GPU timing, and overdraw thresholds.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warnings are throttled by cooldowns and can be disabled through JSON settings or runtime controls.

## Overdraw Diagnostics

Numerical overdraw is opt-in and bounded.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw measurement requires `PerfMeterRenderGraphFeature`, replacement shader support, fragment UAV/storage-buffer support, compute shader support, a supported graphics API, and async GPU readback. Unsupported targets report `OverdrawState.Unsupported` instead of running the pass.

## Camera And Device Reproducibility

Use snapshots to preserve the environment that produced a performance capture.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Session exports include device and camera metadata so a capture can be understood or reproduced later.

## Custom Metrics

Register project-specific providers without forking PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics are exposed through API reads, session JSON export, MCP latest metrics, and up to eight overlay rows when the `CustomMetrics` module is enabled.

## Agent Automation

A typical MCP-driven run:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
