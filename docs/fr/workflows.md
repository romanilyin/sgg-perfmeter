# Workflows

## Configuration FTUE Et Continuations

Ouvrez `SGG/Perfmeter/Setup` et selectionnez l'onglet **FTUE**. Les verifications requises couvrent la compatibilite, l'integration de rendu, Frame Timing Stats, le chemin du package et un JSON de reglages charge. Les lignes optionnelles peuvent etre installees ou ignorees ; une ligne installee affiche l'action suivante au lieu d'affirmer silencieusement que le workflow est termine.

### Memory Profiler

Apres l'installation de `com.unity.memoryprofiler`, la ligne **Memory Profiler** propose **Open Window/Analysis/Memory Profiler**, **Copy RequestMemorySnapshot Snippet**, **Copy Memory Trigger Snippet**, **Open Runtime** et **Reveal Snapshots** une fois que le dossier gere existe. Les snippets copies sont du code runtime que le projet doit appeler ; FTUE ne demande pas lui-meme de snapshot et ne configure pas les triggers. Les fichiers `.snap` one-shot sont places sous `Temp/PerfMeter/MemorySnapshots` ; ouvrez ou copiez le resultat avant qu'une requete ulterieure ou le nettoyage runtime ne supprime la source geree.

Le snippet one-shot est :

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

Le snippet de trigger opt-in est :

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

Utilisez **Open Runtime** pour examiner le snapshot de capacites/statut. La capture manuelle est le comportement par defaut ; les seuils de trigger restent desactives jusqu'a leur configuration explicite.

### Profile Analyzer

La ligne **Profile Analyzer** installee propose **Open Profile Analyzer** et **Open Runtime**. Commencez d'abord l'enregistrement dans Unity Profiler, puis demarrez et arretez une session PerfMeter dans cet enregistrement. L'ouvreur utilise `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()` pour ouvrir Profile Analyzer et copier l'ID de session ; chargez les donnees Profiler enregistrees et recherchez cet ID. Il n'installe pas Profile Analyzer, ne charge pas les donnees Profiler et n'applique pas automatiquement de filtre.

### Adaptive Performance

La ligne **Adaptive Performance** installee propose **Open Runtime** pour examiner le statut actuel du provider de telemetrie optionnel. L'action FTUE ne demarre aucune session et ne capture rien.

### RenderDoc

RenderDoc est un outil externe et n'est pas fourni avec PerfMeter. Suivez le flux d'integration officiel de Unity :

1. Installez RenderDoc depuis la page officielle de telechargement : <https://renderdoc.org/builds>.
2. Enregistrez les modifications du projet, puis utilisez **Load RenderDoc** dans le menu de l'onglet Game View ou Scene View. Vous pouvez aussi lancer l'Unity Editor ou un Development Build via RenderDoc ; redemarrez Unity si Unity n'expose pas l'attachement apres l'installation. Le guide officiel Unity est <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html>.
3. Cliquez sur **Check Attachment** dans FTUE. Cela actualise uniquement le signal partage des external profilers de Unity ; FTUE ne peut pas detecter l'installation de RenderDoc et Unity ne peut pas distinguer RenderDoc de PIX avec ce signal.
4. Cliquez sur **Copy Capture Snippet**, passez en Play Mode et appelez le code copie depuis le code runtime du projet :

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions("ftue-renderdoc-capture", PerfMeterCaptureTool.RenderDoc, 1));
   ```

5. Dans l'Editor Windows x64, vous pouvez d'abord utiliser **Download Verified Bridge** ou **Install Local Bridge**; seul le bridge separe exactement epingle est installe comme plugin Editor-only, jamais RenderDoc. Redemarrez l'Editor. La requete native copiee utilise `NativeRequired` + `Copy`; MetadataOnly est `DoNotShare` et Copy/Embed sont `ReviewBeforeShare`.

### GraphicsStateCollection

La ligne optionnelle **GraphicsStateCollection** incluse ne necessite aucune installation de package. Elle propose **Open Runtime**, **Copy Trace Snippet**, **Copy Prewarm Snippet** et **Reveal Artifacts**. FTUE ne demande automatiquement ni trace ni prewarm. Suivez cette sequence :

1. En Play Mode, demarrez et laissez active une session PerfMeter avec `PerformanceMeter.StartSession(...)`.
2. Appelez le code de trace copie depuis le code runtime du projet :

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. Interrogez `PerformanceMeter.GetGraphicsStateCollectionStatus()` jusqu'a `State == PerfMeterGraphicsStateCollectionState.Completed`. Utilisez son `ArtifactRelativePath`, qui pointe sous `Temp/PerfMeter/GraphicsStateCollections`, comme entree du prewarm. Arreter la session pendant le tracing annule le trace.
4. Remplacez `<trace-artifact-file>` dans le snippet de prewarm copie par le chemin renvoye :

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. Cliquez sur **Reveal Artifacts** apres un trace pour afficher le dossier d'artefacts local au projet. Le prewarm est synchrone, conserve l'artefact et peut signaler un rechauffement progressif incomplet. La longueur du trace est limitee a 600 frames et les artefacts geres a 64 MiB ; le backend Unity ne fournit pas de preuve de cache misses.

## Bootstrap D'initialisation Complet

Dans **Setup > Initialization Code**, cliquez sur **Refresh from Project Settings**, puis sur **Copy Init Code**. Le `PerfMeterBootstrap` genere integre le snapshot complet et normalise des reglages du projet et appelle `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)` apres le chargement de la scene. Il transporte les reglages d'overlay, de logs, d'alertes, de session par defaut et d'overdraw, respecte `enabled` et `collectionMode: Stopped`, et n'effectue ni `StartSession` ni requete de capture.

Utilisez ce bootstrap explicite plutot que le chemin Resources de reglages sans code lorsque le demarrage gere par le code est prefere. Si les deux sont presents, un appel explicite parse avec succes supprime le callback Resources auto-start pour le domaine courant ; si Resources a deja demarre en premier, le snapshot explicite est applique ensuite et devient authoritative. Un JSON explicite invalide laisse le runtime courant inchange et ne supprime pas un Resources auto-start ulterieur. Les operations de session et d'overdraw par defaut utilisent le snapshot runtime explicite actif.

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

`GenericUnity` conserve la matrice precedente d'`ExternalGPUProfiler` et ne peut authentifier outil/artefact. `NativePreferred` ne peut fallback qu'avant begin; `NativeRequired` jamais. Native RenderDoc est pris en charge uniquement dans l'Editor Unity Windows x64 avec D3D11, D3D12 ou Vulkan.

Generic `Completed` reste seulement le wrapper lifecycle. Le statut natif indique backend kind et generation-bound phase et peut authentifier un `.rdc` finalise. Les artefacts generic/caller restent observes. MCP accepte `backend_mode`, mais le storage mode est choisi via l'API C#.

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

## Corrélation De Session Avec Profile Analyzer

Pendant le profilage, chaque session émet les samples instantanés `SGG.PerfMeter.Session.<sessionId>.Begin` et `.End`. `SGG/Perfmeter/Open Profile Analyzer For Session` ouvre la fenêtre Profile Analyzer facultative et copie l'ID de la session courante dans le presse-papiers. La commande n'installe pas Profile Analyzer, ne charge pas les données du Profiler et n'applique pas de filtre automatiquement ; après avoir chargé la capture concernée, recherchez l'ID copié.

## Fenêtre D'Analyse De Session

Ouvrez `SGG/Perfmeter/Session Analysis` pour consulter en lecture seule, dans l'Editor, la session courante en mémoire. Les onglets virtualisés affichent la timeline des samples conservés, la worst frame faisant autorité avec les détails du sample disponibles, les violations dérivées des budgets CPU-main/CPU-render/GPU et les scopes whole-run/current-scene faisant autorité. CPU-main exclut present wait ; les valeurs et violations GPU exigent une disponibilité explicite du timing GPU.

La fenêtre lit uniquement `GetSessionSummary()` et `GetSessionSamples()` et ne démarre jamais le runtime. Un timing indisponible est affiché comme `Unavailable`, pas comme un zéro numérique. Une session arrêtée reste visible tant que son instance runtime existe ; `PerformanceMeter.Stop()`, un domain reload ou la sortie du Play Mode peuvent supprimer cette session en mémoire.

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

Pour diagnostiquer GRD, vérifiez `DegradedReason`, le support SRP, la configuration du projet, le support compute, la compatibilité du mode URP et `ActivityAvailability`. `IsObservedActive` est l'état enabled global de Unity. Utilisez `Effectiveness` uniquement comme contexte BRG agrégé: `AvailableNoSample`/`Unavailable` ne signifient pas une charge nulle, et des compteurs BRG positifs ne prouvent pas l'utilisation GRD d'un renderer précis.

Dans le bundle de capture, le schema `sgg.perfmeter.capture-context` version `1` conserve `render` et ajoute `render_integration`. Pour un external GPU capture, le contexte est figé au premier sample de la phase `Capturing`; un bundle Memory Profiler l'enregistre à la fin de la requête mémoire. Les schemas JSON/CSV de session ne changent pas. L'API publique ne fournit pas de viewer stable RenderGraph/CustomPass ni de pass targets; ce workflow ne promet donc pas de navigation dans l'Editor.
