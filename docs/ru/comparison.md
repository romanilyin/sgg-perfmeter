# Сравнение с Advanced FPS Counter и Graphy

Это сравнение продукта и архитектуры, а не измеренный runtime-бенчмарк.

## Коротко

Advanced FPS Counter и Graphy - сильные универсальные визуальные оверлеи. Они хороши, когда нужен быстрый подключаемый FPS/memory/device HUD с широкой поддержкой старых версий Unity и визуальной настройкой.

SGG PerfMeter намеренно уже и диагностичнее: Unity `6000.4+`, URP `17.4+`, Render Graph, структурированные снимки, экспорт сессий, диагностика overdraw, воспроизводимые метаданные камеры/устройства и автоматизация через MCP/API.

## Таблица сравнения

| Область | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Основное позиционирование | 🔵 Диагностика URP Render Graph + API профилирования, понятный агентам | ⚠️ Гибкий игровой счетчик FPS/memory/device | ⚠️ Визуальный монитор FPS/memory/audio stats + debugger |
| Цель Unity | ⚠️ Unity `6000.4+`, URP `17.4+` | 🔵 Широкая поддержка старых версий Unity | 🔵 Широкая поддержка старых версий Unity |
| UI backend | 🔵 Оверлей UI Toolkit | ⚠️ Метки uGUI Canvas/Text | ⚠️ Модули uGUI Text/Image |
| Источник таймингов | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Runtime sampling frame/update | ⚠️ История на `Time.unscaledDeltaTime` |
| Разделение CPU/GPU | 🔵 CPU frame, main thread, render thread, present wait, GPU когда доступно | 🛑 Нет аналогичного разделения | 🛑 Нет аналогичного разделения |
| Классификация узких мест | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 Нет аналога | 🛑 Нет аналога |
| Счетчики рендера | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 Нет набора счетчиков URP/SRP | 🛑 Нет набора счетчиков URP/SRP |
| Воспроизводимость устройства/камеры | 🔵 Структурированные снимки устройства и камеры | ⚠️ Только панель устройства | ⚠️ Только панель устройства |
| Сессии | 🔵 Ограниченный recorder, warm-up, scope сцены, худшие кадры, экспорт JSON/CSV | 🛑 Не основная функция | ⚠️ Похоже на идею из roadmap |
| Overdraw | 🔵 Числовое измерение + визуальная heatmap через URP Render Graph | 🛑 Нет | 🛑 Нет |
| Автоматизация | 🔵 Набор команд MCP и публичные снимки | 🛑 Нет | 🛑 Нет |

## Что SGG PerfMeter делает лучше

- Объясняет вероятные узкие места через CPU frame, main thread, render thread, present wait, GPU timing и данные бюджета кадра.
- Показывает счетчики рендера, ориентированные на URP, и диагностику Render Graph.
- Создает воспроизводимые отчеты производительности со сценой, устройством, камерой, настройками, сэмплами сессии, сводками и метаданными худших кадров.
- Дает инструментам и агентам структурированные данные через публичный API и команды MCP.
- Включает ограниченное измерение overdraw и визуальную heatmap как явную диагностику.

## Что конкуренты все еще делают лучше

- Оба конкурента поддерживают более широкий диапазон старых версий Unity, и для проектов на legacy-версиях это преимущество.
- Advanced FPS Counter имеет очень прямой UX подключаемого визуального счетчика, зрелую настройку в Inspector, hotkeys/circle gesture toggles, UI-паттерны min/max/average и примеры VR/world-space.
- Graphy имеет сильные публичные маркетинговые материалы, понятные состояния модулей, визуальную настройку, hotkeys/background mode, зрелый UX debugger packet и широкую узнаваемость.

## Что не утверждать

- SGG PerfMeter не заменяет Unity Profiler, RenderDoc, Profile Analyzer или Frame Debugger.
- SGG PerfMeter не zero-overhead; используйте low-overhead и явно документируйте стоимость диагностики.
- SGG PerfMeter не является пакетом legacy-совместимости для всех платформ и всех render pipelines.
