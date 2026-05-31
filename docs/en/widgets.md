# Implemented Widgets

SGG PerfMeter currently ships 16 high-level runtime overlay widgets. These are the preset composition blocks shown in the setup window and used by visual overlay presets.

`FPS Only` is a preset/layout mode, not a separate widget. It reuses FPS and timing data in a single compact row.

Runtime widget screenshots are reserved but not included yet. Both English and Russian documentation will use the same images under `docs/assets/screenshots/widgets/` because runtime overlay text is not localized.

| Widget ID | Display Name | Kind | Module | Shows | Reserved Screenshot |
| --- | --- | --- | --- | --- | --- |
| `fps.summary-card` | FPS summary card | Card | FPS | Average FPS, current FPS, 1% low, 0.1% low, and budget state. | `../assets/screenshots/widgets/fps-summary-card.png` |
| `timing.cpu-card` | CPU timing card | Card | Timing | CPU frame, main thread, render thread, and frame budget state. | `../assets/screenshots/widgets/timing-cpu-card.png` |
| `timing.gpu-card` | GPU timing card | Card | GPU timing | GPU frame time and valid GPU sample count when Unity exposes GPU timing. | `../assets/screenshots/widgets/timing-gpu-card.png` |
| `timing.frame-spikes-card` | Frame spikes card | Card | FPS / Warnings | Frame spike counters and current warning state. | `../assets/screenshots/widgets/timing-frame-spikes-card.png` |
| `overdraw.card` | Overdraw card | Card | Overdraw / Heatmap | Overdraw measurement state, progress, ratio, and heatmap state. | `../assets/screenshots/widgets/overdraw-card.png` |
| `timing.cpu-budget-bar` | CPU budget bar | Budget bar | Timing | CPU frame time against the selected target-FPS budget. | `../assets/screenshots/widgets/timing-cpu-budget-bar.png` |
| `timing.gpu-budget-bar` | GPU budget bar | Budget bar | GPU timing | GPU frame time against the selected target-FPS budget. | `../assets/screenshots/widgets/timing-gpu-budget-bar.png` |
| `graphs.cpu-timing` | CPU timing graph | Graph | Graphs / Timing | CPU frame, main thread, render thread, and other timing history. | `../assets/screenshots/widgets/graphs-cpu-timing.png` |
| `graphs.gpu-timing` | GPU timing graph | Graph | Graphs / GPU timing | GPU frame timing history with target budget line. | `../assets/screenshots/widgets/graphs-gpu-timing.png` |
| `cpu.cores-bars` | CPU core bars | Panel | CPU core sampling | Per-logical-core CPU load bars where platform sampling is available. | `../assets/screenshots/widgets/cpu-cores-bars.png` |
| `cpu.cores-graphs` | CPU core graphs | Panel | CPU core sampling / Graphs | Per-logical-core CPU load history graphs. | `../assets/screenshots/widgets/cpu-cores-graphs.png` |
| `custom-metrics.panel` | Custom metrics panel | Panel | Custom metrics | Values supplied by project `IPerfMeterCustomMetricProvider` implementations. | `../assets/screenshots/widgets/custom-metrics-panel.png` |
| `rendering.summary-card` | Rendering summary card | Card | Rendering | Draw calls, SetPass calls, batches, and vertices. | `../assets/screenshots/widgets/rendering-summary-card.png` |
| `memory.summary-card` | Memory summary card | Card | Memory / GC / GPU memory | System memory, GC memory, and GPU memory counters. | `../assets/screenshots/widgets/memory-summary-card.png` |
| `batching.summary-card` | Batching summary card | Card | SRP Batcher / BRG | SRP Batcher and BatchRendererGroup / GPU Resident Drawer counters. | `../assets/screenshots/widgets/batching-summary-card.png` |
| `uploads.summary-card` | Uploads summary card | Card | Uploads | Index/upload counters, including index buffer upload bytes in frame. | `../assets/screenshots/widgets/uploads-summary-card.png` |

## Notes

- Presets can enable a subset of these widgets and choose a layout such as `MetricBars`, `CompactCards`, `Graphs`, or `DiagnosticsWide`.
- Text-row and metric-bar rows are lower-level renderers behind the layout system and are intentionally not listed as high-level preset widgets.
