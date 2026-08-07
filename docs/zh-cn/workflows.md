# 工作流

## Runtime Overlay

需要在游戏中立即看到诊断信息时使用 overlay。

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Overlay 使用 UI Toolkit，不会拦截 gameplay input。它支持 FPS-only、compact text、graph、full diagnostics、metric bars、visual themes、module filters、CPU/GPU graphs、CPU core widgets，以及有限的 custom metric rows。

PerfMeter 为 overlay 创建并拥有 versioned UI Toolkit host：Unity `6000.4` 使用 `UIDocument`，Unity `6000.5+` 使用 `PanelRenderer`。这个 owned host 与 foreign UI 分离，并保留 foreign UI 的 panel settings 和 children；rebuild 只移除 PerfMeter-owned container。

## Background Collection

在 tests、device runs 或不需要可见 UI 的 agent workflows 中使用 background mode。

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording And Export

使用 sessions 创建可重复的性能分析窗口。

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session exports 包含 timing、FPS lows、spikes、bottleneck counts、render counters、memory counters、overdraw state、warning/counter availability、scene summaries、worst frames、device metadata、camera metadata、settings metadata 和 custom metrics。

## Alerts

Rules 可以报告 budget violations、low FPS、unavailable GPU timing 和 overdraw thresholds。

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warnings 会受 cooldowns 限流，并可通过 JSON settings 或 runtime controls 禁用。Structured alert logs 与 Editor warnings 相互独立：`PerformanceMeter.SetStructuredLogsEnabled(false)` 只抑制 structured alert 的 `Debug.Log` 输出，而 `PerformanceMeter.SetEditorWarningLogsEnabled(false)` 单独控制 Editor warning logs。Callbacks、alert/history、overlay warnings 和 sessions 仍保持 active。

## External GPU Capture

当 tool 已经 attach 时，使用 capture coordinator 发起有边界的 RenderDoc 或 PIX request：

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

Coordinator 只允许一个 active request，并以 deterministic 顺序经过 `PreRoll`、`Capturing`、`PostRoll` 和 `Completed`。相同的 active ID 是 idempotent，不同的 ID 会作为 overlap 被 reject。Pre-roll 和 post-roll 统计 Unity frames；只有 `Capturing` 会打开 alert capture scope 并调用 Unity 的 experimental `ExternalGPUProfiler`。Editor 或 Development Build 以及 attached tool 是 mandatory gates。`RenderDoc` 支持 Windows/Linux desktop 的 Direct3D 11、Direct3D 12 或 Vulkan；`PIX` 支持 Windows desktop 的 Direct3D 12。

`Completed` 仅表示 guarded Unity wrapper lifecycle 已结束。Unity 不会暴露 attached tool identity 或 authoritative artifact path，因此 `Status.Tool` 只表示 requested tool。`PerfMeterCaptureBundleOptions` overload 会分离 baseline/capture samples，并原子导出 project-local bundle；external artifact 仅为 observed，不是 authoritative。自动化使用 `perfmeter.capture.request/status/cancel/export/capabilities`。

## Overdraw Diagnostics

Numerical overdraw 需要显式启用且有边界。

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Numerical overdraw 和 heatmap 使用 URP Render Graph diagnostic path。Overdraw measurement 需要 `PerfMeterRenderGraphFeature`、replacement shader support、fragment UAV/storage-buffer support、compute shader support、受支持的 graphics API，以及 async GPU readback。HDRP 会将 overdraw/heatmap 报告为 unsupported，但 core overlay、session、API 和 MCP diagnostics 仍可用。不受支持的目标会报告 `OverdrawState.Unsupported`，不会运行 pass。

## Camera And Device Reproducibility

使用 snapshots 保留生成 performance capture 的环境。

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Session exports 包含 device 和 camera metadata，因此之后可以理解或复现 capture。

## Custom Metrics

注册项目特定 providers，无需 fork PerfMeter。

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics 会通过 API reads、session JSON export、MCP latest metrics 暴露；启用 `CustomMetrics` module 时，overlay 最多显示八行。

## Unity Profiler Instrumentation

此 instrumentation 属于 internal scope，仅在 profiling Editor、Development Build 或其他 profiler-enabled build 时可在 Unity Profiler 中查看。没有 Profiler 的 Release player 中，这些 marker/counter 是 no-op，不会生成 instrumentation data；public API、status、MCP 和 export schema 不变。

- Marker 覆盖 collection/frame timing（`SGG.PerfMeter.Collect`、`SGG.PerfMeter.Collect.FrameTiming`）、providers（`SGG.PerfMeter.Provider.CustomMetrics`、`SGG.PerfMeter.Provider.CpuCore`、`SGG.PerfMeter.Provider.DeviceSnapshot`、`SGG.PerfMeter.Provider.CameraSnapshot`）、bottleneck/capture（`SGG.PerfMeter.Bottleneck.Classify`、`SGG.PerfMeter.Capture.Session`、`SGG.PerfMeter.Capture.AlertScope`、`SGG.PerfMeter.Capture.Coordinator`）以及 JSON/CSV export（`SGG.PerfMeter.Export.Json`、`SGG.PerfMeter.Export.Csv`）。`SGG.PerfMeter.Thermal.Sample` 是 reserved internal provider hook。
- Counter 覆盖 CPU/GPU frame time（`SGG.PerfMeter.CPU.FrameTime`、`SGG.PerfMeter.CPU.MainThreadTime`、`SGG.PerfMeter.CPU.RenderThreadTime`、`SGG.PerfMeter.CPU.PresentWaitTime`、`SGG.PerfMeter.GPU.FrameTime`），作为 nanoseconds 的 end-of-frame gauge。`SGG.PerfMeter.CPU.FrameTimingAvailable`、`SGG.PerfMeter.GPU.FrameTimingAvailable`、`SGG.PerfMeter.Capture.AlertScopeActive` 和 `SGG.PerfMeter.Thermal.Available` 用 `0`/`1` 编码 availability/active；`SGG.PerfMeter.Bottleneck.Kind`、`SGG.PerfMeter.Capture.SessionState`、`SGG.PerfMeter.Capture.OverdrawState` 和 `SGG.PerfMeter.Capture.State` 使用 enum code；`SGG.PerfMeter.Provider.CustomMetricCount` 是 count。Counter 使用 `Scripts` category 和 `FlushOnEndOfFrame`。
- 不会生成 synthetic thermal sample；`SGG.PerfMeter.Thermal.Available` 在真实 platform provider 提供 data 前保持 `0`/unavailable。

## Self-Observability And Overhead Budgets

使用 `PerformanceMeter.GetSelfOverhead()` 或 `PerformanceMeter.GetStatus().SelfOverhead` 诊断 collector、custom providers、CPU-core provider、overlay 和 URP/HDRP integration 的 CPU callback cost 与 allocation。测量使用固定 120-frame window、invocation average 和 component-specific CPU/allocation budget。

Inactive render integration 报告 `Unsupported`，未调用的 supported component 报告 `NotMeasured`，GPU self-timing 报告 `Unavailable`。Accounting 仅用于 diagnostics：PerfMeter 不会从现有 CPU/GPU metrics 中 subtract overhead，也不会调整其值。

## Agent Automation

典型 MCP-driven run：

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

`perfmeter.profiler.capabilities {}` 是缓存读取；不会启动 runtime，也不会执行 discovery。

## 可选内存快照 workflow

1. 使用 Unity `6000.4+`，并通过 Package Manager 安装 `com.unity.memoryprofiler` `1.1.0+`。可选的 `SGG.PerfMeter.MemoryProfiler` assembly 会自动注册 backend；没有该 package 时 core integration 保持 unavailable。
2. 在 Play Mode 中读取 `PerformanceMeter.GetMemorySnapshotCapabilities()` 或 `perfmeter.memory.snapshot.capabilities`，确认 backend 和所需 capture flags 可用。
3. 使用 `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))` 请求 manual snapshot，或者使用 `ConfigureMemorySnapshotTriggers(...)` 显式启用 system-memory threshold 或 bounded leak-growth window。
4. 读取 `GetMemorySnapshotStatus()` 或 `perfmeter.memory.snapshot.status`，直到 snapshot 和 correlated bundle 到达 terminal state。使用 `PerformanceMeter.ExportCaptureBundle(captureId)` 或 `perfmeter.capture.export` 导出已准备好的 evidence。

memory-only evidence 通过现有 capture-bundle API 写入 `Temp/PerfMeter/CaptureBundles`。bundle 将 `MemoryProfiler` 记录为 requested tool，包含内存 provenance 和 `.snap` 的 streaming SHA-256，但不包含 external GPU artifact。owned source 位于 `Temp/PerfMeter/MemorySnapshots`；成功 export 后只消费一次。

## 图形 marker 诊断

1. 调用 `PerformanceMeter.GetGraphicsDiagnostics()` 或 `perfmeter.graphics.diagnostics`，读取最新 marker value 与 graphics API context。
2. 检查每个 capability 的 `SampleState`、`Resolution`、`ResolvedRecorderNames`、`Unit`、`DataType`、resolved/sampled component count 和 catalog revision。discovery 是动态的，在 runtime 启动和显式 profiler catalog refresh/reconfigure 时执行。
3. 将这些值视为发现 unit 下的 raw recorder value。marker 可能是 unavailable、available 但没有 sample，或 sampled；numeric 0 不是通用 unavailable signal，值也不保证是 shader/PSO count。

shader marker 先解析 exact `Shader.CreateGPUProgram`，再解析 aliases `Shader.CreateGPUPrograms`、`Shader.CompileGPUProgram`、`Shader.DynamicLoadGPUProgram`。pipeline marker 解析 exact `CreatePSO.Job`。相同的 value 和 provenance 也可通过 `perfmeter.metrics.latest` 和 session JSON/CSV 获取。

## GraphicsStateCollection trace 与 prewarm

1. 在 Unity `6000.4+` 中确认可选 `SGG.PerfMeter.GraphicsStateCollection` assembly 可用。Unity `6000.4` 使用 `UnityEngine.Experimental.Rendering.GraphicsStateCollection`，Unity `6000.5+` 使用 `UnityEngine.Rendering.GraphicsStateCollection` namespace。
2. 在 trace 前启动 PerfMeter session。执行 `StartSession(...)`，然后调用 `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` 或对应的 MCP request。没有 active session 时 request 会被拒绝；session 必须持续 recording 到 trace 完成，`PerformanceMeter.StopSession()` 会取消 active trace。
3. 在 bounded trace 推进期间保持场景运行。普通 Play Mode 中，每个 trace frame 在 `WaitForEndOfFrame` 后 tick；batch mode 中 coordinator 使用 next-frame fallback。此期间被 session 接纳的 sample 会记录 `GraphicsStateTraceId`/`graphics_state_trace_id`，session settings 决定保留多少 correlated sample。
4. 轮询 `GetGraphicsStateCollectionStatus()` 或 `perfmeter.graphics.state_collection.status` 直到 `Completed`，然后按需停止 session。在 active trace 期间停止会取消 trace，并可能在 owned cleanup retry 期间让 `IsBusy`/`is_busy` 保持 true。owned `.graphicsstate` artifact 位于 project-relative 的 `Temp/PerfMeter/GraphicsStateCollections` 下，最大 64 MiB。
5. 将 status 返回的 owned relative path 传给 `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` 或 MCP prewarm command。prewarm 是 synchronous 的，会保留 artifact，并报告 completed warmup 与 `IsWarmedUp`；progressive warmup 可能以 explicit incomplete warning 结束。

graphics-state coordinator 只允许一个 flight，也会拒绝与 active external GPU capture、memory snapshot 或 alert-capture 的 overlap。相同的 active trace ID 返回 `AlreadyActive`，其他 ID 返回 `RejectedOverlap`。`CancelGraphicsStateTrace` 只 cancel 匹配的 active/preparing trace 并清理 pending artifact。如果 owned artifact 删除失败，`HasPendingCleanup`/`has_pending_cleanup` 会保持 true，旁边的 `.delete-pending` sidecar 会在 domain reload 后恢复并重试；`IsBusy`/`is_busy` 和 warning 会保持可见直到成功。Unity backend 不支持 cache-miss tracing，因此没有 cache-miss evidence。

## Render integration context

当需要查看最新 typed render integration 的 pipeline-neutral 信息时，使用 neutral snapshot：

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

也可以通过 MCP 读取相同数据：

```text
perfmeter.render.snapshot {}
```

这些 read 不会启动 runtime collection。请一起检查 `State`、`ObservationAgeFrames`、`LastObservedFrame` 和 `ObservationMatchesCurrentPipeline`。pipeline 或 asset configuration 改变后，之前的 observation 会变成 stale；保留 warning 和 non-match，不要把其 pass、mode、GRD 或 VRS 值当作当前 frame 数据。legacy API `PerformanceMeter.GetRenderGraphSnapshot()` 和 `perfmeter.rendergraph.snapshot` 仍然可用。

诊断 GRD 时，请检查 `DegradedReason`、SRP support、project configuration、compute support、URP mode compatibility 和 `ActivityAvailability`。`IsObservedActive` 是 Unity 的 global enabled state。`Effectiveness` 仅作为 aggregate BRG workload context：`AvailableNoSample`/`Unavailable` 不表示 workload 为零，positive BRG counters 也不证明某个 renderer 使用了 GRD。

capture bundle 的 schema `sgg.perfmeter.capture-context` version `1` 保留已有的 `render` 并添加 `render_integration`。external GPU capture 在 `Capturing` phase 的第一个 sample 冻结 context；Memory Profiler bundle 在 memory request 完成时记录 context。session JSON/CSV schema 不变。public API 没有稳定的 RenderGraph/CustomPass viewer 或 pass target，因此该 workflow 不承诺 Editor navigation。
