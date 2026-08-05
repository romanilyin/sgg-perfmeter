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

- Marker는 collection/frame timing(`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), provider(`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture(`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`), JSON/CSV export(`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`) 범위를 기록합니다. `SGG.PerfMeter.Thermal.Sample`은 reserved internal provider hook입니다.
- Counter는 CPU/GPU frame time(`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`)을 nanoseconds 단위의 end-of-frame gauge로 기록합니다. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive`, `SGG.PerfMeter.Thermal.Available`은 availability/active를 `0`/`1`로 인코딩하고, `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState`는 enum code를 사용하며, `SGG.PerfMeter.Provider.CustomMetricCount`는 count입니다. Counter는 `Scripts` category와 `FlushOnEndOfFrame`을 사용합니다.
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
