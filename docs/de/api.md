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

## Editor Compatibility Status

Die Editor API `PerfMeterSetupActions.GetCompatibilityStatus()` liefert `PerfMeterCompatibilityStatus` und meldet `ImportCompatible` fuer den Unity-`2022.3` Package-Floor, `CoreRuntimeCompatible` fuer den unterstuetzten Unity-`6000.4+` Runtime-Floor und `RenderIntegrationCompatible` fuer aktives URP/HDRP `17.4+` mit verfuegbarem Adapter getrennt. Jeder Wert hat einen Grund. Render-Kompatibilitaet bedeutet nicht, dass Renderer Assets bereits konfiguriert sind; dafuer dient setup status.

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

`PerfMeterCaptureBackendMode.GenericUnity` bleibt der Compatibility-Default fuer Unitys `ExternalGPUProfiler`; Tool- und Artefaktidentitaet bleiben unbestaetigt. `NativePreferred` fordert den optionalen Windows-x64-Editor-Bridge an und darf nur vor Native Begin zurueckfallen; `NativeRequired` faellt nie zurueck. Der native Pfad unterstuetzt D3D11, D3D12 und Vulkan. Status meldet `RequestedBackendMode`, `EffectiveBackendKind`, `NativePhase`, Result Code und Fallback Reason.

`PerfMeterCaptureOptions` verwendet standardmaessig `captureFrames: 1` sowie `preRollFrames: 0` und `postRollFrames: 0`. Ein gueltiges `RequestCapture` startet die Runtime automatisch. `CancelCapture()` ohne ID beendet die aktuell gemeldete aktive Anfrage; die Uebergabe einer ID schuetzt davor, eine neuere Anfrage zu beenden.

Generic und caller-supplied `.rdc`/`.wpix` bleiben observed. Nur der generation-bound native descriptor kann ein finalisiertes `.rdc` authentifizieren. Native MetadataOnly ist `DoNotShare`; Copy/Embed sind separat quota-managed und `ReviewBeforeShare`. Traversal, Reparse Points und Dateien ausserhalb der owned roots werden abgewiesen.

Der Capture-Bundle-Export bietet ausserdem eine nicht blockierende Single-Flight-API: `RequestCaptureBundleExport(..., out exportId)`, `GetCaptureBundleExportStatus(exportId)` und `CancelCaptureBundleExport(exportId)`. Der Status meldet Phase, Fortschritt, Byteanzahl, Abbruch, Wiederholungsversuch, Commit-Pfad und die generische External-Artifact-Envelope. Die bestehende API `ExportCaptureBundle(...)` bleibt ein blockierender Kompatibilitaets-Wrapper, waehrend Serialisierung, Datei-I/O, Hashing, Aufbewahrung und atomischer Commit in einem Worker-Thread ausgefuehrt werden.

Session- und Capture-JSON ergaenzen typisierte Timeline-Events fuer fehlende Samples und Capture-Grenzen. Bestehende Schemaversionen, Sample-Arrays und CSV-Spalten bleiben kompatibel; Legacy- oder unbekannte Timeline-Payloads werden gelesen, ohne Luecken zu erfinden. Custom-Metric-Provider verwenden auf dem aufgewaermten Erfassungspfad einen gecachten Provider-Snapshot und einen wiederverwendbaren, coreeigenen Buffer; Kopien entstehen nur fuer aufbewahrte Samples, Exporte und public snapshots. Die Profiler-Koordination ist ueber `GetProfilerLeaseCapabilities()`, `GetProfilerLeaseStatus()`, `TryAcquireProfilerLease(...)` und `ReleaseProfilerLease(...)` prozesslokal; gehaltene Leases ueberstehen keinen Domain Reload.

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

## Optionale Speicher-Snapshots

Speicher-Snapshots sind eine optionale Integration. Unter Unity `6000.4+` aktiviert `com.unity.memoryprofiler` `1.1.0+` die separate Assembly `SGG.PerfMeter.MemoryProfiler`, die das `MemoryProfiler`-Backend automatisch registriert. Die Core-Assembly hat keine harte Abhaengigkeit.

```csharp
PerfMeterMemorySnapshotCapabilitiesSnapshot capabilities =
    PerformanceMeter.GetMemorySnapshotCapabilities();

if (capabilities.Availability == PerfMeterAvailability.Available)
{
    PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
        new PerfMeterMemorySnapshotOptions("memory-spike-01"));
}

PerfMeterMemorySnapshotStatusSnapshot status = PerformanceMeter.GetMemorySnapshotStatus();
if (status.State == PerfMeterMemorySnapshotState.Completed &&
    PerformanceMeter.GetCaptureBundleStatus(status.CaptureId).IsExportReady)
{
    PerformanceMeter.ExportCaptureBundle(status.CaptureId);
}
```

Die oeffentliche Oberflaeche umfasst `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` und `GetMemorySnapshotTriggers()`. Ein eigenes Backend implementiert `IPerfMeterMemorySnapshotBackend`; die optionale Assembly liefert das Unity-Memory-Profiler-Backend.

`PerfMeterMemorySnapshotOptions` verwendet standardmaessig managed/native object flags, 1 GiB minimalen freien Speicher und 300 Sekunden cooldown. `RequestMemorySnapshot` ist standardmaessig manuell und liefert explizite Ergebnisse wie `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest` oder `Failed`. Leseaufrufe starten die Runtime nicht; eine gueltige Anfrage tut dies.

`ConfigureMemorySnapshotTriggers` aktiviert die opt-in Heuristiken fuer System-Speicherschwelle und begrenztes Leak-Wachstum. `GetMemorySnapshotTriggers()` ist standardmaessig disabled. Trigger-Anfragen verwenden dieselben Single-Flight-, cooldown-, free-space- und capture-flag-Schutzregeln wie manuelle Anfragen.

## Grafikdiagnose und GraphicsStateCollection

Die Grafikdiagnose erweitert die vorhandenen Snapshots. `PerformanceMeter.GetGraphicsDiagnostics()` liefert die neuesten Markerwerte fuer die Erstellung von Shader-GPU-Programmen und Graphics-Pipelines zusammen mit Graphics-API-Kontext, Parallel-PSO-Faehigkeit und der Revision des Profiler-Metrikkatalogs.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

Der Katalog entdeckt Unity-`ProfilerRecorder`-Deskriptoren beim Runtime-Start sowie bei einem ausdruecklichen Refresh/Reconfigure. Fuer den Shader verwendet er den exakten Namen `Shader.CreateGPUProgram` und die Aliase `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` und `Shader.DynamicLoadGPUProgram`. Fuer die Graphics-Pipeline wird der exakte Name `CreatePSO.Job` verwendet. Jede Capability bewahrt `Resolution` (`None`, `Exact` oder `Alias`), `ResolvedRecorderNames`, `Category`, die entdeckten Werte `Unit` und `DataType` sowie `ResolvedComponentCount` und `SampledComponentCount`. `PerfMeterMetricsSnapshot` und Session-JSON/CSV enthalten dieselben Markerwerte, Capability-Metadaten und die Katalogrevision.

Die Marker-Verfuegbarkeit ist dynamisch. Verwende `SampleState` (`Unavailable`, `AvailableNoSample` oder `AvailableSampled`) und die Capability-Metadaten; ein Nullwert beweist nicht, dass ein Marker fehlt. Die Werte sind rohe Recorder-Werte in der entdeckten Einheit. Sie sind nicht grundsaetzlich Shader- oder PSO-Counts und werden nicht in eine gemeinsame Einheit umgerechnet.

Die optionale Assembly `SGG.PerfMeter.GraphicsStateCollection` ist auf Unity `6000.4+` begrenzt und registriert das Unity-Backend, wenn es verfuegbar ist. Unity `6000.4` verwendet `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, Unity `6000.5+` dagegen `UnityEngine.Rendering.GraphicsStateCollection`. Die Core-Assembly bleibt von diesem Backend unabhaengig.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.25f, 240));

PerfMeterGraphicsStateCollectionRequestResult request =
    PerformanceMeter.RequestGraphicsStateTrace(
        new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", traceFrames: 60));

PerfMeterGraphicsStateCollectionStatusSnapshot status =
    PerformanceMeter.GetGraphicsStateCollectionStatus();
if (status.State == PerfMeterGraphicsStateCollectionState.Completed)
{
    PerformanceMeter.PrewarmGraphicsStateCollection(
        new PerfMeterGraphicsStatePrewarmOptions(status.ArtifactRelativePath));
}
```

Die oeffentliche State-Collection-Oberflaeche umfasst `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)` und `CancelGraphicsStateTrace(string captureId)`. Ein eigenes Backend implementiert `IPerfMeterGraphicsStateCollectionBackend` und meldet Trace-/Prewarm-, Cache-Miss- und Parallel-PSO-Faehigkeiten.

`PerfMeterGraphicsStateTraceOptions` benoetigt eine nichtleere `CaptureId`, akzeptiert 1–600 Trace-Frames und verwendet standardmaessig 60 Frames sowie 1 GiB minimalen freien Speicher. Ein Trace ist nur waehrend einer laufenden PerfMeter-Session gueltig. Korrelierte Session-Samples tragen die aktive Capture-ID als `GraphicsStateTraceId` (`graphics_state_trace_id` in Exporten). Session-Sampling-Einstellungen steuern die Dichte korrelierter Samples, nicht die angeforderte Trace-Framezahl.

`PerfMeterGraphicsStateCollectionStatusSnapshot` stellt `IsBusy` und `HasPendingCleanup` bereit. `IsBusy` ist waehrend Vorbereitung, Trace, Trace-Ende, Prewarm, Cleanup oder persistiertem pending cleanup true; `HasPendingCleanup` kennzeichnet gezielt ein eigenes Artefakt, das auf einen Cleanup-Retry wartet. Wenn `PerformanceMeter.StopSession()` waehrend eines aktiven Traces aufgerufen wird, bricht es den Trace ab; die Session muss daher bis zum Trace-Ende aufzeichnen. Bei fehlgeschlagener Loeschung erzeugt die eigene Datei einen benachbarten `.delete-pending`-Sidecar-Marker; nach einem Domain Reload wird der Marker wiederhergestellt und das Cleanup erneut versucht. Der Status bleibt sichtbar und busy, bis Artefakt und Marker entfernt sind.

Der Coordinator erlaubt jeweils nur einen Graphics-State-Flight. Dieselbe aktive ID liefert `AlreadyActive`; ein anderer Trace oder Prewarm waehrend Vorbereitung, Trace, Abschluss, Cleanup oder einer anderen Capture-Domaene liefert `RejectedOverlap`. `CancelGraphicsStateTrace` trifft nur die passende aktive oder vorbereitende ID, bricht das Backend ab und entfernt das ausstehende eigene Artefakt. Cleanup-Fehler bleiben sichtbar und koennen einen Ersatz blockieren, bis die Bereinigung erneut gelingt.

## Render-Integrationskontext

Der additive, integrationsneutrale Snapshot ist ueber beide Methoden verfuegbar:

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` stellt `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph` und `Warning` bereit. Die verschachtelten GRD- und VRS-Snapshots enthalten Availability, Konfigurations-/Support-Felder, Activity-Availability und Warnungen.

Lesen ist vor dem Runtime-Start sicher und startet keine Sammlung. Eine unterstuetzte aktuelle Pipeline kann `Available` bei `State = NotObserved` sein. Gehoert die letzte Observation zu einer anderen Pipeline-Konfiguration, ist `ObservationMatchesCurrentPipeline` `false`; Frame/Age bleiben explizit und die Warnung kennzeichnet veraltete Daten. Veraltete Felder duerfen nicht als aktuelle Observation behandelt werden.

URP verwendet das oeffentliche aktuelle `UniversalRenderingData.renderingMode` und meldet die in diesem Frame tatsaechlich geplanten PerfMeter-Passes. HDRP meldet den tatsaechlich beobachteten PerfMeter-`CustomPass`, aber der effektive Rendering-Modus ist nicht verfuegbar. `GpuResidentDrawer` meldet konfigurierten Modus, SRP-/Projekt-/Compute-Support, aktuelles URP-Forward+ und Cluster-Modus-Kompatibilitaet sowie globale Runtime-Aktivitaet aus `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()`. Bei HDRP bleiben Forward+-/Rendering-Modus-Felder `Unknown`. `VariableRateShading` meldet autoritativen Hardware-Support aus `SystemInfo`/`ShadingRateInfo`; Konfiguration und Aktivitaet bleiben `Unknown`, sofern kein typisierter Adapter sie meldet.

`LegacyRenderGraph` ist eine eingebettete Compatibility-Fassade fuer `GetRenderGraphSnapshot()`. Private/interne Pass-/Ressourcen-Reflection wurde entfernt; daher bleiben die Legacy-Counter `-1`. Die stabile oeffentliche Unity-API bietet ausserdem keinen RenderGraph-/CustomPass-Viewer und keine Pass-Ziele; Editor-Navigation wird von dieser API nicht versprochen.

`GpuResidentDrawer` enthaelt zusaetzlich `ProjectConfigurationAvailability`, `IsProjectConfigurationSupported`, `ComputeShaderAvailability`, `SupportsComputeShaders`, `ForwardPlusActivityAvailability`, `IsObservedForwardPlusActive`, `RenderingModeCompatibilityAvailability`, `IsRenderingModeCompatible`, `ActivitySource`, `DegradedReason` und `Effectiveness`. `PerfMeterGpuResidentDrawerReason` liefert strukturierte Fallback-Zustaende. `PerfMeterGpuResidentDrawerEffectivenessSnapshot` enthaelt BRG-Draw-Call-/Instanzwerte samt Profiler-Capability-Provenienz; nicht gesampelte Werte sind in C# `-1` und im JSON `null`. Es handelt sich um aggregierte BatchRendererGroup-Counter, nicht um autoritative GRD-Evidence pro Renderer.

`PerfMeterGraphicsStatePrewarmOptions` akzeptiert nur einen eigenen project-relativen `.graphicsstate`-Pfad und einen optionalen `MaxStateCount` von 0 bis 1.000.000. Prewarm laeuft synchron, bewahrt das Artefakt und meldet `CompletedWarmupCount` und `IsWarmedUp`; ein erfolgreiches, aber unvollstaendiges progressives Warmup enthaelt eine Warnung. `TraceCacheMisses` bleibt fuer erweiterbare Backends vorhanden, aber das Unity-Backend unterstuetzt keine Cache-Miss-Evidence; eine solche Anfrage liefert `Unavailable`.

## Sitzungskorrelation

`PerformanceMeter.GetSessionSummary().SessionId` ist eine 32-stellige hexadezimale Kennung in Kleinschreibung. Sie wird durch `StartSession` erzeugt, bleibt nach `StopSession` stabil, aendert sich bei einer neuen Sitzung und ist leer, wenn keine Sitzung existiert. Session-JSON enthaelt denselben Wert als oberstes Feld `session_id`; CSV fuegt ihn als letzte Spalte `session_id` an und erhaelt damit die Position vorhandener Spalten; `perfmeter.session.summary` liefert ihn als `session_id`.
