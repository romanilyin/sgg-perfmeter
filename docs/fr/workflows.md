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

`Completed` signifie uniquement que le wrapper lifecycle protege est termine. Unity n'expose ni l'identite de l'outil attache ni un path d'artefact faisant autorite; `Status.Tool` est uniquement l'outil demande, et non l'identite verifiee de l'outil attache. Verifiez l'artefact `.rdc`/`.wpix` dans l'outil externe. Les tests automatises utilisent un fake backend; la confirmation par un outil reel reste un release gate. L'orchestration MCP, les capture bundles et les artefacts correles restent un travail futur separe.

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
