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

## Structured Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshots 包含 Unity/platform/OS/CPU/GPU/API/display/window/support 信息。Camera snapshots 包含 scene、transform、projection、clipping、pixel rect、target display，以及可用时的 URP camera settings。

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
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

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

Overdraw diagnostics 是显式 diagnostic modes，可能增加 GPU work。
