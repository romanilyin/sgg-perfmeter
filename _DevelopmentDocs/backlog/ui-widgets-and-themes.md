# UI Widgets And Themes Backlog

Статус: design backlog. Не является обещанием текущего release scope.

## Roadmap Status

Центральные приоритеты и зависимости находятся в `roadmap.md`.

`StructuredLog` toggle, stable numeric geometry и versioned owned panel host выпущены в package version `2026.8.5-2`.

### Released: Frame-accurate hitch strip

Released in `2026.8.11-2`:

- a dedicated full-width bottom strip advances once per collected frame independently of overlay text refresh;
- invalid timing advances the bounded history as an explicit gap instead of fake zero;
- one-frame hitches retain raw height and budget severity without temporal smoothing;
- narrow plots use only a peak-preserving min/max envelope;
- the warmed ring-buffer update allocates `0 B/frame` and does not rebuild the visual tree.

### Release candidate: Smoothed graph raw-peak backdrop (`PM-UI-009`)

Implemented for release candidate `2026.8.15-1`: сохранить читаемую сглаженную форму CPU/GPU timing graphs, но не скрывать короткие raw spikes, которые throttled graph sampling уменьшает или полностью пропускает.

Реализованное поведение:

- существующая сглаженная линия и её заливка остаются основным foreground-слоем с прежней геометрией, цветами и smoothing semantics;
- для того же history window хранится raw peak evidence; при horizontal compression каждый pixel bucket сохраняет максимальный valid raw sample, поэтому одиночный spike не усредняется;
- red peak backdrop рисуется раньше сглаженной линии/заливки и clip-ится так, чтобы видимой оставалась только часть raw peak выше сглаженной формы;
- backdrop использует semantic red/error token темы, но не меняет цвет foreground-графика;
- raw peaks не участвуют в расчёте vertical `min/max`, scale labels или budget lines: graph использует тот же scale, который получил бы без backdrop;
- raw peak проецируется в уже выбранный scale и clip-ится по верхней границе plot; даже очень высокий spike не расширяет диапазон и не сжимает сглаженный график;
- invalid, unavailable, non-finite samples и history gaps не создают красный peak;
- backdrop не меняет alert/spike counters, smoothing formula, RAW strip и public metric values.

Критерии приёмки:

1. При short raw spike выше сглаженного значения красный peak виден над foreground-графиком в той же X-позиции, а foreground остаётся полностью читаемым.
2. Scale, scale labels, budget line и координаты сглаженной серии совпадают с результатом для того же smoothing history без включённого peak backdrop.
3. Spike выше текущего scale clip-ится по верхней границе без auto-scale; spike внутри диапазона сохраняет относительную высоту.
4. Несколько raw samples, попавших в один pixel bucket, дают один peak по максимальному valid sample, а не average.
5. Raw sample, не превышающий сглаженную форму, а также gap/invalid sample не создают видимый красный участок.
6. Scale invariance, clipping, gaps и peak-preserving compression имеют deterministic EditMode coverage; focused review подтверждает вызов backdrop painter до foreground painter, а PlayMode проверяет отсутствие visual-tree rebuild.
7. Warmed sample/update path сохраняет действующие bounded-history и allocation constraints; новый слой не создаёт per-frame elements или managed allocations.

Граница текущего scope: это default visual refinement существующих CPU/GPU timing graphs без нового public API, preset/schema field или user toggle. Отдельная configurability задача потребуется только при подтверждённой необходимости отключать или настраивать backdrop.

### Released: Custom metric graph channels

Released in `2026.8.11-2`:

- visual preset JSON binds up to four series by case-sensitive stable metric ID;
- each series has explicit signed display-space `min/max`, non-zero `displayScale`, color and unit;
- missing, unavailable and non-finite samples advance as gaps instead of fake zero;
- the renderer uses each channel's own configured range and never performs implicit cross-unit normalization;
- configuration is additive to preset schema v1, while bounded histories and warmed updates remain allocation-free.

### Released: Bounded descriptors and theme manifests

Released in `2026.8.11-2`:

- visual presets now apply bounded width, gap, ordered admission and supported explicit height metadata at configuration boundaries while retaining a safe fixed block order;
- narrow graph layouts hide fixed legend and scale columns before plot overflow;
- `PerfMeterOverlayThemeRegistry` exposes read-only manifests for built-in semantic tokens and explicit optional asset paths;
- projects can register at most 16 stable-ID descriptors that compose existing overlay modules, without arbitrary renderer execution;
- limits are explicit for widgets, graph points, active full graphs, width, gap and height;
- steady-state value/history updates keep the existing visual tree.

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
