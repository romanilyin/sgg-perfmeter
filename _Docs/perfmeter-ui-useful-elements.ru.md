# Самые наглядные элементы UI PerfMeter

Оценка основана на семи локальных референсах из `.ScreenReferences/` и текущих целях PerfMeter: быстро определить bottleneck, не мешать игре, не вносить заметный overhead и поддерживать agent-readable/custom metrics workflow.

## Критерии

- Пользователь должен понять состояние кадра за 1-2 секунды.
- Элемент должен показывать не только число, но и отношение к бюджету или тренду.
- Элемент не должен занимать много места в compact overlay.
- Стоимость отрисовки должна соответствовать диагностической пользе.
- Элемент должен масштабироваться от built-in метрик к custom metrics.

## Рейтинг полезности

| Ранг | Элемент | Почему полезен | Где использовать |
| :-- | :-- | :-- | :-- |
| 1 | CPU/GPU frame budget bars с markers `16.67 ms` и `33.3 ms` | Самый быстрый способ понять, насколько CPU/GPU близки к 60/30 FPS бюджету. Цветные зоны green/yellow/red дают мгновенный сигнал. | Compact, Balanced, FullDiagnostics. |
| 2 | Combined CPU/GPU history graph с target lines | Показывает тренд и spikes, а не только последний sample. Хорошо отделяет постоянную нагрузку от редких провалов. | Balanced и FullDiagnostics, в compact как small sparkline. |
| 3 | Bottleneck/violations panel | Превращает сырые метрики в ответ: GPU bound, CPU main, CPU render, present limited, spike rate. Это самое actionable представление. | Balanced и FullDiagnostics. |
| 4 | Верхняя KPI-лента: FPS, frame time, CPU main, GPU, spikes, bound state | Дает полную сводку без чтения всего dashboard. Особенно хороша в R2/R5. | Все layouts, в compact сократить до FPS/CPU/GPU/spikes. |
| 5 | Mini metric tiles со sparkline для render counters | Draw calls, SetPass, batches, vertices и SRP instances лучше читать вместе с микротрендом. Так видно, что именно изменилось во время gameplay. | Balanced и FullDiagnostics. |
| 6 | Spike heatmap или severity strip | Лучше обычного числа spikes, потому что показывает плотность и время возникновения. В R5 heatmap особенно полезен рядом с timeline. | FullDiagnostics, session analysis, compact как severity strip. |
| 7 | Stacked breakdown bars для CPU/GPU pipeline | Проценты по Main/Render/Other или Graphics/Post/Shading дают хорошую диагностику без перегруженного donut. | FullDiagnostics, capture/session views. |
| 8 | Compact overlay плюс expandable details drawer | Лучший UX из R6 для постоянной игры: маленькая сводка не мешает, детали раскрываются по запросу. | Default runtime overlay candidate. |
| 9 | Memory progress bars/ring gauges | Полезны для долгих сессий и mobile, но обычно вторичны после frame timing. Bars читаются точнее, rings выглядят лучше. | Balanced, FullDiagnostics, mobile presets. |
| 10 | Overdraw tile/ring с явным state | Overdraw важен, но opt-in. Виджет должен показывать Off/Unsupported/Measuring/Completed и ratio, иначе пользователь легко неверно прочитает данные. | OverdrawDiagnostic и FullDiagnostics. |

## Лучшие комбинации по режимам

| Режим | Состав |
| :-- | :-- |
| `Compact` | FPS large tile, CPU budget bar, GPU budget bar, 3-5 mini tiles: spikes, draw calls, batches, memory, GPU memory. Optional details drawer. |
| `Balanced` | KPI-лента, CPU/GPU budget bars, combined history graph, mini render counters, memory bars, status ribbon. |
| `FullDiagnostics` | KPI-лента, multi-lane timeline, spike heatmap, bottleneck panel, budget violations, render/memory cards, top CPU/GPU scopes, recent spikes. |
| `SessionAnalysis` | Timeline with cursor/tooltip, frame inspector, recent spikes table, worst-frame summary, CPU/GPU breakdown bars. |
| `OverdrawDiagnostic` | Overdraw ratio/state tile, heatmap toggle state, GPU frame budget, fillrate-related history, draw/vertices counters. |

## Элементы, которые лучше считать вторичными

| Элемент | Причина |
| :-- | :-- |
| Большой центральный semicircle gauge | Очень эффектный, но занимает площадь, где можно показать history и bottleneck. Хорош для demo/full theme, не для default compact. |
| Radial spike chart | Красиво выглядит, но timeline heatmap точнее показывает время и частоту spikes. |
| Слишком сложные угловые HUD-рамки | Создают стиль, но не добавляют диагностики. Стоит оставить темам, а не базовому widget layer. |
| Heavy glow/backdrop blur | Может ухудшить стоимость overlay и исказить восприятие профайлера. Лучше имитировать дешевыми textures и alpha layers. |
| Плотные таблицы в постоянном overlay | Полезны при анализе, но мешают gameplay. Вынести в expanded/full/session modes. |

## Рекомендуемый default visual direction

Для первого набора красивых layouts лучше взять не самый декоративный cyber HUD, а гибрид R1/R4/R6: glass panels, крупные числа, budget bars, небольшие sparklines и expandable details. Такой стиль достаточно premium, хорошо читается поверх сцены и проще реализуется в UITK без дорогих эффектов.

Для advanced/full dashboard стоит взять структуру R5: tabs, KPI row, timeline, bottlenecks, budget violations, recent spikes. Это самый полезный референс для реальной диагностики, но его нужно держать как expanded mode, а не как постоянный overlay.

Sci-fi элементы из R3/R7 лучше использовать как theme pack: угловые frames, thin neon lines, semicircle gauge и ring widgets. Они усиливают визуальную идентичность, но не должны определять базовую архитектуру виджетов.
