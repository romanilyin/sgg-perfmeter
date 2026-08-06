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

## Self-Observability And Overhead Budgets

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability は、固定 120-frame window で CPU callback cost を low-overhead に計測します。average は invocation 単位です。全体 state は `NotInitialized`、`Collecting`、`Ready`、component state は `NotMeasured`、`Collecting`、`Ready`、`Unsupported` です。

Components は `Collector`、`CustomMetricProviders`、`CpuCoreProvider`、`Overlay`、`UrpRenderIntegration`、`HdrpRenderIntegration` です。各 component は window/invocation count、average/maximum CPU milliseconds、total/average allocated bytes、budget、`NotEvaluated`/`WithinBudget`/`Exceeded` state を公開します。

| Component | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU self-timing は明示的に `Unavailable` です。これらの diagnostics は既存の CPU/GPU metrics から overhead を差し引かず、値を補正しません。

## Dynamic Profiler Metric Catalog

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` と `GetProfilerMetricCapabilities()` は cache 済み catalog を読み取ります。catalog state は `NotInitialized`、`Ready`、`Error` です。各 capability は `Unavailable`、`AvailableNoSample`、`AvailableSampled` を示し、`Resolution` は provenance `None`、`Exact`、`Alias` を示します。Discovery は runtime startup と明示的な refresh/reconfigure のときだけ実行され、steady-state collection 中には実行されません。既存の numeric metrics は compatibility values として残るため、availability の authoritative signal には capability の `SampleState`/`IsAvailable` を使用してください。

## Structured Snapshots

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Device snapshots には Unity/platform/OS/CPU/GPU/API/display/window/support information が含まれます。Camera snapshots には scene、transform、projection、clipping、pixel rect、target display、利用可能な場合の URP/HDRP camera settings が含まれます。

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
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` のデフォルトは `true` で、構造化 alert の `Debug.Log` 出力だけを制御します。`false` にしても `AlertFired` callback、最新の alerts と alert history、overlay warnings、Editor warning logs、sessions は無効になりません。`PerformanceMeter.SetEditorWarningLogsEnabled(bool)` は Editor warning logs を独立して制御します。

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

coordinator は active request を 1 件だけ許可し、`PreRoll`、`Capturing`、`PostRoll`、`Completed` を deterministic に進みます。同じ active ID の再実行は idempotent で、異なる active ID は overlap として reject されます。`Canceled`、`Unavailable`、`Error` は明示的な terminal state です。

組み込み backend は Unity の experimental な `ExternalGPUProfiler` を、Editor または Development Build で、external tool が attach 済みの場合に限り、対応する desktop platform/API の組み合わせで wrap します。対応する組み合わせは、Windows/Linux desktop の Direct3D 11、Direct3D 12、Vulkan 上の `RenderDoc` と、Windows desktop の Direct3D 12 上の `PIX` です。Unity は attach された tool の identity を公開しないため、`RenderDoc` または `Pix` を明示的に選択してください。`Status.Tool` は requested tool だけを示し、attached tool の verified identity ではありません。`Completed` は Unity wrapper lifecycle の完了だけを確認し、external `.rdc`/`.wpix` artifact や artifact path の存在を検証・返却しません。automated tests は fake backend を使用し、real external tool と artifact の確認は release gate のままです。Capture bundles、artifact provenance、MCP capture control は別の future scope です。

`PerfMeterCaptureOptions` の default は `captureFrames: 1`、`preRollFrames: 0`、`postRollFrames: 0` です。有効な `RequestCapture` は runtime を自動的に開始します。ID なしの `CancelCapture()` は現在報告されている active request を対象にし、ID を渡すと新しい request を誤って cancel することを防げます。

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

Overdraw diagnostics は明示的な diagnostic modes であり、GPU work を追加する場合があります。HDRP では、これらの APIs は HDRP heatmap output を約束せず、overdraw と heatmap の unsupported state を安全に返します。
