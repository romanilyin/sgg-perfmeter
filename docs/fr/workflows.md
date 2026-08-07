# Workflows

## Overlay Runtime

Utilisez l'overlay lorsque vous avez besoin d'une visibilite immediate dans le jeu.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

L'overlay utilise UI Toolkit et n'intercepte pas les entrees de gameplay. Il prend en charge FPS-only, texte compact, graphes, diagnostics complets, barres de metriques, themes visuels, filtres de modules, graphes CPU/GPU, widgets de coeurs CPU et un nombre limite de lignes de metriques personnalisees.

PerfMeter cree et possede un host UI Toolkit versionne pour l'overlay : Unity `6000.4` utilise `UIDocument`, tandis que Unity `6000.5+` utilise `PanelRenderer`. Ce host est separe de l'UI etrangere et preserve ses panel settings et children ; les rebuilds suppriment uniquement le container appartenant a PerfMeter.

## Collecte En Arriere-Plan

Utilisez le mode arriere-plan pour les tests, les executions sur appareil ou les workflows d'agents ou l'interface visible n'est pas necessaire.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Enregistrement Et Export De Session

Utilisez les sessions pour des fenetres de profilage reproductibles.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Les exports de session incluent timing, FPS lows, spikes, comptes de goulets d'etranglement, compteurs de rendu, compteurs memoire, etat d'overdraw, disponibilite des avertissements/compteurs, resumes de scenes, pires frames, metadonnees d'appareil, metadonnees de camera, metadonnees de reglages et metriques personnalisees.

## Alertes

Les regles peuvent signaler des violations de budget, des FPS faibles, un timing GPU indisponible et des seuils d'overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Les avertissements Editor sont limites par des cooldowns et peuvent etre desactives via les reglages JSON ou les controles runtime. Les logs d'alertes structurees et les avertissements Editor sont independants : `PerformanceMeter.SetStructuredLogsEnabled(false)` supprime uniquement la sortie `Debug.Log` des alertes structurees, tandis que `PerformanceMeter.SetEditorWarningLogsEnabled(false)` controle separement les logs d'avertissement Editor. Les callbacks, alerts/history, avertissements de l'overlay et sessions restent actifs.

## External GPU Capture

Utilisez le capture coordinator pour une requete RenderDoc ou PIX bornee lorsque l'outil est deja attache:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

Le coordinator n'autorise qu'une seule requete active et avance de maniere deterministe dans `PreRoll`, `Capturing`, `PostRoll` et `Completed`. La meme ID active est idempotente; une ID differente est rejetee comme chevauchement. Le pre-roll et le post-roll comptent les frames Unity; seul `Capturing` ouvre le alert capture scope et invoque l'`ExternalGPUProfiler` experimental de Unity. Les gates obligatoires sont l'Editor ou un Development Build et un outil attache. `RenderDoc` est autorise sur desktop Windows/Linux avec Direct3D 11, Direct3D 12 ou Vulkan; `PIX` est autorise sur desktop Windows avec Direct3D 12.

`Completed` signifie uniquement que le wrapper lifecycle protege est termine. Unity n'expose ni l'identite de l'outil attache ni un path d'artefact faisant autorite; `Status.Tool` est uniquement l'outil demande. L'overload avec `PerfMeterCaptureBundleOptions` separe les samples baseline/capture et exporte atomiquement un bundle local au projet; un artefact externe reste observe, non autoritatif. Pour l'automatisation, utilisez `perfmeter.capture.request/status/cancel/export/capabilities`.

## Diagnostics D'overdraw

L'overdraw numerique est opt-in et borne.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

L'overdraw numerique et la heatmap utilisent le diagnostic path URP Render Graph. La mesure d'overdraw necessite `PerfMeterRenderGraphFeature`, la prise en charge des replacement shaders, la prise en charge fragment UAV/storage-buffer, la prise en charge des compute shaders, une API graphique prise en charge et async GPU readback. HDRP signale overdraw/heatmap comme unsupported, tandis que les core overlay, session, API et MCP diagnostics restent disponibles. Les cibles non prises en charge signalent `OverdrawState.Unsupported` au lieu d'executer la passe.

## Reproductibilite Camera Et Device

Utilisez les snapshots pour conserver l'environnement qui a produit une capture de performance.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Les exports de session incluent les metadonnees de device et de camera pour comprendre ou reproduire une capture plus tard.

## Metriques Personnalisees

Enregistrez des providers propres au projet sans forker PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Les metriques personnalisees sont exposees par les lectures API, l'export JSON de session, les dernieres metriques MCP et jusqu'a huit lignes d'overlay lorsque le module `CustomMetrics` est active.

## Instrumentation Unity Profiler

L'instrumentation est interne et visible uniquement pendant le profilage de l'Editor, d'un Development Build ou d'un autre build avec Profiler active. Dans les Release players sans Profiler, ces markers/counters sont no-op et ne produisent aucune donnee d'instrumentation; les schemas public API, status, MCP et export restent inchanges.

- Les markers couvrent collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) et export JSON/CSV (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` est un hook interne reserve pour les providers.
- Les counters couvrent les temps de frame CPU/GPU (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) comme des gauges de fin de frame en nanosecondes. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` et `SGG.PerfMeter.Thermal.Available` codent disponibilite/actif en `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` et `SGG.PerfMeter.Capture.State` utilisent des codes d'enum; `SGG.PerfMeter.Provider.CustomMetricCount` est un count. Tous les counters utilisent la categorie `Scripts` et `FlushOnEndOfFrame`.
- Aucun sample thermique synthetique n'est emis; `SGG.PerfMeter.Thermal.Available` reste a `0`/indisponible jusqu'a ce qu'un provider de plateforme reel fournisse des donnees.

## Self-Observability Et Budgets D'Overhead

Utilisez `PerformanceMeter.GetSelfOverhead()` ou `PerformanceMeter.GetStatus().SelfOverhead` pour diagnostiquer le cout des callbacks CPU et les allocations du collector, des custom providers, du CPU-core provider, de l'overlay et de l'integration URP/HDRP. La mesure utilise des fenetres fixes de 120 frames, des moyennes par invocation et des budgets CPU/allocation propres a chaque composant.

L'integration render inactive signale `Unsupported`, un composant pris en charge sans appel signale `NotMeasured` et le self-timing GPU signale `Unavailable`. L'accounting est uniquement diagnostique: PerfMeter ne soustrait aucun overhead et n'ajuste pas les metriques CPU/GPU existantes.

## Automatisation Par Agents

Execution typique pilotee par MCP:

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

`perfmeter.profiler.capabilities {}` est une lecture du cache; elle ne demarre ni le runtime ni la discovery.

## Workflow de snapshot mémoire optionnel

1. Utilisez Unity `6000.4+` et installez `com.unity.memoryprofiler` `1.1.0+` avec le Package Manager. L'assembly optionnelle `SGG.PerfMeter.MemoryProfiler` enregistre alors automatiquement le backend; sans ce package, l'intégration core reste unavailable.
2. En Play Mode, lisez `PerformanceMeter.GetMemorySnapshotCapabilities()` ou `perfmeter.memory.snapshot.capabilities` et vérifiez la disponibilité du backend et des flags demandés.
3. Demandez un snapshot manuel avec `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))`, ou configurez `ConfigureMemorySnapshotTriggers(...)` pour activer explicitement un seuil de mémoire système ou une fenêtre bornée de croissance de fuite.
4. Consultez `GetMemorySnapshotStatus()` ou `perfmeter.memory.snapshot.status` jusqu'à ce que le snapshot et son bundle corrélé atteignent un état terminal. Exportez l'evidence prête avec `PerformanceMeter.ExportCaptureBundle(captureId)` ou `perfmeter.capture.export`.

L'evidence uniquement mémoire passe par l'API existante des capture bundles sous `Temp/PerfMeter/CaptureBundles`. Le bundle indique `MemoryProfiler` comme outil demandé, contient la provenance mémoire et un SHA-256 en streaming du `.snap`, et ne contient aucun artefact GPU externe. La source possédée se trouve sous `Temp/PerfMeter/MemorySnapshots`; un export réussi ne l'utilise qu'une fois.

## Diagnostic des marqueurs graphiques

1. Appelez `PerformanceMeter.GetGraphicsDiagnostics()` ou `perfmeter.graphics.diagnostics` pour lire les dernières valeurs des marqueurs et le contexte de l'API graphique.
2. Vérifiez pour chaque capability `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, les component counts résolus/échantillonnés et la révision du catalogue. La discovery est dynamique: elle a lieu au démarrage du runtime et lors d'un refresh/reconfigure explicite du catalogue du profiler.
3. Traitez les valeurs comme des valeurs brutes du recorder dans leurs units découvertes. Un marqueur peut être unavailable, disponible sans sample ou sampled; zéro n'est pas un signal universel d'unavailability et la valeur n'est pas nécessairement un count de shader ou de PSO.

Le shader marker résout d'abord exactement `Shader.CreateGPUProgram`, puis les alias `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` et `Shader.DynamicLoadGPUProgram`. Le pipeline marker résout exactement `CreatePSO.Job`. Les mêmes valeurs et provenance sont disponibles avec `perfmeter.metrics.latest` et les JSON/CSV de session.

## Trace et prewarm GraphicsStateCollection

1. Sous Unity `6000.4+`, vérifiez que l'assembly optionnelle `SGG.PerfMeter.GraphicsStateCollection` est disponible. Elle utilise le namespace `UnityEngine.Experimental.Rendering.GraphicsStateCollection` sous Unity `6000.4` et `UnityEngine.Rendering.GraphicsStateCollection` sous Unity `6000.5+`.
2. Démarrez une session PerfMeter avant le trace. Appelez `StartSession(...)`, puis `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` ou la demande MCP correspondante. Sans session active, la demande est rejetée; la session doit rester en enregistrement jusqu'à la fin du trace, et `PerformanceMeter.StopSession()` annule un trace actif.
3. Laissez le scénario s'exécuter pendant que le trace borné avance. En Play Mode normal, chaque trace frame est tickée après `WaitForEndOfFrame`; en batch mode, le coordinator utilise un fallback au frame suivant. Les samples de session admis pendant cet intervalle portent `GraphicsStateTraceId`/`graphics_state_trace_id`; les réglages de session déterminent le nombre de samples corrélés conservés.
4. Interrogez `GetGraphicsStateCollectionStatus()` ou `perfmeter.graphics.state_collection.status` jusqu'à `Completed`, puis arrêtez la session si nécessaire. Un arrêt pendant le trace actif l'annule et peut laisser `IsBusy`/`is_busy` à true pendant le retry du cleanup owned. L'artefact `.graphicsstate` owned est relatif au projet, sous `Temp/PerfMeter/GraphicsStateCollections`, et limité à 64 Mio.
5. Passez le chemin relatif owned indiqué à `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` ou à la commande MCP de prewarm. Le prewarm est synchrone, conserve l'artefact et indique les warmups terminés et `IsWarmedUp`; un progressive warmup peut se terminer avec un warning explicite d'incomplétude.

Le coordinator graphics-state n'admet qu'un flight et rejette aussi l'overlap avec un external GPU capture, un memory snapshot ou un alert-capture actifs. Le même trace ID actif renvoie `AlreadyActive`; un autre ID renvoie `RejectedOverlap`. `CancelGraphicsStateTrace` n'annule qu'un trace actif/en préparation correspondant et nettoie son artefact en attente. Si l'artefact owned ne peut pas être supprimé, `HasPendingCleanup`/`has_pending_cleanup` reste true, un sidecar voisin `.delete-pending` est conservé puis restauré et retenté après un domain reload; `IsBusy`/`is_busy` et le warning restent visibles jusqu'à la réussite. Le backend Unity ne prend pas en charge le cache-miss tracing: aucune evidence de cache-miss n'est disponible.

## Contexte d'intégration du rendu

Utilisez le snapshot neutre lorsqu'une vue indépendante du pipeline sur la dernière render integration typée est nécessaire:

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

Les mêmes données sont accessibles par MCP:

```text
perfmeter.render.snapshot {}
```

Ces lectures ne démarrent pas la collecte du runtime. Vérifiez ensemble `State`, `ObservationAgeFrames`, `LastObservedFrame` et `ObservationMatchesCurrentPipeline`. Après un changement de pipeline ou de configuration d'asset, l'observation précédente est obsolète; conservez le warning et le non-match et ne considérez pas ses valeurs de pass, mode, GRD ou VRS comme actuelles. L'API legacy `PerformanceMeter.GetRenderGraphSnapshot()` et la commande `perfmeter.rendergraph.snapshot` restent disponibles.

Dans le bundle de capture, le schema `sgg.perfmeter.capture-context` version `1` conserve `render` et ajoute `render_integration`. Pour un external GPU capture, le contexte est figé au premier sample de la phase `Capturing`; un bundle Memory Profiler l'enregistre à la fin de la requête mémoire. Les schemas JSON/CSV de session ne changent pas. L'API publique ne fournit pas de viewer stable RenderGraph/CustomPass ni de pass targets; ce workflow ne promet donc pas de navigation dans l'Editor.
