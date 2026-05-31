# Workflow

## Runtime Overlay

Используйте overlay, когда нужна быстрая видимость прямо в игре.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Overlay использует UI Toolkit и не перехватывает gameplay input. Он поддерживает FPS-only, compact text, graph, full diagnostics, metric bars, visual themes, module filters, CPU/GPU graphs, CPU core widgets и ограниченные custom metric rows.

## Background Collection

Background mode подходит для тестов, device runs и agent workflows без видимого UI.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording И Export

Sessions нужны для повторяемых profiling windows.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session exports включают timing, FPS lows, spikes, bottleneck counts, render counters, memory counters, overdraw state, warning/counter availability, scene summaries, worst frames, device metadata, camera metadata, settings metadata и custom metrics.

## Alerts

Rules могут сообщать budget violations, low FPS, unavailable GPU timing и overdraw thresholds.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warnings ограничены cooldown и могут быть отключены через JSON settings или runtime controls.

## Overdraw Diagnostics

Numerical overdraw является opt-in и bounded.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw measurement требует `PerfMeterRenderGraphFeature`, replacement shader support, fragment UAV/storage-buffer support, compute shader support, supported graphics API и async GPU readback. Unsupported targets возвращают `OverdrawState.Unsupported` вместо запуска pass.

## Camera И Device Reproducibility

Snapshots сохраняют окружение, в котором получен performance capture.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Session exports включают device и camera metadata, чтобы capture можно было понять или воспроизвести позже.

## Custom Metrics

Регистрируйте project-specific providers без форка PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics доступны через API reads, session JSON export, MCP latest metrics и до восьми overlay rows при включенном модуле `CustomMetrics`.

## Agent Automation

Типичный MCP-driven run:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
