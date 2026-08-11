# API Runtime

Namespace:

```csharp
using SGG.PerfMeter;
```

Toutes les API de lecture sont sures avant le demarrage du runtime. Les lectures renvoient des snapshots arretes/par defaut au lieu de lever une exception parce que le runtime n'est pas actif.

## Cycle De Vie

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Modes de collecte:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Etat Et Metriques

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Groupes de metriques principaux:

- FPS: moyenne, 1% low, 0.1% low, nombre de spikes.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame quand disponible.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: memoire systeme/application, memoire reservee GC, memoire GPU quand disponible.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced ou unknown.
- Overdraw: etat, progression, ratio et visibilite de heatmap.

La disponibilite des compteurs est exposee par `AvailableCounters`, `UnavailableCounters` et les avertissements.

## Self-Observability Et Budgets D'Overhead

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
PerfMeterSelfOverheadWindowSnapshot sessionOverhead = PerformanceMeter.GetSelfOverheadWindow(
    PerfMeterSelfOverheadWindowKind.Session,
    PerformanceMeter.GetSessionSummary().SessionId);
```

La self-observability publie des mesures low-overhead du cout des callbacks CPU dans des fenetres fixes de 120 frames. Les moyennes sont calculees par invocation. L'etat global est `NotInitialized`, `Collecting` ou `Ready`; l'etat d'un composant est `NotMeasured`, `Collecting`, `Ready` ou `Unsupported`.

Les composants sont `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` et `HdrpRenderIntegration`. Chacun expose les nombres de frames/invocations, les millisecondes CPU moyennes/maximales, les allocations totales/moyennes, les budgets et les etats `NotEvaluated`/`WithinBudget`/`Exceeded`. La provenance additive inclut l'epoch, les premiere/derniere frames mesurees, le nombre de callback frames, une raison d'inactivite typee et la disponibilite explicite de l'attribution GPU.

`GetSelfOverheadWindow(...)` renvoie l'observation URP liee exactement a une session ou capture, avec identite, epoch, limites de frames, containment et evidence quality/pipeline/renderer ainsi que feature installed/enabled/enqueued. Les resultats inactifs utilisent des raisons typees comme `RendererFeatureNotInstalled`, `RendererFeatureDisabled`, `PassNotEnqueued`, `NoCameraCallbackObserved`, `WindowIncomplete` ou `CaptureWindowMismatch`; une evidence manquante renvoie `UnknownInactiveReason`. Une capture ulterieure ne peut pas reutiliser une epoch terminee.

| Composant | Budget CPU | Budget d'allocation |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

Le scope URP mesure uniquement l'enregistrement CPU-side de `RecordRenderGraph()` appartenant au package et l'allocation du current thread. Avec plusieurs cameras, les invocations peuvent depasser les callback frames. L'attribution GPU est explicitement `Unavailable`; CPU, GPU, hitches et GC whole-frame restent du contexte et ne sont pas attribues a PerfMeter par proximite temporelle. Ces diagnostics ne soustraient rien aux metriques CPU/GPU existantes et ne les ajustent pas.

## Catalogue Dynamique Des Metriques Profiler

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` et `GetProfilerMetricCapabilities()` lisent le catalogue en cache. L'etat du catalogue est `NotInitialized`, `Ready` ou `Error`; chaque capability indique `Unavailable`, `AvailableNoSample` ou `AvailableSampled`, et `Resolution` donne la provenance `None`, `Exact` ou `Alias`. La discovery s'effectue uniquement au demarrage du runtime et lors d'un refresh/reconfigure explicite, pas pendant la collecte steady-state. Les valeurs numeriques existantes restent des valeurs de compatibilite; utilisez `SampleState`/`IsAvailable` de la capability comme signal d'autorite pour la disponibilite.

## Snapshots Structures

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Les snapshots de device incluent des informations Unity/platform/OS/CPU/GPU/API/display/window/support. Les snapshots de camera incluent scene, transform, projection, clipping, pixel rect, target display et reglages de camera URP/HDRP quand disponibles.

## Charges Des Coeurs CPU

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Chaque snapshot expose `CoreIndex`, `LoadPercent` et `Available`. Le tableau peut etre vide avant le demarrage runtime, pendant le warm-up du sampler ou sur les plateformes non prises en charge; traitez cela comme une information de capacite de plateforme, pas comme un echec d'appel API.

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

Les anciens modes d'overlay et les flags de modules semantiques restent disponibles pour la compatibilite et le filtrage.

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

Les options de session incluent frames/secondes de warm-up, intervalle d'echantillonnage, nombre maximal d'echantillons, reset-on-scene-load et fenetres d'ignore de chargement de scene.

## Alertes

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` vaut `true` par defaut et controle uniquement la sortie `Debug.Log` des alertes structurees. La valeur `false` ne desactive ni les callbacks `AlertFired`, ni les alertes recentes ou l'historique des alertes, ni les avertissements de l'overlay, ni les logs d'avertissement Editor, ni les sessions. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` controle independamment les logs d'avertissement Editor.

## Editor Compatibility Status

L'API Editor `PerfMeterSetupActions.GetCompatibilityStatus()` renvoie `PerfMeterCompatibilityStatus` et separe `ImportCompatible` pour le floor Unity `2022.3`, `CoreRuntimeCompatible` pour le runtime pris en charge Unity `6000.4+`, et `RenderIntegrationCompatible` pour URP/HDRP actif `17.4+` avec adapter disponible. Chaque resultat contient une raison. La compatibilite render ne signifie pas que les renderer assets sont configures; utilisez setup status pour la configuration.

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

Le coordinator autorise une seule requete active et avance de maniere deterministe dans `PreRoll`, `Capturing`, `PostRoll` et `Completed`. Rejouer la meme ID active est idempotent; une autre ID active est rejetee pour chevauchement. `Canceled`, `Unavailable` et `Error` sont des etats terminaux explicites.

`PerfMeterCaptureBackendMode.GenericUnity` reste le default de compatibilite pour `ExternalGPUProfiler`; l'identite de l'outil et de l'artefact n'est pas authentifiee. `NativePreferred` demande le bridge optionnel Windows x64 Editor et ne peut fallback qu'avant native begin; `NativeRequired` ne fallback jamais. Le chemin natif prend en charge D3D11, D3D12 et Vulkan. Le statut indique `RequestedBackendMode`, `EffectiveBackendKind`, `NativePhase`, result code et fallback reason.

Les valeurs par defaut de `PerfMeterCaptureOptions` sont `captureFrames: 1`, `preRollFrames: 0` et `postRollFrames: 0`. Un `RequestCapture` valide demarre automatiquement le runtime. `CancelCapture()` sans ID annule la requete active actuellement rapportee; passer une ID protege contre l'annulation d'une requete plus recente.

Les `.rdc`/`.wpix` generiques ou fournis par le caller restent observes. Seul le descriptor natif lie a la generation peut authentifier un `.rdc` finalise. Native MetadataOnly utilise `DoNotShare`; Copy/Embed ont des quotas separes et `ReviewBeforeShare`. Traversal, reparse points et fichiers hors des owned roots sont rejetes.

L'export du capture bundle propose aussi une API single-flight non bloquante : `RequestCaptureBundleExport(..., out exportId)`, `GetCaptureBundleExportStatus(exportId)` et `CancelCaptureBundleExport(exportId)`. Le statut indique phase, progression, octets, annulation, nouvelle tentative, chemin de commit et enveloppe generique d'artefact externe. L'API existante `ExportCaptureBundle(...)` reste un wrapper de compatibilite bloquant, tandis que serialisation, E/S de fichiers, hashing, retention et commit atomique s'executent dans un worker thread.

Les JSON de session et capture ajoutent des evenements de timeline types pour les samples manquants et les limites de capture. Les versions de schema, tableaux de samples et colonnes CSV existants restent compatibles; les payloads legacy ou inconnus sont lus sans inventer de gaps. Les providers de custom metrics utilisent un provider snapshot en cache et un buffer reutilisable appartenant au core sur le warmed collection path; les copies ne sont creees que pour les samples conserves, les exports et les public snapshots. La coordination du Profiler est locale au processus via `GetProfilerLeaseCapabilities()`, `GetProfilerLeaseStatus()`, `TryAcquireProfilerLease(...)` et `ReleaseProfilerLease(...)`; les leases detenues ne survivent pas a un domain reload.

## Metriques Personnalisees

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Les exceptions des providers sont signalees comme snapshots de metriques personnalisees indisponibles et n'interrompent pas la collecte des metriques principales.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Les diagnostics d'overdraw sont des modes de diagnostic explicites et peuvent ajouter du travail GPU. En HDRP, ces APIs signalent en securite unsupported state pour overdraw et heatmap, sans promettre HDRP heatmap output.

## Snapshots mémoire optionnels

Les snapshots mémoire sont une intégration optionnelle. Sous Unity `6000.4+`, `com.unity.memoryprofiler` `1.1.0+` active l'assembly séparée `SGG.PerfMeter.MemoryProfiler`, qui enregistre automatiquement le backend `MemoryProfiler`. L'assembly core n'a aucune dépendance obligatoire.

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

La surface publique comprend `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` et `GetMemorySnapshotTriggers()`. Un backend personnalisé implémente `IPerfMeterMemorySnapshotBackend`; l'assembly optionnelle fournit le backend Unity Memory Profiler.

`PerfMeterMemorySnapshotOptions` utilise par défaut les flags d'objets managed/native, 1 Gio d'espace disque libre minimum et un cooldown de 300 secondes. `RequestMemorySnapshot` est manuel par défaut et renvoie des résultats explicites tels que `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest` ou `Failed`. Les lectures ne démarrent pas le runtime; une requête valide le fait.

`ConfigureMemorySnapshotTriggers` active explicitement l'heuristique de seuil de mémoire système et de croissance de fuite bornée. `GetMemorySnapshotTriggers()` est désactivé par défaut. Les requêtes déclenchées utilisent les mêmes protections single-flight, cooldown, espace libre et capture-flags que les requêtes manuelles.

## Diagnostic graphique et GraphicsStateCollection

Le diagnostic graphique ajoute des données aux snapshots existants. `PerformanceMeter.GetGraphicsDiagnostics()` renvoie les dernières valeurs des marqueurs de création de programmes GPU de shader et de graphics pipeline, avec le contexte de l'API graphique, la capacité PSO parallèle et la révision du catalogue de métriques du profiler.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

Le catalogue découvre les descripteurs `ProfilerRecorder` de Unity au démarrage du runtime et lors d'un refresh/reconfigure explicite. Pour le shader, il utilise le nom exact `Shader.CreateGPUProgram` et les alias `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` et `Shader.DynamicLoadGPUProgram`. Pour le graphics pipeline, il utilise le nom exact `CreatePSO.Job`. Chaque capability conserve `Resolution` (`None`, `Exact` ou `Alias`), `ResolvedRecorderNames`, `Category`, les valeurs découvertes `Unit` et `DataType`, ainsi que `ResolvedComponentCount` et `SampledComponentCount`. `PerfMeterMetricsSnapshot` et les JSON/CSV de session contiennent les mêmes valeurs de marqueurs, métadonnées de capability et révision du catalogue.

La disponibilité des marqueurs est dynamique. Utilisez `SampleState` (`Unavailable`, `AvailableNoSample` ou `AvailableSampled`) et les métadonnées de capability; une valeur nulle ne prouve pas l'absence du marqueur. Les valeurs sont des valeurs brutes du recorder et conservent l'unité découverte: elles ne sont pas universellement des counts de shaders ou de PSO et PerfMeter ne les convertit pas vers une unité commune.

L'assembly optionnelle `SGG.PerfMeter.GraphicsStateCollection` cible Unity `6000.4+` et enregistre le backend Unity lorsqu'il est disponible. Elle utilise `UnityEngine.Experimental.Rendering.GraphicsStateCollection` sous Unity `6000.4` et `UnityEngine.Rendering.GraphicsStateCollection` sous Unity `6000.5+`. L'assembly core reste indépendante de ce backend.

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

La surface publique de state collection comprend `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)` et `CancelGraphicsStateTrace(string captureId)`. Un backend personnalisé implémente `IPerfMeterGraphicsStateCollectionBackend` et indique ses capacités de trace/prewarm, cache-miss et PSO parallèle.

`PerfMeterGraphicsStateTraceOptions` exige un `CaptureId` non vide, accepte 1–600 trace frames et utilise par défaut 60 frames et 1 Gio d'espace libre minimum. Un trace n'est valide que pendant l'enregistrement d'une session PerfMeter. Les samples de session corrélés portent l'ID de capture actif dans `GraphicsStateTraceId` (`graphics_state_trace_id` dans les exports). Les paramètres de sampling de la session contrôlent la densité des samples corrélés, pas le nombre de trace frames demandé.

`PerfMeterGraphicsStateCollectionStatusSnapshot` expose `IsBusy` et `HasPendingCleanup`. `IsBusy` vaut true pendant la préparation, le trace, la fin du trace, le prewarm, le cleanup ou un cleanup en attente persisté; `HasPendingCleanup` identifie précisément un artefact owned qui attend une nouvelle tentative de cleanup. Si `PerformanceMeter.StopSession()` est appelé pendant un trace actif, il annule ce trace; la session doit donc rester en enregistrement jusqu'à la fin du trace. En cas d'échec de suppression, un sidecar owned `.delete-pending` est créé à côté de l'artefact; après un domain reload, le marker est restauré et le cleanup est retenté. Le status reste visible et busy jusqu'à la suppression de l'artefact et du marker.

Le coordinator n'autorise qu'un seul graphics-state flight. Le même ID actif renvoie `AlreadyActive`; un autre trace ou prewarm pendant la préparation, le trace, la finalisation, le cleanup ou un autre domaine de capture renvoie `RejectedOverlap`. `CancelGraphicsStateTrace` ne correspond qu'à l'ID actif ou en préparation, annule le backend et supprime l'artefact owned en attente. Les échecs de cleanup restent visibles et peuvent bloquer le remplacement jusqu'à une nouvelle tentative réussie.

`PerfMeterGraphicsStatePrewarmOptions` accepte uniquement un chemin `.graphicsstate` owned relatif au projet et un `MaxStateCount` optionnel de 0 à 1 000 000. Le prewarm est synchrone, conserve l'artefact et indique `CompletedWarmupCount` et `IsWarmedUp`; un progressive warmup réussi mais incomplet ajoute un warning. `TraceCacheMisses` est présent pour les backends extensibles, mais le backend Unity ne prend pas en charge l'evidence de cache-miss: cette demande renvoie `Unavailable`.

## Contexte d'intégration du rendu

Le snapshot additif et neutre vis-à-vis de l'intégration est disponible via les deux méthodes:

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` expose `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph` et `Warning`. Les snapshots imbriqués GRD et VRS exposent leur availability, leurs champs de configuration/support, l'activity availability et leurs warnings.

Les lectures sont sûres avant le démarrage du runtime et ne lancent pas la collecte. Un pipeline courant supporté peut être `Available` avec `State = NotObserved`; si la dernière observation appartient à une autre configuration de pipeline, `ObservationMatchesCurrentPipeline` vaut `false`, frame/age restent explicites et le warning signale des données obsolètes. Ne traitez pas ces champs obsolètes comme une observation actuelle.

URP utilise le `UniversalRenderingData.renderingMode` public de la frame courante et indique les passes PerfMeter effectivement planifiés pour cette frame. HDRP indique le `CustomPass` PerfMeter réellement observé, mais le effective rendering mode est indisponible. `GpuResidentDrawer` indique le mode configuré, le support SRP/projet/compute, Forward+ et la compatibilité du mode clustered de la frame URP, ainsi que l'activité runtime globale via `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()`. Sous HDRP, les champs Forward+/rendering mode restent `Unknown`. `VariableRateShading` indique le support matériel faisant autorité de `SystemInfo`/`ShadingRateInfo`; configuration et activité restent `Unknown` sauf si un typed adapter les démontre.

`LegacyRenderGraph` est une façade de compatibilité intégrée pour `GetRenderGraphSnapshot()`. La reflection privée/interne des passes et ressources a été supprimée: les legacy counters restent donc à `-1`. L'API publique stable de Unity n'expose pas non plus de viewer RenderGraph/CustomPass ni de pass targets; cette API ne promet pas de navigation dans l'Editor.

`GpuResidentDrawer` ajoute `ProjectConfigurationAvailability`, `IsProjectConfigurationSupported`, `ComputeShaderAvailability`, `SupportsComputeShaders`, `ForwardPlusActivityAvailability`, `IsObservedForwardPlusActive`, `RenderingModeCompatibilityAvailability`, `IsRenderingModeCompatible`, `ActivitySource`, `DegradedReason` et `Effectiveness`. `PerfMeterGpuResidentDrawerReason` fournit des états de fallback structurés. `PerfMeterGpuResidentDrawerEffectivenessSnapshot` contient les draw calls/instances BRG et la provenance des capabilities Profiler; sans sample, les valeurs sont `-1` en C# et `null` en JSON. Ce sont des compteurs BatchRendererGroup agrégés, pas une preuve GRD par renderer.

## Corrélation De Session

`PerformanceMeter.GetSessionSummary().SessionId` est un identifiant hexadécimal en minuscules de 32 caractères. Il est créé par `StartSession`, reste stable après `StopSession`, change au démarrage d'une nouvelle session et est vide lorsqu'aucune session n'existe. Le JSON de session expose la même valeur dans le champ racine `session_id`; le CSV l'ajoute comme dernière colonne `session_id` afin de préserver les positions existantes; `perfmeter.session.summary` la renvoie comme `session_id`.
