# Runtime API

Namespace:

```csharp
using SGG.PerfMeter;
```

All read APIs are safe before the runtime starts. Reads return stopped/default snapshots instead of throwing because the runtime is not active.

## Lifecycle

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Collection modes:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Status And Metrics

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Key metric groups:

- FPS: average, 1% low, 0.1% low, spike counts.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame when available.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: system/app memory, GC reserved memory, GPU memory when available.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced, or unknown.
- Overdraw: state, progress, ratio, and heatmap visibility.

Counter availability is exposed through `AvailableCounters`, `UnavailableCounters`, and warnings.

## Self-Observability And Overhead Budgets

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability reports low-overhead CPU callback measurements in fixed 120-frame windows. Averages are per invocation. Overall state is `NotInitialized`, `Collecting`, or `Ready`; component state is `NotMeasured`, `Collecting`, `Ready`, or `Unsupported`.

Components are `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration`, and `HdrpRenderIntegration`. Each component exposes window and invocation counts, average/maximum CPU milliseconds, total/average allocated bytes, configured budgets, and `NotEvaluated`/`WithinBudget`/`Exceeded` budget states.

| Component | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU self-timing is explicitly `Unavailable`. These diagnostics do not subtract from or adjust existing CPU/GPU metrics.

## Dynamic Profiler Metric Catalog

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` and `GetProfilerMetricCapabilities()` read the cached catalog. The catalog state is `NotInitialized`, `Ready`, or `Error`; each capability reports `Unavailable`, `AvailableNoSample`, or `AvailableSampled`, with `None`, `Exact`, or `Alias` resolution provenance. Discovery runs only at runtime startup and explicit refresh/reconfigure, not during steady-state collection. Existing numeric metrics remain compatibility values; use capability `SampleState`/`IsAvailable` as the authoritative availability signal.

## Structured Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshots include Unity/platform/OS/CPU/GPU/API/display/window/support information. Camera snapshots include scene, transform, projection, clipping, pixel rect, target display, and URP/HDRP camera settings when available.

## CPU Core Loads

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Each snapshot exposes `CoreIndex`, `LoadPercent`, and `Available`. The array can be empty before runtime startup, during sampler warm-up, or on unsupported platforms; treat that as platform capability information, not as a failed API call.

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

Legacy overlay modes and semantic module flags remain available for compatibility and filtering.

## Sessions

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

Session options include warm-up frames/seconds, sample interval, maximum samples, reset-on-scene-load, and scene-load ignore windows.

## Alerts

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` is `true` by default and controls only the structured alert `Debug.Log` output. Setting it to `false` does not disable `AlertFired` callbacks, latest alerts or alert history, overlay warnings, Editor warning logs, or sessions. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` controls Editor warning logs independently.

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

The coordinator allows one active request and advances deterministically through `PreRoll`, `Capturing`, `PostRoll`, and `Completed`. Repeating an active ID is idempotent; a different active ID is rejected as overlap. `Canceled`, `Unavailable`, and `Error` are explicit terminal states.

The built-in backend wraps Unity's experimental `ExternalGPUProfiler` only in the Editor or a Development Build, only when an external tool is attached, and only for supported desktop platform/API combinations. Select `RenderDoc` or `Pix` explicitly because Unity does not expose the attached tool identity; `Status.Tool` is the requested tool, not verified attached-tool identity. `Completed` confirms only the Unity wrapper lifecycle; it does not verify or return an external `.rdc`/`.wpix` artifact. Capture bundles, artifact provenance, and MCP capture control are separate future scope.

`PerfMeterCaptureOptions` defaults to one capture frame with no pre-roll or post-roll. `RequestCapture` starts the runtime when the request is valid. `CancelCapture()` without an ID cancels the currently reported active request; passing an ID protects against canceling a newer request.

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Provider exceptions are reported as unavailable custom metric snapshots and do not interrupt core metric collection.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw diagnostics are explicit diagnostic modes and can add GPU work. In HDRP these APIs safely report unsupported state for overdraw and heatmap instead of promising HDRP heatmap output.
