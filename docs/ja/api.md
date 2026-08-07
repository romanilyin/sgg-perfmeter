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

## Editor Compatibility Status

Editor API `PerfMeterSetupActions.GetCompatibilityStatus()` は `PerfMeterCompatibilityStatus` を返し、Unity `2022.3` package floor の `ImportCompatible`、supported runtime Unity `6000.4+` の `CoreRuntimeCompatible`、available adapter を持つ active URP/HDRP `17.4+` の `RenderIntegrationCompatible` を分離します。各結果には reason があります。render compatibility は renderer assets の設定完了を意味しないため、configuration readiness には setup status を使用します。

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

組み込み backend は Unity の experimental な `ExternalGPUProfiler` を、Editor または Development Build で、external tool が attach 済みの場合に限り、対応する desktop platform/API の組み合わせで wrap します。対応する組み合わせは、Windows/Linux desktop の Direct3D 11、Direct3D 12、Vulkan 上の `RenderDoc` と、Windows desktop の Direct3D 12 上の `PIX` です。Unity は attach された tool の identity を公開しないため、`RenderDoc` または `Pix` を明示的に選択してください。`Status.Tool` は requested tool だけを示し、attached tool の verified identity ではありません。`Completed` は Unity wrapper lifecycle の完了だけを確認し、external `.rdc`/`.wpix` artifact や artifact path の存在を検証・返却しません。automated tests は fake backend を使用し、real external tool と artifact の確認は release gate のままです。

`PerfMeterCaptureOptions` の default は `captureFrames: 1`、`preRollFrames: 0`、`postRollFrames: 0` です。有効な `RequestCapture` は runtime を自動的に開始します。ID なしの `CancelCapture()` は現在報告されている active request を対象にし、ID を渡すと新しい request を誤って cancel することを防げます。

`PerfMeterCaptureBundleOptions` overload は capture samples を baseline session から分離し、opt-in screenshot を含められます。`PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady` の後、`PerformanceMeter.ExportCaptureBundle(captureId)` は `Temp/PerfMeter/CaptureBundles` に SHA-256 manifest、samples、alerts、context、optional screenshot、external artifact metadata を持つ versioned bundle を atomic に作成します。project-local `.rdc`/`.wpix` は observed artifact にすぎず authoritative ではありません。traversal、reparse point、project 外の file は reject されます。

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

## オプションのメモリスナップショット

メモリスナップショットはオプションの統合機能です。Unity `6000.4+` で `com.unity.memoryprofiler` `1.1.0+` を解決すると、分離された `SGG.PerfMeter.MemoryProfiler` assembly が有効になり、`MemoryProfiler` backend を自動登録します。core assembly に hard dependency はありません。

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

公開 API は `RegisterMemorySnapshotBackend(...)`、`UnregisterMemorySnapshotBackend(...)`、`GetMemorySnapshotCapabilities()`、`GetMemorySnapshotStatus()`、`RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`、`ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)`、`GetMemorySnapshotTriggers()` です。独自 backend は `IPerfMeterMemorySnapshotBackend` を実装します。オプション assembly は Unity Memory Profiler backend を提供します。

`PerfMeterMemorySnapshotOptions` の既定値は managed/native object flags、最低 1 GiB の空き容量、300 秒の cooldown です。`RequestMemorySnapshot` は既定で manual capture を行い、`Started`、`AlreadyActive`、`RejectedOverlap`、`Cooldown`、`Unavailable`、`InsufficientDiskSpace`、`InvalidRequest`、`Failed` などの明示的な結果を返します。read API は runtime を起動せず、有効な request は起動します。

`ConfigureMemorySnapshotTriggers` で system-memory threshold と bounded leak-growth heuristic を明示的に opt-in できます。`GetMemorySnapshotTriggers()` の既定値は disabled です。trigger による request にも manual request と同じ single-flight、cooldown、空き容量、capture-flag の guard が適用されます。

## グラフィックス診断と GraphicsStateCollection

Graphics diagnostics は既存の snapshot に情報を追加します。`PerformanceMeter.GetGraphicsDiagnostics()` は shader GPU-program creation と graphics-pipeline creation marker の最新値、graphics API context、parallel PSO capability、profiler metric catalog revision を返します。

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

catalog は runtime start 時と明示的な refresh/reconfigure 時に Unity `ProfilerRecorder` descriptor を discovery します。shader semantic は exact name `Shader.CreateGPUProgram` と aliases `Shader.CreateGPUPrograms`、`Shader.CompileGPUProgram`、`Shader.DynamicLoadGPUProgram` を使用します。graphics-pipeline semantic は exact name `CreatePSO.Job` を使用します。各 capability には `Resolution`（`None`、`Exact`、`Alias`）、`ResolvedRecorderNames`、`Category`、検出された `Unit`、`DataType`、`ResolvedComponentCount`、`SampledComponentCount` が保持されます。`PerfMeterMetricsSnapshot` と session JSON/CSV にも同じ marker value、capability metadata、catalog revision が含まれます。

marker availability は動的です。`SampleState`（`Unavailable`、`AvailableNoSample`、`AvailableSampled`）と capability metadata を使って判断してください。値が zero でも marker がないとは限りません。値は recorder の raw value で、検出された unit を保持します。shader count や PSO count とは限らず、共通 unit への変換も行いません。

optional の `SGG.PerfMeter.GraphicsStateCollection` assembly は Unity `6000.4+` を対象とし、利用可能な場合に Unity backend を登録します。Unity `6000.4` では `UnityEngine.Experimental.Rendering.GraphicsStateCollection`、Unity `6000.5+` では `UnityEngine.Rendering.GraphicsStateCollection` を使用します。core assembly はこの backend に依存しません。

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

public state-collection API は `RegisterGraphicsStateCollectionBackend(...)`、`UnregisterGraphicsStateCollectionBackend(...)`、`GetGraphicsStateCollectionCapabilities()`、`GetGraphicsStateCollectionStatus()`、`RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`、`PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)`、`CancelGraphicsStateTrace(string captureId)` です。custom backend は `IPerfMeterGraphicsStateCollectionBackend` を実装し、trace/prewarm、cache-miss、parallel-PSO capability を報告します。

`PerfMeterGraphicsStateTraceOptions` には空でない `CaptureId` が必要で、1–600 trace frames を受け付けます。既定値は 60 frames と最低 1 GiB の free disk です。trace は PerfMeter session が recording 中の場合だけ有効です。correlated session sample には active capture ID が `GraphicsStateTraceId`（export では `graphics_state_trace_id`）として入り、session の sampling 設定は trace frame 数ではなく correlated sample の密度を決めます。

`PerfMeterGraphicsStateCollectionStatusSnapshot` は `IsBusy` と `HasPendingCleanup` を公開します。`IsBusy` は preparation、trace、trace の終了、prewarm、cleanup、または persisted pending cleanup の間 true です。`HasPendingCleanup` は cleanup retry を待つ owned artifact を明示します。active trace 中に `PerformanceMeter.StopSession()` を呼ぶと trace は cancel されるため、trace 完了まで session は recording を続ける必要があります。owned artifact の削除に失敗すると、隣接する owned `.delete-pending` sidecar marker が作られ、domain reload 後に復元されて cleanup が再試行されます。artifact と marker が消えるまで status は visible かつ busy のままです。

coordinator は一度に一つの graphics-state flight だけを許可します。同じ active ID は `AlreadyActive`、準備中・trace 中・終了中・cleanup 中、または別の capture domain で別の trace/prewarm を行うと `RejectedOverlap` です。`CancelGraphicsStateTrace` は一致する active/preparing ID だけを対象にし、backend を cancel して pending owned artifact を削除します。cleanup failure は表示され、再試行が成功するまで置き換えを妨げる場合があります。

`PerfMeterGraphicsStatePrewarmOptions` は owned project-relative `.graphicsstate` path と、0–1,000,000 の任意の `MaxStateCount` を受け付けます。prewarm は synchronous に実行され、artifact を保持し、`CompletedWarmupCount` と `IsWarmedUp` を報告します。successful でも incomplete な progressive warmup には warning が付きます。`TraceCacheMisses` は拡張 backend のために存在しますが、Unity backend は cache-miss evidence をサポートしないため、指定すると `Unavailable` になります。

## Render integration context

integration-neutral な additive snapshot は次の両方の method から取得できます。

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` は `RenderPipeline`、`RenderPipelineAssetSource`、`LastObservedFrame`、`ObservationAgeFrames`、`ObservationMatchesCurrentPipeline`、`ObservedCameraEntityId`、`ObservedCameraName`、`ObservedCameraType`、`IntegrationId`、`IntegrationName`、`IntegrationVersion`、`PassKind`、`PassName`、`InjectionPoint`、`PerfMeterPassCount`、`EffectiveRenderingMode`、`GpuResidentDrawer`、`VariableRateShading`、`LegacyRenderGraph`、`Warning` を公開します。nested GRD/VRS snapshot は availability、configuration/support field、activity availability、warning を持ちます。

read は runtime start 前でも安全で、collection を開始しません。supported な current pipeline は `State = NotObserved` のまま `Available` になる場合があります。最後の observation が別の pipeline configuration に属する場合、`ObservationMatchesCurrentPipeline` は `false` になり、frame/age と warning に stale 状態が明示されます。stale な field を current observation として扱わないでください。

URP は public な current-frame `UniversalRenderingData.renderingMode` と、その frame で実際に schedule された PerfMeter pass を報告します。HDRP は実際に観測された PerfMeter `CustomPass` を報告しますが、effective rendering mode は利用できません。`GpuResidentDrawer` は configured mode、SRP/project/compute support、URP frame の Forward+ と clustered-mode compatibility、`IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` による global runtime activity を報告します。HDRP の Forward+/rendering-mode field は `Unknown` のままです。`VariableRateShading` は `SystemInfo`/`ShadingRateInfo` の authoritative hardware support を報告します。

`LegacyRenderGraph` は `GetRenderGraphSnapshot()` のための embedded compatibility facade です。private/internal な pass/resource reflection は削除され、legacy counter は `-1` のままです。安定した Unity public API は RenderGraph/CustomPass viewer や pass target も公開しないため、この API は Editor navigation を提供・約束しません。

`GpuResidentDrawer` はさらに `ProjectConfigurationAvailability`、`IsProjectConfigurationSupported`、`ComputeShaderAvailability`、`SupportsComputeShaders`、`ForwardPlusActivityAvailability`、`IsObservedForwardPlusActive`、`RenderingModeCompatibilityAvailability`、`IsRenderingModeCompatible`、`ActivitySource`、`DegradedReason`、`Effectiveness` を持ちます。`PerfMeterGpuResidentDrawerReason` は structured fallback state を示します。`PerfMeterGpuResidentDrawerEffectivenessSnapshot` は BRG draw call/instance と Profiler capability provenance を保持し、未 sample 値は C# で `-1`、JSON で `null` です。これは BatchRendererGroup aggregate counter であり、renderer ごとの authoritative GRD evidence ではありません。

## セッション相関

`PerformanceMeter.GetSessionSummary().SessionId` は小文字の 32 文字 hexadecimal identifier です。`StartSession` で作成され、`StopSession` 後も変わらず、新しい session の開始時に変更され、session がない場合は空です。session JSON は同じ値を root の `session_id` として公開します。CSV は既存 column の位置を保つため最後に `session_id` column を追加し、`perfmeter.session.summary` も `session_id` を返します。
