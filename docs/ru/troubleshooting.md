# Troubleshooting

Используйте этот checklist, если PerfMeter не показывает ожидаемые данные.

## Overlay Не Появляется

- Откройте `SGG/Perfmeter/Setup` и проверьте, что overlay visibility включен.
- Проверьте, что collection mode равен `Overlay`, а не `Background` или `Stopped`.
- Если используется zero-code setup, проверьте файл `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Если используется manual bootstrap, проверьте вызов `PerformanceMeter.EnsureRunning()` после загрузки сцены.
- Войдите в Play Mode; Edit Mode API-вызовы безопасны, но не создают runtime overlay.

## Нет Frame Timing Или GPU Timing

- Включите Player Settings -> Rendering -> Frame Timing Stats.
- На Android используйте Vulkan, если важен GPU frame timing.
- OpenGL/OpenGLES считайте degraded mode для GPU timing.
- Проверяйте `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` и `Warning`, прежде чем считать counter доступным.

## Overdraw Measurement Не Двигается

- Установите `PerfMeterRenderGraphFeature` в активный URP renderer.
- Проверьте, что active camera использует renderer с этой feature.
- Проверьте поддержку fragment UAV/storage buffers, compute shaders и async GPU readback на target backend.
- Запускайте bounded measurement через `PerformanceMeter.RequestOverdrawMeasurement(frameCount)`.
- Если target не поддерживается, PerfMeter вернет `OverdrawState.Unsupported` и не будет планировать pass.

## Session Export Не Получается

- Экспортируйте в project-local path.
- Не перезаписывайте существующий export, если workflow явно не удаляет его заранее.
- Держите `MaxSamples` bounded для долгих прогонов.
- Используйте warm-up frames/seconds, чтобы startup spikes не попадали в summary.

## Alerts Слишком Шумные

- Настройте thresholds и consecutive-frame windows в JSON settings.
- Увеличьте Editor warning cooldowns.
- Отключите Editor warning logs, если достаточно callbacks или structured logs.

## Данные Отличаются Между Устройствами

Это нормально. GPU timings, profiler counters, display information, async readback и overdraw support зависят от graphics API, platform, Unity version и устройства. Используйте device snapshots и warnings в session exports, чтобы объяснять различия.
