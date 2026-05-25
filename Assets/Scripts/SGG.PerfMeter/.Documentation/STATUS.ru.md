# Статус SGG PerfMeter

## Текущая готовность

- Идентификатор пакета: `com.sungeargames.perfmeter` / `SGG PerfMeter`; текущая private release candidate версия - `2026.5.20-1`.
- Реализованы runtime API, custom metric providers для project-specific counters, sampling нагрузки CPU по ядрам на Windows 11/Windows Editor через `NtQuerySystemInformation`, sampling нагрузки CPU по ядрам на Apple platforms через `host_processor_info`, sampling нагрузки CPU по ядрам на Android/Linux через `/proc/stat`, collection modes, device/environment snapshot с monitor names, camera snapshot для воспроизводимых captures, safe Render Graph analytics snapshot, session recorder с bounded samples, warm-up seconds, reset/scene-scope summaries, worst-frame metadata и JSON/CSV export, rule/alert engine с MCP alert commands, JSON settings для zero-code setup, JSON tunables для overlay/rules/session/overdraw, Package Manager samples, вкладка `Presets` с button-based overlay presets/modules/theme/layout/font/tunables, сгруппированные Runtime controls с active-state подсветкой, runtime toggle для Editor warning logs, сбор метрик, UI Toolkit overlay по умолчанию с `MetricBars` и `CpuCoreBars`, едиными display layouts включая `Custom`, themes, module filtering, limited custom metric rows, allocation-conscious text field refresh, stacked CPU/GPU графиками, отдельной боковой панелью CPU core percent bars и per-core graph grid, metric bars с min/max по краям, цветным current, явными risk ticks avg/1%/0.1% и выровненной статистикой справа, цветными graph legend badges с темными value strips и min/max текстовой историей, URP Render Graph marker feature, Editor setup/runtime tabs, opt-in численное измерение overdraw и visual overdraw heatmap.
- Есть EditMode API/classifier tests и PlayMode runtime smoke tests; classifier mixed-load edge cases, overdraw stale-readback safety и heatmap toggles покрыты. Android S23 Vulkan/GLES smoke validation пройдена; более широкая player-build validation еще pending.
- Пакет подготовлен как private/internal release candidate для проверки в Unity 6000.4+ / URP 17.4+; публичный релиз остается отложенным. Unity 2022.3-6000.3 import'ятся только ради compile-safety и официально не поддерживаются.

## Public Runtime API

- `PerformanceMeter.EnsureRunning()` запускает singleton runtime, когда это возможно.
- `PerformanceMeter.Stop()` останавливает сбор метрик и runtime-объекты overlay.
- `PerformanceMeter.GetStatus()` / `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)` возвращают статус, читаемый агентами.
- `PerformanceMeter.GetLatestMetrics()` / `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)` возвращают immutable snapshots метрик с FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing и overdraw значениями.
- `PerformanceMeter.GetDeviceInfo()` / `PerformanceMeter.TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)` возвращают immutable device/environment snapshot с Unity/platform/CPU/GPU/screen/display/monitor info без запуска runtime.
- `PerformanceMeter.GetCameraSnapshot(...)` / `PerformanceMeter.TryGetCameraSnapshot(...)` возвращают immutable camera snapshot с transform/projection/clip/pixel rect/target display/URP camera settings без запуска runtime.
- `PerformanceMeter.GetRenderGraphSnapshot()` / `PerformanceMeter.TryGetRenderGraphSnapshot(...)` возвращают последнюю наблюдавшуюся диагностику `PerfMeterRenderGraphFeature.RecordRenderGraph` без запуска runtime; internal pass/resource/aliasing/merge counts деградируют до `-1` с warning, если URP не раскрывает их безопасно.
- `PerformanceMeter.GetSettings()` возвращает snapshot JSON-настроек zero-code setup или safe defaults, если JSON отсутствует.
- `PerformanceMeter.SetCollectionMode(...)` и `PerformanceMeter.CollectionMode` управляют `Stopped`, `Background`, `Overlay` и `OverdrawDiagnostic`; `PerfMeterStatusSnapshot.CollectionMode` зеркалирует active mode для MCP/agents.
- `PerformanceMeter.StartSession(...)`, `PerformanceMeter.StopSession()`, `PerformanceMeter.ResetStats()`, `PerformanceMeter.GetSessionSummary()`, `PerformanceMeter.GetSessionSamples()`, `PerformanceMeter.ExportSessionJson(path)`, `PerformanceMeter.ExportSessionCsv(path)` и `PerformanceMeter.IsSessionRecording` управляют session recorder с `WarmupFrames`, `WarmupSeconds`, `SampleIntervalSeconds`, `MaxSamples`, reset-on-scene-load/scene-ignore options, dropped-sample count, focus loss/pause telemetry, whole-run/current-scene summaries, worst-frame metadata, metadata из settings/device/camera snapshots и JSON/CSV export.
- `PerformanceMeter.GetLatestAlerts()`, `PerformanceMeter.ClearAlerts()` и `PerformanceMeter.AlertFired` предоставляют rule alerts; status snapshot содержит active/fired counts и latest alert summary.
- `PerformanceMeter.RegisterCustomMetricProvider(...)`, `UnregisterCustomMetricProvider(...)`, `ClearCustomMetricProviders()` и `GetCustomMetrics()` предоставляют public extension point для project-specific counters; provider exceptions возвращаются как unavailable custom metric warning и не ломают runtime collection.
- `CollectionFrame` указывает Unity frame, на котором собран snapshot; значения `FrameTimingManager` могут быть задержаны относительно этого кадра. Focus/pause gaps и невалидные `FrameTimingManager` samples вне диапазона `(0, 60000] ms` не попадают в FPS stats, session samples, alerts и overlay history.
- `PerfMeterBottleneck.PresentLimited` отделяет ожидание present/VSync/frame pacing от balanced frames и CPU/GPU-bound frames.
- `PerformanceMeter.SetOverlayVisible(bool visible)`, `PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, legacy-mapping `PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme theme)`, `PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout layout)`, `PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily fontFamily)`, `PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset preset)`, `PerformanceMeter.SetOverlayModules(PerfMeterOverlayModule modules)`, `PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)`, `PerformanceMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerformanceMeter.SetEditorWarningLogsEnabled(bool enabled)`, `PerformanceMeter.IsOverlayVisible`, `PerformanceMeter.OverlayCorner`, `PerformanceMeter.OverlayMode`, `PerformanceMeter.OverlayTheme`, `PerformanceMeter.OverlayLayout`, `PerformanceMeter.OverlayFontFamily`, `PerformanceMeter.OverlayPreset`, `PerformanceMeter.OverlayModules`, `PerformanceMeter.TargetFps` и `PerformanceMeter.EditorWarningLogsEnabled` управляют runtime overlay, module filtering, target line и toggle Editor warning logs.
- `PerformanceMeter.RequestOverdrawMeasurement(int frameCount = 0)` / `PerformanceMeter.CancelOverdrawMeasurement()` управляют ограниченным по времени численным измерением overdraw; `0` использует JSON default frame count, а положительные значения clamp'ятся по JSON max frame count.
- `PerformanceMeter.SetOverdrawHeatmapVisible(bool visible)` и `PerformanceMeter.IsOverdrawHeatmapVisible` управляют visual overdraw heatmap.
- Editor setup actions для агентов: `PerfMeterSetupActions.GetStatusReport()`, `PerfMeterSetupActions.EnableFrameTimingStats()`, `PerfMeterSetupActions.InstallRendererFeatures()`, `PerfMeterSetupActions.CreateDefaultSettings()`, `PerfMeterSetupActions.SaveSettings(...)`, `PerfMeterSetupActions.ApplySettingsToRuntime()`, `PerfMeterSetupActions.RunRecommendedSetup()`.

## Setup

- Откройте `SGG/Perfmeter/Setup` для проверки проекта, списка active/discovered URP renderer assets, установки `PerfMeterRenderGraphFeature` во все или выбранные editable renderers, настройки collection mode и вкладки `Presets`, а также копирования bootstrap-кода при необходимости.
- Вкладка `Presets` создает и редактирует `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`; это project-owned JSON, не `ScriptableObject`. При `enabled=true` и `autoStart=true` runtime auto-start применяет collection mode, overlay settings, rule defaults, session defaults и overdraw limits без ручного bootstrap-кода. Группы кнопок сохраняют active preset, module toggles, theme/layout/font и tunables в JSON и применяют их к runtime overlay/alerts/session defaults.
- Вкладка `Runtime` группирует Play Mode controls по status, collection, visibility, target FPS, overlay layout/theme/font/corner, CPU core panels, Editor warning logs и overdraw; активные radio/toggle варианты подсвечиваются.
- Вкладка `Settings` выбирает язык setup window из XLIFF-файлов в `Editor/UI/Localization`; локализация действует только на Editor-окно и не меняет runtime output или generated snippets.
- Для headless/agent setup вызовите `SGG.PerfMeter.Editor.Setup.PerfMeterSetupActions.RunRecommendedSetup()` из Editor-контекста.
- Setup window/status report явно помечает Unity ниже 6000.4 как import-safe only. Установка renderer feature отключена, пока optional URP assembly недоступна на Unity 6000.4+ с URP 17.4+.
- Package samples покрывают bootstrap/zero-code settings, runtime workflows, editor setup automation, MCP command examples, session export, alerts, overdraw/heatmap и camera snapshot replay.
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
- Аналитика Render Graph passes/aliasing/merge реализована как консервативный spike: наблюдение feature и PerfMeter pass markers доступны, но внутренние счетчики Unity graph могут оставаться `-1` на версиях URP, где их нельзя безопасно прочитать.
- MCP session commands `perfmeter.session.start/stop/summary/export`, runtime reset command `perfmeter.runtime.reset_stats` и mode command `perfmeter.runtime.mode.set` реализованы; export ограничивает path директорией проекта и не перезаписывает существующие файлы.
- MCP alert commands `perfmeter.alerts.latest/clear` реализованы; output содержит alerts, counters, status fields и Editor state.
- MCP latest metrics output содержит `custom_metrics`; session JSON export пишет `custom_metrics` в sample rows и `custom_metric_sample_count` в metadata. Overlay может показывать до восьми строк custom metrics при включенном модуле `CustomMetrics`; unavailable snapshots отображаются как `n/a` с warning text.
- Editor warning alerts имеют прямой JSON toggle `editorWarningsEnabled`, runtime-кнопку `Enable Editor Warning Logs` и cooldown, поэтому warnings можно отключить или ограничить throttling'ом вместо записи каждый кадр; default rule thresholds и consecutive-frame windows также редактируются в JSON/Presets.
- Session start принимает warm-up seconds и scene-load reset/ignore overrides; summary/export output содержит whole-run/current-scene и worst-frame data.
- Пустой overlay marker pass является opt-in diagnostic mode; self-overhead subtraction все еще pending.
- Overlay text refresh теперь использует стабильные field labels, cached enum strings, переиспользуемые buffers для чисел и dirty assignment для value labels; refresh interval, scale, opacity, font size и graph history length настраиваются в JSON. Изменившиеся числовые values и graph legend labels все еще могут materialize managed strings на throttled refresh interval.
- Более широкая manual device validation все еще полезна за пределами текущей Android S23 Vulkan/GLES smoke coverage.

## Release Readiness

- Root release plan: `docs/release-2026.5.20-1.md`.
- Release notes draft: `docs/release-notes-2026.5.20-1.md`.
- Release process: `docs/release-process.md`.
- Local/manual gates: Unity compile, EditMode tests, PlayMode tests, `git diff --check` и optional Android Vulkan/GLES smoke builds.
- GitHub release workflow manual-only (`workflow_dispatch`) и не публикует packages.
