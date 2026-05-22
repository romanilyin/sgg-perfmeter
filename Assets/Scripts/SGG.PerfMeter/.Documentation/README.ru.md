# SGG PerfMeter

SGG PerfMeter предоставляет публичный runtime API для безопасного чтения статуса и последних метрик производительности без обращения к UI или Unity Console.

Позиционирование: SGG PerfMeter - это Unity 6000.4+ / URP 17.4+ Render Graph diagnostics layer и agent-readable profiling API, а не просто FPS counter. Пакет объединяет FrameTimingManager timings, ProfilerRecorder counters, bottleneck classification, overdraw diagnostics, device/camera snapshots, session export, rule alerts, custom metrics, UI Toolkit overlay и MCP commands для Play Mode, билдов, smoke tests и editor/agent automation.

Он дополняет Unity Profiler, RenderDoc, Profile Analyzer и Frame Debugger, но не заменяет их. Для глубоких captures используйте эти инструменты; для легкой runtime telemetry, structured snapshots и repeatable automation используйте PerfMeter.

Текущая версия пакета: `2026.5.20-1`. Это private release candidate; репозиторий остается приватным до явного решения о public switch.

Compatibility note: пакет импортируется в Unity `2022.3`-`6000.3` ради compile-safety, но эти версии официально не поддерживаются. Runtime overlay, Render Graph features, overdraw passes и support/bug reports требуют Unity `6000.4+` с URP `17.4+`.

## API для агентов

- Namespace: `SGG.PerfMeter`
- Статус: `PerformanceMeter.GetStatus()` или `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)`
- Метрики: `PerformanceMeter.GetLatestMetrics()` или `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)`
- Device/environment: `PerformanceMeter.GetDeviceInfo()` или `PerformanceMeter.TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)` возвращает Unity/platform/CPU/GPU/screen/monitor snapshot без запуска runtime.
- Camera snapshot: `PerformanceMeter.GetCameraSnapshot(...)` или `PerformanceMeter.TryGetCameraSnapshot(out PerfMeterCameraSnapshot snapshot, ...)` возвращает положение, ориентацию и параметры камеры для воспроизводимых performance captures.
- Render Graph snapshot: `PerformanceMeter.GetRenderGraphSnapshot()` или `PerformanceMeter.TryGetRenderGraphSnapshot(out PerfMeterRenderGraphSnapshot snapshot)` возвращает последнюю наблюдавшуюся диагностику PerfMeter Render Graph feature без запуска runtime.
- Настройки: `PerformanceMeter.GetSettings()` возвращает snapshot JSON-настроек zero-code setup или safe defaults, если JSON отсутствует.
- Sessions: `PerformanceMeter.StartSession()`, `PerformanceMeter.StartSession(PerfMeterSessionOptions options)`, `PerformanceMeter.StopSession()`, `PerformanceMeter.ResetStats()`, `PerformanceMeter.GetSessionSummary()`, `PerformanceMeter.GetSessionSamples()`, `PerformanceMeter.ExportSessionJson(path)`, `PerformanceMeter.ExportSessionCsv(path)` и `PerformanceMeter.IsSessionRecording` управляют bounded in-memory recording, reset статистики и экспортом JSON/CSV.
- Alerts: `PerformanceMeter.GetLatestAlerts()`, `PerformanceMeter.ClearAlerts()` и событие `PerformanceMeter.AlertFired` дают active/fired rule alerts без чтения Unity Console.
- Custom metrics: `IPerfMeterCustomMetricProvider`, `PerfMeterCustomMetricSnapshot`, `PerformanceMeter.RegisterCustomMetricProvider(...)`, `UnregisterCustomMetricProvider(...)`, `ClearCustomMetricProviders()` и `GetCustomMetrics()` позволяют проекту добавлять свои счетчики без форка PerfMeter. `PerformanceMeter.GetCpuCoreLoads()` отдает нагрузку CPU по ядрам на Windows 11/Windows Editor через `NtQuerySystemInformation`, на Apple platforms через `host_processor_info` и на Android/Linux через `/proc/stat`.
- Lifecycle/modes: `PerformanceMeter.EnsureRunning()`, `PerformanceMeter.Stop()`, `PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode mode)` и `PerformanceMeter.CollectionMode` управляют collection modes `Stopped`, `Background`, `Overlay` и `OverdrawDiagnostic`.
- Overlay: `PerformanceMeter.SetOverlayVisible(bool visible)`, `PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme theme)`, `PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout layout)`, `PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily fontFamily)`, `PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset preset)`, `PerformanceMeter.SetOverlayModules(PerfMeterOverlayModule modules)`, `PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)`, `PerformanceMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerformanceMeter.IsOverlayVisible`, `PerformanceMeter.OverlayCorner`, `PerformanceMeter.OverlayMode`, `PerformanceMeter.OverlayTheme`, `PerformanceMeter.OverlayLayout`, `PerformanceMeter.OverlayFontFamily`, `PerformanceMeter.OverlayPreset`, `PerformanceMeter.OverlayModules`, `PerformanceMeter.TargetFps` и status snapshot поля `OverlayVisible` / `OverlayCorner` / `OverlayMode` / `OverlayTheme` / `OverlayLayout` / `OverlayFontFamily` / `OverlayPreset` / `OverlayModules` / `TargetFps`
- Overdraw: `PerformanceMeter.RequestOverdrawMeasurement(int frameCount = 0)`, `PerformanceMeter.CancelOverdrawMeasurement()`, `PerformanceMeter.SetOverdrawHeatmapVisible(bool visible)` и `PerformanceMeter.IsOverdrawHeatmapVisible`

Запросы безопасны до запуска runtime: обычное чтение возвращает snapshot со `State = Stopped` и не должно бросать исключения.

## Setup window

Откройте `SGG/Perfmeter/Setup`, чтобы подготовить проект без ручного редактирования URP renderer asset.

- `Project Settings` показывает состояние `Frame Timing Stats` и официальный target поддержки. Unity ниже `6000.4` показываются как import-safe only и unsupported.
- `URP Renderer Features` сначала показывает активные URP renderer assets из Graphics/Quality settings, затем renderer assets из `Assets`, со статусом installed/missing/not-editable; он может добавить `PerfMeterRenderGraphFeature` всем editable missing renderers или только выбранным renderers без создания дублей.
- Вкладка `Presets` создает и редактирует project-owned JSON settings в `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`; runtime читает его как `Resources.Load<TextAsset>("SGG.PerfMeter/perfmeter-settings")`. `ScriptableObject` настройки не используются. Вкладка также выбирает active overlay preset (`Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug`, `Custom`), сохраняет overlay modules, выбирает `Overlay Theme` / `Overlay Layout` / `Overlay Font` и редактирует overlay/rule/session/overdraw tunables.
- Zero-code setup включается через этот JSON: если `enabled` и `autoStart` включены, runtime auto-start применяет collection mode, overlay visible/corner/mode/theme/layout/font/target FPS/preset/modules, tuning, alert defaults, session defaults и overdraw limits без ручного bootstrap-кода.
- `Initialization Code` показывает bootstrap-код для запуска runtime overlay; настройки `Overlay Visible`, `Target FPS`, `Overlay Corner`, `Overlay Mode`, `Overlay Theme`, `Overlay Layout` и `Overlay Font` сразу меняют код, который копирует `Copy Init Code`.
- Вкладка `Runtime` предназначена для Play Mode: в Edit Mode кнопки отключены, а в Play Mode можно переключать collection mode, target FPS, режим/угол/theme/layout/font overlay, скрывать или показывать overlay, запускать короткое измерение overdraw и включать heatmap.

Те же действия доступны агентам и Editor-скриптам без открытия окна через публичный Editor API:

```csharp
using SGG.PerfMeter.Editor.Setup;
using UnityEngine;

Debug.Log(PerfMeterSetupActions.GetStatusReport());
Debug.Log(PerfMeterSetupActions.RunRecommendedSetup());
Debug.Log(PerfMeterSetupActions.CreateDefaultSettings());
Debug.Log(PerfMeterSetupActions.CopyInitializationSnippetToClipboard());
```

`RunRecommendedSetup()` включает `Frame Timing Stats`, устанавливает renderer feature в editable URP renderers и сохраняет default JSON settings для zero-code setup. Если проект не должен запускать PerfMeter автоматически, выключите `Auto Start` на вкладке `Presets` и сохраните JSON.

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
	{
		PerformanceMeter.EnsureRunning();
		PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
		PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
		PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
		PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.ClassicDark);
		PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.Classic);
		PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.Manrope);
		PerformanceMeter.SetOverlayVisible(true);
	}
}
```

## Samples

Импортируйте samples через Package Manager или копируйте их из `Assets/Scripts/SGG.PerfMeter/Samples~` при локальной разработке из этого репозитория.

- `Bootstrap and Zero-Code Settings` содержит минимальный bootstrap component и пример zero-code settings в `Resources/SGG.PerfMeter/perfmeter-settings.json`, включая overlay/rule/session/overdraw tunables.
- `Runtime Workflows` содержит примеры overlay preset switching, bounded session JSON/CSV export, alert callback, overdraw/heatmap controls и camera snapshot replay.
- `Editor and MCP Automation` содержит setup menu actions и MCP command examples для agent-driven runs.

## Runtime-метрики

Runtime singleton обновляет snapshots в `Update()` реальными значениями из `FrameTimingManager` и `ProfilerRecorder`. Путь сбора метрик избегает per-frame allocation со стороны PerfMeter; overlay text refresh throttled и использует переиспользуемые field rows, поэтому неизменившиеся labels не получают новые managed strings.

`CollectionFrame` - это `Time.frameCount`, на котором PerfMeter собрал snapshot. Это не гарантированно тот же кадр, который описывает `FrameTimingManager`, потому что Unity frame timings могут приходить с задержкой в несколько кадров. При потере фокуса/паузы сбор временно пропускает кадры, а после возврата фокуса игнорирует короткое warm-up окно, чтобы stale timings не попали в FPS, session samples, alerts и overlay history.

- Тайминги: `CpuFrameTimeMs`, `CpuMainThreadFrameTimeMs`, `CpuRenderThreadFrameTimeMs`, `CpuMainThreadPresentWaitTimeMs`, `GpuFrameTimeMs`.
- FPS-статистика: `AverageFps`, `OnePercentLowFps`, `PointOnePercentLowFps`, `FrameSampleCount`, `GpuValidSampleCount`, `FrameSpikeCount`, `SevereFrameSpikeCount`; источник - валидный `FrameTiming.cpuFrameTime` в диапазоне `(0, 60000] ms`, не `Time.deltaTime`.
- Render counters: `DrawCalls`, `SetPassCalls`, `Batches`, `Vertices`, `SrpBatcherInstances`.
- BRG/GRD counters, если доступны: `BrgDrawCalls`, `BrgInstances`, `IndexBufferUploadInFrameBytes` через `PerfMeterStatusSnapshot.AvailableCounters` / `UnavailableCounters`.
- Memory counters: `SystemUsedMemoryBytes`, `GcReservedMemoryBytes`, `GpuMemoryBytes`.
- Классификация: `Bottleneck` (`GpuBound`, `CpuMainThreadBound`, `CpuRenderThreadBound`, `PresentLimited`, `Balanced`, `Unknown`) с текущим `FrameBudgetMs`, который определяется `PerfMeterTargetFps`; `PresentLimited` означает заметное ожидание present/VSync/frame pacing при CPU/GPU work ниже бюджета.
- Overdraw: `OverdrawState`, `OverdrawProgress` и `OverdrawRatio` доступны в status/metrics snapshots; `OverdrawHeatmapVisible` доступен в status snapshots для чтения агентами без UI и управления visual heatmap.
- Collection mode: `PerfMeterStatusSnapshot.CollectionMode` сообщает `Stopped`, `Background`, `Overlay` или `OverdrawDiagnostic`. `Background` сохраняет metrics/session collection без видимого overlay.
- Device/environment snapshot: `PerfMeterDeviceSnapshot` содержит Unity version, platform, OS, CPU/RAM, GPU/API/capabilities, screen/current resolution/fullscreen state, main window position, render-safe display layout state и список `PerfMeterDisplaySnapshot` с системными названиями мониторов через `Screen.GetDisplayLayout(List<DisplayInfo>)`. Если layout недоступен, используется fallback из `Screen.currentResolution`.
- Camera snapshot: `PerfMeterCameraSnapshot` содержит camera name/id, scene name/path, position, rotation quaternion, Euler angles, forward/up vectors, projection, FOV/orthographic size, clip planes, aspect, pixel rect, target display, depth, clear flags, culling mask, HDR/MSAA flags и URP `UniversalAdditionalCameraData`, если компонент уже есть на камере.
- Render Graph snapshot: `PerfMeterRenderGraphSnapshot` сообщает, наблюдался ли `PerfMeterRenderGraphFeature.RecordRenderGraph`, последний frame/camera, запрошенные PerfMeter pass markers для этого кадра и консервативные reflected pass/resource/aliasing/merge counts, если Unity их раскрывает. Если URP internals нельзя безопасно прочитать, counts равны `-1`, а `Warning` описывает degraded state.
- Session summary/export: `PerfMeterSessionSummarySnapshot` содержит sample count, dropped sample count, first/last frame, duration, average/min/max frame time и FPS, bottleneck/spike counts, focus loss/pause counts, суммарную длительность focus/pause gap, warnings, стартовые settings/device/camera metadata, scene names, whole-run/current-scene scope summaries и worst-frame summaries. `PerfMeterSessionOptions` задает `WarmupFrames`, `WarmupSeconds`, `SampleIntervalSeconds`, `MaxSamples`, `ResetOnSceneLoad`, `SceneLoadIgnoreFrames` и `SceneLoadIgnoreSeconds`; zero-code defaults для них сохраняются в JSON. `GetSessionSamples()` возвращает copy массива samples, а `ExportSessionJson(path)` / `ExportSessionCsv(path)` пишут schema/package marker, summary/options/metadata и rows с CPU/GPU/FPS/render/SRP/BRG/upload/memory/overdraw/warning/counter availability metrics. JSON export также включает `custom_metrics` в каждом sample и `custom_metric_sample_count` в metadata.
- Alerts: default rules следят за CPU/GPU frame budget, FPS ниже target, unavailable GPU timing и высоким overdraw ratio. Threshold overdraw ratio, consecutive frame counts, structured log, callback и Editor warning cooldowns берутся из JSON settings, чтобы Editor Console не спамился каждый кадр. `PerfMeterStatusSnapshot` содержит `ActiveAlertCount`, `FiredAlertCount`, `LatestAlertRuleId` и `LatestAlertMessage`.
- Custom metrics: зарегистрированные `IPerfMeterCustomMetricProvider` вызываются во время runtime collection. Исключения провайдера превращаются в unavailable `PerfMeterCustomMetricSnapshot` с `Warning`, поэтому они не прерывают основной сбор метрик или запись сессии. Custom metrics доступны через API, session JSON export, MCP `perfmeter.metrics.latest` и до восьми строк overlay при включенном модуле `CustomMetrics`.

## MCP commands

- `perfmeter.runtime.reset_stats` сбрасывает rolling runtime stats, alert counters и active session counters без остановки runtime.
- `perfmeter.runtime.mode.set` переключает `Stopped`, `Background`, `Overlay` или `OverdrawDiagnostic`; `OverdrawDiagnostic` принимает опциональный `frame_count`.
- `perfmeter.session.start` запускает bounded recording и принимает опциональные `warmup_frames`, `warmup_seconds`, `sample_interval_seconds`, `max_samples`, `reset_on_scene_load`, `scene_load_ignore_frames` и `scene_load_ignore_seconds`.
- `perfmeter.session.stop` останавливает запись и возвращает summary.
- `perfmeter.session.summary` возвращает текущий summary без изменения состояния.
- `perfmeter.session.export` принимает project-local `path` и `format` (`json`/`csv`), не пишет за пределы проекта и не перезаписывает существующий файл.
- `perfmeter.alerts.latest` возвращает active alerts, alert counters, status fields и Editor state.
- `perfmeter.alerts.clear` очищает active alerts, counters и per-rule cooldown state.
- `perfmeter.metrics.latest` возвращает `custom_metrics` для зарегистрированных project-specific providers.
- `perfmeter.rendergraph.snapshot` возвращает последний наблюдавшийся Render Graph feature snapshot и не запускает runtime.

На OpenGL/OpenGLES аппаратный GPU timing может быть недоступен или ненадежен. В этом случае `GpuFrameTimeAvailable` будет `false`, а `PerfMeterStatusSnapshot.Warning` содержит предупреждение; для Android предпочтителен Vulkan.

## Overlay

Runtime overlay создается программно на UI Toolkit (`UIDocument`, `PanelSettings`, `VisualElement`, `Label`). Он создается только во время Play Mode/runtime; EditMode API-вызовы остаются безопасными и не создают видимый UI document.

- Layout абсолютный, `PickingMode.Ignore`, поэтому overlay не перехватывает input игры; в графических режимах overlay состоит из широкого graph block сверху и строки с text/side panel снизу.
- По умолчанию overlay находится в правом верхнем углу (`TopRight`) и режиме `Full`; доступные углы: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`.
- Режимы: `FpsOnly` показывает одну строку FPS/1%/0.1%, `TextCompact` показывает компактную текстовую сводку, `Graphs` показывает FPS и CPU/GPU графики, `Full` добавляет счетчики render/memory/overdraw.
- Оформление выбирается через `Overlay Theme` (`ClassicDark`, `Glass`, `Cyber`, `HighContrast`), `Overlay Layout` (`Classic`, `CompactCards`, `DiagnosticsWide`, `OverdrawFocus`, `MetricBars`) и `Overlay Font` (`Manrope`, `JetBrainsMono`, `LegacyRuntime`) во вкладках `Presets` и `Runtime` окна setup. `MetricBars` заменяет нижний text block тонкими realtime-полосками: min/max стоят по краям бара, current остается цветным, на баре видны отдельные risk ticks для average/1%/0.1%, а справа идут average, 1% и 0.1% фиксированными колонками с пробелами между ними.
- Presets группируют режим и набор module flags: `Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug` и `Custom`. CPU-core modules включаются только вручную. Module flags (`Fps`, `Timing`, `Graphs`, `Rendering`, `SrpBatcher`, `Brg`, `Uploads`, `Memory`, `Gc`, `GpuMemory`, `Overdraw`, `Heatmap`, `Warnings`, `CustomMetrics`, `CpuCores`, `CpuCoreBars`, `CpuCoreGraphs`) фильтруют строки и панели overlay и скрывают graph block, когда `Graphs` отключен. `CpuCores` оставляет legacy rows внутри `MetricBars`; `CpuCoreBars` показывает отдельную компактную боковую панель percent bars рядом с основным text/MetricBars block; `CpuCoreGraphs` показывает отдельную task-manager-style сетку per-core графиков.
- Text fields обновляются по JSON-настраиваемому interval из последних runtime snapshots, а не каждый кадр; default interval равен 0.25 seconds.
- Text block разбит на стабильные labels с именами полей и value labels; enum names кэшируются, числа форматируются через переиспользуемый buffer, а value label получает новое значение только при изменении текста вместо пересборки одного большого `StringBuilder.ToString()` block.
- Графики рисуются через UI Toolkit `generateVisualContent`; CPU-граф использует stacked area для `render`, `main` и остатка до `frame`, а `frame` рисуется верхней границей без суммирования `frame + main + render`.
- Справа от графиков выводятся цветные подписи-плашки одинаковой ширины `frame`, `other`, `main`, `render` и `gpu`; значения идут на темной полосе колонками current, average, худшие 1% и 0.1% только с пробелами между ними.
- Если GPU timing временно недоступен, GPU-плашка становится серой, текущее значение заменяется underscore placeholder, а средние/история используют только валидные samples. Невалидные `FrameTimingManager` samples и focus/pause gaps не добавляются в графики и min/max историю.
- Целевой FPS выбирается из `15/30/60/90/120/144/240`; соответствующий `FrameBudgetMs` задает красную target line и target label слева от графика и участвует в масштабе `max(averageTimeMs * 1.1, FrameBudgetMs * 1.2)`.
- Текстовые режимы показывают current/min/max за внутреннее окно истории overlay для timing, render counters и memory; graph/history length настраивается в JSON.
- Предупреждения удерживаются короткое время, чтобы transient GPU timing gaps не мигали каждый refresh.
- В `Full` отображаются state, bottleneck, FPS/lows/spikes, CPU/GPU timings, draw calls, SetPass, batches, vertices, SRP/BRG counters, index uploads, overdraw state/progress/ratio, memory, опциональные строки/панели нагрузки CPU по ядрам на Windows/Apple/Android/Linux, до восьми строк custom metrics при включенном модуле и warning text при наличии. CPU core bars и graph lines используют цвет нагрузки: зеленый ниже 25%, желтый от 25% до 75%, красный выше 75%. На Apple Silicon ядра показываются плоским списком logical cores; PerfMeter не маркирует E/P cores.
- `PerformanceMeter.SetOverlayVisible(false)` скрывает retained UI без остановки сбора метрик; `PerformanceMeter.SetOverlayVisible(true)` гарантирует запуск runtime и показывает overlay в Play Mode.
- `PerformanceMeter.IsOverlayVisible` и `PerfMeterStatusSnapshot.OverlayVisible` возвращают фактическое состояние видимости overlay.

## URP Render Graph Renderer Feature

`PerfMeterRenderGraphFeature` добавляет opt-in URP 17 Render Graph marker pass с dedicated profiling sampler `SGG.PerfMeter.Overlay`, opt-in pass для численного измерения overdraw и visual heatmap pass. Overlay marker по умолчанию отключен, потому что нужен только для diagnostic/self-overhead measurement и будущего вычитания overhead самого инструмента через `ProfilerRecorder`.

- Предпочтительный способ установки: `SGG/Perfmeter/Setup` -> выберите missing renderers или используйте `Install All Missing`.
- Renderer assets внутри `Packages` показываются как not editable; если package-owned renderer должен содержать feature, скопируйте или настройте его вручную.
- Ручной способ: добавьте feature в renderer asset, например `Assets/Settings/PC_Renderer.asset` для PC или `Assets/Settings/Mobile_Renderer.asset` для Mobile.
- В Inspector renderer asset выберите `Add Renderer Feature` -> `Perf Meter Render Graph Feature`.
- Оставьте `Enabled` включенным, `Render Pass Event` по умолчанию `AfterRenderingPostProcessing` или переключите на `AfterRendering`, если нужен самый поздний marker после всего кадра.
- `Marker Name` по умолчанию `SGG.PerfMeter.Overlay`; меняйте его только если downstream `ProfilerRecorder` будет искать другое имя.
- Включайте `Record Overlay Marker Pass` только для диагностики self-overhead PerfMeter; численное измерение overdraw не требует пустой marker pass.
- Overdraw по умолчанию ограничен `Game Cameras Only`; задайте `Camera Name Filter`, если в multi-camera проекте нужно мерить только одну камеру.
- Feature использует `RecordRenderGraph(RenderGraph, ContextContainer)` и `AddRasterRenderPass`; legacy-only path не используется.

## Измерение Overdraw

Измерение overdraw является opt-in и ограничено по времени. Вызовите `PerformanceMeter.RequestOverdrawMeasurement()` для JSON-настраиваемого окна по умолчанию или передайте свое положительное значение `frameCount`; runtime ограничит его JSON-полем `maxFrameCount`. Для ранней остановки используйте `PerformanceMeter.CancelOverdrawMeasurement()`.

- Runtime по умолчанию находится в `Off` и не записывает численный overdraw pass, пока нет активного запроса.
- `PerfMeterRenderGraphFeature` должен быть добавлен в активный URP renderer. Во время активного запроса он записывает Render Graph raster pass с profiling marker `SGG.PerfMeter.Overdraw`.
- Pass повторно рендерит scene renderer list с hidden replacement shader `Hidden/SGG/PerfMeter/OverdrawCounter`: `ZTest Always`, `ZWrite Off`, `ColorMask 0`.
- Fragment shader выполняет atomic increment в GPU `GraphicsBuffer`, затем readback pass вызывает `AsyncGPUReadback` без CPU stall.
- `OverdrawRatio` считается как `TotalFragments / RenderedCameraPixels` и усредняется по завершенным readback samples.
- Async readbacks защищены measurement session id, поэтому stale callbacks от отмененного или перезапущенного измерения игнорируются.
- Shader требует поддержку fragment UAV/storage buffer, compute shaders, поддерживаемый graphics API и `AsyncGPUReadback`; неподдерживаемые targets переходят в `Unsupported` с предупреждением до планирования Render Graph pass.
- Visual heatmap управляется отдельно через `PerformanceMeter.SetOverdrawHeatmapVisible(true/false)`. Pass повторно рендерит scene renderer list с `Hidden/SGG/PerfMeter/OverdrawHeatmap`, `ZTest Always`, `ZWrite Off` и additive blending прямо поверх активного camera color target. Чем ярче/теплее пиксель, тем больше повторное fragment coverage.
- Heatmap pass только визуальный: он не обновляет `OverdrawRatio` и может оставаться включенным без активного численного измерения.
- Progress растет после async readback, запланированного из Render Graph overdraw pass. Если progress остается `0`, проверьте, что renderer feature установлен в renderer, который использует активная камера, и что target backend поддерживает shader instrumentation.

```csharp
using SGG.PerfMeter;

PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
PerformanceMeter.RequestOverdrawMeasurement();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status))
{
	UnityEngine.Debug.Log($"PerfMeter: {status.State}, overdraw {status.OverdrawState} {status.OverdrawProgress:P0}, heatmap {status.OverdrawHeatmapVisible}, {status.Warning}");
}

if (PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics))
{
	UnityEngine.Debug.Log($"GPU {metrics.GpuFrameTimeMs:0.00} ms, Draws {metrics.DrawCalls}, Overdraw {metrics.OverdrawRatio:0.00}");
}
```

## Лицензия

Этот пакет лицензирован по **Stinger Royalty-Free EULA 1.0**.

- Основная версия: русский текст в [`LICENSE.ru.md`](../LICENSE.ru.md).
- Английский справочный текст: [`LICENSE.md`](../LICENSE.md), для удобства чтения.
- Уведомления проекта: [`NOTICE.md`](../NOTICE.md) и [`NOTICE.ru.md`](../NOTICE.ru.md).
- SPDX identifier: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.
- Лицензиар: ROMAN ILYIN.
- Канонический репозиторий: https://github.com/romanilyin/sgg-perfmeter.

Бесплатно для личных, внутренних, открытых и коммерческих Конечных продуктов. Без роялти. Standalone-продажа, перепродажа, платное перераспространение или отдельная коммерциализация этого Актива или Производных активов запрещены.

Русская версия EULA является основной и имеет преимущественную силу. При расхождении, противоречии или различии толкования между русской и английской версиями применяется русская версия.
