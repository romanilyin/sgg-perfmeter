# Limitaciones

SGG PerfMeter está diseñado como una capa de diagnóstico runtime de bajo overhead. No sustituye a capturas profundas de Unity Profiler, RenderDoc, Profile Analyzer o Frame Debugger.

## Alcance De Plataforma Y Pipeline

- Target runtime soportado: Unity `6000.4+` con URP `17.4+` Render Graph o HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline no está soportado ni planificado.
- HDRP overdraw y heatmap no tienen soporte. Los proyectos HDRP mantienen diagnostics de FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup y MCP.
- Unity `2022.3` hasta `6000.3` puede importar por seguridad de compilación, pero el comportamiento runtime y el target con soporte son Unity `6000.4+`.

## Disponibilidad De Timing

- El GPU timing puede no estar disponible, llegar con retraso o ser poco fiable según la plataforma y graphics API.
- `CollectionFrame` es el frame de Unity donde PerfMeter recolectó el snapshot, no necesariamente el frame exacto de hardware representado por `FrameTimingManager`.
- En Android conviene preferir Vulkan cuando el GPU frame timing importa.
- OpenGL/OpenGLES debe tratarse como modo degradado para GPU timing e instrumentación de overdraw.

## Disponibilidad De Contadores

Los profiler counters varían por plataforma, versión de Unity, configuración de render pipeline y graphics API. Usa `AvailableCounters`, `UnavailableCounters` y warnings en lugar de asumir que todos los contadores existen en todas partes.

## Coste Y Soporte De Overdraw

El overdraw numérico y el heatmap visual son modos de diagnóstico. Añaden trabajo de render y deben usarse en ventanas acotadas, no como UI de gameplay en estado estable.

El overdraw numérico en URP requiere:

- `PerfMeterRenderGraphFeature` instalado en el URP renderer activo;
- soporte de UAV/storage-buffer en fragment-stage;
- soporte de compute shader;
- graphics API compatible;
- soporte de async GPU readback.

Los targets no compatibles, incluido HDRP, informan `OverdrawState.Unsupported` con warnings.

## Coste Del Overlay

El overlay está diseñado para cuidar las asignaciones y usa throttling, pero los valores numéricos cambiantes y las etiquetas de gráficos aún pueden materializar strings managed en el intervalo de refresco. Los diagnósticos visuales pesados y los modos con gráficos deben validarse en dispositivos objetivo.

## Estado De Validación

La validación actual incluye cobertura automatizada EditMode, HDRP smoke validation en Unity `6000.4.10f1` y validación smoke previa en Android S23 Vulkan/GLES. Sigue siendo útil ampliar cobertura de player builds y dispositivos antes de tratar los datos como evidencia de aprobación para release.
