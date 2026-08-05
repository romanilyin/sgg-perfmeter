# Runtime API

Namespace:

```csharp
using SGG.PerfMeter;
```

Alle Lese-APIs sind sicher, bevor die Runtime startet. Reads geben stopped/default snapshots zurueck statt Exceptions zu werfen.

## Lifecycle

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Collection modes:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Status Und Metrics

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
```

Wichtige Metrikgruppen:

- FPS: average, 1% low, 0.1% low, spike counts.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame wenn verfuegbar.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: system/app memory, GC reserved memory, GPU memory wenn verfuegbar.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced oder unknown.
- Overdraw: state, progress, ratio und heatmap visibility.

Counter-Verfuegbarkeit wird ueber `AvailableCounters`, `UnavailableCounters` und warnings gemeldet.

## Dynamischer Profiler-Metrikkatalog

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` und `GetProfilerMetricCapabilities()` lesen den gecachten Katalog. Der Katalogstatus ist `NotInitialized`, `Ready` oder `Error`; jede Capability meldet `Unavailable`, `AvailableNoSample` oder `AvailableSampled`, und `Resolution` zeigt die Provenienz `None`, `Exact` oder `Alias`. Discovery laeuft nur beim Runtime-Start und bei explizitem Refresh/Reconfigure, nicht in der Steady-State-Collection. Bestehende numerische Metriken bleiben Compatibility-Werte; `SampleState`/`IsAvailable` der Capability ist das massgebliche Verfuegbarkeitssignal.

## Strukturierte Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshots enthalten Unity/platform/OS/CPU/GPU/API/display/window/support information. Camera snapshots enthalten scene, transform, projection, clipping, pixel rect, target display und URP/HDRP camera settings, wenn verfuegbar.

## CPU Core Loads

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Jeder Snapshot enthaelt `CoreIndex`, `LoadPercent` und `Available`. Das Array kann vor Runtime-Start, waehrend sampler warm-up oder auf nicht unterstuetzten Plattformen leer sein.

## Overlay

```csharp
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.ClassicDark);
PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.Manrope);
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

## Sessions

```csharp
PerformanceMeter.StartSession();
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));
PerformanceMeter.StopSession();
PerformanceMeter.ResetStats();

PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerfMeterSessionSampleSnapshot[] samples = PerformanceMeter.GetSessionSamples();

PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

## Alerts

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` ist standardmaessig `true` und steuert nur die strukturierte Alert-`Debug.Log`-Ausgabe. `false` deaktiviert weder `AlertFired`-Callbacks, aktuelle Alerts oder Alert-History, Overlay-Warnungen, Editor-Warnungslogs noch Sessions. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` steuert Editor-Warnungslogs unabhaengig.

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Provider-Exceptions werden als nicht verfuegbare custom metric snapshots gemeldet und unterbrechen die Kernsammlung nicht.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw-Diagnostik nutzt explizite Diagnosemodi und kann GPU-Arbeit hinzufuegen. In HDRP melden diese APIs sicher unsupported state fuer overdraw und heatmap, statt HDRP heatmap output zu versprechen.
