# Workflows

## Runtime-Overlay

Nutze den Overlay, wenn du sofortige Sichtbarkeit im Spiel brauchst.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Der Overlay nutzt UI Toolkit und faengt Gameplay-Eingaben nicht ab. Er unterstuetzt FPS-only, compact text, graphs, full diagnostics, metric bars, visual themes, module filters, CPU/GPU graphs, CPU core widgets und begrenzte custom metric rows.

## Background Collection

Background mode eignet sich fuer Tests, Device-Runs oder Agent-Workflows ohne sichtbare UI.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording Und Export

Sessions dienen wiederholbaren Profiling-Fenstern.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));
PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session-Exports enthalten timings, FPS lows, spikes, bottleneck counts, render counters, memory counters, overdraw state, warning/counter availability, scene summaries, worst frames, device/camera/settings metadata und custom metrics.

## Alerts

Regeln koennen Budget-Verletzungen, niedrige FPS, fehlendes GPU timing und overdraw thresholds melden.

## Overdraw-Diagnostik

Numerical overdraw wird explizit aktiviert und laeuft in einem begrenzten Fenster.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Numerical overdraw und heatmap nutzen den URP Render Graph diagnostic path. Overdraw measurement erfordert `PerfMeterRenderGraphFeature`, replacement shader support, fragment UAV/storage-buffer support, compute shader support, eine unterstuetzte graphics API und async GPU readback. HDRP meldet overdraw/heatmap als unsupported, waehrend core overlay, session, API und MCP diagnostics verfuegbar bleiben.

## Kamera- Und Geraete-Reproduzierbarkeit

Snapshots bewahren die Umgebung, in der ein Performance-Capture entstanden ist.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

## Custom Metrics

Registriere projektspezifische Provider ohne PerfMeter zu forken.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

## Agent-Automation

Typischer MCP-Run:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
