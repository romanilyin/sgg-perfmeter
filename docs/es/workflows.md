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

Las advertencias del Editor se limitan con cooldowns y pueden desactivarse mediante configuración JSON o controles runtime.

## Diagnósticos De Overdraw

El overdraw numérico es opt-in y acotado.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

La medición de overdraw requiere `PerfMeterRenderGraphFeature`, soporte de replacement shader, soporte de fragment UAV/storage-buffer, soporte de compute shader, una graphics API compatible y async GPU readback. Los targets no compatibles informan `OverdrawState.Unsupported` en vez de ejecutar el pass.

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

## Automatización Con Agentes

Una ejecución típica dirigida por MCP:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
