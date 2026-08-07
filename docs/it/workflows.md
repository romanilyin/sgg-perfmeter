# Workflow

## Overlay Runtime

Usa l'overlay quando ti serve visibilita immediata dentro il gioco.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

L'overlay usa UI Toolkit e non intercetta l'input di gameplay. Supporta FPS-only, testo compatto, grafici, diagnostica completa, barre metriche, temi visivi, filtri modulo, grafici CPU/GPU, widget dei core CPU e righe limitate di metriche personalizzate.

PerfMeter crea e possiede un host UI Toolkit versionato per l'overlay: Unity `6000.4` usa `UIDocument`, mentre Unity `6000.5+` usa `PanelRenderer`. L'host di proprieta e separato dalla UI estranea e ne conserva panel settings e children; i rebuild rimuovono solo il container di proprieta di PerfMeter.

## Raccolta In Background

Usa la modalita background per test, esecuzioni su dispositivo o workflow agent in cui non serve UI visibile.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Registrazione Ed Esportazione Sessioni

Usa le sessioni per finestre di profiling ripetibili.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Le esportazioni sessione includono timing, FPS lows, spikes, conteggi dei colli di bottiglia, contatori render, contatori memoria, stato overdraw, disponibilita di warning/counter, riepiloghi scena, frame peggiori, metadati dispositivo, metadati camera, metadati impostazioni e custom metrics.

## Alert

Le regole possono segnalare violazioni del budget, FPS bassi, GPU timing non disponibile e soglie overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Gli avvisi Editor sono limitati da cooldown e possono essere disabilitati tramite impostazioni JSON o controlli runtime. I log degli alert strutturati e gli avvisi Editor sono indipendenti: `PerformanceMeter.SetStructuredLogsEnabled(false)` sopprime solo l'output `Debug.Log` degli alert strutturati, mentre `PerformanceMeter.SetEditorWarningLogsEnabled(false)` controlla separatamente i log di avviso Editor. Callback, alerts/history, avvisi dell'overlay e sessioni restano attivi.

## External GPU Capture

Usa il capture coordinator per una richiesta limitata di RenderDoc o PIX quando lo strumento e gia collegato:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

Il coordinator consente una sola richiesta attiva e avanza in modo deterministico attraverso `PreRoll`, `Capturing`, `PostRoll` e `Completed`. Lo stesso ID attivo e idempotente; un ID diverso viene rifiutato come sovrapposizione. Pre-roll e post-roll contano i frame Unity; solo `Capturing` apre l'alert capture scope e invoca l'`ExternalGPUProfiler` sperimentale di Unity. I gate obbligatori sono Editor o Development Build e uno strumento collegato. `RenderDoc` e consentito su desktop Windows/Linux con Direct3D 11, Direct3D 12 o Vulkan; `PIX` e consentito su desktop Windows con Direct3D 12.

`Completed` significa solo che il wrapper lifecycle protetto e terminato. Unity non espone l'identita dello strumento collegato ne un path autorevole dell'artefatto; `Status.Tool` e solo lo strumento richiesto. L'overload con `PerfMeterCaptureBundleOptions` separa i samples baseline/capture ed esporta atomicamente un bundle locale al progetto; un artefatto esterno resta osservato, non autorevole. Per l'automazione usa `perfmeter.capture.request/status/cancel/export/capabilities`.

## Diagnostica Overdraw

Il numerical overdraw e opt-in e limitato nel tempo.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Numerical overdraw e heatmap usano il diagnostic path URP Render Graph. La misurazione overdraw richiede `PerfMeterRenderGraphFeature`, supporto replacement shader, supporto fragment UAV/storage-buffer, supporto compute shader, una graphics API supportata e async GPU readback. HDRP riporta overdraw/heatmap come unsupported, mentre core overlay, session, API e MCP diagnostics restano disponibili. I target non supportati restituiscono `OverdrawState.Unsupported` invece di eseguire il pass.

## Riproducibilita Di Camera E Device

Usa gli snapshot per conservare l'ambiente che ha prodotto una cattura prestazionale.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Le esportazioni sessione includono metadati di device e camera, cosi una cattura puo essere compresa o riprodotta in seguito.

## Custom Metrics

Registra provider specifici del progetto senza fare fork di PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Le custom metrics sono esposte tramite letture API, esportazione sessione JSON, MCP latest metrics e fino a otto righe overlay quando il modulo `CustomMetrics` e abilitato.

## Strumentazione Unity Profiler

La strumentazione e interna ed e visibile solo profilando l'Editor, un Development Build o un altro build con Profiler abilitato. Nei Release player senza Profiler, questi marker/counter sono no-op e non producono dati di strumentazione; gli schemi di public API, status, MCP ed export restano invariati.

- I marker coprono collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), provider (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) ed export JSON/CSV (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` e un hook interno riservato per i provider.
- I counter coprono i tempi frame CPU/GPU (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) come gauge di fine frame in nanosecondi. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` e `SGG.PerfMeter.Thermal.Available` codificano availability/active come `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` e `SGG.PerfMeter.Capture.State` usano enum codes; `SGG.PerfMeter.Provider.CustomMetricCount` e un count. Tutti i counter usano la categoria `Scripts` e `FlushOnEndOfFrame`.
- Non viene emesso alcun sample termico sintetico; `SGG.PerfMeter.Thermal.Available` resta a `0`/non disponibile finche un provider di piattaforma reale non fornisce dati.

## Self-Observability E Budget Dell'Overhead

Usa `PerformanceMeter.GetSelfOverhead()` o `PerformanceMeter.GetStatus().SelfOverhead` per diagnosticare costo dei callback CPU e allocazioni di collector, custom providers, CPU-core provider, overlay e integrazione URP/HDRP. La misurazione usa finestre fisse di 120 frame, medie per invocazione e budget CPU/allocazione specifici per componente.

La render integration inattiva riporta `Unsupported`, un componente supportato senza chiamate riporta `NotMeasured` e il self-timing GPU riporta `Unavailable`. L'accounting e solo diagnostico: PerfMeter non sottrae overhead e non modifica le metriche CPU/GPU esistenti.

## Automazione Agent

Una tipica esecuzione guidata da MCP:

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

`perfmeter.profiler.capabilities {}` e una lettura dalla cache; non avvia il runtime e non esegue la discovery.

## Workflow per snapshot di memoria opzionali

1. Usa Unity `6000.4+` e installa `com.unity.memoryprofiler` `1.1.0+` tramite Package Manager. L'assembly opzionale `SGG.PerfMeter.MemoryProfiler` registra quindi automaticamente il backend; senza questo pacchetto l'integrazione core resta unavailable.
2. In Play Mode, leggi `PerformanceMeter.GetMemorySnapshotCapabilities()` o `perfmeter.memory.snapshot.capabilities` e verifica la disponibilità del backend e dei flag richiesti.
3. Richiedi uno snapshot manuale con `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))`, oppure configura `ConfigureMemorySnapshotTriggers(...)` per abilitare esplicitamente una soglia della memoria di sistema o una finestra limitata di crescita delle perdite.
4. Leggi `GetMemorySnapshotStatus()` o `perfmeter.memory.snapshot.status` finché lo snapshot e il bundle correlato raggiungono uno stato terminale. Esporta l'evidence pronta con `PerformanceMeter.ExportCaptureBundle(captureId)` o `perfmeter.capture.export`.

L'evidence solo memoria passa attraverso l'API esistente dei capture bundle sotto `Temp/PerfMeter/CaptureBundles`. Il bundle registra `MemoryProfiler` come strumento richiesto, include la provenienza della memoria e uno SHA-256 in streaming per il `.snap`, senza includere un artefatto GPU esterno. La sorgente posseduta si trova sotto `Temp/PerfMeter/MemorySnapshots`; un export riuscito la consuma una sola volta.

## Diagnostica dei marker grafici

1. Chiama `PerformanceMeter.GetGraphicsDiagnostics()` o `perfmeter.graphics.diagnostics` per leggere gli ultimi valori dei marker e il contesto della graphics API.
2. Controlla per ogni capability `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, i component counts risolti/campionati e la revisione del catalogo. La discovery e dinamica: avviene all'avvio del runtime e durante un refresh/reconfigure esplicito del catalogo del profiler.
3. Tratta i valori come raw recorder values nelle loro units scoperte. Un marker puo essere unavailable, disponibile senza sample o sampled; lo zero numerico non e un segnale universale di unavailable e il valore non e garantito come count di shader o PSO.

Lo shader marker risolve prima esattamente `Shader.CreateGPUProgram`, quindi gli alias `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` e `Shader.DynamicLoadGPUProgram`. Il pipeline marker risolve esattamente `CreatePSO.Job`. Gli stessi valori e la provenance sono disponibili tramite `perfmeter.metrics.latest` e session JSON/CSV.

## Correlazione Della Sessione Con Profile Analyzer

Durante il profiling, ogni sessione emette i sample istantanei `SGG.PerfMeter.Session.<sessionId>.Begin` e `.End`. `SGG/Perfmeter/Open Profile Analyzer For Session` apre la finestra opzionale di Profile Analyzer e copia negli appunti l'ID della sessione corrente. Il comando non installa Profile Analyzer, non carica i dati del Profiler e non applica automaticamente un filtro; dopo aver caricato la cattura pertinente, cercare l'ID copiato.

## Finestra Di Analisi Della Sessione

Apri `SGG/Perfmeter/Session Analysis` per una vista Editor di sola lettura della sessione corrente in memoria. Le tab virtualizzate mostrano la timeline dei sample conservati, il worst frame autorevole con i dettagli del sample disponibili, le violazioni derivate dei budget CPU-main/CPU-render/GPU e gli scope autorevoli whole-run/current-scene. CPU-main esclude present wait; valori e violazioni GPU richiedono disponibilita esplicita del timing GPU.

La finestra legge solo `GetSessionSummary()` e `GetSessionSamples()` e non avvia mai il runtime. Il timing non disponibile viene mostrato come `Unavailable`, non come zero numerico. Una sessione arrestata resta visibile finche esiste la sua istanza runtime; `PerformanceMeter.Stop()`, un domain reload o l'uscita dal Play Mode possono eliminare la sessione in memoria.

## Trace e prewarm di GraphicsStateCollection

1. Su Unity `6000.4+`, verifica che sia disponibile l'assembly opzionale `SGG.PerfMeter.GraphicsStateCollection`. Usa il namespace `UnityEngine.Experimental.Rendering.GraphicsStateCollection` su Unity `6000.4` e `UnityEngine.Rendering.GraphicsStateCollection` su Unity `6000.5+`.
2. Avvia una sessione PerfMeter prima del trace. Esegui `StartSession(...)`, poi `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` o la richiesta MCP corrispondente. Senza una sessione attiva la richiesta viene rifiutata; la sessione deve restare in registrazione fino alla fine del trace e `PerformanceMeter.StopSession()` annulla un trace attivo.
3. Lascia in esecuzione lo scenario mentre procede il trace limitato. In Play Mode normale ogni trace frame viene tickato dopo `WaitForEndOfFrame`; in batch mode il coordinator usa un fallback al frame successivo. I sample di sessione ammessi in questo intervallo riportano `GraphicsStateTraceId`/`graphics_state_trace_id`; le impostazioni della sessione determinano quanti sample correlati vengono conservati.
4. Interroga `GetGraphicsStateCollectionStatus()` o `perfmeter.graphics.state_collection.status` fino a `Completed`, poi arresta la sessione se necessario. Arrestarla durante il trace attivo lo annulla e puo lasciare `IsBusy`/`is_busy` true mentre viene ritentato il cleanup owned. L'artefatto `.graphicsstate` owned e relativo al progetto, si trova sotto `Temp/PerfMeter/GraphicsStateCollections` ed e limitato a 64 MiB.
5. Passa il path relativo owned indicato a `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` o al comando MCP di prewarm. Il prewarm e sincrono, conserva l'artefatto e riporta i warmup completati e `IsWarmedUp`; un progressive warmup puo terminare con un warning esplicito di incompletezza.

Il coordinator graphics-state ammette un solo flight e rifiuta anche l'overlap con external GPU capture, memory snapshot o alert-capture attivi. Lo stesso trace ID attivo restituisce `AlreadyActive`; un altro ID restituisce `RejectedOverlap`. `CancelGraphicsStateTrace` annulla solo un trace attivo/in preparazione corrispondente e pulisce l'artefatto in attesa. Se un artefatto owned non puo essere cancellato, `HasPendingCleanup`/`has_pending_cleanup` resta true, un sidecar adiacente `.delete-pending` viene mantenuto e ripristinato/ritentato dopo un domain reload; `IsBusy`/`is_busy` e il warning restano visibili fino al successo. Il backend Unity non supporta il cache-miss tracing, quindi non e disponibile alcuna evidence di cache-miss.

## Contesto di render integration

Usa lo snapshot neutrale quando serve una vista indipendente dalla pipeline dell'ultima render integration tipizzata:

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

Gli stessi dati sono leggibili tramite MCP:

```text
perfmeter.render.snapshot {}
```

Queste letture non avviano la raccolta del runtime. Controlla insieme `State`, `ObservationAgeFrames`, `LastObservedFrame` e `ObservationMatchesCurrentPipeline`. Dopo un cambio di pipeline o di asset configuration, l'observation precedente è stale; conserva warning e non-match e non interpretare i suoi valori di pass, mode, GRD o VRS come correnti. La API legacy `PerformanceMeter.GetRenderGraphSnapshot()` e il comando `perfmeter.rendergraph.snapshot` restano disponibili.

Per diagnosticare GRD, controlla `DegradedReason`, supporto SRP, configurazione del progetto, supporto compute, compatibilità del modo URP e `ActivityAvailability`. `IsObservedActive` è lo stato enabled globale di Unity. Usa `Effectiveness` solo come contesto BRG aggregato: `AvailableNoSample`/`Unavailable` non significano workload zero e counter BRG positivi non provano l'uso GRD di un renderer specifico.

Nel capture bundle, lo schema `sgg.perfmeter.capture-context` versione `1` conserva `render` e aggiunge `render_integration`. Per un external GPU capture, il context viene congelato al primo sample della fase `Capturing`; un bundle Memory Profiler lo registra quando la richiesta di memoria termina. Gli schema JSON/CSV di sessione non cambiano. La API pubblica non offre un viewer stabile RenderGraph/CustomPass né pass target, quindi questo workflow non promette navigazione nell'Editor.
