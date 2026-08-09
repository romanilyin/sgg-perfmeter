# API Runtime

Namespace:

```csharp
using SGG.PerfMeter;
```

Todas las APIs de lectura son seguras antes de que arranque el runtime. Las lecturas devuelven snapshots detenidos/predeterminados en vez de lanzar excepciones porque el runtime no está activo.

## Ciclo De Vida

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Modos de recolección:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Estado Y Métricas

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Grupos clave de métricas:

- FPS: average, 1% low, 0.1% low, recuentos de spikes.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame cuando está disponible.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: system/app memory, GC reserved memory, GPU memory cuando está disponible.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced o unknown.
- Overdraw: estado, progreso, ratio y visibilidad de heatmap.

La disponibilidad de contadores se expone mediante `AvailableCounters`, `UnavailableCounters` y warnings.

## Self-Observability Y Budgets De Overhead

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability informa mediciones low-overhead del coste de callbacks CPU en ventanas fijas de 120 frames. Los promedios son por invocacion. El estado general es `NotInitialized`, `Collecting` o `Ready`; el estado de componente es `NotMeasured`, `Collecting`, `Ready` o `Unsupported`.

Los componentes son `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` y `HdrpRenderIntegration`. Cada uno expone recuentos de frames e invocaciones, milisegundos CPU medios/maximos, bytes asignados totales/medios, budgets y estados `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Componente | Budget CPU | Budget de asignacion |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

El self-timing de GPU es explicitamente `Unavailable`. Estos diagnostics no restan ni ajustan las metricas CPU/GPU existentes.

## Catálogo dinámico de métricas del Profiler

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` y `GetProfilerMetricCapabilities()` leen el catálogo en caché. El estado del catálogo es `NotInitialized`, `Ready` o `Error`; cada capability informa `Unavailable`, `AvailableNoSample` o `AvailableSampled`, y `Resolution` indica la procedencia `None`, `Exact` o `Alias`. El discovery solo se ejecuta durante el arranque del runtime y en refresh/reconfigure explícitos, no durante la recolección steady-state. Los valores numéricos existentes siguen siendo valores de compatibilidad; usa `SampleState`/`IsAvailable` de la capability como señal autoritativa de disponibilidad.

## Snapshots Estructurados

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Los snapshots de device incluyen información de Unity/platform/OS/CPU/GPU/API/display/window/support. Los snapshots de camera incluyen scene, transform, projection, clipping, pixel rect, target display y URP/HDRP camera settings cuando están disponibles.

## Cargas De CPU Cores

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Cada snapshot expone `CoreIndex`, `LoadPercent` y `Available`. El array puede estar vacío antes del arranque runtime, durante el warm-up del sampler o en plataformas no compatibles; trátalo como información de capacidad de la plataforma, no como una llamada API fallida.

## Overlay

```csharp
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.ClassicDark);
PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.Manrope);
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Los modos de overlay legacy y las flags semánticas de módulos siguen disponibles para compatibilidad y filtrado.

## Sesiones

```csharp
PerformanceMeter.StartSession();
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));
PerformanceMeter.StopSession();
PerformanceMeter.ResetStats();

PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerfMeterSessionSampleSnapshot[] samples = PerformanceMeter.GetSessionSamples();

PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Las opciones de sesión incluyen frames/segundos de warm-up, intervalo de sample, samples máximos, reset-on-scene-load y ventanas de ignorar carga de escena.

## Alertas

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` es `true` de forma predeterminada y controla únicamente la salida `Debug.Log` de alertas estructuradas. El valor `false` no desactiva los callbacks `AlertFired`, las alertas recientes ni el historial de alertas, los warnings del overlay, los logs de warnings del Editor ni las sesiones. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` controla los logs de warnings del Editor de forma independiente.

## Editor Compatibility Status

La API de Editor `PerfMeterSetupActions.GetCompatibilityStatus()` devuelve `PerfMeterCompatibilityStatus` y separa `ImportCompatible` para el floor Unity `2022.3`, `CoreRuntimeCompatible` para runtime compatible Unity `6000.4+` y `RenderIntegrationCompatible` para URP/HDRP activo `17.4+` con adapter disponible. Cada resultado incluye una razón. La compatibilidad de render no implica que los renderer assets estén configurados; usa setup status para configuration readiness.

## External GPU Capture Coordinator

```csharp
PerfMeterCaptureOptions options = new PerfMeterCaptureOptions(
    "renderdoc-spike-01",
    PerfMeterCaptureTool.RenderDoc,
    captureFrames: 1,
    preRollFrames: 30,
    postRollFrames: 30);

PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(options);
PerfMeterCaptureStatusSnapshot capture = PerformanceMeter.GetCaptureStatus();
if (capture.IsActive && userRequestedCancellation)
{
    PerformanceMeter.CancelCapture(capture.CaptureId);
}
```

El coordinator permite una sola solicitud activa y avanza de forma determinista por `PreRoll`, `Capturing`, `PostRoll` y `Completed`. Repetir la misma ID activa es idempotente; una ID activa diferente se rechaza por solapamiento. `Canceled`, `Unavailable` y `Error` son estados terminales explícitos.

El backend integrado envuelve el `ExternalGPUProfiler` experimental de Unity solo en el Editor o en un Development Build, solo cuando hay una herramienta externa conectada y solo para combinaciones compatibles de plataforma/API de escritorio. Las combinaciones compatibles son `RenderDoc` en escritorio Windows/Linux con Direct3D 11, Direct3D 12 o Vulkan, y `PIX` en escritorio Windows con Direct3D 12. Selecciona `RenderDoc` o `Pix` explícitamente porque Unity no expone la identidad de la herramienta conectada. `Status.Tool` es únicamente la herramienta solicitada, no la identidad verificada de la herramienta conectada. `Completed` confirma únicamente el wrapper lifecycle de Unity; no verifica ni devuelve un artefacto externo `.rdc`/`.wpix` ni su path. Los tests automatizados usan un fake backend; la confirmación con la herramienta externa real y el artefacto sigue siendo un release gate.

Los valores predeterminados de `PerfMeterCaptureOptions` son `captureFrames: 1`, `preRollFrames: 0` y `postRollFrames: 0`. Un `RequestCapture` válido inicia el runtime automáticamente. `CancelCapture()` sin ID cancela la solicitud activa que se muestra actualmente; pasar una ID protege contra cancelar una solicitud más nueva.

El overload con `PerfMeterCaptureBundleOptions` separa los capture samples de la baseline session y puede incluir un screenshot opt-in. Cuando `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady`, `PerformanceMeter.ExportCaptureBundle(captureId)` crea atómicamente un bundle versionado bajo `Temp/PerfMeter/CaptureBundles` con manifest SHA-256, samples, alerts, contexto, screenshot opcional y metadata del artefacto externo. Un `.rdc`/`.wpix` local al proyecto solo es un artefacto observado, nunca autoritativo; se rechazan traversal, reparse points y archivos fuera del proyecto.

La exportación del capture bundle también dispone de una API single-flight no bloqueante: `RequestCaptureBundleExport(..., out exportId)`, `GetCaptureBundleExportStatus(exportId)` y `CancelCaptureBundleExport(exportId)`. El estado informa de fase, progreso, bytes, cancelación, reintentos, ruta de commit y el envelope genérico de artefactos externos. La API existente `ExportCaptureBundle(...)` sigue siendo un wrapper de compatibilidad bloqueante, mientras que serialización, E/S de archivos, hashing, retención y commit atómico se ejecutan en un worker thread.

Los JSON de sesión y capture añaden eventos tipados de timeline para samples ausentes y límites de captura. Las versiones de schema, arrays de samples y columnas CSV existentes siguen siendo compatibles; los payloads legacy o desconocidos se leen sin inventar gaps. Los providers de custom metrics usan un provider snapshot cacheado y un buffer reutilizable propiedad del core en el warmed collection path; solo se crean copias para samples retenidos, exports y public snapshots. La coordinación del Profiler es local al proceso mediante `GetProfilerLeaseCapabilities()`, `GetProfilerLeaseStatus()`, `TryAcquireProfilerLease(...)` y `ReleaseProfilerLease(...)`; las leases activas no sobreviven a un domain reload.

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Las excepciones de providers se informan como snapshots de custom metric no disponibles y no interrumpen la recolección de métricas core.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Los diagnósticos de overdraw son modos de diagnóstico explícitos y pueden añadir trabajo de GPU. En HDRP estas APIs informan de forma segura unsupported state para overdraw y heatmap, sin prometer HDRP heatmap output.

## Snapshots de memoria opcionales

Los snapshots de memoria son una integración opcional. En Unity `6000.4+`, `com.unity.memoryprofiler` `1.1.0+` habilita la assembly separada `SGG.PerfMeter.MemoryProfiler`, que registra automáticamente el backend `MemoryProfiler`. La assembly core no tiene una dependencia obligatoria.

```csharp
PerfMeterMemorySnapshotCapabilitiesSnapshot capabilities =
    PerformanceMeter.GetMemorySnapshotCapabilities();

if (capabilities.Availability == PerfMeterAvailability.Available)
{
    PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
        new PerfMeterMemorySnapshotOptions("memory-spike-01"));
}

PerfMeterMemorySnapshotStatusSnapshot status = PerformanceMeter.GetMemorySnapshotStatus();
if (status.State == PerfMeterMemorySnapshotState.Completed &&
    PerformanceMeter.GetCaptureBundleStatus(status.CaptureId).IsExportReady)
{
    PerformanceMeter.ExportCaptureBundle(status.CaptureId);
}
```

La superficie pública incluye `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` y `GetMemorySnapshotTriggers()`. Un backend personalizado implementa `IPerfMeterMemorySnapshotBackend`; la assembly opcional proporciona el backend de Unity Memory Profiler.

`PerfMeterMemorySnapshotOptions` usa por defecto flags de objetos managed/native, 1 GiB de espacio libre mínimo y un cooldown de 300 segundos. `RequestMemorySnapshot` es manual por defecto y devuelve resultados explícitos como `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest` o `Failed`. Las lecturas no inician el runtime; una solicitud válida sí lo hace.

`ConfigureMemorySnapshotTriggers` habilita de forma opt-in la heurística de umbral de memoria del sistema y de crecimiento acotado de fugas. `GetMemorySnapshotTriggers()` está deshabilitado por defecto. Las solicitudes activadas por triggers usan los mismos guards de single-flight, cooldown, espacio libre y flags de captura que las solicitudes manuales.

## Diagnóstico de gráficos y GraphicsStateCollection

El diagnóstico de gráficos añade información a los snapshots existentes. `PerformanceMeter.GetGraphicsDiagnostics()` devuelve los últimos valores de los markers de creación de programas GPU de shaders y de graphics pipelines, junto con el contexto de la API gráfica, la capacidad de PSO paralelo y la revisión del catálogo de métricas del profiler.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

El catálogo descubre los descriptores de `ProfilerRecorder` de Unity al iniciar el runtime y durante un refresh/reconfigure explícito. Para el shader usa el nombre exacto `Shader.CreateGPUProgram` y los alias `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` y `Shader.DynamicLoadGPUProgram`. Para el graphics pipeline usa el nombre exacto `CreatePSO.Job`. Cada capability conserva `Resolution` (`None`, `Exact` o `Alias`), `ResolvedRecorderNames`, `Category`, los valores descubiertos `Unit` y `DataType`, y `ResolvedComponentCount` y `SampledComponentCount`. `PerfMeterMetricsSnapshot` y el JSON/CSV de sesión incluyen los mismos valores de markers, metadata de capability y revisión del catálogo.

La disponibilidad de los markers es dinámica. Usa `SampleState` (`Unavailable`, `AvailableNoSample` o `AvailableSampled`) y la metadata de capability; un valor cero no demuestra que falte el marker. Los valores son valores crudos del recorder y conservan la unidad descubierta: no son universalmente counts de shaders o PSO y PerfMeter no los convierte a una unidad común.

La assembly opcional `SGG.PerfMeter.GraphicsStateCollection` está limitada a Unity `6000.4+` y registra el backend de Unity cuando está disponible. En Unity `6000.4` usa `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, mientras que en Unity `6000.5+` usa `UnityEngine.Rendering.GraphicsStateCollection`. La assembly core permanece independiente de ese backend.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.25f, 240));

PerfMeterGraphicsStateCollectionRequestResult request =
    PerformanceMeter.RequestGraphicsStateTrace(
        new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", traceFrames: 60));

PerfMeterGraphicsStateCollectionStatusSnapshot status =
    PerformanceMeter.GetGraphicsStateCollectionStatus();
if (status.State == PerfMeterGraphicsStateCollectionState.Completed)
{
    PerformanceMeter.PrewarmGraphicsStateCollection(
        new PerfMeterGraphicsStatePrewarmOptions(status.ArtifactRelativePath));
}
```

La superficie pública de state collection es `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)` y `CancelGraphicsStateTrace(string captureId)`. Un backend personalizado implementa `IPerfMeterGraphicsStateCollectionBackend` e informa de sus capacidades de trace/prewarm, cache-miss y PSO paralelo.

`PerfMeterGraphicsStateTraceOptions` requiere un `CaptureId` no vacío, acepta 1–600 trace frames y usa por defecto 60 frames y 1 GiB de espacio libre mínimo. Un trace solo es válido mientras se graba una sesión de PerfMeter. Los samples de sesión correlacionados llevan el capture ID activo en `GraphicsStateTraceId` (`graphics_state_trace_id` en los exports). La configuración de sampling de la sesión controla la densidad de samples correlacionados, no el número solicitado de trace frames.

`PerfMeterGraphicsStateCollectionStatusSnapshot` expone `IsBusy` y `HasPendingCleanup`. `IsBusy` es true durante la preparación, el trace, el final del trace, el prewarm, el cleanup o un cleanup pendiente persistido; `HasPendingCleanup` identifica específicamente un artifact owned que espera un retry de cleanup. Si se llama a `PerformanceMeter.StopSession()` mientras hay un trace activo, el trace se cancela, por lo que la sesión debe seguir grabando hasta que termine. Si falla el borrado de un artifact owned, se crea un sidecar owned `.delete-pending` adyacente; después de un domain reload el marker se restaura y el cleanup se reintenta. El estado permanece visible y busy hasta limpiar el artifact y el marker.

El coordinator permite un solo graphics-state flight. El mismo ID activo devuelve `AlreadyActive`; otro trace o prewarm durante preparación, trace, finalización, cleanup o cualquier otro capture domain devuelve `RejectedOverlap`. `CancelGraphicsStateTrace` solo coincide con el ID activo o en preparación, cancela el backend y elimina el artifact owned pendiente. Los fallos de cleanup permanecen visibles y pueden bloquear un reemplazo hasta que se reintente la limpieza.

## Contexto de integración de render

El snapshot aditivo y neutral respecto a la integración está disponible mediante ambos métodos:

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` expone `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph` y `Warning`. Los snapshots anidados de GRD y VRS incluyen availability, campos de configuración/support, activity availability y warnings.

Las lecturas son seguras antes de iniciar el runtime y no comienzan la recolección. Un pipeline actual soportado puede ser `Available` con `State = NotObserved`; si la última observation pertenece a otra configuración del pipeline, `ObservationMatchesCurrentPipeline` es `false`, frame/age siguen explícitos y el warning identifica los datos stale. No trates esos campos stale como una observación actual.

URP usa el `UniversalRenderingData.renderingMode` público del frame actual e informa de los passes de PerfMeter realmente programados para ese frame. HDRP informa del `CustomPass` real de PerfMeter, pero el effective rendering mode no está disponible. `GpuResidentDrawer` informa del modo configurado, soporte de SRP/proyecto/compute, Forward+ y compatibilidad del modo clustered del frame actual en URP, y actividad global del runtime mediante `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()`. En HDRP, los campos de Forward+/rendering mode siguen `Unknown`. `VariableRateShading` informa del soporte de hardware autoritativo de `SystemInfo`/`ShadingRateInfo`; configuration y activity siguen en `Unknown` salvo que un typed adapter las demuestre.

`LegacyRenderGraph` es una facade de compatibilidad incluida para `GetRenderGraphSnapshot()`. Se eliminó la reflection privada/interna de passes y recursos, por lo que los legacy counters permanecen en `-1`. La API pública estable de Unity tampoco expone un viewer de RenderGraph/CustomPass ni pass targets; esta API no promete navegación en el Editor.

`GpuResidentDrawer` añade `ProjectConfigurationAvailability`, `IsProjectConfigurationSupported`, `ComputeShaderAvailability`, `SupportsComputeShaders`, `ForwardPlusActivityAvailability`, `IsObservedForwardPlusActive`, `RenderingModeCompatibilityAvailability`, `IsRenderingModeCompatible`, `ActivitySource`, `DegradedReason` y `Effectiveness`. `PerfMeterGpuResidentDrawerReason` ofrece estados de fallback estructurados. `PerfMeterGpuResidentDrawerEffectivenessSnapshot` incluye valores BRG de draw calls/instancias y provenance de las capabilities del Profiler; los valores sin sample son `-1` en C# y `null` en JSON. Son contadores agregados de BatchRendererGroup, no evidencia autoritativa de GRD por renderer.

`PerfMeterGraphicsStatePrewarmOptions` acepta únicamente un path `.graphicsstate` owned y relativo al proyecto, además de un `MaxStateCount` opcional entre 0 y 1.000.000. Prewarm es síncrono, conserva el artifact e informa de `CompletedWarmupCount` y `IsWarmedUp`; un progressive warmup correcto pero incompleto incluye un warning. `TraceCacheMisses` existe para backends extensibles, pero el backend de Unity no admite evidencia de cache-miss, por lo que esa solicitud devuelve `Unavailable`.

## Correlación De Sesiones

`PerformanceMeter.GetSessionSummary().SessionId` es un identificador hexadecimal de 32 caracteres en minúsculas. Se crea con `StartSession`, permanece estable después de `StopSession`, cambia al iniciar una sesión nueva y está vacío cuando no existe ninguna sesión. El JSON de sesión expone el mismo valor como `session_id` en la raíz; CSV lo añade como última columna `session_id` para conservar las posiciones existentes; `perfmeter.session.summary` lo devuelve como `session_id`.
