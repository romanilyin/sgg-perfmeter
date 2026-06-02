# Widget Implementati

SGG PerfMeter include attualmente 16 widget overlay runtime di alto livello. Sono i blocchi di composizione preset mostrati nella finestra di setup e usati dai preset overlay visivi.

`FPS Only` e una modalita preset/layout, non un widget separato. Riusa dati FPS e timing in una singola riga compatta.

La maggior parte dei gruppi metrici ha forme testuali e grafiche. Le forme testuali sono card o righe `MetricBars` con valori numerici, mentre le forme grafiche sono barre budget o grafici storici. Il preset selezionato decide quale forma viene mostrata; la stessa sorgente metrica puo apparire in layout diversi.

Il testo dell'overlay runtime non e localizzato, quindi la documentazione italiana usa gli stessi screenshot dei widget.

| Widget ID | Screenshot | Tipo | Modulo | Mostra |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | Average FPS, current FPS, 1% low, 0.1% low e stato budget. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame, main thread, render thread e stato frame budget. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | GPU frame time e conteggio dei sample GPU validi quando Unity espone il GPU timing. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Contatori frame spike e stato warning corrente. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Stato della misurazione overdraw, progresso, ratio e stato heatmap. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | CPU frame time rispetto al budget target-FPS selezionato. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | GPU frame time rispetto al budget target-FPS selezionato. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Grafico | Graphs / Timing | CPU frame, main thread, render thread e altro storico timing. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Grafico | Graphs / GPU timing | Storico GPU frame timing con linea di target budget. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | Barre del carico CPU per core logico dove il platform sampling e disponibile. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | Grafici storici del carico CPU per core logico. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | Valori forniti da implementazioni `IPerfMeterCustomMetricProvider` del progetto. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls, SetPass calls, batches e vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory, GC memory e GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | Contatori SRP Batcher e BatchRendererGroup / GPU Resident Drawer. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | Contatori index/upload, inclusi index buffer upload bytes nel frame. |

## Catture Metric Bar

Il layout predefinito `MetricBars` renderizza righe compatte per categorie osservate di frequente:

| Cattura | Mostra |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, frame budget e indicatori low-FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | Righe timing CPU/GPU rispetto al target FPS selezionato. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches e vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | Contatori SRP Batcher e BatchRendererGroup / GPU Resident Drawer. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory, GC memory e GPU memory counters. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Contatori upload e index-buffer upload bytes. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Righe custom metric fornite dal progetto. |

## Note

- I preset possono abilitare un sottoinsieme di questi widget e scegliere un layout come `MetricBars`, `CompactCards`, `Graphs` o `DiagnosticsWide`.
- Le righe testuali e metric-bar sono renderer di livello inferiore dietro il sistema di layout ed espongono versioni testuali degli stessi gruppi metrici che possono apparire come card, barre budget o grafici in altri layout.
