# План развития SGG PerfMeter по итогам сравнения с AFPS и Graphy

Источник: исходное сравнение `_Docs/sgg-perfmeter-competitor-comparison.md` и актуализированный локальный reference от 2026-05-19.
Дата: 2026-05-19  
Статус: historical roadmap / implementation mostly completed после iteration 43. Current release-facing comparison lives in `_Docs/marketing/`.

Примечание: разделы с `[Done]` ниже оставлены как historical implementation requirements, а не как новые задачи. Не начинать повторную реализацию этих пунктов без отдельного запроса.

## Главный вывод

SGG PerfMeter не должен превращаться в еще один универсальный FPS overlay. Его сильная позиция: Unity `6000.4+`, URP `17.4+`, Render Graph diagnostics, `FrameTimingManager`, `ProfilerRecorder`, bottleneck classification, overdraw diagnostics и agent-readable API/MCP.

Из Advanced FPS Counter и Graphy берем не архитектуру uGUI, а продуктовые идеи:

- удобные пресеты отображения;
- конфигурируемость через окно настройки;
- device/environment info;
- session/benchmark recording;
- rule/debug alerts;
- примеры и быстрый bootstrap.

Хоткеи не добавляем. Звук/audio alerts/modules не добавляем.

## Что уже есть и сохраняется как база

- Public API: `PerformanceMeter.GetStatus()`, `GetLatestMetrics()`, `EnsureRunning()`, `Stop()`.
- Метрики: FPS/lows/spikes, CPU/GPU timings, render counters, SRP Batcher, BRG/GRD, index upload, memory.
- Bottleneck classification: GPU, CPU main, CPU render, present/VSync limited, balanced/unknown.
- UI Toolkit overlay с режимами `FpsOnly`, `TextCompact`, `Graphs`, `Full`.
- JSON-backed overlay presets/modules: `Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug`, `Custom`.
- Project-owned JSON settings и zero-code auto-start через `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Device/environment snapshot с monitor names.
- Camera snapshot для воспроизводимых performance captures.
- URP Render Graph feature.
- Численное overdraw measurement и visual heatmap.
- MCP команды для setup/runtime/status/metrics/device/camera/overlay/overdraw.

## P0: текущий фокус после iterations 28-31

### 1. [Done] JSON-настройки PerfMeter и вкладка Presets

`ScriptableObject` для настроек не используем. Нужен JSON-конфиг, который редактируется через существующее окно `SGG/Perfmeter/Setup` в отдельной вкладке `Presets`.

Фактический путь файла: `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`. Runtime загружает его через `Resources.Load<TextAsset>("SGG.PerfMeter/perfmeter-settings")`, поэтому JSON попадает в билд и остается project-owned, даже если пакет установлен через UPM.

Содержимое JSON:

- overlay visible/mode/corner/preset;
- target FPS;
- overlay scale, opacity, font size, refresh interval;
- graph history length;
- warning thresholds;
- default rules;
- enabled modules/counters;
- session recorder defaults;
- overdraw safety limits;
- zero-code setup defaults.

Вкладка `Presets` в setup window должна:

- создавать JSON-настройки, если их нет;
- показывать текущий активный preset;
- редактировать overlay preset и module visibility;
- редактировать rule thresholds и cooldowns;
- редактировать session defaults;
- применять настройки в Play Mode без ручного кода;
- генерировать или обновлять zero-code setup.

Zero-code setup нужен через то же окно. Предпочтительный вариант: setup window создает runtime bootstrap/initializer asset or component в проекте и настраивает его через JSON, чтобы пользователь мог включить PerfMeter без написания bootstrap-кода. При этом публичный API остается рабочим для ручной и agent-driven настройки.

Важно: JSON-настройки должны быть версионируемыми. Добавить поле `schemaVersion`, безопасный fallback при неизвестной версии и human-readable формат без бинарных Unity dependencies.

### 2. [Done] Session recorder и CSV/JSON export

Добавляем повторяемые profiling-сессии, чтобы PerfMeter был не только overlay, но и инструментом валидации.

Форма API:

```csharp
PerformanceMeter.StartSession(options);
PerformanceMeter.StopSession();
PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson(path);
PerformanceMeter.ExportSessionCsv(path);
```

MCP:

- `perfmeter.session.start`
- `perfmeter.session.stop`
- `perfmeter.session.summary`
- `perfmeter.session.export`

Содержимое сессии:

- timestamp, frame, scene name;
- Unity version, platform, graphics API;
- active URP asset/renderer, если доступно;
- CPU frame/main/render/present wait;
- GPU frame time и флаг доступности;
- average FPS, 1% low, 0.1% low;
- spikes/severe spikes;
- draw calls, SetPass, batches, vertices;
- SRP Batcher, BRG/GRD, index upload;
- system/GC/GPU memory;
- bottleneck classification;
- overdraw result, если измерялся;
- warnings и unavailable counters;
- camera snapshot для воспроизведения точки теста.

Важно: запись должна иметь sample interval и max samples, чтобы не создать бесконечный рост памяти. Export может аллоцировать при завершении, но runtime collection должен оставаться легким.

### 3. [Done] Camera snapshot для воспроизводимых тестов

В каждый snapshot сессии нужно добавлять состояние камеры, чтобы позже можно было воспроизвести тест в той же точке.

Минимальный набор:

- camera name;
- camera instance id;
- scene path/name;
- position;
- rotation quaternion;
- euler angles для удобства чтения;
- forward/up vectors;
- projection type: perspective/orthographic;
- field of view;
- orthographic size;
- near/far clip;
- aspect;
- pixel rect;
- target display;
- depth;
- clear flags;
- culling mask;
- URP additional camera data, если безопасно доступно: render type, renderer index, render post-processing, antialiasing, stop NaN, render shadows.

Для multi-camera сцен надо явно выбрать source camera:

- по умолчанию main/game camera, совпадающая с камерой, для которой идет Render Graph pass;
- опциональный camera-name filter;
- в export хранить все релевантные камеры или хотя бы active sampled camera плюс список обнаруженных game cameras.

Назначение: session export должен позволять агенту или тестовому bootstrap вернуть сцену и камеру в нужное положение перед повторным измерением.

### 4. [Done] Rule/alert system без audio

Берем идею Graphy Debugger, но адаптируем под структурированные метрики SGG.

Форма:

```csharp
public readonly struct PerfMeterRule
{
    public string Id;
    public PerfMeterMetric Metric;
    public PerfMeterComparison Comparison;
    public double Threshold;
    public int ConsecutiveFrames;
    public float CooldownSeconds;
}
```

Типовые правила:

- FPS ниже target;
- 1% low ниже порога;
- GPU frame time выше бюджета;
- CPU main/render выше бюджета;
- present wait высокий;
- draw calls / SetPass выше порога;
- GPU timing unavailable;
- overdraw ratio выше порога;
- memory growth за сессию.

Действия:

- добавить alert в status snapshot;
- structured log;
- C# callback;
- Editor warning;
- screenshot только в Editor/Development build;
- MCP `perfmeter.alerts.latest`.

Editor warning обязательно должен иметь cooldown. Нельзя писать предупреждение в Editor Console каждый кадр или каждый refresh. Cooldown должен быть настраиваемым в JSON и во вкладке `Presets`, а также иметь безопасное значение по умолчанию, например 5-10 секунд на rule id.

Для cooldown хранить last-fired time per rule/action. Желательно разделить cooldown для `EditorWarning`, `StructuredLog` и `Callback`, чтобы частые callbacks не включали спам в Console.

Не добавлять: звук/audio actions. Не добавлять по умолчанию: `Debug.Break()`, screenshot/debug-break alerts, тяжелые side effects.

### 5. [Done] Overlay presets и module visibility

Текущие режимы отвечают за плотность/вид layout. Нужно добавить presets, которые отвечают за смысловой набор модулей.

Пресеты:

- `Minimal`: FPS, 1% low, bottleneck;
- `Timing`: CPU frame/main/render/present/GPU + graph;
- `Rendering`: draw calls, SetPass, batches, vertices, SRP/BRG/uploads;
- `Memory`: system, GC, GPU memory;
- `Overdraw`: state, progress, ratio, heatmap status;
- `FullDiagnostics`: текущий full набор;
- `AgentDebug`: компактный набор для скриншотов/агентов.

API:

```csharp
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.Rendering);
PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.Memory, false);
```

MCP должен зеркалировать это, чтобы агент мог переключить overlay перед скриншотом.

Настройка presets идет через вкладку `Presets` в setup window и сохраняется в JSON. `ScriptableObject` для presets не используем.

### 6. [Done] Device/environment snapshot

AFPS и Graphy хорошо показывают device info. Добавляем это как structured snapshot.

API:

```csharp
PerformanceMeter.GetDeviceInfo();
```

MCP:

- `perfmeter.device.info`

Поля:

- Unity version;
- platform/runtime platform;
- graphics API;
- graphics device name/vendor/version, если доступно;
- compute support;
- async readback support;
- CPU name/core count;
- system memory;
- screen size, current resolution, refresh rate, DPI;
- full screen mode;
- main window position;
- main window display info;
- список мониторов/displays;
- URP asset/renderer name, если доступно;
- Frame Timing Stats status/availability;
- XR status, если безопасно получить.

Информация о мониторах должна включать системные названия мониторов. В проекте `sggtactics` уже есть наш код, который использует `Screen.GetDisplayLayout(List<DisplayInfo>)`, читает `DisplayInfo.name`, `width`, `height`, `workArea`, `refreshRate`, сравнивает с `Screen.mainWindowDisplayInfo` и при пустом списке делает fallback из `Screen.currentResolution`. Этот подход можно скопировать или написать по аналогии.

Для каждого display сохранять:

- index;
- `DisplayInfo.name`;
- width/height;
- work area;
- refresh rate numerator/denominator/value;
- is main window display;
- is current target display, если можно сопоставить.

Использование:

- status summary;
- session export metadata;
- optional overlay module;
- MCP output.

## P0 continuation: recorder/runtime hardening

### 7. [Done] Warm-up, reset, min/max и scene scope

Добавить:

- warm-up frames/seconds перед session recording;
- `PerformanceMeter.ResetStats()`;
- reset on scene load;
- min/max frame time/FPS в summary;
- отдельная статистика current scene и whole run.

### 8. [Done] Background/headless mode

Сейчас overlay можно скрыть без остановки сбора. Нужно формализовать режимы:

- `Overlay`: сбор + UI;
- `Background`: сбор без UI;
- `Stopped`: нет сбора;
- `OverdrawDiagnostic`: временное дорогое состояние.

Это важно для билдов, автоматических тестов и agent runs. Zero-code setup должен уметь запускать runtime сразу в `Background`, если пользователь не хочет видимый overlay.

### 9. [Done] Allocation-conscious overlay refresh

Конкуренты оптимизируют строки и labels, но используют uGUI. Мы оставляем UI Toolkit.

Добавить:

- раздельные stable labels вместо одного большого text block;
- cached enum strings;
- cached field names;
- dirty updates per field;
- переиспользуемые buffers для чисел;
- validation checklist/tests по GC Alloc.

Цель: убрать текущую пересборку managed string через `StringBuilder.ToString()` при refresh.

### 10. [Done] Samples

Добавить package samples:

- minimal bootstrap;
- zero-code setup example;
- overlay preset switching;
- session recording/export;
- rules/alerts;
- overdraw measurement/heatmap;
- MCP/editor automation demo;
- camera snapshot replay example.

Лучшее место: `Samples~/...` внутри пакета.

### 11. [Done] JSON-tunable UI/rule/session settings

Расширять JSON постепенно, без `ScriptableObject` settings:

- overlay scale;
- opacity/background;
- font size;
- refresh interval;
- graph history length;
- warning thresholds;
- default rule set;
- session sample interval/max samples;
- overdraw default/max frame count.

### 12. [Done] Custom metric providers

Опциональная extension point для project-specific counters без форка PerfMeter:

```csharp
public interface IPerfMeterCustomMetricProvider
{
    string Id { get; }
    bool TryCollect(out PerfMeterCustomMetricSnapshot metric);
}
```

Первый проход реализован как export/MCP-first extension point: API регистрации/снятия провайдеров, safe exception handling, `custom_metrics` в session JSON samples и MCP latest metrics output. Затем добавлена ограниченная overlay support через модуль `CustomMetrics`: до восьми text rows без превращения overlay в generic dashboard.

## P2: опционально

- World-space/XR overlay, только если появится явный XR target.
- Alternative graph backend, только если UI Toolkit graph cost станет проблемой.
- Web/server export target только после стабильного file export.
- CI artifacts integration только после стабильного session export.
- [Done, spike] Render Graph pass/aliasing/merge analytics: добавлен safe snapshot/API/MCP для наблюдения `PerfMeterRenderGraphFeature` и консервативных reflected counters; если URP internals недоступны, counters возвращаются как degraded `-1` с warning.

## Что не добавляем

- uGUI overlay architecture.
- IMGUI runtime overlay.
- Audio/spectrum module.
- Sound alerts/audio actions.
- Circle gesture toggles.
- Player hotkeys.
- `ScriptableObject` settings/presets.
- Поддержку старых Unity версий ради широты.
- Built-in/HDRP как равноправные targets.
- Force FPS как core feature.
- Screenshot/debug-break alerts по умолчанию.
- Editor warning без cooldown.
- Прямое копирование кода AFPS.
- Импорт Graphy как зависимости.

## Нужны ли исходники AFPS и Graphy

### AFPS

Исходники AFPS не нужны для реализации плана и юридически лучше их не использовать как источник кода.

Причины:

- в сравнении указано, что у загруженного AFPS package нет license file;
- безопаснее считать AFPS commercial/Asset Store reference;
- можно использовать продуктовые идеи: набор counters, UX настроек, device info;
- нельзя копировать реализацию, классы, assets, shaders, formatting code без явного разрешения лицензии.

Если у нас есть легально приобретенный пакет, его можно использовать только как reference поведения: какие настройки удобны пользователям, какие сценарии покрыты, как выглядит Inspector. Для реализации SGG лучше делать independent implementation.

### Graphy

Исходники Graphy не обязательны, но полезны как reference, потому что Graphy MIT.

Можно анализировать:

- module/preset UX;
- `GraphyDebugger` как концепцию rule/actions;
- benchmark/export roadmap ideas;
- приемы non-alloc formatting.

Но даже с MIT лучше не переносить старую uGUI архитектуру. Если решим копировать конкретный код, придется сохранить MIT attribution. Предпочтительный путь: clean-room реализация идей без копирования кода.

### Когда исходники действительно понадобятся

Исходники/пакеты конкурентов понадобятся только если мы захотим:

- делать точный measured benchmark overhead SGG vs AFPS vs Graphy;
- проверять feature parity по API/UX;
- сравнивать GC Alloc и draw calls конкурентов на одной сцене;
- документировать конкретные отличия не как product comparison, а как измеренный отчет.

Для текущего roadmap и реализации P0/P1 исходники не являются блокером.

## Рекомендуемый порядок итераций

1. Session recorder core: start/stop, bounded samples, summary snapshot, device/camera/settings metadata. `[Done]`
2. Session JSON/CSV export + MCP `perfmeter.session.*`. `[Done]`
3. Rule/alert system + MCP alerts + обязательный cooldown для Editor warning; без sound/audio actions. `[Done]`
4. Session warm-up, reset APIs, scene scope и worst-frame summaries. `[Done]`
5. Explicit background/headless collection mode: `Stopped`, `Background`, `Overlay`, `OverdrawDiagnostic`. `[Done]`
6. Allocation-conscious overlay refresh path и GC Alloc validation checklist/test scene. `[Done]`
7. `Samples~` workflows и bilingual documentation для bootstrap/settings/session/alerts/overdraw/MCP. `[Done]`
8. JSON-tunable UI/rule/session settings. `[Done]`
9. Custom metric providers для export/MCP-first extension point. `[Done]`

## Документация и проверки

При каждой итерации обновлять:

- `.Documentation/README.ru.md`;
- `.Documentation/README.en.md`;
- `.Documentation/STATUS.ru.md`;
- `.Documentation/STATUS.en.md`;
- `_Docs/perfmeter_status.md`;
- `_Docs/perfmeter_iterations.md`.

Проверки:

- Unity batchmode compile;
- EditMode tests для API/summary/rules/settings;
- PlayMode smoke для runtime/session/overlay/device snapshot;
- Android Vulkan smoke для timing/session/overdraw;
- GLES fallback для degraded states.
