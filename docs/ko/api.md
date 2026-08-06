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

## Self-Observability And Overhead Budgets

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability는 고정 120-frame window에서 CPU callback cost를 low-overhead로 측정합니다. Average는 invocation 기준입니다. 전체 state는 `NotInitialized`, `Collecting`, `Ready`이고 component state는 `NotMeasured`, `Collecting`, `Ready`, `Unsupported`입니다.

Component는 `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration`, `HdrpRenderIntegration`입니다. 각 component는 window/invocation count, average/maximum CPU milliseconds, total/average allocated bytes, budget 및 `NotEvaluated`/`WithinBudget`/`Exceeded` state를 노출합니다.

| Component | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU self-timing은 명시적으로 `Unavailable`입니다. 이 diagnostics는 기존 CPU/GPU metrics에서 overhead를 빼거나 값을 조정하지 않습니다.

## Dynamic Profiler Metric Catalog

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` 및 `GetProfilerMetricCapabilities()`는 cache된 catalog를 읽습니다. Catalog state는 `NotInitialized`, `Ready`, `Error`이며, 각 capability는 `Unavailable`, `AvailableNoSample`, `AvailableSampled`를 보고하고 `Resolution`은 `None`, `Exact`, `Alias` provenance를 나타냅니다. Discovery는 runtime startup 및 명시적 refresh/reconfigure에서만 수행되며 steady-state collection 중에는 수행되지 않습니다. 기존 numeric metrics는 compatibility values로 유지되므로 availability의 authoritative signal로 capability의 `SampleState`/`IsAvailable`를 사용합니다.

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
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled`의 기본값은 `true`이며 structured alert의 `Debug.Log` 출력만 제어합니다. `false`로 설정해도 `AlertFired` callback, 최신 alert와 alert history, overlay warning, Editor warning log, session은 비활성화되지 않습니다. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)`는 Editor warning log를 독립적으로 제어합니다.

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

Coordinator는 active request 하나만 허용하며 `PreRoll`, `Capturing`, `PostRoll`, `Completed`를 deterministic하게 진행합니다. 같은 active ID를 반복하면 idempotent이고, 다른 active ID는 overlap으로 reject됩니다. `Canceled`, `Unavailable`, `Error`는 명시적인 terminal state입니다.

내장 backend는 Unity의 experimental `ExternalGPUProfiler`를 Editor 또는 Development Build에서, external tool이 attach된 경우에만, 지원되는 desktop platform/API 조합에 대해 wrap합니다. 지원 조합은 Windows/Linux desktop의 Direct3D 11, Direct3D 12 또는 Vulkan에서 `RenderDoc`, Windows desktop의 Direct3D 12에서 `PIX`입니다. Unity는 attached tool identity를 노출하지 않으므로 `RenderDoc` 또는 `Pix`를 명시적으로 선택해야 합니다. `Status.Tool`은 요청한 tool만 나타내며 attached tool의 verified identity가 아닙니다. `Completed`는 Unity wrapper lifecycle만 확인하며 external `.rdc`/`.wpix` artifact 또는 artifact path를 검증하거나 반환하지 않습니다. Automated tests는 fake backend를 사용하고, real external tool 및 artifact 확인은 release gate로 남습니다.

`PerfMeterCaptureOptions`의 기본값은 `captureFrames: 1`, `preRollFrames: 0`, `postRollFrames: 0`입니다. 유효한 `RequestCapture`는 runtime을 자동으로 시작합니다. ID 없이 `CancelCapture()`를 호출하면 현재 보고된 active request를 취소하며, ID를 전달하면 더 새로운 request를 취소하지 않도록 ownership을 보호합니다.

`PerfMeterCaptureBundleOptions` overload는 capture samples를 baseline session과 분리하고 opt-in screenshot을 포함할 수 있습니다. `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady` 이후 `PerformanceMeter.ExportCaptureBundle(captureId)`는 `Temp/PerfMeter/CaptureBundles` 아래에 SHA-256 manifest, samples, alerts, context, optional screenshot, external artifact metadata가 있는 versioned bundle을 atomic하게 생성합니다. project-local `.rdc`/`.wpix`는 observed artifact일 뿐 authoritative하지 않습니다. traversal, reparse point, project 외부 file은 reject됩니다.

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
