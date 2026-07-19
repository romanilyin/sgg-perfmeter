# UI Widgets And Themes Backlog

Статус: design backlog. Не является обещанием текущего release scope.

## Active Roadmap

| ID | Priority | Status | Scope |
| --- | --- | --- | --- |
| [GitHub #2](https://github.com/romanilyin/sgg-perfmeter/issues/2) | P1 | open | Text overflow and stable numeric geometry |
| [GitHub #1](https://github.com/romanilyin/sgg-perfmeter/issues/1) | P2 | open, partially prepared | Unity 6000.5+ `PanelRenderer` host migration |

### P1: Text overflow and stable numeric geometry

Current limitation:

- `FpsOnly` keeps a fixed `356 px` width while font size, scale, warning state and font family are configurable;
- FPS values combine prefix, number and unit in `NoWrap` labels without reserved numeric slots;
- Manrope digits can change preferred width between updates;
- metric cards keep fixed `144 x 78` geometry and hide overflowing numeric text;
- current tests validate state and object lifecycle, not rendered bounds or numeric-width stability.

Required:

- split metric labels into stable prefix, numeric value and unit cells;
- reserve or measure worst-case numeric widths when the tree, font, size or layout changes;
- use a dedicated monospace numeric font role while preserving the selected family for labels;
- fit card values without ellipsis or silent clipping of diagnostic numbers;
- provide a two-row or equivalent bounded fallback when scaled content cannot fit the panel width;
- add PlayMode bounds and width-stability coverage for both fonts, supported layouts, min/default/max font sizes and representative resolutions.

Acceptance:

- `FpsOnly` and cards contain all numeric values at supported settings without clipping;
- numeric cell widths remain stable across representative value changes;
- extreme scale/font combinations use a deterministic fallback inside the available panel bounds.

### P2: Versioned UI Toolkit panel host

Current limitation:

- Unity 6000.5 still creates the Legacy `UIDocument` component instead of `PanelRenderer`;
- `PerfMeterOverlay` can reuse an unrelated `UIDocument`, replace its panel settings and style its shared root;
- visual rebuild clears the complete `rootVisualElement` instead of removing only the PerfMeter-owned container;
- no reload-callback, duplicate-tree or repeated enable/disable coverage exists for `PanelRenderer`.

Completed preparation in `2026.7.19-1`:

- panel, text and theme settings are shipped as a serialized package resource with Unity-assigned ICU data;
- runtime-created panel/text/theme objects and runtime theme discovery were removed;
- Unity 6000.4 import/compile and Unity 6000.5 Development Player overlay smoke passed.

Required:

- keep `UIDocument` on Unity 6000.4 and use `PanelRenderer` on Unity 6000.5+;
- isolate panel-host ownership from visual-tree construction behind a common root;
- register and unregister the versioned UI reload callback exactly once;
- create a dedicated owned host or preserve foreign host settings and children;
- remove only the `sgg-perfmeter-overlay` container during rebuild;
- add repeated enable/disable, theme/font/layout change, scene reload and destruction tests on both backend versions.

Acceptance:

- Unity 6000.5+ creates no Legacy `UIDocument` for PerfMeter;
- Unity 6000.4 retains the supported `UIDocument` path;
- lifecycle and rebuild operations leave exactly one PerfMeter container and do not modify foreign UI;
- URP and HDRP retain equivalent screen-space overlay behavior.

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
