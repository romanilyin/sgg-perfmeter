# UI Widgets And Themes Backlog

Статус: design backlog. Не является обещанием текущего release scope.

## Roadmap Status

Центральные приоритеты и зависимости находятся в `roadmap.md`.

`StructuredLog` toggle, stable numeric geometry и versioned owned panel host выпущены в package version `2026.8.5-2`.

### Resolved: Text overflow and stable numeric geometry

Implemented in `2026.8.5-2`:

- `FpsOnly` uses separate prefix/value/unit cells and reserved worst-case widths;
- numeric values use the JetBrains Mono numeric role while labels preserve the selected family;
- scaled narrow layouts switch to a deterministic two-row fallback;
- metric cards and budget bars keep bounded geometry and wrap as complete widgets;
- PlayMode tests cover stable value widths, numeric fonts, max-font bounds and responsive layout.

### Resolved: Versioned UI Toolkit panel host

Implemented in `2026.8.5-2`:

- PerfMeter creates a dedicated owned child host and never reuses a foreign `UIDocument`;
- Unity 6000.4 uses `UIDocument`, while Unity 6000.5+ uses `PanelRenderer` with a versioned reload callback;
- rebuild removes only `sgg-perfmeter-overlay` and preserves foreign settings and children;
- repeated enable/disable, theme/font/layout rebuild and duplicate-container behavior have PlayMode coverage;
- the serialized ICU-enabled `PanelSettings` resource remains shared by both backends.

## Цели

- Быстро показывать bottleneck и budget risk.
- Сохранять low-overhead UI Toolkit overlay.
- Не пересоздавать visual tree при обновлении значений.
- Поддерживать unavailable/degraded state для каждой метрики.
- Масштабироваться от built-in metrics к bounded custom metrics.

## Виджеты

| Widget | Назначение | Ограничения |
| --- | --- | --- |
| `MetricTile` | FPS, CPU, GPU, render, memory, overdraw summary. | Cached labels, fixed geometry. |
| `BudgetBar` | Сравнение CPU/GPU/frame time с target budgets. | Fill через transform/custom paint, без layout rebuild. |
| `Sparkline` | Мини-тренды внутри карточек. | Bounded history, throttled repaint. |
| `HistoryGraph` | Основной граф CPU/GPU/FPS. | Downsampling, target lines, unavailable state. |
| `SpikeHeatmap` | Плотность и severity spikes. | Только full/session layouts. |
| `BottleneckPanel` | Ranked reasons for slow frame. | Не показывать ложную уверенность при missing GPU timing. |
| `StatsTable` | Budget violations/recent spikes. | Fixed rows, no per-frame rebuild. |
| `OverdrawWidget` | State/progress/ratio/heatmap. | Явно показывать Off/Unsupported/Measuring/Completed. |
| `CompactDrawer` | Compact overlay плюс раскрываемые детали. | Toggle visibility/transform, не пересоздавать дерево. |

## Themes

Темы должны менять внешний вид, а не смысл метрик.

Нужные сущности:

- Theme manifest JSON: `id`, display name, default layout, asset paths.
- Semantic tokens: CPU, GPU, memory, render, overdraw, warning, error, ok, muted.
- USS theme assets.
- Optional icon atlas.
- Optional cheap frame/noise textures.

Не добавлять expensive blur/glow как default path. Heavy visuals должны быть optional и выключаемыми.

## Layout Presets

Будущие layout descriptors могут хранить:

- anchor/corner;
- scale/opacity/font size;
- список widgets;
- metric bindings;
- thresholds;
- refresh interval per widget class;
- graph history limits.

## Safety Limits

- Max widgets per layout.
- Max graph points per widget.
- Max active full graphs.
- Mobile bounds validation.
- Fallback theme при missing assets.
- Explicit unavailable state вместо exceptions или пустых графиков.
