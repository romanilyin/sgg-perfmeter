# 구현된 Widgets

SGG PerfMeter는 현재 16개의 high-level runtime overlay widget을 제공합니다. 이 widget들은 setup window에 표시되고 visual overlay preset에서 사용되는 preset 구성 block입니다.

`FPS Only`는 별도 widget이 아니라 preset/layout mode입니다. FPS 및 timing data를 single compact row에서 재사용합니다.

대부분의 metric group은 textual form과 graphical form을 모두 갖습니다. Textual form은 numeric value가 있는 card 또는 `MetricBars` row이며, graphical form은 budget bar 또는 history graph입니다. 선택한 preset이 표시될 form을 결정하며, 같은 metric source가 다른 layout에서 다르게 나타날 수 있습니다.

Runtime overlay text는 localization되지 않으므로 widget screenshot은 여러 언어 문서에서 공유됩니다.

| Widget ID | Screenshot | Kind | Module | Shows |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | Average FPS, current FPS, 1% low, 0.1% low, budget state. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame, main thread, render thread, frame budget state. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | Unity가 GPU timing을 노출할 때 GPU frame time 및 valid GPU sample count. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Frame spike counters 및 current warning state. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Overdraw measurement state, progress, ratio, heatmap state. HDRP에서는 overdraw/heatmap의 unsupported state를 표시할 수 있습니다. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | 선택한 target-FPS budget 대비 CPU frame time. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | 선택한 target-FPS budget 대비 GPU frame time. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graph | Graphs / Timing | CPU frame, main thread, render thread 및 기타 timing history. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graph | Graphs / GPU timing | target budget line이 있는 GPU frame timing history. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | platform sampling이 가능한 경우 logical core별 CPU load bar. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | logical core별 CPU load history graph. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | project `IPerfMeterCustomMetricProvider` implementation이 제공하는 값. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls, SetPass calls, batches, vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory, GC memory, GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | SRP Batcher 및 BatchRendererGroup / GPU Resident Drawer counters. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | frame 안의 index buffer upload bytes를 포함한 index/upload counters. |

## Metric Bar Captures

기본 `MetricBars` layout은 자주 확인하는 category에 대해 compact row를 렌더링합니다.

| Capture | Shows |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, frame budget, low-FPS indicators. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | 선택한 target FPS 대비 CPU/GPU timing rows. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches, vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | SRP Batcher 및 BatchRendererGroup / GPU Resident Drawer counters. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory, GC memory, GPU memory counters. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Upload counters 및 index-buffer upload bytes. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | project-provided custom metric rows. |

## Notes

- Preset은 이 widget들의 subset을 활성화하고 `MetricBars`, `CompactCards`, `Graphs`, `DiagnosticsWide` 같은 layout을 선택할 수 있습니다.
- Text-row 및 metric-bar row는 layout system 뒤의 lower-level renderer이며, 다른 layout에서 card, budget bar, graph로 나타날 수 있는 동일한 metric group의 textual version을 노출합니다.
