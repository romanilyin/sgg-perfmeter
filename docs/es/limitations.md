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
- `GenericUnity` usa el `ExternalGPUProfiler` experimental de Unity en Editor/Development Build. Su matriz sigue siendo RenderDoc en Windows/Linux desktop con D3D11/D3D12/Vulkan y PIX en Windows desktop con D3D12; completion no autentica herramienta ni artefacto.
- La ruta nativa opcional solo admite RenderDoc en el Editor Unity Windows x64 con D3D11, D3D12 o Vulkan. Development Player, Linux nativo, IL2CPP, mobile y macOS nativo no están soportados.
- El paquete UPM sigue sin binarios. El bridge fijado y separado solo usa una `renderdoc.dll` ya cargada y nunca instala, carga, inicia ni inyecta RenderDoc.
- Native MetadataOnly usa `DoNotShare` por defecto; Copy/Embed son sensibles, tienen cuotas separadas y requieren `ReviewBeforeShare`. Los artefactos genéricos o del caller siguen observed, no autoritativos.
- La captura de timing circular nativa de PIX no esta disponible. La API de timing de Windows documentada por Microsoft admite captura hacia delante, pero ignora los controles de almacenamiento circular, limite de memoria y descarte; PerfMeter no sustituye el anillo previo a la alerta solicitado por una captura hacia delante sin un limite de almacenamiento documentado ni por una integracion privada de PIX.
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

## Límites y privacidad de los snapshots de memoria opcionales

- La función no está disponible sin `com.unity.memoryprofiler` `1.1.0+` en Unity `6000.4+`; el paquete core no instala ni requiere esa dependencia.
- La captura manual es la única opción predeterminada. Los triggers de umbral de memoria del sistema y crecimiento acotado de fugas son opt-in; cada solicitud está sujeta a guards de single-flight/overlap, cooldown, espacio libre mínimo, backend y flags de captura.
- El staging `.snap` propiedad de PerfMeter está bajo `Temp/PerfMeter/MemorySnapshots` y se limita a 512 MiB. La evidencia solo de memoria se exporta bajo `Temp/PerfMeter/CaptureBundles`, con una cuota total de retención de 2 GiB. Un export correcto es de un solo uso y elimina el source de staging, con warnings explícitos si la limpieza no puede completarse.
- Los snapshots pueden contener memoria sensible del proceso. Protégelos y revísalos antes de compartirlos. El bundle registra `contains_sensitive_memory`, provenance de backend/flags, `memory-snapshot.json` y metadatos SHA-256; no crea un artefacto GPU externo.
- El borrado bloqueado por el sistema operativo y la protección portable managed frente a carreras con reparse points son best-effort. Los paths inseguros o ajenos se rechazan y los fallos de limpieza se mantienen visibles como warnings.
- La evidencia incluye memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, compilación opcional con `com.unity.memoryprofiler@1.1.12` y Unity `6000.4.12f1` full EditMode `182/182` más full PlayMode `14/14`. No es una afirmación sobre release-player ni comportamiento en dispositivos.

## Límites del diagnóstico gráfico y GraphicsStateCollection

- Los markers de creación de programas GPU de shaders y de graphics pipelines son capabilities dinámicas de `ProfilerRecorder`. Unity, la plataforma, la API gráfica y el estado del refresh del catálogo pueden cambiar su availability. Usa `Unavailable`, `AvailableNoSample`, `AvailableSampled` y la provenance; no deduzcas availability de un valor cero.
- Los valores de markers conservan `Unit` y `DataType` del recorder y son valores crudos. No son universalmente counts de shaders o PSO y PerfMeter no los convierte a una unidad común. La metadata de capability incluye resolución exact/alias, nombres de recorders resueltos, component counts resueltos/muestreados y revisión del catálogo.
- La assembly opcional `SGG.PerfMeter.GraphicsStateCollection` está dirigida a Unity `6000.4+`. Usa `UnityEngine.Experimental.Rendering.GraphicsStateCollection` en `6000.4` y `UnityEngine.Rendering.GraphicsStateCollection` en `6000.5+`; las versiones anteriores no están soportadas para esta integración.
- Un trace requiere una sesión activa de PerfMeter. En Play Mode normal los trace frames terminan después del end-of-frame y en batch mode se usa un fallback del frame siguiente. Los samples correlacionados están sujetos al warm-up, intervalo y máximo de samples de la sesión.
- Solo se admite un graphics-state flight, incluyendo preparación, finalización del trace, prewarm y cleanup. Un external GPU capture, memory snapshot o alert-capture activo también provoca rechazo por overlap. `IsBusy`/`is_busy` cubre esos flights y el cleanup persistente; `HasPendingCleanup`/`has_pending_cleanup` informa específicamente de un artifact owned que espera retry. La cancelación coincidente es best-effort; los fallos de cleanup permanecen visibles y pueden retrasar la siguiente solicitud.
- `StopSession()` cancela un trace activo, por lo que se requiere una sesión activa durante todo el trace. Un borrado fallido del artifact owned crea un sidecar adyacente `.delete-pending`; se restaura y se reintenta después de un domain reload. El warning y el estado busy permanecen visibles hasta eliminar el artifact y el marker.
- Prewarm acepta únicamente un artifact owned relativo al proyecto, se ejecuta de forma síncrona, conserva el artifact y puede informar de un progressive warmup incompleto. El backend de Unity no admite cache-miss tracing: la solicitud devuelve `Unavailable` y no se expone evidencia de cache-miss.
- Los artifacts `.graphicsstate` owned se guardan bajo `Temp/PerfMeter/GraphicsStateCollections`, deben ser archivos regulares no vacíos y tienen un límite de 64 MiB. El trace está limitado a 600 frames y el prewarm progresivo a 1.000.000 de states. Se aplican guards de espacio libre mínimo y de paths locales al proyecto.
- La evidencia final es: compile de Unity `6000.4.12f1` aprobado; GSC EditMode targeted `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208` y full post-fix PlayMode `16/16`. También aprobó un compile aislado del optional consumer en Unity `6000.5.6f1`. Los tests full de Unity `6000.5`, el comportamiento de release-player y de dispositivos siguen siendo release gates y no se afirman aquí.

## Límites del contexto de integración de render

- `PerfMeterRenderIntegrationSnapshot` es un contrato de observación neutral respecto a la integración, no un capture profundo de Render Graph o Custom Pass. Las lecturas no inician el runtime; antes de la primera observation el pipeline actual soportado puede estar `Available` con `NotObserved`, y un cambio de pipeline/configuration marca la observación anterior como stale mediante `ObservationMatchesCurrentPipeline: false`, frame/age explícitos y warning.
- URP usa el `UniversalRenderingData.renderingMode` público del frame actual e informa de los passes de PerfMeter realmente programados. HDRP informa del `CustomPass` real de PerfMeter, pero el effective rendering mode sigue unavailable.
- Se eliminó la reflection privada/interna de passes y recursos de Render Graph. La facade legacy mantiene `registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count` y `aliased_resource_count` en `-1` porque no existe una API pública estable para ellos.
- La actividad GRD usa el resultado público de `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` y representa el estado global del runtime, no prueba el uso de GRD por una cámara o renderer concreto. Forward+ en URP es una observación del frame actual; en HDRP la availability del rendering mode/Forward+ sigue `Unknown`.
- La effectiveness de GRD usa contadores agregados BRG de draw calls/instancias con provenance exacta. Pueden incluir otros usuarios de `BatchRendererGroup`, por lo que no demuestran participación GRD por renderer. Los valores no disponibles o aún sin sample se serializan como `null`.
- VRS informa del soporte de hardware autoritativo de `SystemInfo`/`ShadingRateInfo`. Configuration y activity permanecen `Unknown` salvo que un futuro typed adapter las pruebe; no se afirma actividad de VRS.
- Unity no expone un viewer público estable de RenderGraph/CustomPass ni una API de pass targets. Por ello PerfMeter no añade navegación del Editor ni la promete.
- El schema de contexto de captura v1 conserva `render` y añade `render_integration`; los schemas JSON/CSV de sesión no cambian. El contexto de un capture externo se congela en el primer sample de `Capturing`, no se reemplaza con lecturas posteriores.
- Evidencia final de PM-REN-001: main compile de Unity `6000.4.12f1` aprobado; `PerformanceMeterApiTests` targeted `53/53`, `PerfMeterCaptureBundleTests` `15/15` y `PerformanceMeterPlayModeSmokeTests` `12/12`; full EditMode final `215/215` y full PlayMode `16/16` aprobados. Focused review P1/P2 resolved. La compile matrix aislada pasó en Unity `6000.4.12f1` URP `17.4` y HDRP `17.4`, y Unity `6000.5.6f1` URP `17.5` y HDRP `17.5`. La validación de release-player/dispositivos sigue pending; no se afirma ningún release.
- Evidencia final de PM-GRD-001: compile de Unity `6000.4.12f1` aprobado; API targeted `58/58`, capture-bundle `15/15` y PlayMode smoke `12/12`; full EditMode `220/220` y PlayMode `16/16` aprobados. Focused review P1/P2 resolved; aprobó la compile matrix Unity `6000.4`/`6000.5` con URP `17.4`/`17.5` y HDRP `17.4`/`17.5`. El comportamiento release-player/dispositivos sigue pending.
