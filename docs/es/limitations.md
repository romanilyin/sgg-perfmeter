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

## External GPU Capture

- El coordinator permite una solicitud activa y avanza de forma determinista por `PreRoll`, `Capturing`, `PostRoll` y `Completed`. La misma ID activa es idempotente; una ID activa diferente se rechaza por solapamiento.
- El backend usa el `ExternalGPUProfiler` experimental de Unity solo en el Editor o Development Builds, cuando una herramienta externa ya está conectada. `RenderDoc` está limitado al escritorio Windows/Linux con Direct3D 11, Direct3D 12 o Vulkan; `PIX` está limitado al escritorio Windows con Direct3D 12.
- `Completed` confirma únicamente el wrapper lifecycle de Unity. No demuestra que exista un artefacto externo `.rdc`/`.wpix` ni proporciona un path de artefacto.
- Los tests automatizados usan un fake backend. La confirmación de la herramienta externa real y del artefacto sigue siendo un release gate.
- Los correlated bundles y MCP capture control están disponibles, pero un `.rdc`/`.wpix` proporcionado sigue siendo solo un artefacto observado y con hash: Unity no puede autenticar la herramienta conectada ni su asociación con el capture. La verificación con una herramienta real sigue siendo un release-candidate gate.

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

El overlay está diseñado para cuidar las asignaciones y usa throttling, pero los valores numéricos cambiantes y las etiquetas de gráficos aún pueden materializar strings managed en el intervalo de refresco. Tiene dos backend paths de UI Toolkit: un host propio `UIDocument` en Unity `6000.4` y un host propio `PanelRenderer` en Unity `6000.5+`. El host conserva los panel settings y children de la UI ajena y reconstruye únicamente el container propio de PerfMeter. Los valores numéricos usan numeric slots reservados estables y un numeric monospace role; `FpsOnly` usa un fallback determinista y acotado de dos filas cuando una fila no cabe, mientras las tarjetas y barras hacen wrap con logical widths estrechas. Esto reduce el riesgo de clipping, pero no promete todas las resoluciones o escalas arbitrarias; los diagnósticos visuales pesados, los modos con gráficos y el layout resultante deben validarse en dispositivos objetivo.

## Estado De Validación

La validación actual incluye cobertura automatizada EditMode, HDRP smoke validation en Unity `6000.4.10f1` y validación smoke previa en Android S23 Vulkan/GLES. Sigue siendo útil ampliar cobertura de player builds y dispositivos antes de tratar los datos como evidencia de aprobación para release.
