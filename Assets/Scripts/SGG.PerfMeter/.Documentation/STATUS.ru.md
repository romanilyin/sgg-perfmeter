# Статус SGG PerfMeter

## Текущая готовность

- Идентификатор пакета: `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Реализованы runtime API, сбор метрик, UI Toolkit overlay с режимами, stacked CPU/GPU графиками, цветными legend labels и min/max текстовой историей, URP Render Graph marker feature, Editor setup/runtime tabs и opt-in численное измерение overdraw.
- Есть EditMode API/classifier tests и PlayMode runtime smoke tests; player-build smoke validation еще pending.
- Пакет готов для внутренней проверки в Unity 6000.4 / URP 17, но не для публичного релиза.

## Public Runtime API

- `PerformanceMeter.EnsureRunning()` запускает singleton runtime, когда это возможно.
- `PerformanceMeter.Stop()` останавливает сбор метрик и runtime-объекты overlay.
- `PerformanceMeter.GetStatus()` / `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)` возвращают статус, читаемый агентами.
- `PerformanceMeter.GetLatestMetrics()` / `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)` возвращают immutable snapshots метрик с FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing и overdraw значениями.
- `CollectionFrame` указывает Unity frame, на котором собран snapshot; значения `FrameTimingManager` могут быть задержаны относительно этого кадра.
- `PerfMeterBottleneck.PresentLimited` отделяет ожидание present/VSync/frame pacing от balanced frames и CPU/GPU-bound frames.
- `PerformanceMeter.SetOverlayVisible(bool visible)`, `PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerformanceMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerformanceMeter.IsOverlayVisible`, `PerformanceMeter.OverlayCorner`, `PerformanceMeter.OverlayMode` и `PerformanceMeter.TargetFps` управляют runtime overlay и target line.
- `PerformanceMeter.RequestOverdrawMeasurement(int frameCount = 60)` / `PerformanceMeter.CancelOverdrawMeasurement()` управляют ограниченным по времени численным измерением overdraw.
- Editor setup actions для агентов: `PerfMeterSetupActions.GetStatusReport()`, `PerfMeterSetupActions.EnableFrameTimingStats()`, `PerfMeterSetupActions.InstallRendererFeatures()`, `PerfMeterSetupActions.RunRecommendedSetup()`.

## Setup

- Откройте `SGG/Perfmeter/Setup` для проверки проекта, списка active/discovered URP renderer assets, установки `PerfMeterRenderGraphFeature` во все или выбранные editable renderers и копирования bootstrap-кода.
- Для headless/agent setup вызовите `SGG.PerfMeter.Editor.Setup.PerfMeterSetupActions.RunRecommendedSetup()` из Editor-контекста.
- Добавьте `PerfMeterRenderGraphFeature` в активный URP renderer asset, если нужны Render Graph markers или численное измерение overdraw; setup window делает это автоматически для найденных URP renderer assets.
- Включите Player Settings -> Rendering -> Frame Timing Stats перед использованием `FrameTimingManager` в билдах.
- Для Android предпочтителен Vulkan, если важен GPU frame timing; на OpenGL ES timing может быть недоступен или ненадежен.
- Вызывайте `PerformanceMeter.EnsureRunning()` из gameplay/bootstrap кода, затем читайте snapshots из агентов, диагностики или тестов.

## Известные ограничения

- Batchmode `-runTests` является известной локальной проблемой верификации; текущая надежная проверка - compile batchmode.
- Численное измерение overdraw использует replacement shader, atomic fragment counting и `AsyncGPUReadback`; visual heatmap output еще не реализован.
- Overdraw measurement gate'ит неподдерживаемые targets через `OverdrawState.Unsupported`; поведение fragment UAV/storage buffer на ограниченных backend все еще требует проверки на устройствах.
- Аналитика Render Graph passes/aliasing/merge еще не реализована.
- Пустой overlay marker pass является opt-in diagnostic mode; self-overhead subtraction все еще pending.
- Полный zero-allocation refresh overlay еще не реализован; текущий overlay throttles text rebuilds и managed string assignment до refresh interval.
- Manual device validation еще требуется, особенно для поведения Android Vulkan/GLES GPU timing.
