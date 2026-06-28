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

Les avertissements Editor sont limites par des cooldowns et peuvent etre desactives via les reglages JSON ou les controles runtime.

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

## Automatisation Par Agents

Execution typique pilotee par MCP:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
