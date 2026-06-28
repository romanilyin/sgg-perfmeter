# Runtime API

Namespace:

```csharp
using SGG.PerfMeter;
```

모든 read API는 runtime 시작 전에도 안전합니다. runtime이 active 상태가 아니어도 exception을 던지지 않고 stopped/default snapshot을 반환합니다.

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

## Status 및 Metrics

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

주요 metric group:

- FPS: average, 1% low, 0.1% low, spike counts.
- Timing: 사용 가능한 경우 CPU frame, CPU main thread, CPU render thread, present wait, GPU frame.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: 사용 가능한 경우 system/app memory, GC reserved memory, GPU memory.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced, unknown.
- Overdraw: state, progress, ratio, heatmap visibility.

Counter availability는 `AvailableCounters`, `UnavailableCounters`, warnings를 통해 노출됩니다.

## Structured Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshot에는 Unity/platform/OS/CPU/GPU/API/display/window/support 정보가 포함됩니다. Camera snapshot에는 사용 가능한 경우 scene, transform, projection, clipping, pixel rect, target display, URP/HDRP camera settings가 포함됩니다.

## CPU Core Loads

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

각 snapshot은 `CoreIndex`, `LoadPercent`, `Available`을 노출합니다. runtime startup 전, sampler warm-up 중, 또는 unsupported platform에서는 배열이 비어 있을 수 있습니다. 이는 API call 실패가 아니라 platform capability 정보로 처리합니다.

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

Legacy overlay mode와 semantic module flag는 compatibility 및 filtering을 위해 계속 사용할 수 있습니다.

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

Session option에는 warm-up frames/seconds, sample interval, maximum samples, reset-on-scene-load, scene-load ignore window가 포함됩니다.

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

Provider exception은 unavailable custom metric snapshot으로 보고되며 core metric collection을 중단하지 않습니다.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw diagnostics는 명시적인 diagnostic mode이며 GPU work를 추가할 수 있습니다. HDRP에서는 이 API들이 HDRP heatmap output을 약속하지 않고 overdraw와 heatmap의 unsupported state를 안전하게 보고합니다.
