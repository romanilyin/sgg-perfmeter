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

PerfMeter erstellt und besitzt einen versionierten UI Toolkit host fuer den Overlay: Unity `6000.4` verwendet `UIDocument`, Unity `6000.5+` verwendet `PanelRenderer`. Der eigene host ist von fremder UI getrennt und bewahrt deren panel settings und children; bei einem rebuild wird nur der PerfMeter-eigene container entfernt.

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

Strukturierte Alert-Logs und Editor-Warnungen sind unabhaengig: `PerformanceMeter.SetStructuredLogsEnabled(false)` unterdrueckt nur strukturierte Alert-`Debug.Log`-Ausgabe, waehrend `PerformanceMeter.SetEditorWarningLogsEnabled(false)` Editor-Warnungslogs separat steuert. Callbacks, Alerts/History, Overlay-Warnungen und Sessions bleiben aktiv.

## External GPU Capture

Nutze den Capture-Coordinator fuer eine begrenzte RenderDoc- oder PIX-Anfrage, wenn das Tool bereits angehaengt ist:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

Der Coordinator erlaubt nur eine aktive Anfrage und durchlaeuft deterministisch `PreRoll`, `Capturing`, `PostRoll` und `Completed`. Dieselbe aktive ID ist idempotent; eine andere ID wird als Ueberlappung abgewiesen. Pre-roll und post-roll zaehlen Unity-Frames; nur `Capturing` oeffnet den Alert-Capture-Scope und ruft Unitys experimentellen `ExternalGPUProfiler` auf. Die verpflichtenden Gates sind Editor oder Development Build sowie ein angehaengtes Tool. `RenderDoc` ist auf Windows/Linux desktop mit Direct3D 11, Direct3D 12 oder Vulkan erlaubt; `PIX` ist auf Windows desktop mit Direct3D 12 erlaubt.

`Completed` bedeutet nur, dass der geschuetzte Unity wrapper lifecycle beendet wurde. Unity stellt weder die Identitaet des angehaengten Tools noch einen autoritativen Artefaktpfad bereit; `Status.Tool` ist nur das angeforderte Tool und keine verifizierte Identitaet des angehaengten Tools. Pruefe das `.rdc`/`.wpix`-Artefakt im externen Tool. Die automatisierten Tests verwenden ein fake backend; die Bestaetigung durch ein echtes Tool bleibt ein release gate. MCP-Orchestrierung, Capture bundles und korrelierte Artefakte bleiben separate future work.

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

## Unity-Profiler-Instrumentierung

Die Instrumentierung ist intern und nur beim Profiling des Editors, eines Development Builds oder eines anderen profiler-enabled Builds sichtbar. In nicht profiler-enabled Release-Playern sind diese Marker/Counter no-op und erzeugen keine Instrumentierungsdaten; public API-, Status-, MCP- und Export-Schemas bleiben unveraendert.

- Marker decken Collection/Frame-Timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), Provider (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), Bottleneck/Capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) und JSON/CSV-Export (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`) ab. `SGG.PerfMeter.Thermal.Sample` ist ein reservierter interner Provider-Hook.
- Counter decken CPU/GPU-Framezeiten (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) als End-of-frame-Gauges in Nanosekunden ab. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` und `SGG.PerfMeter.Thermal.Available` codieren Verfuegbarkeit/aktiv als `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` und `SGG.PerfMeter.Capture.State` verwenden Enum-Codes; `SGG.PerfMeter.Provider.CustomMetricCount` ist ein Count. Alle Counter nutzen die Kategorie `Scripts` und `FlushOnEndOfFrame`.
- Es wird kein synthetischer Thermal-Sample erzeugt; `SGG.PerfMeter.Thermal.Available` bleibt bei `0`/nicht verfuegbar, bis ein echter Plattform-Provider Daten liefert.

## Self-Observability Und Overhead-Budgets

Nutze `PerformanceMeter.GetSelfOverhead()` oder `PerformanceMeter.GetStatus().SelfOverhead`, um CPU-Callback-Kosten und Allokationen fuer Collector, Custom Provider, CPU-Core-Provider, Overlay sowie URP/HDRP-Integration zu diagnostizieren. Die Messung verwendet feste 120-Frame-Fenster, Durchschnitt pro Aufruf und komponentenspezifische CPU-/Allokationsbudgets.

Die inaktive Render-Integration meldet `Unsupported`, eine unterstuetzte Komponente ohne Aufruf `NotMeasured` und GPU-Self-Timing `Unavailable`. Das Accounting ist rein diagnostisch: PerfMeter zieht keinen Overhead von bestehenden CPU/GPU-Metriken ab und passt diese nicht an.

## Agent-Automation

Typischer MCP-Run:

```text
perfmeter.profiler.capabilities {}
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

`perfmeter.profiler.capabilities {}` ist ein Lesen des Caches; es startet weder die Runtime noch eine Discovery.
