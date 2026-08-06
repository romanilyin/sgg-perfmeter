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

## Editor Compatibility Status

```csharp
using SGG.PerfMeter.Editor.Setup;

PerfMeterCompatibilityStatus compatibility = PerfMeterSetupActions.GetCompatibilityStatus();
bool canImport = compatibility.ImportCompatible;
bool canRunCore = compatibility.CoreRuntimeCompatible;
bool canUseRenderIntegration = compatibility.RenderIntegrationCompatible;
```

This Editor-only snapshot keeps the package import floor (`2022.3`), supported core runtime floor (`6000.4`), and active URP/HDRP render integration (`17.4+` plus the corresponding adapter) separate. Each field has an explicit reason. Render compatibility is capability, not renderer/configuration readiness; use setup status for installation state.

## External GPU Capture Coordinator

```csharp
PerfMeterCaptureOptions options = new PerfMeterCaptureOptions(
    "renderdoc-spike-01",
    PerfMeterCaptureTool.RenderDoc,
    captureFrames: 1,
    preRollFrames: 30,
    postRollFrames: 30);

PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    options,
    new PerfMeterCaptureBundleOptions(includeScreenshot: true));
PerfMeterCaptureStatusSnapshot capture = PerformanceMeter.GetCaptureStatus();
if (capture.IsActive && userRequestedCancellation)
{
    PerformanceMeter.CancelCapture(capture.CaptureId);
}
```

The coordinator allows one active request and advances deterministically through `PreRoll`, `Capturing`, `PostRoll`, and `Completed`. Repeating an active ID is idempotent; a different active ID is rejected as overlap. `Canceled`, `Unavailable`, and `Error` are explicit terminal states.

The built-in backend wraps Unity's experimental `ExternalGPUProfiler` only in the Editor or a Development Build, only when an external tool is attached, and only for supported desktop platform/API combinations. Select `RenderDoc` or `Pix` explicitly because Unity does not expose the attached tool identity; `Status.Tool` is the requested tool, not verified attached-tool identity. `Completed` confirms only the Unity wrapper lifecycle; it does not verify or return an external `.rdc`/`.wpix` artifact.

`PerfMeterCaptureOptions` defaults to one capture frame with no pre-roll or post-roll. `RequestCapture` starts the runtime when the request is valid. `CancelCapture()` without an ID cancels the currently reported active request; passing an ID protects against canceling a newer request.

The bundle overload keeps capture samples separate from baseline session evidence and can include an opt-in runtime screenshot. Once `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady` is true, call `PerformanceMeter.ExportCaptureBundle(captureId)`. Export creates an atomic versioned directory under `Temp/PerfMeter/CaptureBundles` with manifest hashes, session/baseline/capture samples, capture alerts, context, optional screenshot, and external-artifact metadata.

A caller-supplied project-local `.rdc` or `.wpix` path can be copied and hashed as an observed artifact, but Unity cannot authenticate its tool identity or association. It is never marked authoritative; `requireAuthoritativeExternalArtifact: true` fails explicitly. Absolute paths, traversal, reparse points, oversized data, and external files outside the project are rejected. Use `PerformanceMeter.GetCaptureCapabilities()` to inspect current schema, quota, retention, and screenshot limits.

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

## Optional Memory Snapshots

Memory snapshots are an optional integration. On Unity `6000.4+`, `com.unity.memoryprofiler` `1.1.0+` enables the separate `SGG.PerfMeter.MemoryProfiler` assembly, which auto-registers the `MemoryProfiler` backend. The core assembly has no hard dependency.

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

The public surface is `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)`, and `GetMemorySnapshotTriggers()`. A custom backend implements `IPerfMeterMemorySnapshotBackend`; the optional assembly supplies the Unity Memory Profiler backend.

`PerfMeterMemorySnapshotOptions` defaults to managed/native object flags, 1 GiB minimum free disk, and a 300-second cooldown. `RequestMemorySnapshot` is manual by default and returns explicit results such as `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest`, or `Failed`. Reads do not start the runtime; a valid request does.

`ConfigureMemorySnapshotTriggers` enables the opt-in system-memory threshold and bounded leak-growth heuristic. `GetMemorySnapshotTriggers()` is disabled by default. Triggered requests use the same single-flight, cooldown, free-space, and capture-flag guards as manual requests.

## Graphics Diagnostics And State Collections

Graphics diagnostics are additive. `PerformanceMeter.GetGraphicsDiagnostics()` returns the latest shader GPU-program and graphics-pipeline creation marker values together with graphics API context, parallel-PSO capability, and the profiler catalog revision.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

The catalog discovers Unity `ProfilerRecorder` descriptors at runtime startup and on explicit refresh/reconfigure. The shader semantic uses exact name `Shader.CreateGPUProgram` and aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram`, and `Shader.DynamicLoadGPUProgram`. The graphics-pipeline semantic uses exact name `CreatePSO.Job`. Each capability preserves `Resolution` (`None`, `Exact`, or `Alias`), `ResolvedRecorderNames`, `Category`, discovered `Unit`, `DataType`, `ResolvedComponentCount`, and `SampledComponentCount`. `PerfMeterMetricsSnapshot` and session JSON/CSV carry the same marker values, capability metadata, and catalog revision.

Marker availability is dynamic. Use `SampleState` (`Unavailable`, `AvailableNoSample`, or `AvailableSampled`) and the capability metadata instead of treating a zero value as proof that a marker is absent. Values are raw recorder values and retain the discovered unit; they are not universally shader or PSO counts and are not converted to a common unit.

The optional `SGG.PerfMeter.GraphicsStateCollection` assembly is constrained to Unity `6000.4+` and registers the Unity backend when available. It uses `UnityEngine.Experimental.Rendering.GraphicsStateCollection` on Unity `6000.4` and `UnityEngine.Rendering.GraphicsStateCollection` on Unity `6000.5+`. The core assembly remains independent of that backend.

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

The public state-collection surface is `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)`, and `CancelGraphicsStateTrace(string captureId)`. A custom backend implements `IPerfMeterGraphicsStateCollectionBackend` and reports trace/prewarm, cache-miss, and parallel-PSO capabilities.

`PerfMeterGraphicsStateTraceOptions` requires a non-empty `CaptureId`, accepts 1–600 trace frames, and defaults to 60 frames and 1 GiB minimum free disk. A trace is valid only while a PerfMeter session is recording. Correlated session samples carry the active capture ID as `GraphicsStateTraceId` (`graphics_state_trace_id` in exports). Session sampling settings control the density of correlated samples; they do not change the requested trace-frame count.

`PerfMeterGraphicsStateCollectionStatusSnapshot` exposes `IsBusy` and `HasPendingCleanup`. `IsBusy` is true during preparation, tracing, trace ending, prewarm, cleanup, or a persisted pending cleanup; `HasPendingCleanup` specifically identifies an owned artifact waiting for cleanup retry. If `PerformanceMeter.StopSession()` is called while a trace is active, it cancels that trace, so the session must remain recording until trace completion. A failed owned-artifact deletion creates an adjacent owned `.delete-pending` sidecar marker; the marker is restored after domain reload and cleanup is retried. The status remains visible and busy until the artifact and marker are cleared.

The coordinator permits one graphics-state flight at a time. The same active ID returns `AlreadyActive`; another trace or a prewarm during preparation, tracing, ending, cleanup, or another capture domain returns `RejectedOverlap`. `CancelGraphicsStateTrace` only matches the active or preparing ID, cancels the backend, and removes the pending owned artifact. Cleanup failures stay visible and can block a replacement until cleanup is retried.

`PerfMeterGraphicsStatePrewarmOptions` accepts an owned project-relative `.graphicsstate` path and an optional `MaxStateCount` from 0 to 1,000,000. Prewarm is synchronous, preserves the artifact, and reports `CompletedWarmupCount` and `IsWarmedUp`; a successful but incomplete progressive warmup includes a warning. `TraceCacheMisses` is present for backend extensibility, but the Unity backend does not support cache-miss evidence, so requesting it returns `Unavailable`.
