# Runtime API

Пространство имен:

```csharp
using SGG.PerfMeter;
```

Все API чтения безопасны до запуска runtime. Чтение возвращает снимки остановленного состояния или значения по умолчанию, а не исключение из-за неактивного runtime.

## Жизненный цикл

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Режимы сбора:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Статус и метрики

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Основные группы метрик:

- FPS: средний FPS, 1% low, 0.1% low и счетчики spikes.
- Тайминги: CPU frame, CPU main thread, CPU render thread, present wait и GPU frame, когда доступно.
- Рендеринг: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD и uploads.
- Память: system/app memory, GC reserved memory и GPU memory, когда доступно.
- Узкое место: GPU, CPU main, CPU render, present-limited, balanced или unknown.
- Overdraw: state, progress, ratio и heatmap visibility.

Доступность счетчиков видна через `AvailableCounters`, `UnavailableCounters` и warnings.

## Структурированные снимки

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Снимки устройства содержат информацию о Unity, платформе, OS, CPU, GPU, API, дисплее, окне и поддержке возможностей. Снимки камеры содержат scene, transform, projection, clipping, pixel rect, target display и URP camera settings, когда доступно.

## Загрузка CPU-ядер

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Каждый снимок содержит `CoreIndex`, `LoadPercent` и `Available`. Массив может быть пустым до запуска runtime, во время прогрева sampler или на неподдерживаемых платформах; воспринимайте это как информацию о возможностях платформы, а не как ошибку API.

## Оверлей

```csharp
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.ClassicDark);
PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.Manrope);
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Устаревшие режимы оверлея и семантические флаги модулей остаются доступными для совместимости и фильтрации.

## Сессии

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

Опции сессии включают кадры/секунды warm-up, интервал сэмплов, максимальное количество сэмплов, reset-on-scene-load и окна игнорирования после загрузки сцены.

## Alerts

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

## Пользовательские метрики

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Исключения провайдеров превращаются в недоступные снимки пользовательских метрик и не прерывают основной сбор метрик.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Диагностика overdraw - явные диагностические режимы, которые могут добавлять работу GPU.
