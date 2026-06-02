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

Editor warnings ограничены паузой между срабатываниями и могут быть отключены через JSON-настройки или контролы во время выполнения.

## Диагностика overdraw

Числовой overdraw включается явно и работает в ограниченном окне.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Измерение overdraw требует `PerfMeterRenderGraphFeature`, поддержки replacement shader, fragment UAV/storage buffer, compute shaders и async GPU readback, а также поддерживаемого graphics API. Неподдерживаемые цели возвращают `OverdrawState.Unsupported` вместо запуска pass.

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

## MCP-автоматизация

Типичный прогон через MCP:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
