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

Additive result-returning mutations are available when rejection, normalization, or unsupported behavior must not be silent:

```csharp
PerfMeterMutationResultSnapshot modeResult = PerformanceMeter.TrySetCollectionMode(PerfMeterCollectionMode.Background);
PerfMeterMutationResultSnapshot sessionResult = PerformanceMeter.TryStartSession(PerfMeterSessionOptions.Default);
PerfMeterMutationResultSnapshot overdrawResult = PerformanceMeter.TryRequestOverdrawMeasurement(60);
```

`Status` is `Applied`, `NoChange`, `Normalized`, `Rejected`, `Unavailable`, or `Unsupported`; `Reason`, `RequestedValue`, and `EffectiveValue` preserve the machine-readable outcome. Existing `void` lifecycle/session/overdraw methods remain compatibility wrappers. `TryApplyOverlayConfiguration(...)` provides the same contract for a complete overlay configuration.

Collection modes:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Status And Metrics

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDiagnosticsSnapshot diagnostics = PerformanceMeter.GetDiagnostics();

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

`metrics.Bottleneck` remains the instantaneous classification and raw timings remain unchanged. `diagnostics.StableBottleneck` is a separate hysteresis-based result with `Availability`, `Freshness`, `Provenance`, `Confidence`, `Coverage`, typed `Flags`, verification steps, evidence counts/age, and the unmodified latest collector warning. Insufficient, oscillating, or stale evidence publishes `Unknown` instead of a confident stable bottleneck.

## Self-Observability And Overhead Budgets

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
PerfMeterSelfOverheadWindowSnapshot sessionOverhead = PerformanceMeter.GetSelfOverheadWindow(
    PerfMeterSelfOverheadWindowKind.Session,
    PerformanceMeter.GetSessionSummary().SessionId);
```

Self-observability reports low-overhead CPU callback measurements in fixed 120-frame windows. Averages are per invocation. Overall state is `NotInitialized`, `Collecting`, or `Ready`; component state is `NotMeasured`, `Collecting`, `Ready`, or `Unsupported`.

Components are `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration`, and `HdrpRenderIntegration`. Each component exposes window and invocation counts, average/maximum CPU milliseconds, total/average allocated bytes, configured budgets, and `NotEvaluated`/`WithinBudget`/`Exceeded` budget states. Additive provenance includes an epoch, first/last measurement frame, callback-frame count, typed inactive reason, and explicit GPU attribution availability.

`GetSelfOverheadWindow(...)` returns the exact session- or capture-bound URP observation. It includes identity and epoch, capture and measurement frame bounds, containment, active quality/pipeline/renderer evidence, feature installation/enabled state, and enqueue evidence. Inactive results use a closed typed reason such as `RendererFeatureNotInstalled`, `RendererFeatureDisabled`, `PassNotEnqueued`, `NoCameraCallbackObserved`, `WindowIncomplete`, or `CaptureWindowMismatch`; missing evidence returns `UnknownInactiveReason` instead of guessing. A later capture cannot reuse a prior completed epoch.

| Component | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

The URP scope measures package-owned CPU-side `RecordRenderGraph()` registration and current-thread allocation. Its invocation count can exceed callback-frame count when multiple cameras run in one Unity frame. GPU self-timing is explicitly `Unavailable`; whole-frame CPU, GPU, hitch, and GC values remain context and are never attributed to PerfMeter from temporal proximity. These diagnostics do not subtract from or adjust existing CPU/GPU metrics.

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
PerfMeterPlatformTelemetrySnapshot platformTelemetry = PerformanceMeter.GetPlatformTelemetry();
```

Device snapshots include Unity/platform/OS/CPU/GPU/API/display/window/support information. Camera snapshots include scene, transform, projection, clipping, pixel rect, target display, and URP/HDRP camera settings when available.

Platform telemetry uses a core-owned bounded 0.25-second cadence rather than invoking the optional provider every frame. The snapshot reports `LastAttemptTimeSeconds`, `LastSuccessTimeSeconds`, `SampleAgeSeconds`, `Freshness`, `LastAttemptResult`, and whether the latest attempt was forced at a capture boundary. A failed forced attempt remains explicitly `Unavailable`; it is not replaced by an older available sample.

## Settings JSON And Explicit Bootstrap

```csharp
public static bool TryApplySettingsJson(string json, out string warning);
```

`TryApplySettingsJson` parses and normalizes a supported PerfMeter settings JSON string, then applies the accepted snapshot to the runtime. It returns `true` only after the snapshot is applied or its disabled/stopped state is satisfied; `warning` can still contain normalization warnings. Empty, invalid, newer-than-supported schema, or temporarily unapplicable JSON returns `false`, does not mark explicit startup authoritative, and reports the reason through `warning`. Parse rejection leaves the current runtime unchanged.

The Setup window's **Initialization Code** section generates a self-contained `PerfMeterBootstrap` with a complete normalized snapshot in `SettingsJson` and a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` call to this API. The snapshot includes collection/work-mode, overlay and visual-preset data, logging and alert defaults, session defaults, and overdraw limits. A valid snapshot with `enabled: false` or `collectionMode: "Stopped"` stops PerfMeter; another valid collection mode ensures a runtime and applies the collection/overlay settings. The explicit bootstrap does not start a session or capture and does not persist or auto-run capture parameters.

The generated bootstrap is an alternative to the Resources zero-code file at `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` (load path `SGG.PerfMeter/perfmeter-settings`). Resources auto-start additionally checks `autoStart`; an explicit generated call is already the startup decision. A successfully parsed explicit application suppresses Resources auto-start for the current domain and becomes authoritative even if Resources started the runtime first. Invalid explicit JSON leaves the current runtime unchanged and does not suppress a later Resources auto-start. Parameterless session start and default overdraw requests use the active runtime snapshot after explicit application.

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

Visual preset descriptors are bounded by `PerfMeterOverlayLayoutLimits`. Built-in semantic theme colors are available through `PerfMeterOverlayThemeRegistry.GetManifest(...)` / `GetAllManifests()`. Projects can register at most 16 additional module-backed descriptor IDs through `PerfMeterWidgetRegistry.TryRegisterDescriptor(...)`; an extension descriptor composes existing `PerfMeterOverlayModule` rendering only and cannot install arbitrary renderer callbacks.

```csharp
PerfMeterOverlayThemeManifest theme =
    PerfMeterOverlayThemeRegistry.GetManifest(PerfMeterOverlayTheme.Cyber);

var descriptor = new PerfMeterWidgetDescriptor(
    "project.movement-panel",
    "Movement panel",
    "Project",
    "Panel",
    "CustomMetrics",
    "Project movement metrics rendered by the existing custom-metric panel.",
    isPresetBlock: true,
    isDebugOnly: false,
    overlayModules: PerfMeterOverlayModule.CustomMetrics,
    requiredProviders: new[] { "CustomMetrics" });

if (!PerfMeterWidgetRegistry.TryRegisterDescriptor(descriptor, out string warning))
{
    UnityEngine.Debug.LogWarning(warning);
}
```

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
    postRollFrames: 30,
    backendMode: PerfMeterCaptureBackendMode.NativeRequired,
    externalArtifactStorageMode: PerfMeterExternalArtifactStorageMode.Copy);

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

`PerfMeterCaptureBackendMode.GenericUnity` is the compatibility default and wraps Unity's experimental `ExternalGPUProfiler` only in the Editor or a Development Build, when an external tool is attached, on supported desktop platform/API combinations. Unity does not expose attached-tool identity; `Status.Tool` is the requested tool and generic `Completed` does not verify an external artifact. `NativePreferred` requests the optional Windows x64 Editor RenderDoc bridge but may fall back only before native begin. `NativeRequired` never falls back. Native support is limited to Direct3D 11, Direct3D 12, and Vulkan; other players/platforms are unsupported.

`PerfMeterCaptureOptions` defaults to one capture frame, no pre/post-roll, `GenericUnity`, and MetadataOnly. `RequestCapture` starts the runtime when valid. `CancelCapture()` without an ID cancels the current active request; passing an ID protects against canceling a newer request. Status exposes `RequestedBackendMode`, `EffectiveBackendKind`, `NativePhase`, `NativeResultCode`, and `FallbackReason`.

The bundle overload keeps capture samples separate from baseline session evidence and can include an opt-in runtime screenshot. Once `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady` is true, call `PerformanceMeter.ExportCaptureBundle(captureId)`. Export creates an atomic versioned directory under `Temp/PerfMeter/CaptureBundles` with manifest hashes, session/baseline/capture samples, capture alerts, context, optional screenshot, and external-artifact metadata.

A caller-supplied project-local `.rdc` or `.wpix` path and every generic Unity artifact remain observed, not authoritative. The native generation-bound descriptor can authenticate a finalized `.rdc` and satisfy `requireAuthoritativeExternalArtifact`. Native MetadataOnly defaults to `DoNotShare`; Copy/Embed require `ReviewBeforeShare` and separate project-local quota/retention. Absolute paths, traversal, reparse points, oversized data, and files outside owned roots are rejected. Use `PerformanceMeter.GetCaptureCapabilities()` to inspect current schema, quota, retention, and screenshot limits.

Capture-bundle export also has a non-blocking single-flight API: `RequestCaptureBundleExport(..., out exportId)`, `GetCaptureBundleExportStatus(exportId)`, and `CancelCaptureBundleExport(exportId)`. Status reports phase, progress, bytes, cancellation, retry, commit path, and the generic external-artifact envelope. The existing `ExportCaptureBundle(...)` API remains a blocking compatibility wrapper, while serialization, file I/O, hashing, retention, and atomic commit run on a worker thread.

Session and capture JSON add typed timeline events for missing samples and capture boundaries. Existing schema versions, sample arrays, and CSV columns remain compatible; legacy or unknown timeline payloads are read without inventing gaps. Custom metric providers use a cached provider snapshot and reusable core-owned buffer on the warmed collection path, with copies only for retained samples, exports, and public snapshots. Profiler coordination is process-local through `GetProfilerLeaseCapabilities()`, `GetProfilerLeaseStatus()`, `TryAcquireProfilerLease(...)`, and `ReleaseProfilerLease(...)`; held leases do not survive domain reload.

## RenderDoc GPU Command Annotations

The annotation API is independent from capture coordination. It writes typed semantic state into the same GPU command stream as a draw or dispatch when RenderDoc is already loaded, App API `1.7` is available, a capture is active, and the native transport supports the current backend.

```csharp
PerfMeterGpuAnnotationBatch annotations = new PerfMeterGpuAnnotationBatch();
annotations.TryAdd(PerfMeterGpuAnnotationKeys.Module, "com.sungeargames.sky");
annotations.TryAdd(PerfMeterGpuAnnotationKeys.RenderGraphPass, "sky.volumetric_clouds.raymarch");
annotations.TryAdd("SGG.Sky.CloudLayer", 2u);

using (PerfMeterGpuAnnotationScope scope =
       PerfMeterGpuAnnotations.BeginScope(commandBuffer, annotations))
{
    commandBuffer.DispatchCompute(shader, kernel, groupsX, groupsY, groupsZ);
}
```

Use `PerfMeterRenderGraphGpuAnnotations.BeginScope(...)` inside Render Graph passes. It has direct overloads for `RasterCommandBuffer`, `ComputeCommandBuffer`, and `UnsafeCommandBuffer`; raster/compute passes do not need conversion to unsafe passes.

Publish frame or simulation correlation separately. Publication records no GPU command; the latest immutable owner generation is merged when a pass scope begins, and local values override ambient values:

```csharp
PerfMeterGpuAnnotationBatch context = new PerfMeterGpuAnnotationBatch();
context.TryAdd("SGG.Weather.Command.Sequence", sequence);
context.TryAdd("SGG.Weather.SimulationTick", simulationTick);

PerfMeterGpuAnnotations.TryPublishContext("weather.main", generation, context);
// Later, only the active exact generation may clear this owner.
PerfMeterGpuAnnotations.TryClearContext("weather.main", generation);
```

`Capabilities.Availability` distinguishes an absent provider/bridge, old bridge, unloaded RenderDoc, unsupported API/backend, inactive capture, packet-budget exhaustion, invalid data, and internal failure. `ShouldRecord` is the hot-path gate. Normal unavailable states are silent no-ops.

Schema v1 always records `SGG.Annotation.SchemaVersion = 1`. Keys are case-sensitive ASCII paths up to 127 bytes; string values are strict UTF-8 up to 255 bytes; a batch contains at most 32 entries. Supported values are empty, bool, signed/unsigned 32/64-bit integers, float, double, string, and numeric/bool vectors of widths 1–4. Use stable machine IDs. Do not present `Object.GetInstanceID()` as cross-run identity or access `AssetDatabase` from runtime code.

Scopes are non-nested in v1 and must be disposed after recording the described work. The end event clears every key owned by the scope so state does not leak into a neighboring pass. The initial transport is a separately installed Windows x64 Editor bridge and implements D3D12; the UPM package remains binary-free and RenderDoc itself is never shipped or loaded by PerfMeter. The currently published `2026.8.11-1` capture bridge predates these exports and reports `BridgeTooOld`. Vulkan, D3D11, Player, and resource/object annotations require separate validation gates.

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

## Render Integration Context

The additive integration-neutral snapshot is available through both methods:

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` exposes `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph`, and `Warning`. The nested GRD and VRS snapshots expose their availability, configuration/support fields, activity availability, and warnings.

Reads are safe before runtime startup and do not start collection. A supported current pipeline can be `Available` while `State` is `NotObserved`; if the latest observation belongs to another pipeline configuration, `ObservationMatchesCurrentPipeline` is `false`, the age/frame fields remain explicit, and the warning identifies stale data. Do not treat stale fields as current observations.

URP reports the public current-frame `UniversalRenderingData.renderingMode` and the PerfMeter passes actually scheduled for that frame. HDRP reports the observed PerfMeter `CustomPass`, but its effective rendering mode is unavailable. `GpuResidentDrawer` reports configured mode, SRP/project/compute support, current-frame URP Forward+ and clustered-mode compatibility, and global runtime activity from `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()`. HDRP keeps Forward+/rendering-mode fields `Unknown`. `VariableRateShading` reports authoritative `SystemInfo`/`ShadingRateInfo` hardware support; configuration and activity are `Unknown` unless a typed adapter reports them.

`LegacyRenderGraph` is an embedded compatibility facade for `GetRenderGraphSnapshot()`. Private/internal Render Graph pass/resource reflection was removed, so its legacy pass/resource counters remain `-1`. The stable public Unity API also exposes no RenderGraph/CustomPass viewer or pass targets; this API therefore does not provide Editor navigation.

`RenderPipeline` contains `Kind`, `AssetName`, `AssetTypeName`, and `RuntimeTypeName`; `RenderPipelineAssetSource` is `GraphicsSettings`, `QualitySettings`, or `None`. `GpuResidentDrawer` additionally contains `ProjectConfigurationAvailability`, `IsProjectConfigurationSupported`, `ComputeShaderAvailability`, `SupportsComputeShaders`, `ForwardPlusActivityAvailability`, `IsObservedForwardPlusActive`, `RenderingModeCompatibilityAvailability`, `IsRenderingModeCompatible`, `ActivitySource`, `DegradedReason`, and `Effectiveness`. `PerfMeterGpuResidentDrawerReason` provides structured fallback states. `PerfMeterGpuResidentDrawerEffectivenessSnapshot` carries BRG draw-call/instance values and exact profiler-capability provenance; unsampled values use `UnavailableCount` (`-1`) in C# and serialize as `null`. These are aggregate BatchRendererGroup counters, not authoritative per-renderer GRD evidence.

## Session Correlation

`PerformanceMeter.GetSessionSummary().SessionId` is a lowercase 32-character hexadecimal identifier. It is created by `StartSession`, remains stable after `StopSession`, changes when a new session starts, and is empty when no session exists. Session JSON exposes the same value as top-level `session_id`; CSV appends it as the final `session_id` column to preserve existing positional columns; `perfmeter.session.summary` returns it as `session_id`.
