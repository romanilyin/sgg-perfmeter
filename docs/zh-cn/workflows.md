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

Editor warnings 会受 cooldowns 限流，并可通过 JSON settings 或 runtime controls 禁用。

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

## Agent Automation

典型 MCP-driven run：

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
