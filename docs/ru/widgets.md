# Реализованные виджеты

SGG PerfMeter сейчас содержит 16 высокоуровневых виджетов runtime overlay. Это композиционные блоки, которые видны в setup window и используются visual overlay presets.

`FPS Only` - это preset/layout mode, а не отдельный виджет. Он использует FPS и timing data в одной компактной строке.

Скриншоты runtime widgets пока только зарезервированы. Русская и английская документация будут использовать одни и те же изображения из `docs/assets/screenshots/widgets/`, потому что runtime overlay не локализуется.

| Widget ID | Название | Тип | Модуль | Что показывает | Зарезервированный скриншот |
| --- | --- | --- | --- | --- | --- |
| `fps.summary-card` | FPS summary card | Card | FPS | Average FPS, текущий FPS, 1% low, 0.1% low и budget state. | `../assets/screenshots/widgets/fps-summary-card.png` |
| `timing.cpu-card` | CPU timing card | Card | Timing | CPU frame, main thread, render thread и frame budget state. | `../assets/screenshots/widgets/timing-cpu-card.png` |
| `timing.gpu-card` | GPU timing card | Card | GPU timing | GPU frame time и valid GPU sample count, если Unity отдает GPU timing. | `../assets/screenshots/widgets/timing-gpu-card.png` |
| `timing.frame-spikes-card` | Frame spikes card | Card | FPS / Warnings | Счетчики frame spikes и текущий warning state. | `../assets/screenshots/widgets/timing-frame-spikes-card.png` |
| `overdraw.card` | Overdraw card | Card | Overdraw / Heatmap | Состояние измерения overdraw, progress, ratio и heatmap state. | `../assets/screenshots/widgets/overdraw-card.png` |
| `timing.cpu-budget-bar` | CPU budget bar | Budget bar | Timing | CPU frame time относительно выбранного target-FPS budget. | `../assets/screenshots/widgets/timing-cpu-budget-bar.png` |
| `timing.gpu-budget-bar` | GPU budget bar | Budget bar | GPU timing | GPU frame time относительно выбранного target-FPS budget. | `../assets/screenshots/widgets/timing-gpu-budget-bar.png` |
| `graphs.cpu-timing` | CPU timing graph | Graph | Graphs / Timing | История CPU frame, main thread, render thread и other timing. | `../assets/screenshots/widgets/graphs-cpu-timing.png` |
| `graphs.gpu-timing` | GPU timing graph | Graph | Graphs / GPU timing | История GPU frame timing с target budget line. | `../assets/screenshots/widgets/graphs-gpu-timing.png` |
| `cpu.cores-bars` | CPU core bars | Panel | CPU core sampling | Per-logical-core CPU load bars, когда platform sampling доступен. | `../assets/screenshots/widgets/cpu-cores-bars.png` |
| `cpu.cores-graphs` | CPU core graphs | Panel | CPU core sampling / Graphs | Per-logical-core CPU load history graphs. | `../assets/screenshots/widgets/cpu-cores-graphs.png` |
| `custom-metrics.panel` | Custom metrics panel | Panel | Custom metrics | Значения от project `IPerfMeterCustomMetricProvider` implementations. | `../assets/screenshots/widgets/custom-metrics-panel.png` |
| `rendering.summary-card` | Rendering summary card | Card | Rendering | Draw calls, SetPass calls, batches и vertices. | `../assets/screenshots/widgets/rendering-summary-card.png` |
| `memory.summary-card` | Memory summary card | Card | Memory / GC / GPU memory | System memory, GC memory и GPU memory counters. | `../assets/screenshots/widgets/memory-summary-card.png` |
| `batching.summary-card` | Batching summary card | Card | SRP Batcher / BRG | SRP Batcher и BatchRendererGroup / GPU Resident Drawer counters. | `../assets/screenshots/widgets/batching-summary-card.png` |
| `uploads.summary-card` | Uploads summary card | Card | Uploads | Index/upload counters, включая index buffer upload bytes in frame. | `../assets/screenshots/widgets/uploads-summary-card.png` |

## Примечания

- Presets могут включать часть этих widgets и выбирать layout: `MetricBars`, `CompactCards`, `Graphs` или `DiagnosticsWide`.
- Text-row и metric-bar rows являются низкоуровневыми renderers внутри layout system и намеренно не перечислены как high-level preset widgets.
