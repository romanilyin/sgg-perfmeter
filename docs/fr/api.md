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

## Snapshots Structures

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Les snapshots de device incluent des informations Unity/platform/OS/CPU/GPU/API/display/window/support. Les snapshots de camera incluent scene, transform, projection, clipping, pixel rect, target display et reglages de camera URP quand disponibles.

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
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

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

Les diagnostics d'overdraw sont des modes de diagnostic explicites et peuvent ajouter du travail GPU.
