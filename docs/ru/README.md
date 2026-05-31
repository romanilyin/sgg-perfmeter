# Документация SGG PerfMeter

SGG PerfMeter - runtime diagnostics layer и agent-readable profiling API для Unity `6000.4+` URP `17.4+` Render Graph проектов.

Пакет объединяет UI Toolkit overlay, structured snapshots, session export, overdraw diagnostics, alerts, custom metrics и MCP command metadata. Overlay - только один способ вывода; те же данные доступны через публичный C# API и automation-friendly snapshots.

## С чего начать

- [Установка](./installation.md): подключить Unity package из Git или локального source.
- [Быстрый старт](./quick-start.md): запустить первый overlay через `SGG/Perfmeter/Setup`.
- [Workflow](./workflows.md): overlay, sessions, alerts, overdraw, custom metrics и agent automation.
- [Реализованные виджеты](./widgets.md): текущий список runtime overlay widgets.
- [Визуальные пресеты](./presets.md): default visual presets и fullscreen captures.
- [API](./api.md): runtime C# API и snapshot types.
- [MCP](./mcp.md): command surface для Unity MCP/editor agents.
- [Сравнение](./comparison.md): SGG PerfMeter vs Advanced FPS Counter и Graphy.
- [Ограничения](./limitations.md): platform, timing, overdraw и validation notes.
- [Скриншоты](./screenshots.md): index скриншотов и asset paths.
- [Скриншоты Setup Window](./setup-window-screenshots.md): страницы setup window на русском.

## Что решает PerfMeter

Большинство FPS counters показывают текущий FPS. SGG PerfMeter помогает отвечать на более диагностические вопросы:

- Кадр упирается в CPU, GPU, render thread или present/VSync?
- Подозрительны ли draw calls, SetPass, SRP Batcher, BRG/GRD, upload bytes, memory или overdraw?
- Доступен ли GPU timing на этой платформе и graphics API?
- На каком device, display, camera, scene и settings был получен capture?
- Может ли editor tool или AI agent прочитать profiler state без парсинга UI или Unity Console?

## Главные возможности

- `FrameTimingManager` timings для CPU frame, main thread, render thread, present wait и GPU frame time, когда доступно.
- `ProfilerRecorder` counters для render, memory, SRP Batcher, BRG/GRD, uploads и GPU memory, когда доступно.
- Bottleneck classification относительно выбранного target FPS budget.
- UI Toolkit overlay с layouts, graphs, visual presets, themes, module filters и custom metric rows.
- Bounded session recording с warm-up, scene scope, worst frames, JSON/CSV export, device snapshots, camera snapshots и settings metadata.
- Rule alerts с callbacks, structured logs, Editor warning cooldowns и MCP access.
- Opt-in numerical overdraw measurement и visual heatmap через URP Render Graph.
- Device, camera, Render Graph, status, metrics, alerts, session и custom metric snapshots для кода и automation.

## Первый чеклист

- Unity `6000.4+`.
- URP `17.4+`.
- Frame Timing Stats включен в Player Settings.
- `PerfMeterRenderGraphFeature` установлен в активный URP renderer, если нужны Render Graph markers, overdraw measurement или heatmap.
- Vulkan предпочтителен на Android, если важен GPU frame timing.

## Политика документации

GitHub `docs/` - основной источник пользовательской документации. Package-local документация в `Assets/Scripts/SGG.PerfMeter/` намеренно короче и ссылается сюда.

Внутренние development documents лежат в `_DevelopmentDocs/` и не являются основным входом для пользователей.
