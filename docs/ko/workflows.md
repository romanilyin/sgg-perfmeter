# 워크플로

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

Coordinator는 active request 하나만 소유하며 `PreRoll`, `Capturing`, `PostRoll`, `Completed`를 deterministic하게 진행합니다. 같은 active ID는 idempotent이고 다른 ID는 overlap으로 reject됩니다. Pre-roll과 post-roll은 Unity frame을 세며, `Capturing`만 alert capture scope를 열고 Unity의 experimental `ExternalGPUProfiler`를 invoke합니다. Editor 또는 Development Build이고 attached tool이 있어야 하는 gate가 필수입니다. `RenderDoc`은 Windows/Linux desktop의 Direct3D 11, Direct3D 12, Vulkan에서 허용되고, `PIX`는 Windows desktop의 Direct3D 12에서 허용됩니다.

`Completed`는 guarded Unity wrapper lifecycle이 끝났다는 의미뿐입니다. Unity는 attached tool identity나 authoritative artifact path를 노출하지 않으므로 `Status.Tool`은 요청한 tool만 나타냅니다. `PerfMeterCaptureBundleOptions` overload는 baseline/capture samples를 분리하고 project-local bundle을 atomic export합니다. external artifact는 observed일 뿐 authoritative하지 않습니다. automation에는 `perfmeter.capture.request/status/cancel/export/capabilities`를 사용합니다.

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

Inactive render integration은 `Unsupported`, 호출되지 않은 supported component는 `NotMeasured`, GPU self-timing은 `Unavailable`입니다. Accounting은 diagnostics 전용이며 PerfMeter는 기존 CPU/GPU metrics에서 overhead를 빼거나 값을 조정하지 않습니다.

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
