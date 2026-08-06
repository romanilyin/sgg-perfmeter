# Сценарии работы

## Runtime-оверлей

Используйте оверлей, когда нужна быстрая видимость прямо в игре.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Оверлей использует UI Toolkit и не перехватывает игровой ввод. Он поддерживает режим только FPS, компактный текст, графики, полную диагностику, полосы метрик, визуальные темы, фильтры модулей, графики CPU/GPU, виджеты ядер CPU и ограниченные строки пользовательских метрик.

PerfMeter создает и владеет версионированным host UI Toolkit для оверлея: в Unity `6000.4` используется `UIDocument`, а в Unity `6000.5+` — `PanelRenderer`. Собственный host отделен от чужого UI и сохраняет его panel settings и children; при rebuild удаляется только container, принадлежащий PerfMeter.

## Фоновый сбор

Фоновый режим подходит для тестов, прогонов на устройствах и агентских сценариев без видимого UI.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Запись и экспорт сессий

Сессии нужны для повторяемых окон профилирования.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Запустите измеряемый сценарий.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Экспорт сессии включает тайминги, FPS lows, spikes, счетчики узких мест, счетчики рендера, счетчики памяти, состояние overdraw, доступность предупреждений и счетчиков, сводки сцен, худшие кадры, метаданные устройства, камеры и настроек, а также пользовательские метрики.

## Alerts/оповещения

Правила могут сообщать о нарушениях бюджета кадра, низком FPS, недоступном GPU timing и превышении порогов overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warnings ограничены паузой между срабатываниями и могут быть отключены через JSON-настройки или контролы во время выполнения. Логи структурированных alert и Editor warnings независимы: `PerformanceMeter.SetStructuredLogsEnabled(false)` подавляет только structured alert `Debug.Log`, а `PerformanceMeter.SetEditorWarningLogsEnabled(false)` отдельно управляет логами предупреждений Editor. Callback-и, alerts/history, предупреждения оверлея и сессии продолжают работать.

## External GPU Capture

Используйте capture coordinator для ограниченного запроса RenderDoc или PIX, когда tool уже подключена:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

Coordinator поддерживает только один активный запрос и детерминированно проходит `PreRoll`, `Capturing`, `PostRoll` и `Completed`. Повтор того же active ID идемпотентен, а другой ID отклоняется как пересечение. Pre-roll и post-roll считают кадры Unity; только `Capturing` открывает alert capture scope и вызывает экспериментальный `ExternalGPUProfiler` Unity. Обязательны Editor или Development Build и подключенная tool. `RenderDoc` разрешен на desktop Windows/Linux с Direct3D 11, Direct3D 12 или Vulkan; `PIX` разрешен на desktop Windows с Direct3D 12.

`Completed` означает только завершение защищенного wrapper lifecycle. Unity не раскрывает identity подключенной tool или authoritative artifact path; `Status.Tool` показывает только запрошенную tool, а не проверенную identity подключенной tool. Проверяйте `.rdc`/`.wpix` artifact во внешней tool. Overload с `PerfMeterCaptureBundleOptions` отдельно хранит baseline/capture samples и атомарно экспортирует project-local bundle; external artifact остается только observed, не authoritative. Для автоматизации используйте `perfmeter.capture.request/status/cancel/export/capabilities`.

## Диагностика overdraw

Числовой overdraw включается явно и работает в ограниченном окне.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Числовой overdraw и heatmap используют диагностический путь URP Render Graph. Измерение overdraw требует `PerfMeterRenderGraphFeature`, поддержки replacement shader, fragment UAV/storage buffer, compute shaders и async GPU readback, а также поддерживаемого graphics API. HDRP возвращает unsupported для overdraw/heatmap, при этом core overlay, session, API и MCP diagnostics остаются доступны. Неподдерживаемые цели возвращают `OverdrawState.Unsupported` вместо запуска pass.

## Воспроизводимость камеры и устройства

Снимки сохраняют окружение, в котором получен захват производительности.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Экспорт сессии включает метаданные устройства и камеры, чтобы захват можно было понять или воспроизвести позже.

## Пользовательские метрики

Регистрируйте провайдеры проекта без форка PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Пользовательские метрики доступны через API-чтение, экспорт сессии в JSON, latest metrics в MCP и до восьми строк оверлея при включенном модуле `CustomMetrics`.

## Инструментация Unity Profiler

Инструментация является внутренней и видна только при профилировании Editor, Development Build или другого profiler-enabled build. В Release player без Profiler эти markers/counters являются no-op и не создают instrumentation data; public API, status, MCP и export schemas не меняются.

- Маркеры охватывают collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) и JSON/CSV export (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` — зарезервированный internal provider hook.
- Counters охватывают CPU/GPU frame times (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) как end-of-frame gauges в nanoseconds. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` и `SGG.PerfMeter.Thermal.Available` кодируют availability/active как `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` и `SGG.PerfMeter.Capture.State` используют enum codes; `SGG.PerfMeter.Provider.CustomMetricCount` — count. Все counters используют category `Scripts` и `FlushOnEndOfFrame`.
- Synthetic thermal sample не создается; `SGG.PerfMeter.Thermal.Available` остается `0`/unavailable, пока реальный platform provider не начнет поставлять данные.

## Self-observability и бюджеты overhead

Используйте `PerformanceMeter.GetSelfOverhead()` или `PerformanceMeter.GetStatus().SelfOverhead` для диагностики стоимости CPU callbacks и allocations у collector, custom providers, CPU-core provider, overlay и URP/HDRP integration. Измерения используют фиксированные окна по 120 кадров, средние на один вызов и отдельные CPU/allocation budgets для компонентов.

Неактивная render integration возвращает `Unsupported`, поддерживаемый компонент без вызовов — `NotMeasured`, а GPU self-timing — `Unavailable`. Accounting носит только диагностический характер: PerfMeter не вычитает overhead и не корректирует существующие CPU/GPU-метрики.

## MCP-автоматизация

Типичный прогон через MCP:

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

`perfmeter.profiler.capabilities {}` — это чтение кэшированного состояния; команда не запускает runtime и discovery.
