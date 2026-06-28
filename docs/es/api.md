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
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

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
