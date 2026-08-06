# MCP Y Automatización Con Agentes

SGG PerfMeter expone metadatos de comandos para flujos Unity MCP/editor-agent bajo la ruta del paquete:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

El objetivo es salida JSON estructurada para agentes en lugar de parsing de screenshots, parsing de texto del overlay o scraping de Unity Console.

## Grupos De Comandos

| Comando | Propósito |
| --- | --- |
| `perfmeter.setup.status` | Leer estado de setup. |
| `perfmeter.setup.run` | Ejecutar acciones de setup recomendadas. |
| `perfmeter.runtime.status` | Leer estado runtime. |
| `perfmeter.runtime.ensure` | Iniciar runtime si hace falta. |
| `perfmeter.runtime.stop` | Detener runtime. |
| `perfmeter.runtime.reset_stats` | Restablecer rolling stats, contadores de alertas y contadores de sesión activa. |
| `perfmeter.runtime.mode.set` | Cambiar a `Stopped`, `Background`, `Overlay` u `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Leer las métricas más recientes, incluidas custom metrics. |
| `perfmeter.profiler.capabilities` | Leer las capabilities y la procedencia de resolución de métricas del Profiler en caché sin iniciar el runtime ni discovery. |
| `perfmeter.alerts.latest` | Leer alertas activas, contadores y estado de advertencias del Editor. |
| `perfmeter.alerts.clear` | Limpiar alertas activas, contadores y estado de cooldown. |
| `perfmeter.alerts.capture.begin` | Iniciar la clasificacion acotada de una captura externa. |
| `perfmeter.alerts.capture.end` | Finalizar la clasificacion de captura externa correspondiente. |
| `perfmeter.device.info` | Leer información de device, graphics, display, monitor, pipeline y entorno Unity. |
| `perfmeter.camera.snapshot` | Leer transform/projection de camera y URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Leer los últimos diagnostics de render integration observados para URP Render Graph o HDRP Custom Pass. |
| `perfmeter.overlay.set` | Mostrar/ocultar overlay y definir preset, modules, corner, mode y target FPS. |
| `perfmeter.overdraw.start` | Iniciar medición de overdraw acotada. |
| `perfmeter.overdraw.cancel` | Cancelar medición de overdraw activa. |
| `perfmeter.overdraw.heatmap.set` | Mostrar u ocultar overdraw heatmap visual. |
| `perfmeter.session.start` | Iniciar grabación de sesión acotada. |
| `perfmeter.session.stop` | Detener la grabación y devolver resumen. |
| `perfmeter.session.summary` | Leer el resumen de sesión actual. |
| `perfmeter.session.export` | Exportar la sesión actual a JSON o CSV local del proyecto. |
| `perfmeter.capture.request` | Solicitar un capture GPU externo acotado y un bundle correlacionado. |
| `perfmeter.capture.status` | Leer el estado del capture y del bundle. |
| `perfmeter.capture.cancel` | Cancelar el capture activo coincidente. |
| `perfmeter.capture.export` | Exportar atómicamente un bundle listo bajo la raíz local del proyecto. |
| `perfmeter.capture.capabilities` | Leer capacidades de schema, cuota, retención, screenshot y provenance. |

## Self-Overhead En Runtime Status

`perfmeter.runtime.status` incluye el objeto aditivo `self_overhead`; no es un comando separado. Sus claves superiores son `state`, `cpu_timing_available`, `gpu_timing_availability` y `has_budget_violation`.

Los objetos de componente son `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration` y `hdrp_render_integration`. Cada uno contiene `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state` y `allocation_budget_state`.

Los valores describen ventanas fijas de 120 frames para callbacks CPU con promedios por invocacion. La atribucion GPU es `Unavailable`; la render integration inactiva es `Unsupported` y un componente compatible sin llamadas es `NotMeasured`. Los schemas JSON/CSV de sesion no cambian y las metricas CPU/GPU existentes no se ajustan.

## Ejecución Típica De Profiling

```text
perfmeter.profiler.capabilities {}
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Usa `OverdrawDiagnostic` solo para ventanas de diagnóstico URP acotadas porque el overdraw numérico y el render del heatmap añaden trabajo extra de GPU. HDRP informa overdraw/heatmap como unsupported, mientras el resto de diagnostics sigue disponible.
