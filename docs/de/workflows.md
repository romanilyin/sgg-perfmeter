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

`Completed` bedeutet nur, dass der geschuetzte Unity wrapper lifecycle beendet wurde. Unity stellt weder die Identitaet des angehaengten Tools noch einen autoritativen Artefaktpfad bereit; `Status.Tool` ist nur das angeforderte Tool und keine verifizierte Identitaet des angehaengten Tools. Der Overload mit `PerfMeterCaptureBundleOptions` trennt Baseline-/Capture-Samples und exportiert ein projektlokales Bundle atomar; ein externes Artefakt bleibt nur beobachtet, nicht autoritativ. Fuer Automation dienen `perfmeter.capture.request/status/cancel/export/capabilities`.

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

## Workflow fuer optionale Speicher-Snapshots

1. Verwende Unity `6000.4+` und installiere `com.unity.memoryprofiler` `1.1.0+` ueber den Package Manager. Die optionale Assembly `SGG.PerfMeter.MemoryProfiler` registriert danach automatisch das Backend; ohne dieses Paket bleibt die Core-Integration unavailable.
2. Lies im Play Mode `PerformanceMeter.GetMemorySnapshotCapabilities()` oder `perfmeter.memory.snapshot.capabilities` und pruefe Backend sowie benoetigte Capture-Flags.
3. Fordere einen manuellen Snapshot mit `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))` an oder konfiguriere mit `ConfigureMemorySnapshotTriggers(...)` eine ausdruecklich aktivierte System-Speicherschwelle bzw. ein begrenztes Leak-Wachstumsfenster.
4. Lies `GetMemorySnapshotStatus()` oder `perfmeter.memory.snapshot.status`, bis Snapshot und korreliertes Bundle einen terminal state erreichen. Exportiere fertige Evidence mit `PerformanceMeter.ExportCaptureBundle(captureId)` oder `perfmeter.capture.export`.

Memory-only-Evidence wird ueber die bestehende Capture-Bundle-API unter `Temp/PerfMeter/CaptureBundles` geschrieben. Das Bundle fuehrt `MemoryProfiler` als angefordertes Tool, enthaelt Speicher-Provenance und einen Streaming-SHA-256 fuer die `.snap`-Datei und enthaelt kein externes GPU-Artefakt. Die Quelle liegt unter `Temp/PerfMeter/MemorySnapshots`; ein erfolgreicher Export verwendet sie nur einmal.

## Grafikmarker-Diagnose

1. Rufe `PerformanceMeter.GetGraphicsDiagnostics()` oder `perfmeter.graphics.diagnostics` auf, um die neuesten Markerwerte und den Graphics-API-Kontext zu lesen.
2. Pruefe fuer jede Capability `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, aufgeloeste/gesampelte Component-Counts und Katalogrevision. Die Discovery ist dynamisch: Sie erfolgt beim Runtime-Start und bei einem ausdruecklichen Profiler-Katalog-Refresh/Reconfigure.
3. Behandle die Werte als rohe Recorder-Werte in den entdeckten Units. Ein Marker kann unavailable, ohne Sample verfuegbar oder sampled sein; Null ist kein allgemeines unavailable-Signal und der Wert ist nicht zwingend ein Shader- oder PSO-Count.

Der Shader-Marker loest zuerst exakt `Shader.CreateGPUProgram` und danach die Aliase `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` und `Shader.DynamicLoadGPUProgram` auf. Der Pipeline-Marker verwendet exakt `CreatePSO.Job`. Dieselben Werte und Provenance sind ueber `perfmeter.metrics.latest` und Session-JSON/CSV verfuegbar.

## Graphics-State-Trace und Prewarm

1. Stelle unter Unity `6000.4+` sicher, dass die optionale Assembly `SGG.PerfMeter.GraphicsStateCollection` verfuegbar ist. Sie nutzt unter Unity `6000.4` den Namespace `UnityEngine.Experimental.Rendering.GraphicsStateCollection` und unter Unity `6000.5+` `UnityEngine.Rendering.GraphicsStateCollection`.
2. Starte vor dem Trace eine PerfMeter-Session. Verwende `StartSession(...)` und rufe dann `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` oder den passenden MCP-Befehl auf. Ohne aktive Session wird die Anfrage abgelehnt; die Session muss bis zum Trace-Ende aufzeichnen, und `PerformanceMeter.StopSession()` bricht einen aktiven Trace ab.
3. Lass das Szenario laufen, waehrend der begrenzte Trace fortschreitet. Im normalen Play Mode wird jeder Trace-Frame nach `WaitForEndOfFrame` getickt; im Batch Mode verwendet der Coordinator einen Next-Frame-Fallback. Waehren dieses Zeitraums angenommene Session-Samples tragen `GraphicsStateTraceId`/`graphics_state_trace_id`; Session-Einstellungen bestimmen die Anzahl der gespeicherten korrelierten Samples.
4. Frage `GetGraphicsStateCollectionStatus()` oder `perfmeter.graphics.state_collection.status` bis `Completed` ab und stoppe danach bei Bedarf die Session. Ein Stop waehrend des aktiven Traces bricht ihn ab und kann `IsBusy`/`is_busy` true lassen, waehrend das eigene Cleanup erneut versucht wird. Das eigene `.graphicsstate`-Artefakt liegt project-relativ unter `Temp/PerfMeter/GraphicsStateCollections` und ist auf 64 MiB begrenzt.
5. Uebergib den gemeldeten eigenen relativen Pfad an `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` oder den MCP-Prewarm-Befehl. Prewarm ist synchron, bewahrt das Artefakt und meldet abgeschlossene Warmups und `IsWarmedUp`; ein progressives Warmup kann mit einer ausdruecklichen Incomplete-Warnung enden.

Der Graphics-State-Coordinator erlaubt einen Flight und lehnt ausserdem Overlap mit aktivem externem GPU-Capture, Memory-Snapshot oder Alert-Capture ab. Die gleiche aktive Trace-ID liefert `AlreadyActive`, eine andere ID `RejectedOverlap`. `CancelGraphicsStateTrace` bricht nur einen passenden aktiven/vorbereitenden Trace ab und bereinigt sein ausstehendes Artefakt. Wenn ein eigenes Artefakt nicht geloescht werden kann, bleibt `HasPendingCleanup`/`has_pending_cleanup` true, ein benachbarter `.delete-pending`-Sidecar bleibt bestehen und wird nach Domain Reload wiederhergestellt und erneut versucht; `IsBusy`/`is_busy` und die Warnung bleiben bis zum Erfolg sichtbar. Das Unity-Backend unterstuetzt kein Cache-Miss-Tracing, daher ist keine Cache-Miss-Evidence verfuegbar.

## Render-Integrationskontext

Verwende den neutralen Snapshot, wenn ein pipeline-unabhaengiger Blick auf die letzte typisierte Render-Integration benoetigt wird:

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

Dieselben Daten koennen per MCP gelesen werden:

```text
perfmeter.render.snapshot {}
```

Diese Lesevorgaenge starten keine Runtime-Sammlung. Pruefe `State`, `ObservationAgeFrames`, `LastObservedFrame` und `ObservationMatchesCurrentPipeline` gemeinsam. Nach einem Pipeline- oder Asset-Wechsel ist die vorherige Observation veraltet; Warning und Non-Match muessen erhalten bleiben, und ihre Pass-, Modus-, GRD- oder VRS-Werte duerfen nicht als aktuell gelten. Die Legacy-API `PerformanceMeter.GetRenderGraphSnapshot()` und `perfmeter.rendergraph.snapshot` bleiben verfuegbar.

Im Capture-Bundle-Schema `sgg.perfmeter.capture-context` Version `1` bleibt `render` erhalten und `render_integration` kommt hinzu. Bei einem externen GPU-Capture wird dieser Kontext beim ersten Sample der Phase `Capturing` eingefroren; ein Memory-Profiler-Bundle zeichnet ihn beim Abschluss der Speicheranfrage auf. Session-JSON/CSV-Schemas bleiben unveraendert. Die oeffentliche API bietet keinen stabilen RenderGraph-/CustomPass-Viewer und keine Pass-Ziele; dieser Workflow verspricht daher keine Editor-Navigation.
