# Implementierte Widgets

SGG PerfMeter enthaelt 16 High-Level-Widgets fuer den Runtime-Overlay. Diese Kompositionsbloecke werden im Setup-Fenster angezeigt und von Visual Presets genutzt.

`FPS Only` ist ein Preset/Layout-Modus, kein eigenes Widget. Er nutzt FPS- und Timing-Daten in einer kompakten Zeile.

Die meisten Metrikgruppen haben Text- und Grafikformen. Textformen sind Cards oder `MetricBars`-Zeilen mit Zahlenwerten, Grafikformen sind Budget Bars oder History Graphs. Der Preset entscheidet, welche Form gezeigt wird.

Runtime-Overlay-Text ist nicht lokalisiert, deshalb nutzen alle Sprachen dieselben Widget-Screenshots.

| Widget ID | Screenshot | Typ | Modul | Zeigt |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | Average FPS, current FPS, 1% low, 0.1% low und Budget-Status. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame, main thread, render thread und Frame-Budget-Status. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | GPU frame time und gueltige GPU samples, wenn Unity GPU timing liefert. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Frame-spike-Zaehler und aktueller Warning-Status. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Overdraw measurement state, progress, ratio und heatmap state. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | CPU frame time gegen das gewaehlte Ziel-FPS-Budget. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | GPU frame time gegen das gewaehlte Ziel-FPS-Budget. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graph | Graphs / Timing | CPU frame, main thread, render thread und weitere Timing-History. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graph | Graphs / GPU timing | GPU frame timing history mit Zielbudget-Linie. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | CPU-Lastleisten pro logischem Kern, wenn platform sampling verfuegbar ist. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | CPU-Last-History pro logischem Kern. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | Werte von `IPerfMeterCustomMetricProvider`-Implementierungen im Projekt. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls, SetPass calls, batches und vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory, GC memory und GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | SRP Batcher und BatchRendererGroup / GPU Resident Drawer counters. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | Index/upload counters, inklusive index buffer upload bytes im Frame. |

## MetricBars-Screenshots

Das Standardlayout `MetricBars` rendert kompakte Zeilen fuer haeufig beobachtete Kategorien:

| Screenshot | Zeigt |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, Frame-Budget und Low-FPS-Indikatoren. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | CPU/GPU timing rows gegen den gewaehlten Ziel-FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches und vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | SRP Batcher und BatchRendererGroup / GPU Resident Drawer counters. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory, GC memory und GPU memory counters. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Upload counters und index-buffer upload bytes. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Projektdefinierte Custom-Metric-Zeilen. |
