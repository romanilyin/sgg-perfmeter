# 워크플로

## FTUE 설정 및 연속 작업

`SGG/Perfmeter/Setup`을 열고 **FTUE** 탭을 선택합니다. 필수 검사는 compatibility, render integration, Frame Timing Stats, package path, 로드된 settings JSON을 확인합니다. 선택 항목은 설치하거나 건너뛸 수 있으며, 설치된 항목은 workflow가 완료되었다고 조용히 주장하는 대신 다음 action을 표시합니다.

### Memory Profiler

`com.unity.memoryprofiler`를 설치하면, 관리되는 폴더가 존재할 때 **Memory Profiler** 행에서 **Open Window/Analysis/Memory Profiler**, **Copy RequestMemorySnapshot Snippet**, **Copy Memory Trigger Snippet**, **Open Runtime**, **Reveal Snapshots**를 제공합니다. 복사된 snippet은 project가 호출해야 하는 runtime code입니다. FTUE가 직접 snapshot을 요청하거나 trigger를 설정하지는 않습니다. One-shot `.snap` 파일은 `Temp/PerfMeter/MemorySnapshots` 아래에 staging됩니다. 이후 request 또는 runtime cleanup으로 관리되는 source가 제거되기 전에 결과를 열거나 복사하십시오.

One-shot snippet:

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

Opt-in trigger snippet:

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

**Open Runtime**으로 capability/status snapshot을 확인합니다. 수동 capture가 기본값이며, trigger threshold는 명시적으로 설정할 때까지 비활성화되어 있습니다.

### Profile Analyzer

설치된 **Profile Analyzer** 행은 **Open Profile Analyzer**와 **Open Runtime**을 제공합니다. 먼저 Unity Profiler에서 recording을 시작한 다음, 그 recording 안에서 PerfMeter session을 시작하고 중지합니다. opener는 `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()`을 사용해 Profile Analyzer를 열고 session ID를 복사합니다. 기록된 Profiler data를 로드하고 해당 ID를 검색하십시오. Profile Analyzer 설치, Profiler data 로드 또는 filter 자동 적용은 수행하지 않습니다.

### Adaptive Performance

설치된 **Adaptive Performance** 행은 optional telemetry provider의 현재 status를 확인하기 위한 **Open Runtime**을 제공합니다. FTUE action은 session을 시작하거나 capture하지 않습니다.

### RenderDoc

RenderDoc은 external tool이며 PerfMeter에 포함되지 않습니다. Unity 공식 integration flow를 따르십시오.

1. 공식 download page에서 RenderDoc을 설치합니다: <https://renderdoc.org/builds>.
2. project 변경 사항을 저장한 뒤 Game View 또는 Scene View tab menu에서 **Load RenderDoc**을 사용합니다. 또는 RenderDoc을 통해 Unity Editor나 Development Build를 시작할 수 있습니다. 설치 후 Unity가 attachment를 노출하지 않으면 Unity를 재시작하십시오. 공식 Unity guide는 <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html>입니다.
3. FTUE에서 **Check Attachment**를 클릭합니다. 이 동작은 Unity의 shared external-profiler signal만 refresh합니다. FTUE는 RenderDoc 설치를 감지할 수 없으며 Unity도 이 signal만으로 RenderDoc과 PIX를 구분할 수 없습니다.
4. **Copy Capture Snippet**을 클릭하고 Play Mode에 들어간 뒤 복사한 code를 project runtime code에서 invoke합니다.

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions("ftue-renderdoc-capture", PerfMeterCaptureTool.RenderDoc, 1));
   ```

5. Windows x64 Editor에서는 먼저 **Download Verified Bridge** 또는 **Install Local Bridge**를 사용할 수 있습니다. exact pinned 별도 bridge만 Editor-only plugin으로 설치되며 RenderDoc 자체는 설치하지 않습니다. 이후 Editor를 restart합니다. 복사된 native request는 `NativeRequired` + `Copy`를 사용하며 MetadataOnly는 `DoNotShare`, Copy/Embed는 `ReviewBeforeShare`입니다.

### GraphicsStateCollection

번들된 optional **GraphicsStateCollection** 행에는 package install이 필요하지 않습니다. **Open Runtime**, **Copy Trace Snippet**, **Copy Prewarm Snippet**, **Reveal Artifacts**를 제공합니다. FTUE는 trace나 prewarm을 자동으로 request하지 않습니다. 다음 순서를 사용하십시오.

1. Play Mode에서 `PerformanceMeter.StartSession(...)`으로 recording 중인 PerfMeter session을 시작하고 유지합니다.
2. 복사한 trace code를 project runtime code에서 invoke합니다.

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. `State == PerfMeterGraphicsStateCollectionState.Completed`가 될 때까지 `PerformanceMeter.GetGraphicsStateCollectionStatus()`를 poll합니다. `ArtifactRelativePath`를 prewarm input으로 사용합니다. 이 path는 `Temp/PerfMeter/GraphicsStateCollections` 아래를 가리킵니다. tracing 중 session을 중지하면 trace가 취소됩니다.
4. 복사한 prewarm snippet의 `<trace-artifact-file>`을 반환된 path로 교체합니다.

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. trace 후 **Reveal Artifacts**를 클릭해 project-local artifact folder를 표시합니다. Prewarm은 synchronous이며 artifact를 보존하고 incomplete progressive warmup을 보고할 수 있습니다. Trace length는 600 frames, 관리되는 artifact는 64 MiB로 제한됩니다. Unity backend는 cache-miss evidence를 제공하지 않습니다.

## 전체 초기화 Bootstrap

**Setup > Initialization Code**에서 **Refresh from Project Settings**를 클릭한 다음 **Copy Init Code**를 클릭합니다. 생성된 `PerfMeterBootstrap`은 완전히 normalized된 project settings snapshot을 포함하고 scene load 후 `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)`을 호출합니다. overlay, logging, alert, session-default, overdraw settings를 전달하고 `enabled` 및 `collectionMode: Stopped`를 준수하며 `StartSession` 또는 capture request를 수행하지 않습니다.

code-owned startup을 선호한다면 Resources zero-code settings path 대신 이 explicit bootstrap을 사용합니다. 둘 다 있으면 성공적으로 parse된 explicit call이 current domain의 Resources auto-start callback을 억제합니다. Resources가 먼저 시작된 경우 explicit snapshot이 이후 적용되어 authoritative가 됩니다. Invalid explicit JSON은 current runtime을 변경하지 않고 이후 Resources auto-start도 억제하지 않습니다. Session 및 default overdraw operation은 active explicit runtime snapshot을 사용합니다.

## Runtime Overlay

게임 안에서 즉시 볼 수 있는 정보가 필요할 때 overlay를 사용합니다.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

overlay는 UI Toolkit을 사용하며 gameplay input을 가로채지 않습니다. FPS-only, compact text, graph, full diagnostics, metric bars, visual themes, module filters, CPU/GPU graphs, CPU core widgets, 제한된 custom metric rows를 지원합니다.

PerfMeter는 overlay를 위해 versioned UI Toolkit host를 생성하고 소유합니다. Unity `6000.4`에서는 `UIDocument`, Unity `6000.5+`에서는 `PanelRenderer`를 사용합니다. 이 owned host는 foreign UI와 분리되며 foreign UI의 panel settings와 children을 보존합니다. rebuild에서는 PerfMeter가 소유한 container만 제거합니다.

## Background Collection

보이는 UI가 필요 없는 test, device run, agent workflow에는 background mode를 사용합니다.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording 및 Export

반복 가능한 profiling window에는 session을 사용합니다.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session export에는 timing, FPS lows, spikes, bottleneck counts, render counters, memory counters, overdraw state, warning/counter availability, scene summaries, worst frames, device metadata, camera metadata, settings metadata, custom metrics가 포함됩니다.

## Alerts

rule은 budget violation, low FPS, unavailable GPU timing, overdraw threshold를 보고할 수 있습니다.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warning은 cooldown으로 throttled되며 JSON settings 또는 runtime control을 통해 비활성화할 수 있습니다. Structured alert log와 Editor warning은 서로 독립적입니다. `PerformanceMeter.SetStructuredLogsEnabled(false)`는 structured alert의 `Debug.Log` 출력만 억제하고, `PerformanceMeter.SetEditorWarningLogsEnabled(false)`는 Editor warning log를 별도로 제어합니다. callback, alert/history, overlay warning, session은 계속 활성 상태입니다.

## External GPU Capture

tool이 이미 attach된 경우 제한된 RenderDoc 또는 PIX request에는 capture coordinator를 사용합니다.

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

`GenericUnity`는 기존 `ExternalGPUProfiler` matrix를 유지하며 tool/artifact를 authenticate할 수 없습니다. `NativePreferred`는 begin 전까지만 fallback할 수 있고 `NativeRequired`는 fallback하지 않습니다. Native RenderDoc은 Windows x64 Unity Editor의 D3D11, D3D12, Vulkan만 지원합니다.

Generic `Completed`는 wrapper lifecycle만 의미합니다. Native status는 backend kind와 generation-bound phase를 보고하고 finalized `.rdc`를 authenticate할 수 있습니다. Generic/caller artifact는 observed로 유지됩니다. MCP는 `backend_mode`를 받지만 storage mode는 C# API에서 선택합니다.

## Overdraw Diagnostics

numerical overdraw는 opt-in이며 범위가 제한됩니다.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Numerical overdraw와 heatmap은 URP Render Graph diagnostic path를 사용합니다. Overdraw measurement에는 `PerfMeterRenderGraphFeature`, replacement shader support, fragment UAV/storage-buffer support, compute shader support, supported graphics API, async GPU readback이 필요합니다. HDRP는 overdraw/heatmap을 unsupported로 보고하지만 core overlay, session, API, MCP diagnostics는 계속 사용할 수 있습니다. 지원되지 않는 target은 pass를 실행하지 않고 `OverdrawState.Unsupported`를 보고합니다.

## Camera 및 Device 재현성

성능 capture가 생성된 환경을 보존하려면 snapshot을 사용합니다.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Session export에는 device 및 camera metadata가 포함되어 capture를 나중에 이해하거나 재현할 수 있습니다.

## Custom Metrics

PerfMeter를 fork하지 않고 project-specific provider를 등록합니다.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics는 API reads, session JSON export, MCP latest metrics, 그리고 `CustomMetrics` module이 활성화된 경우 최대 8개의 overlay row를 통해 노출됩니다.

## Unity Profiler Instrumentation

이 instrumentation은 internal이며 Editor, Development Build 또는 다른 profiler-enabled build를 profiling할 때만 Unity Profiler에 표시됩니다. Profiler가 없는 Release player에서는 marker/counter가 no-op이고 instrumentation data를 생성하지 않습니다. public API, status, MCP, export schema는 변경되지 않습니다.

- Marker는 collection/frame timing(`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), provider(`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture(`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`), JSON/CSV export(`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`) 범위를 기록합니다. `SGG.PerfMeter.Thermal.Sample`은 reserved internal provider hook입니다.
- Counter는 CPU/GPU frame time(`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`)을 nanoseconds 단위의 end-of-frame gauge로 기록합니다. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive`, `SGG.PerfMeter.Thermal.Available`은 availability/active를 `0`/`1`로 인코딩하고, `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState`, `SGG.PerfMeter.Capture.State`는 enum code를 사용하며, `SGG.PerfMeter.Provider.CustomMetricCount`는 count입니다. Counter는 `Scripts` category와 `FlushOnEndOfFrame`을 사용합니다.
- synthetic thermal sample은 생성되지 않습니다. `SGG.PerfMeter.Thermal.Available`은 `0`/unavailable 상태로 real platform provider가 data를 공급할 때까지 사용할 수 없습니다.

## Self-Observability And Overhead Budgets

`PerformanceMeter.GetSelfOverhead()` 또는 `PerformanceMeter.GetStatus().SelfOverhead`로 collector, custom providers, CPU-core provider, overlay, URP/HDRP integration의 CPU callback cost와 allocation을 진단합니다. 고정 120-frame window, invocation 기준 average, component별 CPU/allocation budget을 사용합니다.

정확한 session/capture receipt에는 `PerformanceMeter.GetSelfOverheadWindow(kind, identity)`를 사용합니다. 결과에는 epoch, frame containment, quality/pipeline/renderer identity, feature installed/enabled/enqueued evidence, callback/invocation bounds, typed inactive reason이 포함됩니다. Capture/session JSON과 MCP status는 동일한 window identity를 유지하며 stale live data를 첨부하지 않고 `CaptureWindowMismatch` 또는 `UnknownInactiveReason`으로 fail closed합니다.

URP value는 package-owned CPU-side `RecordRenderGraph()` registration과 current-thread allocation만 포함합니다. 여러 camera에서는 callback frame보다 많은 invocation이 발생할 수 있습니다. GPU attribution은 명시적으로 `Unavailable`이고 whole-frame CPU/GPU/hitch/GC는 별도 context로 유지됩니다. Accounting은 diagnostics 전용이며 PerfMeter는 기존 CPU/GPU metrics에서 overhead를 빼거나 값을 조정하지 않습니다.

## Agent Automation

일반적인 MCP 기반 run은 다음과 같습니다.

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

`perfmeter.profiler.capabilities {}`는 cache된 state를 읽기만 하며 runtime을 시작하거나 discovery를 수행하지 않습니다.

## 선택적 메모리 스냅샷 workflow

1. Unity `6000.4+`를 사용하고 Package Manager에서 `com.unity.memoryprofiler` `1.1.0+`를 install합니다. 선택적 `SGG.PerfMeter.MemoryProfiler` assembly가 backend를 자동 등록합니다. 이 package가 없으면 core integration은 unavailable 상태입니다.
2. Play Mode에서 `PerformanceMeter.GetMemorySnapshotCapabilities()` 또는 `perfmeter.memory.snapshot.capabilities`를 읽고 backend와 필요한 capture flags를 확인합니다.
3. `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))`로 manual snapshot을 request하거나 `ConfigureMemorySnapshotTriggers(...)`로 system-memory threshold 또는 bounded leak-growth window를 명시적으로 enable합니다.
4. `GetMemorySnapshotStatus()` 또는 `perfmeter.memory.snapshot.status`를 읽어 snapshot과 correlated bundle이 terminal state가 될 때까지 기다립니다. 준비된 evidence는 `PerformanceMeter.ExportCaptureBundle(captureId)` 또는 `perfmeter.capture.export`로 export합니다.

memory-only evidence는 기존 capture-bundle API를 통해 `Temp/PerfMeter/CaptureBundles` 아래에 기록됩니다. bundle은 requested tool로 `MemoryProfiler`를 기록하고 메모리 provenance 및 `.snap`의 streaming SHA-256을 포함하지만 external GPU artifact는 포함하지 않습니다. owned source는 `Temp/PerfMeter/MemorySnapshots` 아래에 있으며 성공한 export에서 한 번만 소비됩니다.

## Graphics marker diagnostics

1. `PerformanceMeter.GetGraphicsDiagnostics()` 또는 `perfmeter.graphics.diagnostics`를 호출해 최신 marker value와 graphics API context를 읽습니다.
2. 각 capability의 `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, resolved/sampled component count, catalog revision을 확인합니다. discovery는 동적이며 runtime start와 명시적 profiler catalog refresh/reconfigure에서 수행됩니다.
3. 값은 발견된 unit의 raw recorder value로 취급합니다. marker는 unavailable, sample 없음 상태의 available, sampled 중 하나일 수 있으며 numeric 0은 universal unavailable signal이 아닙니다. shader/PSO count도 보장되지 않습니다.

shader marker는 exact `Shader.CreateGPUProgram`을 먼저 해석하고 aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram`, `Shader.DynamicLoadGPUProgram`을 이어서 사용합니다. pipeline marker는 exact `CreatePSO.Job`을 해석합니다. 동일한 value와 provenance는 `perfmeter.metrics.latest`와 session JSON/CSV에도 제공됩니다.

## Profile Analyzer 세션 상관관계

Profiler 기록 중 각 session은 순간적인 `SGG.PerfMeter.Session.<sessionId>.Begin` 및 `.End` sample을 생성합니다. `SGG/Perfmeter/Open Profile Analyzer For Session`은 optional Profile Analyzer window를 열고 current session ID를 clipboard에 복사합니다. 이 command는 Profile Analyzer를 설치하거나 Profiler data를 로드하거나 filter를 자동 적용하지 않습니다. 관련 capture를 로드한 뒤 복사된 ID를 검색하십시오.

## 세션 분석 창

`SGG/Perfmeter/Session Analysis`을 열면 Editor 메모리에 있는 current session을 read-only로 확인할 수 있습니다. virtualized tab은 retained sample timeline, 사용 가능한 sample detail을 포함한 authoritative worst frame, derived CPU-main/CPU-render/GPU budget violation, authoritative whole-run/current-scene scope를 표시합니다. CPU-main은 present wait를 제외하며 GPU value와 violation에는 명시적인 GPU timing availability가 필요합니다.

이 window는 `GetSessionSummary()`와 `GetSessionSamples()`만 읽고 runtime을 시작하지 않습니다. 사용할 수 없는 timing은 숫자 0이 아니라 `Unavailable`로 표시됩니다. stopped session은 runtime instance가 존재하는 동안 표시되며 `PerformanceMeter.Stop()`, domain reload 또는 Play Mode 종료 시 메모리 session이 제거될 수 있습니다.

## GraphicsStateCollection trace 및 prewarm

1. Unity `6000.4+`에서 optional `SGG.PerfMeter.GraphicsStateCollection` assembly가 사용 가능한지 확인합니다. Unity `6000.4`에서는 `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, Unity `6000.5+`에서는 `UnityEngine.Rendering.GraphicsStateCollection` namespace를 사용합니다.
2. trace 전에 PerfMeter session을 시작합니다. `StartSession(...)` 후 `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` 또는 해당 MCP request를 실행합니다. active session이 없으면 request가 reject되며, trace가 끝날 때까지 session은 recording 상태여야 합니다. `PerformanceMeter.StopSession()`은 active trace를 cancel합니다.
3. bounded trace가 진행되는 동안 scenario를 실행합니다. 일반 Play Mode에서는 각 trace frame이 `WaitForEndOfFrame` 후 tick되고, batch mode에서는 coordinator가 next-frame fallback을 사용합니다. 이 구간에 admitted된 session sample에는 `GraphicsStateTraceId`/`graphics_state_trace_id`가 기록되고 session settings가 보존할 correlated sample 수를 결정합니다.
4. `GetGraphicsStateCollectionStatus()` 또는 `perfmeter.graphics.state_collection.status`가 `Completed`가 될 때까지 poll하고 필요하면 session을 stop합니다. active trace 중 stop하면 trace가 cancel되고 owned cleanup retry 동안 `IsBusy`/`is_busy`가 true로 남을 수 있습니다. owned `.graphicsstate` artifact는 project-relative `Temp/PerfMeter/GraphicsStateCollections` 아래에 있으며 64 MiB로 제한됩니다.
5. status가 반환한 owned relative path를 `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` 또는 MCP prewarm command에 전달합니다. prewarm은 synchronous하고 artifact를 보존하며 completed warmup과 `IsWarmedUp`를 보고합니다. progressive warmup은 explicit incomplete warning과 함께 끝날 수 있습니다.

graphics-state coordinator는 하나의 flight만 허용하며 active external GPU capture, memory snapshot, alert-capture와의 overlap도 reject합니다. 같은 active trace ID는 `AlreadyActive`, 다른 ID는 `RejectedOverlap`입니다. `CancelGraphicsStateTrace`는 일치하는 active/preparing trace만 cancel하고 pending artifact를 cleanup합니다. owned artifact 삭제에 실패하면 `HasPendingCleanup`/`has_pending_cleanup`이 true로 남고 인접한 `.delete-pending` sidecar가 domain reload 후 복원·재시도됩니다. `IsBusy`/`is_busy`와 warning은 성공할 때까지 표시됩니다. Unity backend는 cache-miss tracing을 지원하지 않으므로 cache-miss evidence는 없습니다.

## Render integration context

pipeline에 중립적인 최신 typed render integration을 읽으려면 neutral snapshot을 사용합니다.

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

같은 데이터는 MCP로도 읽을 수 있습니다.

```text
perfmeter.render.snapshot {}
```

이 read들은 runtime collection을 시작하지 않습니다. `State`, `ObservationAgeFrames`, `LastObservedFrame`, `ObservationMatchesCurrentPipeline`을 함께 확인하십시오. pipeline이나 asset configuration이 바뀌면 이전 observation은 stale이 됩니다. warning과 non-match를 유지하고 pass, mode, GRD, VRS 값을 current frame 값으로 취급하지 마십시오. legacy API `PerformanceMeter.GetRenderGraphSnapshot()`과 `perfmeter.rendergraph.snapshot`은 계속 사용할 수 있습니다.

GRD 진단에서는 `DegradedReason`, SRP support, project configuration, compute support, URP mode compatibility, `ActivityAvailability`를 확인합니다. `IsObservedActive`는 Unity의 global enabled state입니다. `Effectiveness`는 aggregate BRG workload context로만 사용하십시오. `AvailableNoSample`/`Unavailable`은 workload 0을 뜻하지 않으며 positive BRG counter도 특정 renderer의 GRD 사용을 증명하지 않습니다.

capture bundle의 schema `sgg.perfmeter.capture-context` version `1`은 기존 `render`를 유지하고 `render_integration`을 추가합니다. external GPU capture에서는 `Capturing` phase의 첫 sample에서 context를 freeze하고, Memory Profiler bundle에서는 memory request 완료 시 기록합니다. session JSON/CSV schema는 변경되지 않습니다. public API에 안정적인 RenderGraph/CustomPass viewer나 pass target이 없으므로 이 workflow는 Editor navigation을 약속하지 않습니다.
