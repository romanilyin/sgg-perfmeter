# API Runtime

Namespace:

```csharp
using SGG.PerfMeter;
```

Tutte le API di lettura sono sicure prima dell'avvio del runtime. Le letture restituiscono snapshot fermi/predefiniti invece di generare eccezioni quando il runtime non e attivo.

## Ciclo Di Vita

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

## Stato E Metriche

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Gruppi metrici principali:

- FPS: average, 1% low, 0.1% low, conteggi spike.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame quando disponibile.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: system/app memory, GC reserved memory, GPU memory quando disponibile.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced o unknown.
- Overdraw: stato, progresso, ratio e visibilita heatmap.

La disponibilita dei counter e esposta tramite `AvailableCounters`, `UnavailableCounters` e warning.

## Self-Observability E Budget Dell'Overhead

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

La self-observability pubblica misure low-overhead del costo dei callback CPU in finestre fisse di 120 frame. Le medie sono per invocazione. Lo stato complessivo e `NotInitialized`, `Collecting` o `Ready`; lo stato di componente e `NotMeasured`, `Collecting`, `Ready` o `Unsupported`.

I componenti sono `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` e `HdrpRenderIntegration`. Ognuno espone conteggi di frame/invocazioni, millisecondi CPU medi/massimi, allocazioni totali/medie, budget e stati `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Componente | Budget CPU | Budget allocazioni |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

Il self-timing GPU e esplicitamente `Unavailable`. Questi diagnostics non sottraggono ne modificano le metriche CPU/GPU esistenti.

## Catalogo Dinamico Delle Metriche Profiler

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` e `GetProfilerMetricCapabilities()` leggono il catalogo in cache. Lo stato del catalogo e `NotInitialized`, `Ready` o `Error`; ogni capability riporta `Unavailable`, `AvailableNoSample` o `AvailableSampled`, e `Resolution` indica la provenienza `None`, `Exact` o `Alias`. La discovery avviene solo all'avvio del runtime e durante refresh/reconfigure espliciti, non nella raccolta steady-state. I valori numerici esistenti restano valori di compatibilita; usa `SampleState`/`IsAvailable` della capability come segnale autorevole di disponibilita.

## Snapshot Strutturati

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Gli snapshot device includono informazioni su Unity/piattaforma/OS/CPU/GPU/API/display/window/support. Gli snapshot camera includono scena, transform, projection, clipping, pixel rect, target display e impostazioni camera URP/HDRP quando disponibili.

## Carico Dei Core CPU

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Ogni snapshot espone `CoreIndex`, `LoadPercent` e `Available`. L'array puo essere vuoto prima dell'avvio runtime, durante il warm-up del sampler o su piattaforme non supportate; trattalo come informazione di capacita della piattaforma, non come chiamata API fallita.

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

Le modalita overlay legacy e i flag semantici dei moduli restano disponibili per compatibilita e filtraggio.

## Sessioni

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

Le opzioni sessione includono warm-up frames/seconds, intervallo di sample, numero massimo di sample, reset-on-scene-load e finestre di ignoramento del scene-load.

## Alert

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` e `true` per impostazione predefinita e controlla solo l'output `Debug.Log` degli alert strutturati. Il valore `false` non disabilita i callback `AlertFired`, gli alert recenti o la cronologia degli alert, gli avvisi dell'overlay, i log di avviso Editor o le sessioni. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` controlla i log di avviso Editor in modo indipendente.

## Editor Compatibility Status

L'API Editor `PerfMeterSetupActions.GetCompatibilityStatus()` restituisce `PerfMeterCompatibilityStatus` e separa `ImportCompatible` per il floor Unity `2022.3`, `CoreRuntimeCompatible` per il runtime supportato Unity `6000.4+` e `RenderIntegrationCompatible` per URP/HDRP attivo `17.4+` con adapter disponibile. Ogni risultato include una reason. La compatibilita render non implica che i renderer assets siano configurati; usa setup status per la configuration readiness.

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

Il coordinator consente una sola richiesta attiva e avanza in modo deterministico attraverso `PreRoll`, `Capturing`, `PostRoll` e `Completed`. Ripetere lo stesso ID attivo e idempotente; un ID attivo diverso viene rifiutato per sovrapposizione. `Canceled`, `Unavailable` ed `Error` sono stati terminali espliciti.

Il backend integrato avvolge l'`ExternalGPUProfiler` sperimentale di Unity solo nell'Editor o in un Development Build, solo quando uno strumento esterno e gia collegato e solo per combinazioni desktop di piattaforma/API supportate. Le combinazioni supportate sono `RenderDoc` su desktop Windows/Linux con Direct3D 11, Direct3D 12 o Vulkan e `PIX` su desktop Windows con Direct3D 12. Seleziona esplicitamente `RenderDoc` o `Pix`, perche Unity non espone l'identita dello strumento collegato. `Status.Tool` e solo lo strumento richiesto, non l'identita verificata dello strumento collegato. `Completed` conferma solo il wrapper lifecycle di Unity; non verifica ne restituisce un artefatto esterno `.rdc`/`.wpix` o il relativo path. I test automatizzati usano un fake backend; la conferma dello strumento esterno reale e dell'artefatto resta un release gate.

I valori predefiniti di `PerfMeterCaptureOptions` sono `captureFrames: 1`, `preRollFrames: 0` e `postRollFrames: 0`. Un `RequestCapture` valido avvia automaticamente il runtime. `CancelCapture()` senza ID annulla la richiesta attiva attualmente riportata; passare un ID protegge dall'annullamento di una richiesta piu recente.

L'overload con `PerfMeterCaptureBundleOptions` separa i capture samples dalla baseline session e puo includere uno screenshot opt-in. Quando `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady`, `PerformanceMeter.ExportCaptureBundle(captureId)` crea atomicamente un bundle versionato sotto `Temp/PerfMeter/CaptureBundles` con manifest SHA-256, samples, alerts, contesto, screenshot opzionale e metadata dell'artefatto esterno. Un `.rdc`/`.wpix` locale al progetto e solo un artefatto osservato, mai autoritativo; traversal, reparse points e file esterni al progetto vengono rifiutati.

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Le eccezioni dei provider sono riportate come snapshot di custom metric non disponibili e non interrompono la raccolta delle metriche core.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

La diagnostica overdraw usa modalita diagnostiche esplicite e puo aggiungere lavoro GPU. In HDRP queste API riportano in sicurezza unsupported state per overdraw e heatmap, senza promettere HDRP heatmap output.

## Snapshot di memoria opzionali

Gli snapshot di memoria sono un'integrazione opzionale. In Unity `6000.4+`, `com.unity.memoryprofiler` `1.1.0+` abilita l'assembly separata `SGG.PerfMeter.MemoryProfiler`, che registra automaticamente il backend `MemoryProfiler`. L'assembly core non ha una dipendenza obbligatoria.

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

La superficie pubblica comprende `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` e `GetMemorySnapshotTriggers()`. Un backend personalizzato implementa `IPerfMeterMemorySnapshotBackend`; l'assembly opzionale fornisce il backend Unity Memory Profiler.

`PerfMeterMemorySnapshotOptions` usa per impostazione predefinita i flag degli oggetti managed/native, 1 GiB di spazio libero minimo e un cooldown di 300 secondi. `RequestMemorySnapshot` e manuale per impostazione predefinita e restituisce risultati espliciti come `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest` o `Failed`. Le letture non avviano il runtime; una richiesta valida lo fa.

`ConfigureMemorySnapshotTriggers` abilita esplicitamente l'euristica della soglia di memoria di sistema e della crescita limitata delle perdite. `GetMemorySnapshotTriggers()` e disabilitato per impostazione predefinita. Le richieste attivate dai trigger usano le stesse protezioni single-flight, cooldown, spazio libero e capture flags delle richieste manuali.

## Diagnostica grafica e GraphicsStateCollection

La diagnostica grafica aggiunge dati agli snapshot esistenti. `PerformanceMeter.GetGraphicsDiagnostics()` restituisce gli ultimi valori dei marker di creazione dei programmi GPU shader e delle graphics pipeline, insieme al contesto della graphics API, alla capacita di PSO paralleli e alla revisione del catalogo delle metriche del profiler.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

Il catalogo scopre i descrittori `ProfilerRecorder` di Unity all'avvio del runtime e durante un refresh/reconfigure esplicito. Per lo shader usa il nome esatto `Shader.CreateGPUProgram` e gli alias `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` e `Shader.DynamicLoadGPUProgram`. Per la graphics pipeline usa il nome esatto `CreatePSO.Job`. Ogni capability conserva `Resolution` (`None`, `Exact` o `Alias`), `ResolvedRecorderNames`, `Category`, i valori rilevati `Unit` e `DataType`, oltre a `ResolvedComponentCount` e `SampledComponentCount`. `PerfMeterMetricsSnapshot` e i JSON/CSV di sessione includono gli stessi valori dei marker, i metadata della capability e la revisione del catalogo.

La disponibilita dei marker e dinamica. Usa `SampleState` (`Unavailable`, `AvailableNoSample` o `AvailableSampled`) e i metadata della capability; un valore zero non dimostra che il marker sia assente. I valori sono valori raw del recorder e mantengono l'unita rilevata: non sono universalmente conteggi di shader o PSO e PerfMeter non li converte in un'unita comune.

L'assembly opzionale `SGG.PerfMeter.GraphicsStateCollection` e limitato a Unity `6000.4+` e registra il backend Unity quando disponibile. Su Unity `6000.4` usa `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, mentre su Unity `6000.5+` usa `UnityEngine.Rendering.GraphicsStateCollection`. L'assembly core resta indipendente da questo backend.

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

La superficie pubblica di state collection comprende `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)` e `CancelGraphicsStateTrace(string captureId)`. Un backend personalizzato implementa `IPerfMeterGraphicsStateCollectionBackend` e segnala le capacita di trace/prewarm, cache-miss e PSO paralleli.

`PerfMeterGraphicsStateTraceOptions` richiede un `CaptureId` non vuoto, accetta 1–600 trace frames e usa per impostazione predefinita 60 frames e 1 GiB di spazio libero minimo. Un trace e valido solo mentre una sessione PerfMeter e in registrazione. I sample di sessione correlati contengono l'ID di capture attivo in `GraphicsStateTraceId` (`graphics_state_trace_id` negli export). Le impostazioni di sampling della sessione controllano la densita dei sample correlati, non il numero di trace frames richiesto.

`PerfMeterGraphicsStateCollectionStatusSnapshot` espone `IsBusy` e `HasPendingCleanup`. `IsBusy` e true durante preparazione, trace, fine del trace, prewarm, cleanup o cleanup pending persistente; `HasPendingCleanup` identifica specificamente un artefatto owned in attesa di un retry del cleanup. Se viene chiamato `PerformanceMeter.StopSession()` durante un trace attivo, il trace viene annullato; la sessione deve quindi restare in registrazione fino al completamento del trace. Se la cancellazione di un artefatto owned fallisce, viene creato un sidecar owned `.delete-pending` adiacente; dopo un domain reload il marker viene ripristinato e il cleanup viene ritentato. Lo stato resta visibile e busy finche artefatto e marker non sono stati rimossi.

Il coordinator consente un solo graphics-state flight. Lo stesso ID attivo restituisce `AlreadyActive`; un altro trace o prewarm durante preparazione, trace, finalizzazione, cleanup o un altro capture domain restituisce `RejectedOverlap`. `CancelGraphicsStateTrace` corrisponde solo all'ID attivo o in preparazione, annulla il backend e rimuove l'artefatto owned in attesa. Gli errori di cleanup restano visibili e possono bloccare una sostituzione fino a un nuovo tentativo riuscito.

`PerfMeterGraphicsStatePrewarmOptions` accetta solo un path `.graphicsstate` owned relativo al progetto e un `MaxStateCount` opzionale da 0 a 1.000.000. Il prewarm e sincrono, conserva l'artefatto e riporta `CompletedWarmupCount` e `IsWarmedUp`; un progressive warmup riuscito ma incompleto include un warning. `TraceCacheMisses` esiste per backend estensibili, ma il backend Unity non supporta l'evidence di cache-miss: la richiesta restituisce `Unavailable`.

## Contesto di render integration

Lo snapshot additivo e neutrale rispetto all'integrazione è disponibile tramite entrambi i metodi:

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` espone `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph` e `Warning`. Gli snapshot annidati GRD e VRS espongono availability, campi di configurazione/support, activity availability e warning.

Le letture sono sicure prima dell'avvio del runtime e non avviano la raccolta. Una pipeline corrente supportata può essere `Available` con `State = NotObserved`; se l'ultima observation appartiene a un'altra configurazione della pipeline, `ObservationMatchesCurrentPipeline` è `false`, frame/age restano espliciti e il warning segnala dati stale. Non trattare i campi stale come observation corrente.

URP usa il `UniversalRenderingData.renderingMode` pubblico del frame corrente e riporta i pass PerfMeter realmente programmati per quel frame. HDRP riporta il `CustomPass` PerfMeter effettivamente osservato, ma l'effective rendering mode non è disponibile. `GpuResidentDrawer` riporta la modalità configurata e il supporto SRP pubblico; l'attività è `Unknown`. `VariableRateShading` riporta il supporto hardware autorevole di `SystemInfo`/`ShadingRateInfo`; configurazione e attività restano `Unknown` finché un typed adapter non le dimostra.

`LegacyRenderGraph` è una facade di compatibilità incorporata per `GetRenderGraphSnapshot()`. La reflection privata/interna di pass e risorse è stata rimossa, quindi i legacy counter restano a `-1`. La stable public API di Unity non espone inoltre un viewer RenderGraph/CustomPass né pass target; questa API non promette navigazione nell'Editor.

`RenderPipeline` contiene `Kind`, `AssetName`, `AssetTypeName` e `RuntimeTypeName`; `RenderPipelineAssetSource` può essere `GraphicsSettings`, `QualitySettings` o `None`. `GpuResidentDrawer` contiene `Availability`, `ConfiguredMode`, `IsConfigured`, `SupportAvailability`, `IsSupported`, `ActivityAvailability`, `IsObservedActive` e `Warning`. `VariableRateShading` contiene i campi hardware (`SupportsVariableRateShading`, `SupportsPerDrawCall`, `SupportsPerImageTile`, `ImageTileWidth`, `ImageTileHeight`, `GraphicsFormat`) oltre a `ConfigurationAvailability`, `IsConfigured`, `ActivityAvailability`, `IsObservedActive` e `Warning`.
