# Runtime API

Пространство имен:

```csharp
using SGG.PerfMeter;
```

Все API чтения безопасны до запуска PerfMeter. Чтение возвращает снимки остановленного состояния или значения по умолчанию, а не исключение из-за неактивного состояния во время выполнения.

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

## Self-observability и бюджеты overhead

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability публикует low-overhead измерения стоимости CPU callbacks в фиксированных окнах по 120 кадров. Средние значения считаются на один вызов. Общее состояние: `NotInitialized`, `Collecting` или `Ready`; состояние компонента: `NotMeasured`, `Collecting`, `Ready` или `Unsupported`.

Компоненты: `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` и `HdrpRenderIntegration`. Для каждого доступны число кадров и вызовов, среднее/максимальное CPU-время, общий/средний объем allocations, заданные бюджеты и состояния `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Компонент | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU self-timing явно имеет состояние `Unavailable`. Диагностика не вычитает overhead и не корректирует существующие CPU/GPU-метрики.

## Динамический каталог Profiler-метрик

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` и `GetProfilerMetricCapabilities()` читают кэшированный каталог. Состояние каталога — `NotInitialized`, `Ready` или `Error`; каждая capability сообщает `Unavailable`, `AvailableNoSample` или `AvailableSampled`, а `Resolution` указывает provenance `None`, `Exact` или `Alias`. Discovery выполняется только при старте runtime и в явных путях refresh/reconfigure, но не при steady-state collection. Существующие numeric metrics сохраняют compatibility values; authoritative availability определяется по `SampleState`/`IsAvailable` capability.

## Структурированные снимки

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Снимки устройства содержат информацию о Unity, платформе, OS, CPU, GPU, API, дисплее, окне и поддержке возможностей. Снимки камеры содержат scene, transform, projection, clipping, pixel rect, target display и URP/HDRP camera settings, когда доступно.

## Загрузка CPU-ядер

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Каждый снимок содержит `CoreIndex`, `LoadPercent` и `Available`. Массив может быть пустым до запуска PerfMeter, во время прогрева sampler или на неподдерживаемых платформах; воспринимайте это как информацию о возможностях платформы, а не как ошибку API.

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

## Alerts/оповещения

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

Свойство `StructuredLogsEnabled` по умолчанию равно `true` и управляет только структурированным alert `Debug.Log`. Значение `false` не отключает callback `AlertFired`, последние alerts и историю оповещений, предупреждения оверлея, логи предупреждений Editor или сессии. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` независимо управляет логами предупреждений Editor.

## Editor Compatibility Status

Editor API `PerfMeterSetupActions.GetCompatibilityStatus()` возвращает `PerfMeterCompatibilityStatus` и отдельно сообщает `ImportCompatible` для package floor Unity `2022.3`, `CoreRuntimeCompatible` для supported runtime Unity `6000.4+` и `RenderIntegrationCompatible` для active URP/HDRP `17.4+` с доступным adapter. Каждый результат содержит reason. Render compatibility не означает, что renderer assets уже настроены; для configuration readiness используйте setup status.

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

Coordinator допускает только один активный запрос и детерминированно проходит состояния `PreRoll`, `Capturing`, `PostRoll` и `Completed`. Повтор того же активного ID идемпотентен; другой активный ID отклоняется как пересечение. `Canceled`, `Unavailable` и `Error` — явные конечные состояния.

Встроенный backend оборачивает экспериментальный `ExternalGPUProfiler` Unity только в Editor или Development Build, только если внешняя tool уже подключена, и только для поддерживаемых desktop-комбинаций platform/API. Поддерживаются `RenderDoc` на desktop Windows/Linux с Direct3D 11, Direct3D 12 или Vulkan и `PIX` на desktop Windows с Direct3D 12. Выбирайте `RenderDoc` или `Pix` явно, потому что Unity не раскрывает identity подключенной tool. `Status.Tool` — это только запрошенная tool, а не проверенная identity подключенной tool. `Completed` подтверждает только wrapper lifecycle Unity; он не проверяет и не возвращает внешний `.rdc`/`.wpix` artifact или artifact path. Automated tests используют fake backend; подтверждение настоящей external tool и artifact остается release gate.

Значения по умолчанию `PerfMeterCaptureOptions`: `captureFrames: 1`, `preRollFrames: 0` и `postRollFrames: 0`. Валидный `RequestCapture` автоматически запускает runtime. `CancelCapture()` без ID отменяет текущий отображаемый активный запрос; передача ID защищает от отмены более нового запроса.

Overload с `PerfMeterCaptureBundleOptions` сохраняет capture samples отдельно от baseline session evidence и может добавить opt-in screenshot. После `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady` вызовите `PerformanceMeter.ExportCaptureBundle(captureId)`: versioned bundle атомарно создается в `Temp/PerfMeter/CaptureBundles` и содержит manifest с SHA-256, session/baseline/capture samples, capture alerts, context, optional screenshot и external-artifact metadata. Переданный project-local `.rdc`/`.wpix` копируется только как observed artifact и не становится authoritative; paths с traversal/reparse points и файлы вне проекта отклоняются.

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

Диагностика overdraw использует явные диагностические режимы, которые могут добавлять работу GPU. В HDRP эти API безопасно возвращают unsupported state для overdraw и heatmap, не обещая HDRP heatmap output.
