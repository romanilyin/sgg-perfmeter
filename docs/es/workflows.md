# Flujos De Trabajo

## Overlay Runtime

Usa el overlay cuando necesites visibilidad inmediata dentro del juego.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

El overlay usa UI Toolkit y no intercepta la entrada del juego. Soporta FPS-only, texto compacto, gráficos, diagnósticos completos, barras de métricas, temas visuales, filtros de módulos, gráficos CPU/GPU, widgets de CPU cores y filas limitadas de custom metrics.

PerfMeter crea y posee un host versionado de UI Toolkit para el overlay: Unity `6000.4` usa `UIDocument`, mientras que Unity `6000.5+` usa `PanelRenderer`. El host propio está separado de la UI ajena y conserva sus panel settings y children; los rebuilds eliminan únicamente el container propio de PerfMeter.

## Recolección En Background

Usa el modo background para tests, ejecuciones en dispositivos o flujos de agentes donde no se necesita UI visible.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Grabación Y Exportación De Sesiones

Usa sesiones para ventanas de profiling repetibles.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Las exportaciones de sesión incluyen timing, FPS lows, spikes, recuentos de cuellos de botella, contadores de render, contadores de memoria, estado de overdraw, disponibilidad de warnings/counters, resúmenes de escenas, peores frames, metadatos de device, metadatos de camera, metadatos de settings y custom metrics.

## Alertas

Las reglas pueden informar violaciones de budget, FPS bajos, GPU timing no disponible y umbrales de overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Las advertencias del Editor se limitan con cooldowns y pueden desactivarse mediante configuración JSON o controles runtime. Los logs de alertas estructuradas y las advertencias del Editor son independientes: `PerformanceMeter.SetStructuredLogsEnabled(false)` suprime únicamente la salida `Debug.Log` de alertas estructuradas, mientras `PerformanceMeter.SetEditorWarningLogsEnabled(false)` controla por separado los logs de advertencia del Editor. Los callbacks, alerts/history, warnings del overlay y sessions siguen activos.

## External GPU Capture

Usa el capture coordinator para una solicitud acotada de RenderDoc o PIX cuando la herramienta ya está conectada:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

El coordinator admite una sola solicitud activa y avanza de forma determinista por `PreRoll`, `Capturing`, `PostRoll` y `Completed`. La misma ID activa es idempotente; una ID diferente se rechaza como solapamiento. El pre-roll y el post-roll cuentan frames de Unity; solo `Capturing` abre el alert capture scope e invoca el `ExternalGPUProfiler` experimental de Unity. Los gates obligatorios son Editor o Development Build y una herramienta conectada. `RenderDoc` está permitido en escritorio Windows/Linux con Direct3D 11, Direct3D 12 o Vulkan; `PIX` está permitido en escritorio Windows con Direct3D 12.

`Completed` significa que terminó únicamente el wrapper lifecycle protegido. Unity no expone la identidad de la herramienta conectada ni un path de artefacto autoritativo; `Status.Tool` es solo la herramienta solicitada, no una identidad verificada. El overload con `PerfMeterCaptureBundleOptions` separa samples baseline/capture y exporta atómicamente un bundle local al proyecto; un artefacto externo solo queda observado, no autoritativo. Para automatización usa `perfmeter.capture.request/status/cancel/export/capabilities`.

## Diagnósticos De Overdraw

El overdraw numérico es opt-in y acotado.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

El overdraw numérico y la heatmap usan el diagnostic path de URP Render Graph. La medición de overdraw requiere `PerfMeterRenderGraphFeature`, soporte de replacement shader, soporte de fragment UAV/storage-buffer, soporte de compute shader, una graphics API compatible y async GPU readback. HDRP informa overdraw/heatmap como unsupported, mientras core overlay, session, API y MCP diagnostics siguen disponibles. Los targets no compatibles informan `OverdrawState.Unsupported` en vez de ejecutar el pass.

## Reproducibilidad De Camera Y Device

Usa snapshots para conservar el entorno que produjo una captura de rendimiento.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Las exportaciones de sesión incluyen metadatos de device y camera para entender o reproducir una captura más tarde.

## Custom Metrics

Registra providers específicos del proyecto sin hacer fork de PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Las custom metrics se exponen mediante lecturas de API, exportación JSON de sesiones, métricas latest de MCP y hasta ocho filas de overlay cuando el módulo `CustomMetrics` está activado.

## Instrumentación de Unity Profiler

La instrumentación es interna y solo es visible al perfilar el Editor, un Development Build u otro build con Profiler habilitado. En los Release players sin Profiler, estos markers/counters son no-op y no generan datos de instrumentación; los schemas de public API, status, MCP y export no cambian.

- Los markers cubren collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) y export JSON/CSV (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` es un hook interno reservado para providers.
- Los counters cubren tiempos de frame CPU/GPU (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) como gauges de fin de frame en nanosegundos. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` y `SGG.PerfMeter.Thermal.Available` codifican disponibilidad/activo como `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` y `SGG.PerfMeter.Capture.State` usan códigos de enum; `SGG.PerfMeter.Provider.CustomMetricCount` es un recuento. Los counters usan la categoría `Scripts` y `FlushOnEndOfFrame`.
- No se emite ningún sample térmico sintético; `SGG.PerfMeter.Thermal.Available` permanece en `0`/unavailable hasta que un provider de plataforma real proporcione datos.

## Self-Observability Y Budgets De Overhead

Usa `PerformanceMeter.GetSelfOverhead()` o `PerformanceMeter.GetStatus().SelfOverhead` para diagnosticar coste de callbacks CPU y asignaciones de collector, custom providers, CPU-core provider, overlay e integracion URP/HDRP. La medicion usa ventanas fijas de 120 frames, promedios por invocacion y budgets CPU/asignacion especificos por componente.

La render integration inactiva informa `Unsupported`, un componente compatible sin llamadas informa `NotMeasured` y el self-timing GPU informa `Unavailable`. El accounting es solo diagnostico: PerfMeter no resta overhead ni ajusta las metricas CPU/GPU existentes.

## Automatización Con Agentes

Una ejecución típica dirigida por MCP:

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

`perfmeter.profiler.capabilities {}` es una lectura en caché; no inicia el runtime ni realiza discovery.

## Workflow de snapshots de memoria opcionales

1. Usa Unity `6000.4+` e instala `com.unity.memoryprofiler` `1.1.0+` mediante Package Manager. La assembly opcional `SGG.PerfMeter.MemoryProfiler` registra entonces el backend automáticamente; sin ese paquete la integración core permanece unavailable.
2. En Play Mode, lee `PerformanceMeter.GetMemorySnapshotCapabilities()` o `perfmeter.memory.snapshot.capabilities` y confirma la disponibilidad del backend y de los flags solicitados.
3. Solicita un snapshot manual con `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))`, o configura `ConfigureMemorySnapshotTriggers(...)` para activar explícitamente un umbral de memoria del sistema o una ventana acotada de crecimiento de fugas.
4. Consulta `GetMemorySnapshotStatus()` o `perfmeter.memory.snapshot.status` hasta que el snapshot y su bundle correlacionado lleguen a un estado terminal. Exporta la evidencia lista con `PerformanceMeter.ExportCaptureBundle(captureId)` o `perfmeter.capture.export`.

La evidencia solo de memoria se escribe mediante la API existente de capture bundles bajo `Temp/PerfMeter/CaptureBundles`. El bundle registra `MemoryProfiler` como herramienta solicitada, incluye provenance de memoria y un SHA-256 en streaming para el `.snap`, y no incluye un artefacto GPU externo. El source propiedad de PerfMeter está bajo `Temp/PerfMeter/MemorySnapshots`; un export correcto lo consume una sola vez.
