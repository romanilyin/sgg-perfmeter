# Implemented Widgets

SGG PerfMeter は現在、16 個の high-level runtime overlay widgets を含みます。これらは setup window に表示され、visual overlay presets で使用される preset composition blocks です。

`FPS Only` は preset/layout mode であり、別個の widget ではありません。FPS と timing data を single compact row で再利用します。

ほとんどの metric groups には textual form と graphical form があります。textual form は numeric values を持つ cards または `MetricBars` rows で、graphical form は budget bars または history graphs です。表示される form は selected preset によって決まり、同じ metric source が異なる layouts に現れる場合があります。

runtime overlay text は localized されないため、各言語の documentation は同じ widget screenshots を使用します。

| Widget ID | Screenshot | 種類 | Module | 表示内容 |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | Average FPS、current FPS、1% low、0.1% low、budget state。 |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame、main thread、render thread、frame budget state。 |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | Unity が GPU timing を公開する場合の GPU frame time と valid GPU sample count。 |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Frame spike counters と current warning state。 |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Overdraw measurement state、progress、ratio、heatmap state。 |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | selected target-FPS budget に対する CPU frame time。 |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | selected target-FPS budget に対する GPU frame time。 |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graph | Graphs / Timing | CPU frame、main thread、render thread、その他 timing history。 |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graph | Graphs / GPU timing | target budget line 付きの GPU frame timing history。 |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | platform sampling が利用可能な場合の logical core ごとの CPU load bars。 |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | logical core ごとの CPU load history graphs。 |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | project の `IPerfMeterCustomMetricProvider` implementations が提供する values。 |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls、SetPass calls、batches、vertices。 |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory、GC memory、GPU memory counters。 |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | SRP Batcher と BatchRendererGroup / GPU Resident Drawer counters。 |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | frame 内の index buffer upload bytes を含む index/upload counters。 |

## Metric Bar Captures

default `MetricBars` layout は、よく監視する categories を compact rows として表示します。

| Capture | 表示内容 |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS、frame budget、low-FPS indicators。 |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | selected target FPS に対する CPU/GPU timing rows。 |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls、SetPass calls、batches、vertices。 |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | SRP Batcher と BatchRendererGroup / GPU Resident Drawer counters。 |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory、GC memory、GPU memory counters。 |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Upload counters と index-buffer upload bytes。 |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | project-provided custom metric rows。 |

## Notes

- Presets はこれらの widgets の subset を有効化し、`MetricBars`、`CompactCards`、`Graphs`、`DiagnosticsWide` などの layout を選択できます。
- Text-row と metric-bar rows は layout system の背後にある lower-level renderers であり、他の layouts では cards、budget bars、graphs として現れる同じ metric groups の textual versions を公開します。
