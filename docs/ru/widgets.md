# Реализованные Виджеты

SGG PerfMeter включает 16 высокоуровневых виджетов runtime overlay. Это блоки композиции, которые видны в окне настройки и используются визуальными пресетами overlay.

`FPS Only` - это режим пресета/layout, а не отдельный виджет. Он использует FPS и timing data в одной компактной строке.

Текст runtime overlay не локализуется, поэтому русская и английская документация используют одни и те же скриншоты виджетов.

| Widget ID | Скриншот | Тип | Модуль | Что показывает |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="260"> | Card | FPS | Средний FPS, текущий FPS, 1% low, 0.1% low и состояние бюджета кадра. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="260"> | Card | Timing | CPU frame, main thread, render thread и состояние бюджета кадра. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="260"> | Card | GPU timing | GPU frame time и количество валидных GPU samples, если Unity отдает GPU timing. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="260"> | Card | FPS / Warnings | Счетчики frame spikes и текущее warning state. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="260"> | Card | Overdraw / Heatmap | Состояние измерения overdraw, progress, ratio и heatmap state. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="260"> | Budget bar | Timing | CPU frame time относительно выбранного target-FPS budget. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="260"> | Budget bar | GPU timing | GPU frame time относительно выбранного target-FPS budget. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="260"> | Graph | Graphs / Timing | Историю CPU frame, main thread, render thread и other timing. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="260"> | Graph | Graphs / GPU timing | Историю GPU frame timing с target budget line. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="260"> | Panel | CPU core sampling | Per-logical-core CPU load bars, когда platform sampling доступен. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="260"> | Panel | CPU core sampling / Graphs | Per-logical-core CPU load history graphs. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="260"> | Panel | Custom metrics | Значения от project `IPerfMeterCustomMetricProvider` implementations. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="260"> | Card | Rendering | Draw calls, SetPass calls, batches и vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="260"> | Card | Memory / GC / GPU memory | System memory, GC memory и GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="260"> | Card | SRP Batcher / BRG | SRP Batcher и BatchRendererGroup / GPU Resident Drawer counters. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="260"> | Card | Uploads | Index/upload counters, включая index buffer upload bytes in frame. |

## Metric Bar Captures

Layout `MetricBars` по умолчанию показывает компактные строки для часто отслеживаемых групп:

| Скриншот | Что показывает |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="320"> | FPS, frame budget и low-FPS indicators. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="320"> | CPU/GPU timing rows относительно выбранного target FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="320"> | Draw calls, SetPass calls, batches и vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="320"> | SRP Batcher и BatchRendererGroup / GPU Resident Drawer counters. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="320"> | System memory, GC memory и GPU memory counters. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="320"> | Upload counters и index-buffer upload bytes. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="320"> | Custom metric rows из проекта. |

## Примечания

- Presets могут включать часть этих widgets и выбирать layout: `MetricBars`, `CompactCards`, `Graphs` или `DiagnosticsWide`.
- Text-row и metric-bar rows являются низкоуровневыми renderers внутри layout system и перечислены отдельно от high-level preset widgets.
