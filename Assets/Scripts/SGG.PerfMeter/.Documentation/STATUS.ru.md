# Статус SGG PerfMeter

## Текущая готовность

- Идентификатор пакета: `com.sungeargames.perfmeter` / `SGG PerfMeter`; текущая private release candidate версия - `2026.5.18-1`.
- Реализованы runtime API, device/environment snapshot с monitor names, camera snapshot для воспроизводимых captures, session recorder с bounded samples, warm-up seconds, reset/scene-scope summaries, worst-frame metadata и JSON/CSV export, rule/alert engine с MCP alert commands, JSON settings для zero-code setup, вкладка `Presets` с overlay presets/modules, сбор метрик, UI Toolkit overlay с режимами, module filtering, stacked CPU/GPU графиками, цветными legend labels и min/max текстовой историей, URP Render Graph marker feature, Editor setup/runtime tabs, opt-in численное измерение overdraw и visual overdraw heatmap.
- Есть EditMode API/classifier tests и PlayMode runtime smoke tests; classifier mixed-load edge cases, overdraw stale-readback safety и heatmap toggles покрыты. Android S23 Vulkan/GLES smoke validation пройдена; более широкая player-build validation еще pending.
- Пакет подготовлен как private/internal release candidate для проверки в Unity 6000.4 / URP 17; публичный релиз остается отложенным.

## Public Runtime API

- `PerformanceMeter.EnsureRunning()` запускает singleton runtime, когда это возможно.
- `PerformanceMeter.Stop()` останавливает сбор метрик и runtime-объекты overlay.
- `PerformanceMeter.GetStatus()` / `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)` возвращают статус, читаемый агентами.
- `PerformanceMeter.GetLatestMetrics()` / `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)` возвращают immutable snapshots метрик с FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing и overdraw значениями.
- `PerformanceMeter.GetDeviceInfo()` / `PerformanceMeter.TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)` возвращают immutable device/environment snapshot с Unity/platform/CPU/GPU/screen/display/monitor info без запуска runtime.
- `PerformanceMeter.GetCameraSnapshot(...)` / `PerformanceMeter.TryGetCameraSnapshot(...)` возвращают immutable camera snapshot с transform/projection/clip/pixel rect/target display/URP camera settings без запуска runtime.
- `PerformanceMeter.GetSettings()` возвращает snapshot JSON-настроек zero-code setup или safe defaults, если JSON отсутствует.
- `PerformanceMeter.StartSession(...)`, `PerformanceMeter.StopSession()`, `PerformanceMeter.ResetStats()`, `PerformanceMeter.GetSessionSummary()`, `PerformanceMeter.GetSessionSamples()`, `PerformanceMeter.ExportSessionJson(path)`, `PerformanceMeter.ExportSessionCsv(path)` и `PerformanceMeter.IsSessionRecording` управляют session recorder с `WarmupFrames`, `WarmupSeconds`, `SampleIntervalSeconds`, `MaxSamples`, reset-on-scene-load/scene-ignore options, dropped-sample count, whole-run/current-scene summaries, worst-frame metadata, metadata из settings/device/camera snapshots и JSON/CSV export.
- `PerformanceMeter.GetLatestAlerts()`, `PerformanceMeter.ClearAlerts()` и `PerformanceMeter.AlertFired` предоставляют rule alerts; status snapshot содержит active/fired counts и latest alert summary.
- `CollectionFrame` указывает Unity frame, на котором собран snapshot; значения `FrameTimingManager` могут быть задержаны относительно этого кадра.
- `PerfMeterBottleneck.PresentLimited` отделяет ожидание present/VSync/frame pacing от balanced frames и CPU/GPU-bound frames.
- `PerformanceMeter.SetOverlayVisible(bool visible)`, `PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset preset)`, `PerformanceMeter.SetOverlayModules(PerfMeterOverlayModule modules)`, `PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)`, `PerformanceMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerformanceMeter.IsOverlayVisible`, `PerformanceMeter.OverlayCorner`, `PerformanceMeter.OverlayMode`, `PerformanceMeter.OverlayPreset`, `PerformanceMeter.OverlayModules` и `PerformanceMeter.TargetFps` управляют runtime overlay, module filtering и target line.
- `PerformanceMeter.RequestOverdrawMeasurement(int frameCount = 60)` / `PerformanceMeter.CancelOverdrawMeasurement()` управляют ограниченным по времени численным измерением overdraw.
- `PerformanceMeter.SetOverdrawHeatmapVisible(bool visible)` и `PerformanceMeter.IsOverdrawHeatmapVisible` управляют visual overdraw heatmap.
- Editor setup actions для агентов: `PerfMeterSetupActions.GetStatusReport()`, `PerfMeterSetupActions.EnableFrameTimingStats()`, `PerfMeterSetupActions.InstallRendererFeatures()`, `PerfMeterSetupActions.CreateDefaultSettings()`, `PerfMeterSetupActions.SaveSettings(...)`, `PerfMeterSetupActions.ApplySettingsToRuntime()`, `PerfMeterSetupActions.RunRecommendedSetup()`.

## Setup

- Откройте `SGG/Perfmeter/Setup` для проверки проекта, списка active/discovered URP renderer assets, установки `PerfMeterRenderGraphFeature` во все или выбранные editable renderers, настройки вкладки `Presets` и копирования bootstrap-кода при необходимости.
- Вкладка `Presets` создает и редактирует `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`; это project-owned JSON, не `ScriptableObject`. При `enabled=true` и `autoStart=true` runtime auto-start применяет настройки без ручного bootstrap-кода. Active preset и module toggles сохраняются в JSON и применяются к runtime overlay.
- Для headless/agent setup вызовите `SGG.PerfMeter.Editor.Setup.PerfMeterSetupActions.RunRecommendedSetup()` из Editor-контекста.
- Добавьте `PerfMeterRenderGraphFeature` в активный URP renderer asset, если нужны Render Graph markers, численное измерение overdraw или visual overdraw heatmap; setup window делает это автоматически для найденных URP renderer assets.
- Включите Player Settings -> Rendering -> Frame Timing Stats перед использованием `FrameTimingManager` в билдах.
- Для Android предпочтителен Vulkan, если важен GPU frame timing; на OpenGL ES timing может быть недоступен или ненадежен.
- Системные названия мониторов берутся из `Screen.GetDisplayLayout(List<DisplayInfo>)`, а на платформах без display layout используется fallback из `Screen.currentResolution`.
- Вызывайте `PerformanceMeter.EnsureRunning()` из gameplay/bootstrap кода, затем читайте snapshots из агентов, диагностики или тестов.

## Известные ограничения

- Локальный `-runTests` работает, если запускать его без `-quit`; Unity сама пишет XML results и выходит после run.
- Численное измерение overdraw использует replacement shader, atomic fragment counting и `AsyncGPUReadback`; visual heatmap использует отдельный additive replacement shader и дополнительную перерисовку сцены, пока включен.
- Overdraw measurement по умолчанию ограничен Game cameras и может быть сужен через camera-name filter в настройках `PerfMeterRenderGraphFeature`.
- Overdraw measurement gate'ит неподдерживаемые targets через `OverdrawState.Unsupported`; поведение fragment UAV/storage buffer на ограниченных backend все еще требует проверки на устройствах.
- Аналитика Render Graph passes/aliasing/merge еще не реализована.
- MCP session commands `perfmeter.session.start/stop/summary/export` и runtime reset command `perfmeter.runtime.reset_stats` реализованы; export ограничивает path директорией проекта и не перезаписывает существующие файлы.
- MCP alert commands `perfmeter.alerts.latest/clear` реализованы; output содержит alerts, counters, status fields и Editor state.
- Editor warning alerts имеют отдельный JSON-настраиваемый cooldown и не пишут warning каждый кадр.
- Session start принимает warm-up seconds и scene-load reset/ignore overrides; summary/export output содержит whole-run/current-scene и worst-frame data.
- Пустой overlay marker pass является opt-in diagnostic mode; self-overhead subtraction все еще pending.
- Полный zero-allocation refresh overlay еще не реализован; текущий overlay throttles text rebuilds и managed string assignment до refresh interval.
- Более широкая manual device validation все еще полезна за пределами текущей Android S23 Vulkan/GLES smoke coverage.

## Release Readiness

- Root release plan: `docs/release-2026.5.18-1.md`.
- Release notes draft: `docs/release-notes-2026.5.18-1.md`.
- Release process: `docs/release-process.md`.
- Local/manual gates: Unity compile, EditMode tests, PlayMode tests, `git diff --check` и optional Android Vulkan/GLES smoke builds.
- GitHub release workflow manual-only (`workflow_dispatch`) и не публикует packages.
