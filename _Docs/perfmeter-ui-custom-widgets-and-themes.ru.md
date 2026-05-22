# Кастомные виджеты и темы PerfMeter

Документ описывает, какие runtime widgets и theme/layout primitives нужны, чтобы собирать красивые UI Toolkit layouts с выбранными метриками PerfMeter. Это design plan, без изменения кода.

## Принципы

- Оставаться на UI Toolkit runtime overlay, без uGUI/IMGUI для runtime-представления.
- Не пересоздавать visual tree при смене значений. Layout собирается один раз, дальше меняются labels, transforms и repaint data.
- Держать fixed sizes и absolute placement для overlay layouts, особенно для compact/full diagnostics windows.
- Разделить сбор метрик, адаптацию метрик к widgets и отрисовку widgets. Виджет не должен сам ходить в `PerformanceMeter.GetLatestMetrics()`.
- Все widgets должны поддерживать unavailable state: `n/a`, muted color, warning tooltip/details row, без исключений и пустых графиков с мусорными значениями.
- Тема должна задавать внешний вид, но не менять смысл метрик. CPU остается CPU-цветом темы, GPU остается GPU-цветом темы.

## Библиотека кастомных виджетов

| Widget | Для чего нужен | Данные | UITK-реализация |
| :-- | :-- | :-- | :-- |
| `MetricTileWidget` | FPS, frame time, CPU/GPU, draw calls, spikes, memory в карточках. | `title`, `value`, `unit`, `subtitle rows`, `accent`, optional `sparkline`. | Контейнер с cached labels, optional icon, optional embedded sparkline. Size variants: large, regular, mini. |
| `BudgetBarWidget` | Сравнение CPU/GPU/frame time с target budgets. | Current ms, target markers, max scale, status. | Fixed bar track, fill, threshold markers, labels. Fill через transform scale или custom mesh. Colored zones как child layers или generated geometry. |
| `StackedBarWidget` | CPU/GPU pipeline breakdown, memory split, top scopes. | Named segments with value/percent/color. | Один custom-painted bar или fixed absolute segment children. Нужна min visible width и legend mapping. |
| `SparklineWidget` | Мини-тренды внутри KPI cards. | History buffer, min/max/target optional. | Custom paint без axis labels. Поддержать line, area, bars. |
| `HistoryGraphWidget` | Основной график CPU/GPU/FPS за 30-120 секунд. | Multiple series, thresholds, history window. | Custom graph with grid, axis labels, target lines, area fill, legend. Нужны downsampling и ring buffer. |
| `TimelineGraphWidget` | Full diagnostics из R5: несколько lanes на общей шкале. | Frame time, CPU, GPU, spikes, events. | Composite widget: synchronized lanes, vertical cursor, optional tooltip. Включать только в full/capture layouts. |
| `RingGaugeWidget` | Memory, GPU memory, overdraw, present wait percent. | Percent/current/budget/status. | Custom arc track плюс active arc. Подписи отдельными labels. |
| `DonutBreakdownWidget` | CPU breakdown, rendering stats, pipeline shares. | Segment list, center value. | Custom ring segments. Legend/list рядом, чтобы не пытаться читать tiny labels внутри дуги. |
| `SemicircleGaugeWidget` | Большой frame budget gauge в full dashboard. | Current value, budget, thresholds, optional pointer. | Custom arcs, ticks, pointer, center labels. Лучше использовать как theme-specific hero widget. |
| `SpikeHeatmapWidget` | Частота и сила spikes на timeline. | Time bins, severity, category. | Custom rect grid. Цвета severity из темы. Очень полезен для session analysis. |
| `SpikeHistogramWidget` | Компактная severity strip. | Counts by severity/time. | Custom bars, threshold labels. Хороший компактный аналог heatmap. |
| `BottleneckWidget` | Причины просадки: GPU frame, post, CPU main, simulation, spike rate. | Ranked contributors with percent/current/status. | Vertical rows с progress bars и status colors. Может использовать current classifier plus optional session data. |
| `StatsTableWidget` | Budget violations, recent spikes, top CPU/GPU scopes. | Fixed rows/columns. | Fixed row height, cached labels, no dynamic rebuild per refresh. For small tables virtualization не нужна. |
| `StatusRibbonWidget` | VSync, target, bound state, thermal, resolution, API. | Status chips. | Horizontal chip list. Dirty updates only. |
| `CompactDrawerWidget` | R6-style small overlay plus details panel. | Summary metrics plus grouped details. | Two panels, expandable details. Visibility/translation instead of tree recreation. |
| `OverdrawWidget` | Numeric overdraw, heatmap state, unsupported warning. | Ratio, state, heatmap flag. | Ring gauge or tile. Должен явно показывать off/unsupported/stale, потому что overdraw opt-in. |

## Поддержка кастомных тем

Текущая архитектура уже использует JSON settings для zero-code setup. Для тем лучше продолжить этот подход: project-owned JSON выбирает theme/layout, а визуальная часть темы хранится в USS/atlas assets. Не стоит вводить `ScriptableObject` settings без отдельной причины, потому что сейчас настройки намеренно JSON-backed.

| Уровень | Что хранит | Зачем нужно |
| :-- | :-- | :-- |
| Theme manifest JSON | `id`, display name, base theme, asset paths, supported layouts, default scale/opacity/font size. | Позволяет перечислять темы в setup window и безопасно загружать project themes. |
| Theme USS | Цвета, размеры рамок, radii, typography, padding, opacity, classes для variants. | Быстрая смена внешнего вида через style classes и USS variables/custom properties. |
| Semantic tokens | `colorCpu`, `colorGpu`, `colorMemory`, `colorRender`, `colorWarning`, `colorError`, `panelBg`, `panelBorder`, `grid`, `textPrimary`, `textMuted`. | Widgets не знают конкретную палитру и остаются переиспользуемыми. |
| Widget style tokens | `barHeight`, `lineWidth`, `gaugeThickness`, `cardRadius`, `frameCutSize`, `sparklineAlpha`, `gridAlpha`. | Одна тема может быть glass, другая cyber, без переписывания widget logic. |
| Icon atlas | CPU/GPU/memory/render/spike/status/settings icons. | Сохраняет batching и единый визуальный язык. |
| Optional frame textures | 9-slice glass/noise/chamfer corners/glow strips. | Нужны для сложных рамок из R3/R7 без дорогой procedural отрисовки всего декора. |

## Поддержка кастомных layout presets

Нужен отдельный слой layout preset, который описывает, какие widgets показывать, где они стоят и к каким metric bindings подключены. Тема отвечает за вид, preset отвечает за композицию.

Пример концептуального JSON:

```json
{
  "theme": "glass-balanced",
  "layout": "balanced-dashboard",
  "anchor": "TopRight",
  "scale": 1.0,
  "widgets": [
    {
      "id": "fps",
      "type": "MetricTile",
      "metric": "fps.current",
      "rect": [16, 16, 220, 150],
      "variant": "large",
      "accent": "fps"
    },
    {
      "id": "cpu-budget",
      "type": "BudgetBar",
      "metric": "timing.cpuFrameMs",
      "rect": [252, 16, 520, 84],
      "thresholds": [16.67, 33.33],
      "accent": "cpu"
    }
  ]
}
```

## Metric bindings

Красивые layouts будут полезны только если widgets подключаются к метрикам декларативно. Нужен registry имен метрик, который покрывает built-in snapshots и custom metrics.

| Binding group | Примеры bindings |
| :-- | :-- |
| FPS | `fps.current`, `fps.average`, `fps.onePercentLow`, `fps.spikeCount` |
| Timing | `timing.cpuFrameMs`, `timing.cpuMainMs`, `timing.cpuRenderMs`, `timing.gpuMs`, `timing.presentWaitMs` |
| Render | `render.drawCalls`, `render.setPassCalls`, `render.batches`, `render.vertices`, `render.srpBatcherInstances`, `render.brgDrawCalls`, `render.brgInstances` |
| Memory | `memory.systemUsed`, `memory.gcReserved`, `memory.gcUsed`, `memory.gpuMemory` |
| Overdraw | `overdraw.ratio`, `overdraw.state`, `overdraw.heatmapVisible` |
| Status | `status.collectionMode`, `status.bottleneck`, `status.targetFps`, `status.vsync`, `status.graphicsApiWarning` |
| Session | `session.samples`, `session.worstFrame`, `session.spikes`, `session.droppedSamples` |
| Custom | `custom.<provider>.<metric>` with unit, display name, availability and warning text from provider snapshot. |

## Runtime limits для безопасности

| Ограничение | Причина |
| :-- | :-- |
| Max widgets per layout | Защитить overlay от дорогих пользовательских dashboard presets. |
| Max graph points per widget | Контролировать mesh size и repaint cost. |
| Max active full graphs | Не рисовать пять больших history graphs в постоянном compact overlay. |
| Refresh interval per widget class | KPI может обновляться чаще, tables и heavy timeline реже. |
| Optional heavy visuals | Blur, glow, cursor tooltip, radial gauges и multi-lane timeline должны быть выключаемыми. |
| Theme asset validation | Отсутствующая USS/atlas не должна ломать overlay; нужен fallback theme. |
| Mobile scale/bounds validation | Layout не должен уходить за экран при 16:9, 19.5:9, tablet и low DPI. |

## Что потребуется в setup/editor UX

- Theme picker с preview: built-in themes плюс project themes из `Assets/Resources/SGG.PerfMeter/Themes`.
- Layout picker: Compact, Balanced, FullDiagnostics, SessionAnalysis, OverdrawDiagnostic и custom JSON layouts.
- Widget list editor: включить/выключить widget, выбрать binding, поменять target thresholds, history length и display unit.
- Validation report: missing metric binding, unsupported widget, too many graph points, asset not found, layout out of screen bounds.
- Runtime apply: применение темы/layout без перезапуска Play Mode, но без пересоздания collectors.
- Export/import: тема и layout должны быть project-owned files, чтобы команда могла версионировать их отдельно от package code.

## Минимальная реализация по шагам

| Шаг | Результат |
| :-- | :-- |
| 1 | Ввести theme tokens и два встроенных USS themes: `Glass` и `CyberMinimal`. |
| 2 | Вынести текущие overlay presets в декларативные layout descriptors с fixed rects. |
| 3 | Добавить базовые widgets: `MetricTile`, `BudgetBar`, `Sparkline`, `HistoryGraph`, `StatusRibbon`. |
| 4 | Добавить custom-painted `RingGauge`, `DonutBreakdown`, `SpikeHeatmap`. |
| 5 | Добавить project theme/layout loading из JSON и setup preview/validation. |
| 6 | Добавить expanded/full diagnostics widgets: `TimelineGraph`, `BottleneckWidget`, `StatsTable`, `CompactDrawer`. |
