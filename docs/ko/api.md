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

## Editor Compatibility Status

Editor API `PerfMeterSetupActions.GetCompatibilityStatus()`는 `PerfMeterCompatibilityStatus`를 반환하며 Unity `2022.3` package floor의 `ImportCompatible`, supported runtime Unity `6000.4+`의 `CoreRuntimeCompatible`, available adapter가 있는 active URP/HDRP `17.4+`의 `RenderIntegrationCompatible`를 분리합니다. 각 결과에는 reason이 있습니다. render compatibility는 renderer assets 설정 완료를 뜻하지 않으므로 configuration readiness에는 setup status를 사용합니다.

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

## 선택적 메모리 스냅샷

메모리 스냅샷은 선택적 통합 기능입니다. Unity `6000.4+`에서 `com.unity.memoryprofiler` `1.1.0+`를 resolve하면 별도 `SGG.PerfMeter.MemoryProfiler` assembly가 활성화되고 `MemoryProfiler` backend를 자동 등록합니다. core assembly에는 hard dependency가 없습니다.

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

공개 surface는 `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)`, `GetMemorySnapshotTriggers()`입니다. 사용자 정의 backend는 `IPerfMeterMemorySnapshotBackend`를 구현하며 선택적 assembly가 Unity Memory Profiler backend를 제공합니다.

`PerfMeterMemorySnapshotOptions`의 기본값은 managed/native object flags, 최소 1 GiB free disk, 300초 cooldown입니다. `RequestMemorySnapshot`은 기본적으로 manual capture이며 `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest`, `Failed` 같은 명시적 결과를 반환합니다. read는 runtime을 시작하지 않지만 유효한 request는 runtime을 시작합니다.

`ConfigureMemorySnapshotTriggers`로 system-memory threshold 및 bounded leak-growth heuristic를 명시적으로 opt-in할 수 있습니다. `GetMemorySnapshotTriggers()`의 기본 상태는 disabled입니다. trigger request에도 manual request와 동일한 single-flight, cooldown, free-space, capture-flag guard가 적용됩니다.

## 그래픽 진단 및 GraphicsStateCollection

그래픽 진단은 기존 snapshot에 정보를 추가합니다. `PerformanceMeter.GetGraphicsDiagnostics()`는 최신 shader GPU-program creation 및 graphics-pipeline creation marker 값, graphics API context, parallel PSO capability, profiler metric catalog revision을 반환합니다.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

catalog는 runtime start와 명시적 refresh/reconfigure 때 Unity `ProfilerRecorder` descriptor를 discovery합니다. shader semantic은 exact name `Shader.CreateGPUProgram`과 aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram`, `Shader.DynamicLoadGPUProgram`을 사용합니다. graphics-pipeline semantic은 exact name `CreatePSO.Job`을 사용합니다. 각 capability는 `Resolution`(`None`, `Exact`, `Alias`), `ResolvedRecorderNames`, `Category`, 발견된 `Unit`, `DataType`, `ResolvedComponentCount`, `SampledComponentCount`를 보존합니다. `PerfMeterMetricsSnapshot`과 session JSON/CSV에도 같은 marker value, capability metadata, catalog revision이 포함됩니다.

marker availability는 동적입니다. `SampleState`(`Unavailable`, `AvailableNoSample`, `AvailableSampled`)와 capability metadata를 사용해야 하며, 값이 0이라고 marker가 없다는 뜻은 아닙니다. 값은 recorder의 raw value이고 발견된 unit을 유지합니다. 항상 shader count나 PSO count인 것은 아니며 공통 unit으로 변환되지 않습니다.

선택적 `SGG.PerfMeter.GraphicsStateCollection` assembly는 Unity `6000.4+`를 대상으로 하며 사용 가능한 경우 Unity backend를 등록합니다. Unity `6000.4`에서는 `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, Unity `6000.5+`에서는 `UnityEngine.Rendering.GraphicsStateCollection` namespace를 사용합니다. core assembly는 이 backend와 독립적입니다.

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

public state-collection surface는 `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)`, `CancelGraphicsStateTrace(string captureId)`입니다. custom backend는 `IPerfMeterGraphicsStateCollectionBackend`를 구현하고 trace/prewarm, cache-miss, parallel-PSO capability를 보고합니다.

`PerfMeterGraphicsStateTraceOptions`에는 비어 있지 않은 `CaptureId`가 필요하고 1–600 trace frames를 받습니다. 기본값은 60 frames와 최소 1 GiB free disk입니다. trace는 PerfMeter session이 recording 중일 때만 유효합니다. correlated session sample에는 active capture ID가 `GraphicsStateTraceId`(export에서는 `graphics_state_trace_id`)로 들어가며, session sampling 설정은 trace frame 수가 아니라 correlated sample density를 결정합니다.

`PerfMeterGraphicsStateCollectionStatusSnapshot`은 `IsBusy`와 `HasPendingCleanup`을 제공합니다. `IsBusy`는 preparation, trace, trace 종료, prewarm, cleanup 또는 persisted pending cleanup 동안 true이고, `HasPendingCleanup`은 cleanup retry를 기다리는 owned artifact를 구체적으로 나타냅니다. active trace 중 `PerformanceMeter.StopSession()`을 호출하면 trace가 cancel되므로 trace가 끝날 때까지 session은 recording 상태를 유지해야 합니다. owned artifact 삭제가 실패하면 인접한 owned `.delete-pending` sidecar marker가 생성되고 domain reload 후 복원되어 cleanup이 재시도됩니다. artifact와 marker가 정리될 때까지 status는 visible하고 busy한 상태로 남습니다.

coordinator는 한 번에 하나의 graphics-state flight만 허용합니다. 같은 active ID는 `AlreadyActive`, preparation/trace/finalization/cleanup 중 또는 다른 capture domain에서 다른 trace/prewarm을 요청하면 `RejectedOverlap`입니다. `CancelGraphicsStateTrace`는 일치하는 active/preparing ID만 취소하고 backend를 cancel한 뒤 pending owned artifact를 삭제합니다. cleanup failure는 표시되며 재시도가 성공할 때까지 replacement를 막을 수 있습니다.

`PerfMeterGraphicsStatePrewarmOptions`는 owned project-relative `.graphicsstate` path와 0–1,000,000 범위의 선택적 `MaxStateCount`를 받습니다. prewarm은 synchronous하게 실행되고 artifact를 보존하며 `CompletedWarmupCount`와 `IsWarmedUp`를 보고합니다. 성공했지만 incomplete한 progressive warmup에는 warning이 포함됩니다. `TraceCacheMisses`는 확장 backend를 위해 존재하지만 Unity backend는 cache-miss evidence를 지원하지 않으므로 지정하면 `Unavailable`을 반환합니다.

## Render integration context

통합 방식에 중립적인 additive snapshot은 다음 두 method로 읽을 수 있습니다.

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot`은 `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph`, `Warning`을 제공합니다. 중첩된 GRD/VRS snapshot은 availability, configuration/support field, activity availability와 warning을 포함합니다.

read는 runtime 시작 전에도 안전하며 collection을 시작하지 않습니다. 지원되는 current pipeline은 `State = NotObserved`인 상태에서도 `Available`일 수 있습니다. 마지막 observation이 다른 pipeline configuration에 속하면 `ObservationMatchesCurrentPipeline`은 `false`가 되고 frame/age와 warning으로 stale 상태가 표시됩니다. stale field를 current observation으로 해석하지 마십시오.

URP는 public current-frame `UniversalRenderingData.renderingMode`와 해당 frame에 실제로 schedule된 PerfMeter pass를 보고합니다. HDRP는 실제로 관찰된 PerfMeter `CustomPass`를 보고하지만 effective rendering mode는 사용할 수 없습니다. `GpuResidentDrawer`는 configured mode, SRP/project/compute support, URP frame의 Forward+ 및 clustered-mode compatibility, `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()`의 global runtime activity를 보고합니다. HDRP의 Forward+/rendering-mode field는 `Unknown`으로 유지됩니다. `VariableRateShading`은 `SystemInfo`/`ShadingRateInfo`의 authoritative hardware support를 보고합니다.

`LegacyRenderGraph`는 `GetRenderGraphSnapshot()`을 위한 embedded compatibility facade입니다. private/internal pass/resource reflection은 제거되었으므로 legacy counter는 `-1`로 유지됩니다. 안정적인 Unity public API는 RenderGraph/CustomPass viewer나 pass target도 제공하지 않으므로 이 API는 Editor navigation을 제공하거나 약속하지 않습니다.

`GpuResidentDrawer`는 추가로 `ProjectConfigurationAvailability`, `IsProjectConfigurationSupported`, `ComputeShaderAvailability`, `SupportsComputeShaders`, `ForwardPlusActivityAvailability`, `IsObservedForwardPlusActive`, `RenderingModeCompatibilityAvailability`, `IsRenderingModeCompatible`, `ActivitySource`, `DegradedReason`, `Effectiveness`를 제공합니다. `PerfMeterGpuResidentDrawerReason`은 structured fallback state를 나타냅니다. `PerfMeterGpuResidentDrawerEffectivenessSnapshot`은 BRG draw call/instance와 Profiler capability provenance를 보존하며 sample이 없으면 C#에서 `-1`, JSON에서 `null`입니다. 이는 BatchRendererGroup aggregate counter이며 renderer별 authoritative GRD evidence가 아닙니다.
