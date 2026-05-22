# Визуальные элементы UI PerfMeter

Документ фиксирует элементы из семи локальных референсов в `.ScreenReferences/`. Сами PNG остаются локальными референсами и не должны попадать в коммит.

## Карта референсов

| ID | Файл | Общий тип дизайна | Ключевая идея |
| :-- | :-- | :-- | :-- |
| R1 | `ChatGPT Image 22 мая 2026 г., 12_08_17 (1).png` | Glass dashboard | Большие мягкие панели, крупный FPS, отдельные CPU/GPU budget bars, history graphs, metric cards, нижний status bar. |
| R2 | `ChatGPT Image 22 мая 2026 г., 12_08_17 (2).png` | Card analytics dashboard | Верхняя KPI-лента, breakdown cards, большой stacked history, правый список статусов, footer warning strip. |
| R3 | `ChatGPT Image 22 мая 2026 г., 12_08_17 (3).png` | Sci-fi technical panel | Угловая рамка окна, большой semicircle budget gauge, donut breakdown, bar budgets, нижняя лента toggles. |
| R4 | `ChatGPT Image 22 мая 2026 г., 12_09_25 (1).png` | Modern glass compact dashboard | Центрированное окно, крупный FPS плюс area sparkline, две budget bars с зонами, mini cards, memory progress. |
| R5 | `ChatGPT Image 22 мая 2026 г., 12_09_25 (2).png` | Dense profiler workstation | Tabs, toolbar, top metric tiles, multi-lane timeline, tooltip, spike heatmap, bottleneck panels, tables. |
| R6 | `ChatGPT Image 22 мая 2026 г., 12_09_26 (3).png` | Compact floating overlay | Маленькое окно в углу, CPU/GPU bars, mini graphs, metric tiles, expandable details drawer. |
| R7 | `ChatGPT Image 22 мая 2026 г., 12_09_26 (4).png` | Cyber HUD | Неоновые угловые панели, центральный gauge, radial spike chart, ring gauges, segmented memory bar. |

## Группы элементов

| Элемент | Где виден | Назначение | Что нужно для UITK |
| :-- | :-- | :-- | :-- |
| Скругленная glass-панель | R1, R2, R4, R6 | Базовый контейнер для overlay, не перекрывает сцену агрессивно. | `VisualElement` с фиксированными размерами, `background-color` с alpha, `border-radius`, `border-color`, вложенный тонкий highlight-border. True backdrop blur в runtime UITK нет, поэтому blur нужно имитировать затемнением, noise/gradient texture или отдельным Render Graph blur pass только если он оправдан. |
| Угловая sci-fi рамка | R3, R7 | Сильный визуальный стиль для full dashboard. | Стандартный `border-radius` не делает chamfer/notch формы. Нужен кастомный frame element через `generateVisualContent`, 9-slice sprites или набор corner/edge элементов из атласа. Для неона - отдельные тонкие линии/текстуры, без дорогого размытия. |
| Header bar, tabs, toolbar | R3, R5, R6, R7 | Навигация по режимам, статус запуска, pin/close/settings/actions. | Обычные UITK-кнопки и labels с фиксированными style classes. Иконки из единого atlas/vector set. Для runtime overlay лучше минимум интерактива; полный toolbar уместен в full diagnostics. |
| Footer/status ribbon | R1, R2, R3, R7 | Быстрый статус: VSync, bound state, resolution, target FPS, samples, hotkeys. | Горизонтальный контейнер с chips/labels. Значения обновлять только при dirty state. Цвета брать из semantic theme tokens. |
| KPI tile | R1, R2, R4, R5, R6, R7 | Быстрое чтение одной метрики: FPS, CPU, GPU, spikes, draw calls. | Компонент с title/value/unit/subvalues/icon/accent. Фиксированная геометрия, cached labels, грязное обновление текста. Варианты размера: large, regular, mini. |
| Budget horizontal bar | R1, R3, R4, R6 | Сравнение текущего frame time с целевыми бюджетами 10/16.67/33.3 ms. | Контейнер с fill child, threshold markers, labels, optional colored zones. Для дешевого обновления fill лучше менять transform scale или custom mesh, а не перестраивать layout дерева. |
| Segmented/stacked bar | R2, R5, R7 | Breakdown CPU/GPU/memory по долям. | Несколько абсолютных segment children или один custom-painted mesh. Нужна нормализация сумм, min visible width для малых долей и legend mapping. |
| Mini bar sparkline | R1 | Компактная история render counters в карточках. | Custom `VisualElement` с ring buffer значений и отрисовкой прямоугольников через `generateVisualContent`. Обновлять на throttled refresh, не создавать child на каждый столбик. |
| Line sparkline | R4, R5, R6 | Быстрый тренд в KPI tile. | Custom paint polyline/filled area. История хранится в bounded buffer. Grid и axis обычно отключены ради читаемости. |
| History line/area graph | R1, R2, R3, R4, R5, R7 | Основная диагностика трендов CPU/GPU/FPS/spikes во времени. | Custom graph widget: grid, axes, threshold lines, labels, optional area fill, multiple series. Нужны downsampling, fixed history length, cached mesh/points и `MarkDirtyRepaint` только при новых samples. |
| Multi-lane timeline | R5 | Глубокая аналитика: frame time, CPU, GPU, spike heatmap на одном времени. | Составной widget с синхронной X-шкалой, несколькими graph lanes, heatmap lane, cursor/tooltip. Уместен для full diagnostics, не для постоянного компактного overlay. |
| Tooltip/crosshair | R5 | Инспекция конкретного кадра. | Абсолютно позиционированный overlay element над графиком. Должен включаться только при pointer hover/focus или capture mode, чтобы не обновляться каждый кадр. |
| Donut breakdown | R3, R7 | Показ долей CPU breakdown, rendering stats, memory/overdraw категорий. | UITK не имеет conic-gradient. Нужен custom arc mesh через `generateVisualContent`/Painter2D или precomputed ring segments. Подписи лучше отдельными labels рядом. |
| Ring gauge | R1, R4, R7 | Процент заполнения memory, GPU memory, present wait, overdraw. | Custom arc element с background track и active arc. Для стабильности использовать ограниченное число сегментов и обновлять только value/threshold colors. |
| Semicircle speedometer | R3, R7 | Большой бюджет кадра, эффектный центральный фокус. | Custom gauge element: arcs, ticks, pointer, center labels, thresholds. Стоит использовать только в large/full layouts, потому что занимает много площади. |
| Radial spike chart | R7 | Декоративная визуализация распределения spikes. | Custom polar chart. Информативность ниже timeline heatmap, поэтому лучше оставить как theme-specific optional widget. |
| Spike histogram/severity strip | R3, R5 | Быстро показывает частоту и силу spikes. | Histogram/heatmap custom paint. Бины по времени или severity, цветовая шкала green/yellow/red/magenta. Хорошо сочетается с budget alerts. |
| Bottleneck bars | R5 | Показывают, что именно съедает бюджет. | Список rows: label, progress bar, value, percent/status icon. Использует те же primitives, что breakdown bars. |
| Budget violation table | R2, R5 | Сводка отклонений от target budgets. | UITK rows с фиксированными колонками. Использовать labels, status icons, цветовые classes warning/error. |
| Frame inspector/top scopes/recent spikes table | R5 | Детальный анализ capture/session. | Table/list widget с virtualization не обязателен для маленьких списков, но нужны fixed row heights, clipped content, no per-frame rebuild. |
| Metric details drawer | R6 | Компактный overlay плюс раскрываемая детализация. | Два абсолютных окна с shared theme. Expand/collapse должен менять visibility/transform, а не пересоздавать дерево. Хороший кандидат для default compact workflow. |
| Icons and status chips | Все | Быстрое распознавание категорий: CPU/GPU/memory/render/spikes/settings. | Единый icon atlas, `Image` или vector icons. Цвет и размер через theme tokens. Избегать уникальных материалов и больших отдельных texture imports. |
| Glow, gradients, noise | R1, R3, R7 | Визуальная глубина и premium look. | В runtime UITK безопаснее использовать baked textures/atlases и layered translucent elements. Настоящий blur/glow через пост-эффекты увеличит стоимость overlay и должен быть optional. |

## Общие требования к реализации в UI Toolkit

- Базовые панели и layout должны быть fixed/absolute, чтобы обновление чисел и графиков не вызывало пересчет всего layout.
- Для графиков, gauges, donut charts, heatmaps и sci-fi рамок нужны кастомные `VisualElement` с отрисовкой в `generateVisualContent`; стандартных UITK controls для этих форм недостаточно.
- Историю метрик хранить в bounded ring buffers, отдельно от визуального дерева. Виджеты должны получать уже агрегированные samples.
- Графики должны обновляться по `RefreshIntervalSeconds` или dirty threshold, а не каждый `Update` без необходимости.
- Текстовые значения должны использовать существующую стратегию cached labels/dirty assignment; большие строки и пересоздание markup каждый refresh нежелательны.
- Все цвета должны быть semantic: CPU, GPU, memory, render, overdraw, warning, error, ok, muted. Это нужно для тем и accessibility.
- Все иконки и декоративные текстуры должны попадать в один atlas, иначе красивые темы быстро испортят batching UITK.
- Interactive элементы из R5 и R6 нужны только в expanded/full modes. Постоянный компактный overlay должен оставаться легким и не мешать игре.
- Heavy visuals вроде blur, glow, large radial gauges, tooltips и multi-lane timeline должны иметь feature flags или layout modes, чтобы не нарушить принцип low-overhead profiler UI.
