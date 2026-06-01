# Implemented Widgets

SGG PerfMeter currently ships 16 high-level runtime overlay widgets. These are the preset composition blocks shown in the setup window and used by visual overlay presets.

`FPS Only` is a preset/layout mode, not a separate widget. It reuses FPS and timing data in a single compact row.

Most metric groups have both textual and graphical forms. Textual forms are cards or `MetricBars` rows with numeric values, while graphical forms are budget bars or history graphs. The selected preset decides which form is shown; the same metric source can appear in different layouts.

Runtime overlay text is not localized, so English and Russian documentation use the same widget screenshots.

| Widget ID | Screenshot | Kind | Module | Shows |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | Average FPS, current FPS, 1% low, 0.1% low, and budget state. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame, main thread, render thread, and frame budget state. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | GPU frame time and valid GPU sample count when Unity exposes GPU timing. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Frame spike counters and current warning state. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Overdraw measurement state, progress, ratio, and heatmap state. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | CPU frame time against the selected target-FPS budget. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | GPU frame time against the selected target-FPS budget. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graph | Graphs / Timing | CPU frame, main thread, render thread, and other timing history. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graph | Graphs / GPU timing | GPU frame timing history with target budget line. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | Per-logical-core CPU load bars where platform sampling is available. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | Per-logical-core CPU load history graphs. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | Values supplied by project `IPerfMeterCustomMetricProvider` implementations. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls, SetPass calls, batches, and vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory, GC memory, and GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | SRP Batcher and BatchRendererGroup / GPU Resident Drawer counters. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | Index/upload counters, including index buffer upload bytes in frame. |

## Metric Bar Captures

The default `MetricBars` layout renders compact rows for frequently watched categories:

| Capture | Shows |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, frame budget, and low-FPS indicators. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | CPU/GPU timing rows against the selected target FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches, and vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | SRP Batcher and BatchRendererGroup / GPU Resident Drawer counters. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory, GC memory, and GPU memory counters. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Upload counters and index-buffer upload bytes. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Project-provided custom metric rows. |

## Notes

- Presets can enable a subset of these widgets and choose a layout such as `MetricBars`, `CompactCards`, `Graphs`, or `DiagnosticsWide`.
- Text-row and metric-bar rows are lower-level renderers behind the layout system and expose textual versions of the same metric groups that may appear as cards, budget bars, or graphs in other layouts.
