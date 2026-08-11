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
| `perfmeter.compatibility.status` | Leer por separado la compatibilidad de import, core runtime e integración de render activa. |
| `perfmeter.runtime.status` | Leer estado runtime. |
| `perfmeter.runtime.ensure` | Iniciar runtime si hace falta. |
| `perfmeter.runtime.stop` | Detener runtime. |
| `perfmeter.runtime.reset_stats` | Restablecer rolling stats, contadores de alertas y contadores de sesión activa. |
| `perfmeter.runtime.mode.set` | Cambiar a `Stopped`, `Background`, `Overlay` u `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Leer las métricas más recientes, incluidas custom metrics. |
| `perfmeter.profiler.capabilities` | Leer las capabilities y la procedencia de resolución de métricas del Profiler en caché sin iniciar el runtime ni discovery. |
| `perfmeter.profiler.lease.capabilities` | Leer recursos de profiler lease locales al proceso y semántica de reload. |
| `perfmeter.profiler.lease.status` | Leer el estado actual o coincidente de profiler lease local al proceso. |
| `perfmeter.alerts.latest` | Leer alertas activas, contadores y estado de advertencias del Editor. |
| `perfmeter.alerts.clear` | Limpiar alertas activas, contadores y estado de cooldown. |
| `perfmeter.alerts.capture.begin` | Iniciar la clasificacion acotada de una captura externa. |
| `perfmeter.alerts.capture.end` | Finalizar la clasificacion de captura externa correspondiente. |
| `perfmeter.device.info` | Leer información de device, graphics, display, monitor, pipeline y entorno Unity. |
| `perfmeter.camera.snapshot` | Leer transform/projection de camera y URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Leer los últimos diagnostics de render integration observados para URP Render Graph o HDRP Custom Pass. |
| `perfmeter.render.snapshot` | Leer el snapshot neutral de render integration, con freshness, contexto de cámara/pass, GRD/VRS y la facade legacy de Render Graph. |
| `perfmeter.overlay.set` | Mostrar/ocultar overlay y definir preset, modules, corner, mode y target FPS. |
| `perfmeter.overdraw.start` | Iniciar medición de overdraw acotada. |
| `perfmeter.overdraw.cancel` | Cancelar medición de overdraw activa. |
| `perfmeter.overdraw.heatmap.set` | Mostrar u ocultar overdraw heatmap visual. |
| `perfmeter.session.start` | Iniciar grabación de sesión acotada. |
| `perfmeter.session.stop` | Detener la grabación y devolver resumen. |
| `perfmeter.session.summary` | Leer el resumen de sesión actual. |
| `perfmeter.session.export` | Exportar la sesión actual a JSON o CSV local del proyecto. |
| `perfmeter.capture.request` | Solicitar un capture GPU acotado; `backend_mode` opcional: `GenericUnity`, `NativePreferred` o `NativeRequired`. El storage mode nativo solo se elige en la API C#. |
| `perfmeter.capture.status` | Leer el estado del capture y del bundle. |
| `perfmeter.capture.cancel` | Cancelar el capture activo coincidente. |
| `perfmeter.capture.export` | Exportar atómicamente un bundle listo bajo la raíz local del proyecto. |
| `perfmeter.capture.export.request` | Encolar un export single-flight y devolver su export ID y progreso. |
| `perfmeter.capture.export.status` | Leer fase, progreso, cancelación, retry y autoridad del artefacto. |
| `perfmeter.capture.export.cancel` | Solicitar la cancelación del export activo coincidente. |
| `perfmeter.capture.capabilities` | Leer capacidades de schema, cuota, retención, screenshot y provenance. |

Usa preferentemente `perfmeter.capture.export.request`, consulta después `perfmeter.capture.export.status` y llama opcionalmente a `perfmeter.capture.export.cancel`. El comando legacy `perfmeter.capture.export` bloquea por compatibilidad. Las respuestas incluyen el envelope genérico `external_artifact` con asociación, autoridad, finalización, contenido, política de privacidad/uso compartido, tamaño y hashes de origen y post-copia. Los comandos read-only de lease exponen el estado de conflicto local al proceso sin adquirir una lease.

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

## Comandos de snapshots de memoria

| Comando | Propósito y entradas principales |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Solicitar un snapshot manual con `capture_id`, booleanos opcionales de flags de captura, `minimum_free_disk_mb` y `cooldown_seconds`. |
| `perfmeter.memory.snapshot.status` | Leer el estado del snapshot y del bundle correlacionado sin iniciar el runtime ni exponer el source path temporal. |
| `perfmeter.memory.snapshot.capabilities` | Leer provenance del backend, flags compatibles, el límite de 512 MiB y la raíz temporal propiedad de PerfMeter. |
| `perfmeter.memory.snapshot.triggers.configure` | Activar o desactivar explícitamente los triggers de umbral de memoria del sistema y crecimiento acotado de fugas, su ventana de frames, flags, guard de espacio libre y cooldown. |

Los comandos de solicitud y configuración de triggers requieren Play Mode. La automatización está deshabilitada por defecto. Secuencia típica:

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Espera hasta que el bundle esté listo para exportar y usa el comando existente `perfmeter.capture.export`. Un bundle solo de memoria usa `requested_tool: MemoryProfiler`, incluye `memory-snapshot.json` y provenance en el manifest, y no contiene un artefacto GPU externo. El export correcto es de un solo uso y elimina el source de staging propiedad de PerfMeter.

## Comandos de diagnóstico gráfico y GraphicsStateCollection

Los siguientes seis comandos forman la superficie de PM-GFX-001:

| Comando | Propósito y entradas principales |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Leer los últimos valores de markers de creación de programas GPU de shaders y de graphics pipelines, la provenance dinámica de capabilities, la revisión del catálogo y el contexto de la API gráfica. Sin entradas. |
| `perfmeter.graphics.state_collection.request` | Iniciar un trace acotado. Requiere Play Mode y una sesión activa de PerfMeter; `capture_id` es obligatorio, `trace_frames` es 1–600 (por defecto 60) y `minimum_free_disk_mb` por defecto es 1024. |
| `perfmeter.graphics.state_collection.status` | Leer availability, state, progreso, identidad del backend, counts, `is_busy`, `has_pending_cleanup`, warnings y el path relativo al proyecto del artifact owned. Sin entradas. |
| `perfmeter.graphics.state_collection.capabilities` | Leer provenance del backend, soporte de trace/prewarm, soporte de cache-miss y PSO paralelo, requisito de sesión, límites de 600 frames y 64 MiB y raíz de artifacts owned. Sin entradas. |
| `perfmeter.graphics.state_collection.cancel` | Cancelar el trace activo o en preparación que coincida y limpiar su artifact pendiente. Requiere `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Cargar y hacer prewarm síncrono de un artifact owned relativo al proyecto en Play Mode. `relative_path` es obligatorio; `max_state_count` es 0–1.000.000 y por defecto 0. |

`perfmeter.graphics.diagnostics` devuelve `shader_gpu_program_creation_value` y `graphics_pipeline_creation_value`, además de `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count` y `sampled_component_count` para cada capability. `perfmeter.metrics.latest` y los exports de sesión exponen la misma metadata de markers. Los valores conservan la unidad descubierta del recorder y no son universalmente counts de shaders o PSO; usa `sample_state` en vez de interpretar cero como unavailable.

La respuesta de state incluye `result`, `availability`, `state`, `capture_id`, frames de trace solicitados/completados, ID/versión del backend, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup` y `warning`. `is_busy` permanece true durante preparación, trace, finalización, prewarm, cleanup o cleanup persistente; `has_pending_cleanup` identifica un artifact owned pendiente de retry. Un borrado fallido se persiste mediante un sidecar owned `.delete-pending`, que se restaura y se reintenta tras un domain reload. `StopSession` cancela un trace activo, por lo que la sesión debe permanecer activa hasta terminar. El trace llega a su estado terminal después de tickear los frames solicitados al final del frame; en batch mode usa un fallback del frame siguiente. Los samples admitidos por una sesión activa incluyen `graphics_state_trace_id` igual a `capture_id`.

Secuencia típica de trace y prewarm:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

Solo se admite un graphics-state flight. Un ID activo repetido devuelve `AlreadyActive`; otro trace/prewarm en overlap devuelve `RejectedOverlap`. Cancel solo coincide con el ID activo/en preparación. El backend de Unity informa `supports_cache_miss_tracing: false`: la evidencia de cache-miss no está soportada y el schema MCP de prewarm no ofrece ese input. Los artifacts son propiedad de PerfMeter, están bajo `Temp/PerfMeter/GraphicsStateCollections` y tienen un límite de 64 MiB.

## Snapshot de integración de render

`perfmeter.render.snapshot {}` es un comando read-only sin inputs. No inicia el runtime. La respuesta usa `schema_version: 1` y devuelve `render_integration` con pipeline/source actuales, frame y age de la observation, `observation_matches_current_pipeline`, identidad de la cámara observada, metadata de integration/pass/injection, cantidad de passes de PerfMeter realmente programados, effective rendering mode cuando está disponible, contexto anidado `gpu_resident_drawer` y `variable_rate_shading`, y `legacy_render_graph`.

`gpu_resident_drawer` incluye soporte de proyecto/compute, actividad global pública con `activity_source`, compatibilidad URP Forward+/clustered, `degraded_reason` y `effectiveness` BRG anidada. Los valores son `null` salvo que la capability sea `AvailableSampled`; recorder names, resolución exact/alias y component counts conservan la provenance. `scope: "brg_aggregate"` no demuestra uso de GRD por renderer.

El comando es el equivalente MCP de `PerformanceMeter.GetRenderIntegrationSnapshot()` y `TryGetRenderIntegrationSnapshot(...)`. Una observation stale se informa con non-match y warning explícitos, no como actual. `perfmeter.rendergraph.snapshot` permanece como facade legacy. El comando no añade navegación del Editor: las API estables de Unity no exponen un viewer de RenderGraph/CustomPass ni información de pass targets.
