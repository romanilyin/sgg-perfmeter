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

- Marker 覆盖 collection/frame timing（`SGG.PerfMeter.Collect`、`SGG.PerfMeter.Collect.FrameTiming`）、providers（`SGG.PerfMeter.Provider.CustomMetrics`、`SGG.PerfMeter.Provider.CpuCore`、`SGG.PerfMeter.Provider.DeviceSnapshot`、`SGG.PerfMeter.Provider.CameraSnapshot`）、bottleneck/capture（`SGG.PerfMeter.Bottleneck.Classify`、`SGG.PerfMeter.Capture.Session`、`SGG.PerfMeter.Capture.AlertScope`）以及 JSON/CSV export（`SGG.PerfMeter.Export.Json`、`SGG.PerfMeter.Export.Csv`）。`SGG.PerfMeter.Thermal.Sample` 是 reserved internal provider hook。
- Counter 覆盖 CPU/GPU frame time（`SGG.PerfMeter.CPU.FrameTime`、`SGG.PerfMeter.CPU.MainThreadTime`、`SGG.PerfMeter.CPU.RenderThreadTime`、`SGG.PerfMeter.CPU.PresentWaitTime`、`SGG.PerfMeter.GPU.FrameTime`），作为 nanoseconds 的 end-of-frame gauge。`SGG.PerfMeter.CPU.FrameTimingAvailable`、`SGG.PerfMeter.GPU.FrameTimingAvailable`、`SGG.PerfMeter.Capture.AlertScopeActive` 和 `SGG.PerfMeter.Thermal.Available` 用 `0`/`1` 编码 availability/active；`SGG.PerfMeter.Bottleneck.Kind`、`SGG.PerfMeter.Capture.SessionState` 和 `SGG.PerfMeter.Capture.OverdrawState` 使用 enum code；`SGG.PerfMeter.Provider.CustomMetricCount` 是 count。Counter 使用 `Scripts` category 和 `FlushOnEndOfFrame`。
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
