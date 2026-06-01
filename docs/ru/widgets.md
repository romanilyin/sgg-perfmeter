# Реализованные виджеты

SGG PerfMeter включает 16 высокоуровневых виджетов runtime-оверлея. Это блоки композиции, которые видны в окне настройки и используются визуальными пресетами оверлея.

`FPS Only` - это режим пресета/layout, а не отдельный виджет. Он использует FPS и данные таймингов в одной компактной строке.

Почти у всех групп метрик есть текстовая и графическая форма. Текстовая форма - это карточки или строки `MetricBars` с числовыми значениями, а графическая форма - полосы бюджета или графики истории. Пресет выбирает, какую форму показать; один и тот же источник данных может отображаться в разных layout-режимах.

Текст runtime-оверлея не локализуется, поэтому русская и английская документация используют одни и те же скриншоты виджетов.

| ID виджета | Скриншот | Тип | Модуль | Что показывает |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Карточка | FPS | Средний FPS, текущий FPS, 1% low, 0.1% low и состояние бюджета кадра. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Карточка | Timing | CPU frame, main thread, render thread и состояние бюджета кадра. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Карточка | GPU timing | GPU frame time и количество валидных GPU samples, если Unity отдает GPU timing. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Карточка | FPS / Warnings | Счетчики frame spikes и текущее warning state. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Карточка | Overdraw / Heatmap | Состояние измерения overdraw, progress, ratio и состояние heatmap. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Полоса бюджета | Timing | CPU frame time относительно выбранного бюджета target FPS. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Полоса бюджета | GPU timing | GPU frame time относительно выбранного бюджета target FPS. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | График | Graphs / Timing | Историю CPU frame, main thread, render thread и прочих таймингов. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | График | Graphs / GPU timing | Историю GPU frame timing с линией target budget. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Панель | CPU core sampling | Полосы загрузки CPU по логическим ядрам, когда platform sampling доступен. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Панель | CPU core sampling / Graphs | Графики истории загрузки CPU по логическим ядрам. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Панель | Custom metrics | Значения от реализаций `IPerfMeterCustomMetricProvider` в проекте. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Карточка | Rendering | Draw calls, SetPass calls, batches и vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Карточка | Memory / GC / GPU memory | System memory, GC memory и GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Карточка | SRP Batcher / BRG | Счетчики SRP Batcher и BatchRendererGroup / GPU Resident Drawer. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Карточка | Uploads | Счетчики index/upload, включая index buffer upload bytes за кадр. |

## Скриншоты MetricBars

Layout `MetricBars` по умолчанию показывает компактные строки для часто отслеживаемых групп:

| Скриншот | Что показывает |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, бюджет кадра и индикаторы low-FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | Строки CPU/GPU timing относительно выбранного target FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches и vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | Счетчики SRP Batcher и BatchRendererGroup / GPU Resident Drawer. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | Счетчики System memory, GC memory и GPU memory. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Счетчики upload и index-buffer upload bytes. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Строки пользовательских метрик из проекта. |

## Примечания

- Пресеты могут включать часть этих виджетов и выбирать layout: `MetricBars`, `CompactCards`, `Graphs` или `DiagnosticsWide`.
- Text-row и metric-bar rows являются низкоуровневыми renderers внутри layout system и показывают текстовые версии тех же групп метрик, которые в других layout-режимах могут выглядеть как карточки, полосы бюджета или графики.
