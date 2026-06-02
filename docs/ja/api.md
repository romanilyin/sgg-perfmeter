# Runtime API

Namespace:

```csharp
using SGG.PerfMeter;
```

すべての read APIs は runtime start 前でも安全です。runtime が active でない場合も例外を投げず、stopped/default snapshots を返します。

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

主な metric groups:

- FPS: average、1% low、0.1% low、spike counts。
- Timing: 利用可能な場合の CPU frame、CPU main thread、CPU render thread、present wait、GPU frame。
- Rendering: draw calls、SetPass、batches、vertices、SRP Batcher、BRG/GRD、uploads。
- Memory: system/app memory、GC reserved memory、利用可能な場合の GPU memory。
- Bottleneck: GPU、CPU main、CPU render、present-limited、balanced、unknown。
- Overdraw: state、progress、ratio、heatmap visibility。

Counter availability は `AvailableCounters`、`UnavailableCounters`、warnings で公開されます。

## Structured Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshots には Unity/platform/OS/CPU/GPU/API/display/window/support information が含まれます。Camera snapshots には scene、transform、projection、clipping、pixel rect、target display、利用可能な場合の URP camera settings が含まれます。

## CPU Core Loads

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

各 snapshot は `CoreIndex`、`LoadPercent`、`Available` を公開します。runtime startup 前、sampler warm-up 中、unsupported platforms では array が空になる場合があります。これは API call の失敗ではなく platform capability information として扱ってください。

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

legacy overlay modes と semantic module flags は compatibility と filtering のため引き続き利用できます。

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

Session options には warm-up frames/seconds、sample interval、maximum samples、reset-on-scene-load、scene-load ignore windows が含まれます。

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

Provider exceptions は unavailable custom metric snapshots として報告され、core metric collection を中断しません。

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw diagnostics は明示的な diagnostic modes であり、GPU work を追加する場合があります。
