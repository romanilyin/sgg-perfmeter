# Widgets Implementados

SGG PerfMeter atualmente inclui 16 widgets runtime de overlay de alto nivel. Estes sao os blocos de composicao de presets mostrados na janela de setup e usados por visual overlay presets.

`FPS Only` e um modo de preset/layout, nao um widget separado. Ele reutiliza dados de FPS e timing em uma unica linha compacta.

A maioria dos grupos de metricas tem formas textuais e graficas. Formas textuais sao cards ou linhas `MetricBars` com valores numericos, enquanto formas graficas sao barras de budget ou graficos de historico. O preset selecionado decide qual forma e mostrada; a mesma fonte de metrica pode aparecer em layouts diferentes.

O texto do overlay runtime nao e localizado, entao as documentacoes localizadas usam os mesmos screenshots de widgets.

| Widget ID | Screenshot | Tipo | Modulo | Mostra |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Card | FPS | FPS medio, FPS atual, 1% low, 0.1% low e estado de budget. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Card | Timing | CPU frame, main thread, render thread e estado de frame budget. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Card | GPU timing | GPU frame time e contagem de amostras GPU validas quando Unity expoe GPU timing. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Card | FPS / Warnings | Contadores de frame spikes e estado atual de aviso. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Card | Overdraw / Heatmap | Estado de medicao de overdraw, progresso, ratio e estado do heatmap; em HDRP pode mostrar unsupported state para overdraw/heatmap. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Budget bar | Timing | CPU frame time contra o budget do target-FPS selecionado. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Budget bar | GPU timing | GPU frame time contra o budget do target-FPS selecionado. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graph | Graphs / Timing | CPU frame, main thread, render thread e outro historico de timing. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graph | Graphs / GPU timing | Historico de GPU frame timing com linha de target budget. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | Barras de carga por nucleo logico de CPU onde sampling da plataforma esta disponivel. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | Graficos de historico de carga por nucleo logico de CPU. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | Valores fornecidos por implementacoes de projeto de `IPerfMeterCustomMetricProvider`. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Card | Rendering | Draw calls, SetPass calls, batches e vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Card | Memory / GC / GPU memory | System memory, GC memory e GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Card | SRP Batcher / BRG | SRP Batcher e BatchRendererGroup / GPU Resident Drawer counters. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Card | Uploads | Index/upload counters, incluindo bytes de upload de index buffer no frame. |

## Capturas De Metric Bar

O layout padrao `MetricBars` renderiza linhas compactas para categorias observadas com frequencia:

| Captura | Mostra |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, frame budget e indicadores de low-FPS. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | Linhas de timing CPU/GPU contra o target FPS selecionado. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches e vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | SRP Batcher e BatchRendererGroup / GPU Resident Drawer counters. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | System memory, GC memory e GPU memory counters. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Upload counters e bytes de upload de index-buffer. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Linhas de custom metric fornecidas pelo projeto. |

## Observacoes

- Presets podem ativar um subconjunto destes widgets e escolher um layout como `MetricBars`, `CompactCards`, `Graphs` ou `DiagnosticsWide`.
- Linhas de texto e linhas de metric-bar sao renderizadores de nivel mais baixo por tras do sistema de layout e expoem versoes textuais dos mesmos grupos de metricas que podem aparecer como cards, barras de budget ou graficos em outros layouts.
