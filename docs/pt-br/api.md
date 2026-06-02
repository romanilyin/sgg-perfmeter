# API Runtime

Namespace:

```csharp
using SGG.PerfMeter;
```

Todas as APIs de leitura sao seguras antes do runtime iniciar. Leituras retornam snapshots parados/padrao em vez de lancar excecoes porque o runtime nao esta ativo.

## Ciclo De Vida

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Modos de coleta:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Status E Metrics

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Principais grupos de metricas:

- FPS: media, 1% low, 0.1% low, contagens de spikes.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame quando disponivel.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: system/app memory, GC reserved memory, GPU memory quando disponivel.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced ou unknown.
- Overdraw: estado, progresso, ratio e visibilidade de heatmap.

A disponibilidade de counters e exposta por `AvailableCounters`, `UnavailableCounters` e avisos.

## Snapshots Estruturados

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Snapshots de device incluem informacoes de Unity/plataforma/SO/CPU/GPU/API/display/janela/suporte. Snapshots de camera incluem cena, transform, projection, clipping, pixel rect, target display e configuracoes URP da camera quando disponiveis.

## Cargas Dos Nucleos De CPU

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Cada snapshot expoe `CoreIndex`, `LoadPercent` e `Available`. O array pode estar vazio antes da inicializacao runtime, durante o warm-up do sampler ou em plataformas nao suportadas; trate isso como informacao de capacidade da plataforma, nao como falha de chamada da API.

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

Modos legados de overlay e flags semanticas de modulo continuam disponiveis para compatibilidade e filtragem.

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

Opcoes de sessao incluem warm-up em frames/segundos, intervalo de amostra, maximo de amostras, reset-on-scene-load e janelas para ignorar carregamento de cena.

## Alerts

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Excecoes de providers sao reportadas como snapshots de custom metric indisponiveis e nao interrompem a coleta das metricas principais.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Diagnosticos de overdraw sao modos diagnosticos explicitos e podem adicionar trabalho de GPU.
