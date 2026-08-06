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
```

La self-observability publie des mesures low-overhead du cout des callbacks CPU dans des fenetres fixes de 120 frames. Les moyennes sont calculees par invocation. L'etat global est `NotInitialized`, `Collecting` ou `Ready`; l'etat d'un composant est `NotMeasured`, `Collecting`, `Ready` ou `Unsupported`.

Les composants sont `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` et `HdrpRenderIntegration`. Chacun expose les nombres de frames/invocations, les millisecondes CPU moyennes/maximales, les allocations totales/moyennes, les budgets et les etats `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Composant | Budget CPU | Budget d'allocation |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

Le self-timing GPU est explicitement `Unavailable`. Ces diagnostics ne soustraient rien aux metriques CPU/GPU existantes et ne les ajustent pas.

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

Le backend integre enveloppe l'`ExternalGPUProfiler` experimental de Unity uniquement dans l'Editor ou un Development Build, seulement lorsqu'un outil externe est deja attache, et uniquement pour les combinaisons plateforme/API desktop prises en charge. Les combinaisons prises en charge sont `RenderDoc` sur desktop Windows/Linux avec Direct3D 11, Direct3D 12 ou Vulkan, et `PIX` sur desktop Windows avec Direct3D 12. Selectionnez explicitement `RenderDoc` ou `Pix`, car Unity n'expose pas l'identite de l'outil attache. `Status.Tool` est uniquement l'outil demande, et non l'identite verifiee de l'outil attache. `Completed` confirme uniquement le wrapper lifecycle de Unity; il ne verifie ni ne renvoie un artefact externe `.rdc`/`.wpix` ou son path. Les tests automatises utilisent un fake backend; la confirmation par l'outil externe reel et de l'artefact reste un release gate.

Les valeurs par defaut de `PerfMeterCaptureOptions` sont `captureFrames: 1`, `preRollFrames: 0` et `postRollFrames: 0`. Un `RequestCapture` valide demarre automatiquement le runtime. `CancelCapture()` sans ID annule la requete active actuellement rapportee; passer une ID protege contre l'annulation d'une requete plus recente.

L'overload avec `PerfMeterCaptureBundleOptions` separe les capture samples de la baseline session et peut inclure un screenshot opt-in. Quand `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady`, `PerformanceMeter.ExportCaptureBundle(captureId)` cree atomiquement un bundle versionne sous `Temp/PerfMeter/CaptureBundles` avec manifest SHA-256, samples, alerts, contexte, screenshot optionnel et metadata d'artefact externe. Un `.rdc`/`.wpix` local au projet reste un artefact observe, jamais autoritatif; traversal, reparse points et fichiers hors projet sont rejetes.

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
