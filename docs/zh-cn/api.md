# Runtime API

Namespace：

```csharp
using SGG.PerfMeter;
```

所有 read APIs 在 runtime 启动前都是安全的。Runtime 未激活时，读取会返回 stopped/default snapshots，而不会抛出异常。

## Lifecycle

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Collection modes：

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

关键 metric groups：

- FPS：average、1% low、0.1% low、spike counts。
- Timing：CPU frame、CPU main thread、CPU render thread、present wait、可用时的 GPU frame。
- Rendering：draw calls、SetPass、batches、vertices、SRP Batcher、BRG/GRD、uploads。
- Memory：system/app memory、GC reserved memory、可用时的 GPU memory。
- Bottleneck：GPU、CPU main、CPU render、present-limited、balanced 或 unknown。
- Overdraw：state、progress、ratio 和 heatmap visibility。

Counter availability 通过 `AvailableCounters`、`UnavailableCounters` 和 warnings 暴露。

## Self-Observability And Overhead Budgets

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability 使用固定 120-frame window，以 low-overhead 方式报告 CPU callback cost。Average 按 invocation 计算。整体 state 为 `NotInitialized`、`Collecting` 或 `Ready`；component state 为 `NotMeasured`、`Collecting`、`Ready` 或 `Unsupported`。

Components 包括 `Collector`、`CustomMetricProviders`、`CpuCoreProvider`、`Overlay`、`UrpRenderIntegration` 和 `HdrpRenderIntegration`。每个 component 暴露 window/invocation count、average/maximum CPU milliseconds、total/average allocated bytes、budget 以及 `NotEvaluated`/`WithinBudget`/`Exceeded` state。

| Component | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU self-timing 明确为 `Unavailable`。这些 diagnostics 不会从现有 CPU/GPU metrics 中 subtract overhead，也不会调整其值。

## Dynamic Profiler Metric Catalog

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` 和 `GetProfilerMetricCapabilities()` 读取缓存的 catalog。Catalog state 为 `NotInitialized`、`Ready` 或 `Error`；每个 capability 报告 `Unavailable`、`AvailableNoSample` 或 `AvailableSampled`，`Resolution` 表示 `None`、`Exact` 或 `Alias` provenance。Discovery 只在 runtime 启动和显式 refresh/reconfigure 时执行，不会在 steady-state collection 中执行。现有 numeric metrics 仍是 compatibility values；availability 应以 capability 的 `SampleState`/`IsAvailable` 作为 authoritative signal。

## Structured Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshots 包含 Unity/platform/OS/CPU/GPU/API/display/window/support 信息。Camera snapshots 包含 scene、transform、projection、clipping、pixel rect、target display，以及可用时的 URP/HDRP camera settings。

## CPU Core Loads

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

每个 snapshot 暴露 `CoreIndex`、`LoadPercent` 和 `Available`。数组在 runtime 启动前、sampler warm-up 期间或不受支持的平台上可能为空；应将其视为平台能力信息，而不是 API 调用失败。

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

Legacy overlay modes 和 semantic module flags 仍可用于 compatibility 和 filtering。

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

Session options 包含 warm-up frames/seconds、sample interval、maximum samples、reset-on-scene-load 和 scene-load ignore windows。

## Alerts

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` 默认值为 `true`，只控制 structured alert 的 `Debug.Log` 输出。设置为 `false` 不会禁用 `AlertFired` callbacks、latest alerts 或 alert history、overlay warnings、Editor warning logs 或 sessions。`PerformanceMeter.SetEditorWarningLogsEnabled(bool)` 独立控制 Editor warning logs。

## Editor Compatibility Status

Editor API `PerfMeterSetupActions.GetCompatibilityStatus()` 返回 `PerfMeterCompatibilityStatus`，分别报告 Unity `2022.3` package floor 的 `ImportCompatible`、supported runtime Unity `6000.4+` 的 `CoreRuntimeCompatible`，以及具备 available adapter 的 active URP/HDRP `17.4+` `RenderIntegrationCompatible`。每个结果都有 reason。render compatibility 不表示 renderer assets 已完成配置；configuration readiness 请使用 setup status。

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

Coordinator 只允许一个 active request，并以 deterministic 顺序经过 `PreRoll`、`Capturing`、`PostRoll` 和 `Completed`。重复相同的 active ID 是 idempotent；不同的 active ID 会因 overlap 被 reject。`Canceled`、`Unavailable` 和 `Error` 是明确的 terminal state。

内置 backend 仅在 Editor 或 Development Build、external tool 已 attach 且 desktop platform/API 组合受支持时 wrap Unity 的 experimental `ExternalGPUProfiler`。支持的组合是 Windows/Linux desktop 上使用 Direct3D 11、Direct3D 12 或 Vulkan 的 `RenderDoc`，以及 Windows desktop 上使用 Direct3D 12 的 `PIX`。由于 Unity 不会暴露 attached tool identity，请显式选择 `RenderDoc` 或 `Pix`。`Status.Tool` 仅表示 requested tool，不是 verified attached-tool identity。`Completed` 只确认 Unity wrapper lifecycle，不验证或返回 external `.rdc`/`.wpix` artifact，也不返回 artifact path。Automated tests 使用 fake backend；real external tool 和 artifact 的确认仍是 release gate。

`PerfMeterCaptureOptions` 的默认值是 `captureFrames: 1`、`preRollFrames: 0` 和 `postRollFrames: 0`。有效的 `RequestCapture` 会自动启动 runtime。不带 ID 的 `CancelCapture()` 会取消当前报告的 active request；传入 ID 可以防止误取消更新的 request。

`PerfMeterCaptureBundleOptions` overload 会将 capture samples 与 baseline session 分离，并可包含 opt-in screenshot。当 `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady` 后，`PerformanceMeter.ExportCaptureBundle(captureId)` 会在 `Temp/PerfMeter/CaptureBundles` 下原子创建 versioned bundle，其中包含 SHA-256 manifest、samples、alerts、context、optional screenshot 和 external artifact metadata。project-local `.rdc`/`.wpix` 仅是 observed artifact，绝不标记为 authoritative；traversal、reparse point 和项目外文件会被拒绝。

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Provider exceptions 会作为 unavailable custom metric snapshots 报告，不会中断核心 metric collection。

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw diagnostics 是显式 diagnostic modes，可能增加 GPU work。在 HDRP 中，这些 API 会安全报告 overdraw 和 heatmap 的 unsupported state，而不会承诺 HDRP heatmap output。

## 可选内存快照

内存快照是可选集成。在 Unity `6000.4+` 中安装并解析 `com.unity.memoryprofiler` `1.1.0+` 后，独立的 `SGG.PerfMeter.MemoryProfiler` assembly 会启用并自动注册 `MemoryProfiler` backend。core assembly 没有 hard dependency。

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

公开 API 包括 `RegisterMemorySnapshotBackend(...)`、`UnregisterMemorySnapshotBackend(...)`、`GetMemorySnapshotCapabilities()`、`GetMemorySnapshotStatus()`、`RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`、`ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` 和 `GetMemorySnapshotTriggers()`。自定义 backend 实现 `IPerfMeterMemorySnapshotBackend`；可选 assembly 提供 Unity Memory Profiler backend。

`PerfMeterMemorySnapshotOptions` 默认使用 managed/native object flags、最低 1 GiB 可用磁盘空间和 300 秒 cooldown。`RequestMemorySnapshot` 默认执行 manual capture，并返回 `Started`、`AlreadyActive`、`RejectedOverlap`、`Cooldown`、`Unavailable`、`InsufficientDiskSpace`、`InvalidRequest` 或 `Failed` 等明确结果。读取 API 不会启动 runtime；有效 request 会启动 runtime。

`ConfigureMemorySnapshotTriggers` 可显式 opt-in system-memory threshold 和 bounded leak-growth heuristic。`GetMemorySnapshotTriggers()` 默认是 disabled。trigger request 与 manual request 使用相同的 single-flight、cooldown、free-space 和 capture-flag guard。

## 图形诊断与 GraphicsStateCollection

图形诊断会在现有 snapshot 上增加信息。`PerformanceMeter.GetGraphicsDiagnostics()` 返回最新的 shader GPU-program creation 与 graphics-pipeline creation marker 值、graphics API context、parallel PSO capability 以及 profiler metric catalog revision。

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

catalog 会在 runtime 启动以及显式 refresh/reconfigure 时 discovery Unity `ProfilerRecorder` descriptor。shader semantic 使用 exact name `Shader.CreateGPUProgram` 和 aliases `Shader.CreateGPUPrograms`、`Shader.CompileGPUProgram`、`Shader.DynamicLoadGPUProgram`。graphics-pipeline semantic 使用 exact name `CreatePSO.Job`。每个 capability 都保留 `Resolution`（`None`、`Exact`、`Alias`）、`ResolvedRecorderNames`、`Category`、发现的 `Unit`、`DataType`、`ResolvedComponentCount` 和 `SampledComponentCount`。`PerfMeterMetricsSnapshot` 及 session JSON/CSV 也包含相同的 marker value、capability metadata 和 catalog revision。

marker availability 是动态的。请使用 `SampleState`（`Unavailable`、`AvailableNoSample`、`AvailableSampled`）以及 capability metadata；值为 0 并不表示 marker 不存在。值是 recorder 的 raw value，并保留发现的 unit；它们不一定是 shader 或 PSO count，PerfMeter 也不会转换到统一 unit。

可选的 `SGG.PerfMeter.GraphicsStateCollection` assembly 面向 Unity `6000.4+`，在可用时自动注册 Unity backend。Unity `6000.4` 使用 `UnityEngine.Experimental.Rendering.GraphicsStateCollection`，Unity `6000.5+` 使用 `UnityEngine.Rendering.GraphicsStateCollection`。core assembly 不依赖该 backend。

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

公开的 state-collection API 包括 `RegisterGraphicsStateCollectionBackend(...)`、`UnregisterGraphicsStateCollectionBackend(...)`、`GetGraphicsStateCollectionCapabilities()`、`GetGraphicsStateCollectionStatus()`、`RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`、`PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)` 和 `CancelGraphicsStateTrace(string captureId)`。自定义 backend 实现 `IPerfMeterGraphicsStateCollectionBackend`，并报告 trace/prewarm、cache-miss 和 parallel-PSO capability。

`PerfMeterGraphicsStateTraceOptions` 要求非空 `CaptureId`，接受 1–600 个 trace frames，默认使用 60 frames 和最低 1 GiB 可用磁盘空间。trace 只有在 PerfMeter session 正在 recording 时才有效。correlated session sample 会在 `GraphicsStateTraceId`（export 中为 `graphics_state_trace_id`）中携带 active capture ID。session sampling 设置控制 correlated sample 的密度，不改变请求的 trace frame 数。

`PerfMeterGraphicsStateCollectionStatusSnapshot` 暴露 `IsBusy` 和 `HasPendingCleanup`。`IsBusy` 在 preparation、trace、trace 结束、prewarm、cleanup 或 persisted pending cleanup 期间保持 true；`HasPendingCleanup` 专门表示正在等待 cleanup retry 的 owned artifact。如果在 active trace 期间调用 `PerformanceMeter.StopSession()`，trace 会被取消，因此 session 必须持续 recording 到 trace 完成。owned artifact 删除失败时，会在旁边创建 owned `.delete-pending` sidecar marker；domain reload 后 marker 会恢复并重新尝试 cleanup。在 artifact 和 marker 清理完成前，status 会保持可见且 busy。

coordinator 一次只允许一个 graphics-state flight。相同的 active ID 返回 `AlreadyActive`；在 preparation、trace、finalization、cleanup 或其他 capture domain 中请求另一个 trace/prewarm 会返回 `RejectedOverlap`。`CancelGraphicsStateTrace` 只匹配 active/preparing ID，会 cancel backend 并删除 pending owned artifact。cleanup failure 会保持可见，并可能阻止替换直到重试成功。

`PerfMeterGraphicsStatePrewarmOptions` 只接受 owned project-relative `.graphicsstate` path，以及 0–1,000,000 范围内可选的 `MaxStateCount`。prewarm 是 synchronous 的，会保留 artifact，并报告 `CompletedWarmupCount` 与 `IsWarmedUp`；成功但 incomplete 的 progressive warmup 会带有 warning。`TraceCacheMisses` 为可扩展 backend 保留，但 Unity backend 不支持 cache-miss evidence，因此指定后返回 `Unavailable`。
