# Диагностика проблем

Используйте этот список проверок, если PerfMeter не показывает ожидаемые данные.

## Оверлей не появляется

- Откройте `SGG/Perfmeter/Setup` и проверьте, что видимость оверлея включена.
- Проверьте, что режим сбора равен `Overlay`, а не `Background` или `Stopped`.
- Если используется настройка без кода, проверьте файл `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Если используется ручной запуск, проверьте вызов `PerformanceMeter.EnsureRunning()` после загрузки сцены.
- Войдите в Play Mode; Edit Mode API-вызовы безопасны, но не создают runtime-оверлей.

## Нет Frame Timing или GPU Timing

- Включите Player Settings -> Rendering -> Frame Timing Stats.
- На Android используйте Vulkan, если важен GPU frame timing.
- OpenGL/OpenGLES считайте режимом с ограничениями для GPU timing.
- Проверяйте `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` и `Warning`, прежде чем считать счетчик доступным.

## Измерение overdraw не движется

- Установите `PerfMeterRenderGraphFeature` в активный URP Renderer.
- Проверьте, что активная камера использует Renderer с этой feature.
- Проверьте поддержку fragment UAV/storage buffers, compute shaders и async GPU readback на целевом backend.
- Запускайте ограниченное измерение через `PerformanceMeter.RequestOverdrawMeasurement(frameCount)`.
- Если цель не поддерживается, PerfMeter вернет `OverdrawState.Unsupported` и не будет планировать pass.

## Экспорт сессии не получается

- Экспортируйте в путь внутри проекта.
- Не перезаписывайте существующий экспорт, если сценарий явно не удаляет его заранее.
- Держите `MaxSamples` ограниченным для долгих прогонов.
- Используйте кадры/секунды warm-up, чтобы startup spikes не попадали в summary.

## Alerts слишком шумные

- Настройте thresholds и окна последовательных кадров в JSON-настройках.
- Увеличьте паузы между Editor warnings.
- Отключите логи Editor warnings, если достаточно callbacks или структурированных логов.

## Данные Отличаются Между Устройствами

Это нормально. GPU timings, profiler counters, display information, async readback и поддержка overdraw зависят от graphics API, платформы, версии Unity и устройства. Используйте снимки устройства и warnings в экспортах сессий, чтобы объяснять различия.
