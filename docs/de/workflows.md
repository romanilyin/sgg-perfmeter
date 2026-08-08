# Workflows

## FTUE-Setup und Fortsetzungen

Öffnen Sie `SGG/Perfmeter/Setup` und wählen Sie den Tab **FTUE**. Die erforderlichen Prüfungen decken Kompatibilität, Render-Integration, Frame Timing Stats, den Package-Pfad und eine geladene Settings-JSON ab. Optionale Zeilen können installiert oder übersprungen werden; eine installierte Zeile zeigt die nächste Aktion an, statt stillschweigend zu behaupten, der Workflow sei abgeschlossen.

### Memory Profiler

Nach der Installation von `com.unity.memoryprofiler` bietet die Zeile **Memory Profiler** **Open Window/Analysis/Memory Profiler**, **Copy RequestMemorySnapshot Snippet**, **Copy Memory Trigger Snippet**, **Open Runtime** und **Reveal Snapshots**, sobald der verwaltete Ordner existiert. Die kopierten Snippets sind Laufzeitcode, den das Projekt aufrufen muss; FTUE fordert selbst keinen Snapshot an und konfiguriert keine Trigger. One-shot-`.snap`-Dateien werden unter `Temp/PerfMeter/MemorySnapshots` bereitgestellt. Öffnen oder kopieren Sie das Ergebnis, bevor eine spätere Anfrage oder Laufzeitbereinigung den verwalteten Quellordner entfernt.

Das One-shot-Snippet lautet:

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

Das optionale Trigger-Snippet lautet:

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

Verwenden Sie **Open Runtime**, um den Capability-/Status-Snapshot zu prüfen. Manuelle Aufnahmen sind der Standard; Trigger-Schwellenwerte bleiben deaktiviert, bis sie ausdrücklich konfiguriert werden.

### Profile Analyzer

Die installierte Zeile **Profile Analyzer** bietet **Open Profile Analyzer** und **Open Runtime**. Beginnen Sie zuerst mit der Aufzeichnung im Unity Profiler und starten und stoppen Sie dann eine PerfMeter-Session innerhalb dieser Aufzeichnung. Der Öffner verwendet `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()`, um Profile Analyzer zu öffnen und die Session-ID zu kopieren; laden Sie die aufgezeichneten Profiler-Daten und suchen Sie nach dieser ID. Er installiert Profile Analyzer nicht, lädt keine Profiler-Daten und wendet nicht automatisch einen Filter an.

### Adaptive Performance

Die installierte Zeile **Adaptive Performance** bietet **Open Runtime**, damit der aktuelle Status des optionalen Telemetrie-Providers geprüft werden kann. Die FTUE-Aktion startet weder eine Session noch eine Aufnahme.

### RenderDoc

RenderDoc ist ein externes Tool und wird nicht mit PerfMeter gebündelt. Folgen Sie dem offiziellen Unity-Integrationsablauf:

1. Installieren Sie RenderDoc von der offiziellen Download-Seite: <https://renderdoc.org/builds>.
2. Speichern Sie die Projektänderungen und verwenden Sie dann **Load RenderDoc** im Tab-Menü der Game View oder Scene View. Alternativ können Sie den Unity Editor oder einen Development Build über RenderDoc starten; starten Sie Unity neu, falls Unity die Verbindung nach der Installation nicht anbietet. Der offizielle Unity-Leitfaden ist <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html>.
3. Klicken Sie in FTUE auf **Check Attachment**. Dadurch wird nur Unitys gemeinsames Signal für externe Profiler aktualisiert; FTUE kann die RenderDoc-Installation nicht erkennen und Unity kann RenderDoc anhand dieses Signals nicht von PIX unterscheiden.
4. Klicken Sie auf **Copy Capture Snippet**, wechseln Sie in den Play Mode und rufen Sie den kopierten Code aus dem Laufzeitcode des Projekts auf:

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions("ftue-renderdoc-capture", PerfMeterCaptureTool.RenderDoc, 1));
   ```

5. Verwenden Sie **Open Runtime** für den Aufnahmestatus. Die kopierte Anfrage wird nicht gespeichert und nicht automatisch aufgerufen. Sie unterliegt den Anforderungen an Editor/Development Build, angehängtes Tool, Desktop-Plattform und Graphics API. `Completed` bestätigt nur den Lifecycle des Unity-Wrappers; es identifiziert weder das angehängte Tool, authentifiziert kein `.rdc`-Artefakt noch gibt es einen Artefaktpfad zurück.

### GraphicsStateCollection

Die gebündelte optionale Zeile **GraphicsStateCollection** benötigt keine Package-Installation. Sie bietet **Open Runtime**, **Copy Trace Snippet**, **Copy Prewarm Snippet** und **Reveal Artifacts**. FTUE fordert weder automatisch einen Trace noch ein Prewarm an. Verwenden Sie diese Abfolge:

1. Starten Sie im Play Mode eine aufzeichnende PerfMeter-Session mit `PerformanceMeter.StartSession(...)` und lassen Sie sie aktiv.
2. Rufen Sie den kopierten Trace-Code aus dem Laufzeitcode des Projekts auf:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. Fragen Sie `PerformanceMeter.GetGraphicsStateCollectionStatus()` ab, bis `State == PerfMeterGraphicsStateCollectionState.Completed` gilt. Verwenden Sie `ArtifactRelativePath`, der auf einen Pfad unter `Temp/PerfMeter/GraphicsStateCollections` zeigt, als Eingabe für das Prewarm. Das Stoppen der Session während des Tracings bricht den Trace ab.
4. Ersetzen Sie `<trace-artifact-file>` im kopierten Prewarm-Snippet durch den zurückgegebenen Pfad:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. Klicken Sie nach einem Trace auf **Reveal Artifacts**, um den projektlokalen Artefaktordner zu öffnen. Prewarm ist synchron, bewahrt das Artefakt und kann ein unvollständiges progressives Aufwärmen melden. Die Trace-Länge ist auf 600 Frames und die verwalteten Artefakte auf 64 MiB begrenzt; das Unity-Backend liefert keine Hinweise auf Cache-Misses.

## Vollständiger Initialisierungs-Bootstrap

Klicken Sie unter **Setup > Initialization Code** auf **Refresh from Project Settings** und anschließend auf **Copy Init Code**. Der generierte `PerfMeterBootstrap` bettet den vollständigen normalisierten Projekt-Settings-Snapshot ein und ruft nach dem Laden der Szene `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)` auf. Er übernimmt Overlay-, Logging-, Alert-, Session-Default- und Overdraw-Einstellungen, berücksichtigt `enabled` und `collectionMode: Stopped` und führt weder `StartSession` noch eine Capture-Anfrage aus.

Verwenden Sie diesen expliziten Bootstrap anstelle des codefreien Resources-Settings-Pfads, wenn der Start vom Code gesteuert werden soll. Wenn beide vorhanden sind, unterdrückt ein erfolgreich geparster expliziter Aufruf den Resources-Auto-Start-Callback für die aktuelle Domain; falls Resources zuerst gestartet wurde, wird der explizite Snapshot danach angewendet und ist maßgeblich. Ungültiges explizites JSON lässt die aktuelle Runtime unverändert und unterdrückt keinen späteren Resources-Auto-Start. Session- und Standard-Overdraw-Operationen verwenden den aktiven expliziten Runtime-Snapshot.

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

## Sitzungskorrelation Mit Profile Analyzer

Waerend der Profiler-Aufzeichnung erzeugt jede Sitzung die unmittelbaren Samples `SGG.PerfMeter.Session.<sessionId>.Begin` und `.End`. `SGG/Perfmeter/Open Profile Analyzer For Session` oeffnet das optionale Profile-Analyzer-Fenster und kopiert die aktuelle Sitzungs-ID in die Zwischenablage. Der Befehl installiert Profile Analyzer nicht, laedt keine Profiler-Daten und setzt keinen Filter automatisch; suchen Sie nach dem Laden des passenden Captures nach der kopierten ID.

## Fenster Fuer Sitzungsanalyse

Oeffnen Sie `SGG/Perfmeter/Session Analysis` fuer eine schreibgeschuetzte Editor-Ansicht der aktuellen Sitzung im Speicher. Virtualisierte Tabs zeigen die Timeline der gespeicherten Samples, den autoritativen Worst Frame mit Sample-Details, abgeleitete CPU-Main-/CPU-Render-/GPU-Budgetverletzungen sowie die autoritativen Whole-Run-/Current-Scene-Scopes. CPU-Main schliesst Present Wait aus; GPU-Werte und -Verletzungen benoetigen explizit verfuegbares GPU-Timing.

Das Fenster liest nur `GetSessionSummary()` und `GetSessionSamples()` und startet die Runtime nie. Nicht verfuegbares Timing erscheint als `Unavailable`, nicht als numerische Null. Eine gestoppte Sitzung bleibt sichtbar, solange ihre Runtime-Instanz existiert; `PerformanceMeter.Stop()`, Domain Reload oder das Beenden des Play Mode koennen die Sitzung im Speicher verwerfen.

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

Pruefe fuer GRD zuerst `DegradedReason`, SRP-Support, Projektkonfiguration, Compute-Support, URP-Moduskompatibilitaet und `ActivityAvailability`. `IsObservedActive` ist Unitys globaler Enabled-Zustand. `Effectiveness` ist nur aggregierter BRG-Workload-Kontext: `AvailableNoSample`/`Unavailable` bedeuten nicht null Workload, und positive BRG-Counter beweisen keine GRD-Nutzung eines bestimmten Renderers.

Im Capture-Bundle-Schema `sgg.perfmeter.capture-context` Version `1` bleibt `render` erhalten und `render_integration` kommt hinzu. Bei einem externen GPU-Capture wird dieser Kontext beim ersten Sample der Phase `Capturing` eingefroren; ein Memory-Profiler-Bundle zeichnet ihn beim Abschluss der Speicheranfrage auf. Session-JSON/CSV-Schemas bleiben unveraendert. Die oeffentliche API bietet keinen stabilen RenderGraph-/CustomPass-Viewer und keine Pass-Ziele; dieser Workflow verspricht daher keine Editor-Navigation.
