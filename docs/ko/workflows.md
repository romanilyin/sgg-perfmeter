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

Editor warning은 cooldown으로 throttled되며 JSON settings 또는 runtime control을 통해 비활성화할 수 있습니다.

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

## Agent Automation

일반적인 MCP 기반 run은 다음과 같습니다.

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
