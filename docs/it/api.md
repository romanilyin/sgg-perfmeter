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
