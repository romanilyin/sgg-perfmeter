# 已实现 Widgets

SGG PerfMeter 目前随附 16 个 high-level runtime overlay widgets。这些 widgets 是 setup window 中显示并被 visual overlay presets 使用的 preset composition blocks。

`FPS Only` 是 preset/layout mode，不是单独的 widget。它在单行 compact row 中复用 FPS 和 timing data。

大多数 metric groups 同时具有文本和图形形式。文本形式是 cards 或 `MetricBars` rows，显示 numeric values；图形形式是 budget bars 或 history graphs。所选 preset 决定显示哪种形式；同一 metric source 可以出现在不同 layouts 中。

Runtime overlay text 未本地化，因此中文文档使用相同的 widget screenshots。

| Widget ID | Screenshot | 类型 | Module | 显示内容 |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | Average FPS、current FPS、1% low、0.1% low 和 budget state。 |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame、main thread、render thread 和 frame budget state。 |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | Unity 暴露 GPU timing 时的 GPU frame time 和 valid GPU sample count。 |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Frame spike counters 和当前 warning state。 |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Overdraw measurement state、progress、ratio 和 heatmap state。 |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | CPU frame time 相对于所选 target-FPS budget。 |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | GPU frame time 相对于所选 target-FPS budget。 |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graph | Graphs / Timing | CPU frame、main thread、render thread 和其他 timing history。 |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graph | Graphs / GPU timing | GPU frame timing history 和 target budget line。 |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | 平台 sampling 可用时的 per-logical-core CPU load bars。 |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | Per-logical-core CPU load history graphs。 |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | 项目 `IPerfMeterCustomMetricProvider` implementations 提供的 values。 |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls、SetPass calls、batches 和 vertices。 |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory、GC memory 和 GPU memory counters。 |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | SRP Batcher 和 BatchRendererGroup / GPU Resident Drawer counters。 |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | Index/upload counters，包括 frame 中的 index buffer upload bytes。 |

## Metric Bar Captures

默认 `MetricBars` layout 会为常关注类别渲染 compact rows：

| Capture | 显示内容 |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS、frame budget 和 low-FPS indicators。 |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | CPU/GPU timing rows，相对于所选 target FPS。 |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls、SetPass calls、batches 和 vertices。 |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | SRP Batcher 和 BatchRendererGroup / GPU Resident Drawer counters。 |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory、GC memory 和 GPU memory counters。 |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Upload counters 和 index-buffer upload bytes。 |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | 项目提供的 custom metric rows。 |

## 说明

- Presets 可以启用这些 widgets 的子集，并选择 `MetricBars`、`CompactCards`、`Graphs` 或 `DiagnosticsWide` 等 layout。
- Text-row 和 metric-bar rows 是 layout system 后面的 lower-level renderers，提供相同 metric groups 的文本版本；这些 metric groups 在其他 layouts 中可能以 cards、budget bars 或 graphs 形式出现。
