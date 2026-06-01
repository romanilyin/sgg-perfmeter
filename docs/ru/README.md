# Документация SGG PerfMeter

SGG PerfMeter - слой runtime-диагностики и профилировочный API для Unity `6000.4+` URP `17.4+` проектов на Render Graph.

Пакет помогает понять, почему кадр стал медленным: CPU, GPU, render thread, ожидание present/VSync, render counters, память, overdraw и доступность платформенных счетчиков. Те же данные можно смотреть в UI Toolkit overlay, читать из C# API, записывать в JSON/CSV-сессии и использовать в editor/agent automation.

## С Чего Начать

- [Установка](./installation.md): подключить Unity package из Git или локального source.
- [Быстрый старт](./quick-start.md): запустить первый overlay через `SGG/Perfmeter/Setup`.
- [Workflow](./workflows.md): overlay, sessions, alerts, overdraw, custom metrics и automation.
- [Реализованные виджеты](./widgets.md): текущий список runtime overlay widgets.
- [Визуальные пресеты](./presets.md): default visual presets и fullscreen captures.
- [API](./api.md): runtime C# API и snapshot types.
- [MCP](./mcp.md): команды для Unity MCP/editor agents.
- [Сравнение](./comparison.md): SGG PerfMeter vs Advanced FPS Counter и Graphy.
- [Ограничения](./limitations.md): platform, timing, overdraw и validation notes.
- [Troubleshooting](./troubleshooting.md): быстрые проверки, если overlay или counters не работают.
- [Скриншоты](./screenshots.md): галереи setup window, presets и runtime widgets.
- [Скриншоты Setup Window](./setup-window-screenshots.md): страницы setup window на русском.
- [Проверки contributor changes](./contributor-checks.md): минимальные проверки для PR и документационных правок.

## Что Решает PerfMeter

Большинство FPS counters показывают текущий FPS. SGG PerfMeter помогает отвечать на более практические вопросы:

- кадр упирается в CPU, GPU, render thread или present/VSync;
- подозрительны ли draw calls, SetPass, SRP Batcher, BRG/GRD, upload bytes, memory или overdraw;
- доступен ли GPU timing на этой platform/graphics API;
- какие device, display, camera, scene и settings дали этот capture;
- может ли editor tool или AI agent прочитать profiler state без парсинга UI или Unity Console.

## Главные Возможности

- `FrameTimingManager` timings для CPU frame, main thread, render thread, present wait и GPU frame time, когда доступно.
- `ProfilerRecorder` counters для render, memory, SRP Batcher, BRG/GRD, uploads и GPU memory, когда доступно.
- Bottleneck classification относительно выбранного target FPS budget.
- UI Toolkit overlay с layouts, graphs, visual presets, themes, module filters и custom metric rows.
- Bounded session recording с warm-up, scene scope, worst frames, JSON/CSV export, device snapshots, camera snapshots и settings metadata.
- Rule alerts с callbacks, structured logs, Editor warning cooldowns и MCP access.
- Opt-in numerical overdraw measurement и visual heatmap через URP Render Graph.
- Device, camera, Render Graph, status, metrics, alerts, session и custom metric snapshots для кода и automation.

## Первый Чеклист

- Unity `6000.4+`.
- URP `17.4+`.
- Frame Timing Stats включен в Player Settings.
- `PerfMeterRenderGraphFeature` установлен в активный URP renderer, если нужны Render Graph markers, overdraw measurement или heatmap.
- Vulkan предпочтителен на Android, если важен GPU frame timing.

## Политика Документации

GitHub `docs/` - основной источник пользовательской документации. Package-local документация в `Assets/Scripts/SGG.PerfMeter/` намеренно короче и ссылается сюда.

Внутренние development documents лежат в `_DevelopmentDocs/` и не являются основным входом для пользователей.
