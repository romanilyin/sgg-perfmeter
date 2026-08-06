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

## Self-Observability Und Overhead-Budgets

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability meldet CPU-Callback-Kosten mit geringem Overhead in festen 120-Frame-Fenstern. Durchschnittswerte gelten pro Aufruf. Der Gesamtstatus ist `NotInitialized`, `Collecting` oder `Ready`; der Komponentenstatus ist `NotMeasured`, `Collecting`, `Ready` oder `Unsupported`.

Komponenten sind `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` und `HdrpRenderIntegration`. Jede Komponente liefert Fenster-/Aufrufzahlen, durchschnittliche/maximale CPU-Millisekunden, gesamte/durchschnittliche Allokationen, Budgets und die Zustaende `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Komponente | CPU-Budget | Allokationsbudget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU-Self-Timing ist ausdruecklich `Unavailable`. Diese Diagnose zieht nichts von bestehenden CPU/GPU-Metriken ab und passt sie nicht an.

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

## External GPU Capture Coordinator

```csharp
PerfMeterCaptureOptions options = new PerfMeterCaptureOptions(
    "renderdoc-spike-01",
    PerfMeterCaptureTool.RenderDoc,
    captureFrames: 1,
    preRollFrames: 30,
    postRollFrames: 30);

PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(options);
PerfMeterCaptureStatusSnapshot capture = PerformanceMeter.GetCaptureStatus();
if (capture.IsActive && userRequestedCancellation)
{
    PerformanceMeter.CancelCapture(capture.CaptureId);
}
```

Der Coordinator erlaubt genau eine aktive Anfrage und durchlaeuft deterministisch `PreRoll`, `Capturing`, `PostRoll` und `Completed`. Das Wiederholen derselben aktiven ID ist idempotent; eine andere aktive ID wird wegen Ueberlappung abgewiesen. `Canceled`, `Unavailable` und `Error` sind explizite terminal states.

Das eingebaute Backend kapselt Unitys experimentellen `ExternalGPUProfiler` nur im Editor oder in einem Development Build, nur wenn ein externes Tool angehaengt ist und nur fuer unterstuetzte Desktop-Plattform-/API-Kombinationen. Unterstuetzte Kombinationen sind `RenderDoc` auf Windows/Linux desktop mit Direct3D 11, Direct3D 12 oder Vulkan sowie `PIX` auf Windows desktop mit Direct3D 12. Waehle `RenderDoc` oder `Pix` explizit, weil Unity die Identitaet des angehaengten Tools nicht offenlegt. `Status.Tool` ist nur das angeforderte Tool und keine verifizierte Identitaet des angehaengten Tools. `Completed` bestaetigt nur den Unity wrapper lifecycle; es verifiziert kein externes `.rdc`/`.wpix`-Artefakt und liefert keinen Artefaktpfad. Automatisierte Tests verwenden ein fake backend; die Bestaetigung durch echtes externes Tool und Artefakt bleibt ein release gate. Capture bundles, artifact provenance und MCP capture control bleiben separate future scope.

`PerfMeterCaptureOptions` verwendet standardmaessig `captureFrames: 1` sowie `preRollFrames: 0` und `postRollFrames: 0`. Ein gueltiges `RequestCapture` startet die Runtime automatisch. `CancelCapture()` ohne ID beendet die aktuell gemeldete aktive Anfrage; die Uebergabe einer ID schuetzt davor, eine neuere Anfrage zu beenden.

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
