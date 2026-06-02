# Widgets Implementados

SGG PerfMeter incluye actualmente 16 widgets runtime de overlay de alto nivel. Son los bloques de composición de presets mostrados en la ventana de setup y usados por los visual overlay presets.

`FPS Only` es un modo de preset/layout, no un widget separado. Reutiliza datos de FPS y timing en una sola fila compacta.

La mayoría de grupos de métricas tienen formas textuales y gráficas. Las formas textuales son tarjetas o filas `MetricBars` con valores numéricos, mientras que las formas gráficas son barras de budget o gráficos de historial. El preset seleccionado decide qué forma se muestra; la misma fuente de métricas puede aparecer en layouts distintos.

El texto del overlay runtime no está localizado, por lo que la documentación en español usa los mismos screenshots de widgets compartidos.

| Widget ID | Screenshot | Tipo | Módulo | Muestra |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Tarjeta | FPS | Average FPS, current FPS, 1% low, 0.1% low y estado de budget. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Tarjeta | Timing | CPU frame, main thread, render thread y estado de frame budget. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Tarjeta | GPU timing | GPU frame time y recuento de muestras GPU válidas cuando Unity expone GPU timing. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Tarjeta | FPS / Warnings | Contadores de frame spikes y estado de warning actual. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Tarjeta | Overdraw / Heatmap | Estado de medición de overdraw, progreso, ratio y estado de heatmap. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Barra de budget | Timing | CPU frame time frente al budget del target-FPS seleccionado. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Barra de budget | GPU timing | GPU frame time frente al budget del target-FPS seleccionado. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Gráfico | Graphs / Timing | Historial de CPU frame, main thread, render thread y otros timings. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Gráfico | Graphs / GPU timing | Historial de GPU frame timing con línea de target budget. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panel | CPU core sampling | Barras de carga CPU por core lógico cuando el sampling de plataforma está disponible. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panel | CPU core sampling / Graphs | Gráficos de historial de carga CPU por core lógico. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panel | Custom metrics | Valores suministrados por implementaciones de `IPerfMeterCustomMetricProvider` del proyecto. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Tarjeta | Rendering | Draw calls, SetPass calls, batches y vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Tarjeta | Memory / GC / GPU memory | System memory, GC memory y GPU memory counters. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Tarjeta | SRP Batcher / BRG | Contadores de SRP Batcher y BatchRendererGroup / GPU Resident Drawer. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Tarjeta | Uploads | Contadores de index/upload, incluidos bytes de subida de index buffer en el frame. |

## Capturas De Metric Bars

El layout predeterminado `MetricBars` renderiza filas compactas para categorías observadas con frecuencia:

| Captura | Muestra |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, frame budget e indicadores de FPS bajos. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | Filas de CPU/GPU timing frente al target FPS seleccionado. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches y vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | Contadores de SRP Batcher y BatchRendererGroup / GPU Resident Drawer. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | Contadores de system memory, GC memory y GPU memory. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Contadores de upload y bytes de subida de index-buffer. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Filas de custom metrics proporcionadas por el proyecto. |

## Notas

- Los presets pueden activar un subconjunto de estos widgets y elegir un layout como `MetricBars`, `CompactCards`, `Graphs` o `DiagnosticsWide`.
- Las filas de texto y metric-bar son renderers de menor nivel detrás del sistema de layouts y exponen versiones textuales de los mismos grupos de métricas que pueden aparecer como tarjetas, barras de budget o gráficos en otros layouts.
