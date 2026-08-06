# Workflows

## Runtime Overlay

Use the overlay when you need immediate in-game visibility.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

The overlay uses UI Toolkit and does not intercept gameplay input. It supports FPS-only, compact text, graph, full diagnostics, metric bars, visual themes, module filters, CPU/GPU graphs, CPU core widgets, and limited custom metric rows.

PerfMeter creates and owns a versioned UI Toolkit host for the overlay: Unity `6000.4` uses `UIDocument`, while Unity `6000.5+` uses `PanelRenderer`. The owned host is separate from foreign UI and preserves foreign panel settings and children; rebuilds remove only the PerfMeter-owned container.

## Background Collection

Use background mode for tests, device runs, or agent workflows where visible UI is not needed.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording And Export

Use sessions for repeatable profiling windows.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session exports include timing, FPS lows, spikes, bottleneck counts, render counters, memory counters, overdraw state, warning/counter availability, scene summaries, worst frames, device metadata, camera metadata, settings metadata, and custom metrics.

## Alerts

Rules can report budget violations, low FPS, unavailable GPU timing, and overdraw thresholds.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
PerfMeterAlertHistorySnapshot history = PerformanceMeter.GetAlertHistory();
```

Editor warnings are throttled by cooldowns and can be disabled through JSON settings or runtime controls. Structured alert logs and Editor warnings are independent: `PerformanceMeter.SetStructuredLogsEnabled(false)` suppresses only structured alert `Debug.Log` output, while `PerformanceMeter.SetEditorWarningLogsEnabled(false)` controls Editor warning logs. Callbacks, alert/history data, overlay warnings, and sessions remain active.

Alert history identifies its interval and reset reason and separates lifecycle, steady-state, and explicit capture firings. PerfMeter cannot infer an external screenshot from a slow frame. Wrap known capture work with `PerformanceMeter.BeginAlertCapture(captureId)` and `PerformanceMeter.EndAlertCapture(captureId)` when authoritative capture provenance is required.

## External GPU Capture

Use the capture coordinator for a bounded RenderDoc or PIX request when the tool is already attached:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30),
    new PerfMeterCaptureBundleOptions(includeScreenshot: true));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
PerfMeterCaptureBundleStatusSnapshot bundle = PerformanceMeter.GetCaptureBundleStatus("gpu-spike");
```

Only one request can own the coordinator. Pre-roll and post-roll count Unity frames; only `Capturing` opens the alert capture scope and invokes Unity's experimental `ExternalGPUProfiler`. RenderDoc is allowed on Windows/Linux desktop with Direct3D 11, Direct3D 12, or Vulkan. PIX is allowed on Windows desktop with Direct3D 12. The Editor/Development Build and attached-tool gates are mandatory.

`Completed` means the guarded begin/end lifecycle finished. Unity does not expose the attached tool identity or authoritative artifact path through this API, so `Status.Tool` is only the requested tool and the `.rdc`/`.wpix` artifact must be verified in the external tool.

The bundle overload excludes capture frames from normal baseline session evidence and correlates both sample sets with capture alerts and context. When `bundle.IsExportReady`, `PerformanceMeter.ExportCaptureBundle("gpu-spike")` atomically creates a project-local versioned bundle under `Temp/PerfMeter/CaptureBundles`. Screenshots are opt-in and explicitly unavailable in batch mode or outside Play Mode. A caller-supplied external artifact is only an observed, hashed copy; it is not authoritative because Unity cannot authenticate its source or capture association. Equivalent MCP commands are `perfmeter.capture.request/status/cancel/export/capabilities`.

## Overdraw Diagnostics

Numerical overdraw is opt-in and bounded.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Numerical overdraw and heatmap use the URP Render Graph diagnostic path. Overdraw measurement requires `PerfMeterRenderGraphFeature`, replacement shader support, fragment UAV/storage-buffer support, compute shader support, a supported graphics API, and async GPU readback. HDRP reports overdraw and heatmap as unsupported, while core overlay, session, API, and MCP diagnostics remain available. Unsupported targets report `OverdrawState.Unsupported` instead of running the pass.

## Camera And Device Reproducibility

Use snapshots to preserve the environment that produced a performance capture.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Session exports include device and camera metadata so a capture can be understood or reproduced later.

## Custom Metrics

Register project-specific providers without forking PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics are exposed through API reads, session JSON export, MCP latest metrics, and up to eight overlay rows when the `CustomMetrics` module is enabled.

## Unity Profiler Instrumentation

The instrumentation is internal and visible only while profiling the Editor, a Development Build, or another profiler-enabled build. Non-profiler Release players treat these markers/counters as no-ops and produce no instrumentation data; public API, status, MCP, and export schemas are unchanged.

- Markers cover collection/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`), and JSON/CSV export (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` is a reserved internal provider hook.
- Counters cover CPU/GPU frame times (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) as end-of-frame gauges in nanoseconds. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive`, and `SGG.PerfMeter.Thermal.Available` encode availability/active state as `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState`, and `SGG.PerfMeter.Capture.State` use enum codes; `SGG.PerfMeter.Provider.CustomMetricCount` is a count. Counters use the `Scripts` category and `FlushOnEndOfFrame`.
- No synthetic thermal sample is emitted; `SGG.PerfMeter.Thermal.Available` remains `0`/unavailable until a real platform provider supplies data.

## Self-Observability And Overhead Budgets

Use `PerformanceMeter.GetSelfOverhead()` or `PerformanceMeter.GetStatus().SelfOverhead` to inspect diagnostic CPU callback cost and allocations for collector, custom providers, CPU-core provider, overlay, and URP/HDRP integration. Measurements use fixed 120-frame windows, per-invocation averages, and component-specific CPU/allocation budgets.

The inactive render integration reports `Unsupported`; a supported component without calls reports `NotMeasured`; GPU self-timing reports `Unavailable`. Accounting is diagnostic only: PerfMeter does not subtract overhead from or otherwise adjust existing CPU/GPU metrics.

## Agent Automation

A typical MCP-driven run:

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

`perfmeter.profiler.capabilities {}` is a cached read; it does not start the runtime or perform discovery.

## Optional Memory Snapshot Workflow

1. Use Unity `6000.4+` and install `com.unity.memoryprofiler` `1.1.0+` through Package Manager. The optional `SGG.PerfMeter.MemoryProfiler` assembly then auto-registers; projects without this package keep the core integration unavailable.
2. In Play Mode, read `PerformanceMeter.GetMemorySnapshotCapabilities()` or `perfmeter.memory.snapshot.capabilities` and confirm that the backend and requested flags are available.
3. Request a manual snapshot with `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))`, or configure `ConfigureMemorySnapshotTriggers(...)` for an explicitly enabled system-memory threshold or bounded leak-growth window.
4. Poll `GetMemorySnapshotStatus()` or `perfmeter.memory.snapshot.status` until the snapshot and its correlated bundle reach a terminal state. Export ready evidence with `PerformanceMeter.ExportCaptureBundle(captureId)` or `perfmeter.capture.export`.

Memory-only evidence is written through the existing capture-bundle API under `Temp/PerfMeter/CaptureBundles`. The bundle records `MemoryProfiler` as the requested tool, includes memory-snapshot provenance and a streaming SHA-256 for the `.snap`, and does not include an external GPU artifact. The source is owned under `Temp/PerfMeter/MemorySnapshots`; a successful export consumes it once.

## Graphics Marker Diagnostics

1. Call `PerformanceMeter.GetGraphicsDiagnostics()` or `perfmeter.graphics.diagnostics` to read the latest marker values and graphics API context.
2. Check each capability's `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, resolved/sampled component counts, and catalog revision. Discovery is dynamic: it occurs at runtime startup and explicit profiler-catalog refresh/reconfigure.
3. Treat values as raw recorder values in their discovered units. A marker may be unavailable, available without a sample, or sampled; a numeric zero is not a universal unavailable signal and the value is not guaranteed to be a shader or PSO count.

The shader marker resolves exact `Shader.CreateGPUProgram` before the aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram`, and `Shader.DynamicLoadGPUProgram`. The pipeline marker resolves exact `CreatePSO.Job`. The same values and provenance are available through `perfmeter.metrics.latest` and session JSON/CSV.

## Graphics-State Trace And Prewarm

1. On Unity `6000.4+`, ensure the optional `SGG.PerfMeter.GraphicsStateCollection` assembly is available. It uses the experimental `UnityEngine.Experimental.Rendering.GraphicsStateCollection` namespace on Unity `6000.4` and the `UnityEngine.Rendering.GraphicsStateCollection` namespace on Unity `6000.5+`.
2. Start a PerfMeter session before requesting the trace. Use `PerformanceMeter.StartSession(...)`, then call `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` or the matching MCP request. The request is rejected without an active session, and the session must remain recording through trace completion; `PerformanceMeter.StopSession()` cancels an active trace.
3. Keep the scenario running while the bounded trace advances. In normal Play Mode each trace frame is ticked after `WaitForEndOfFrame`; in batch mode the coordinator uses a next-frame fallback. Session samples admitted during this interval carry `GraphicsStateTraceId`/`graphics_state_trace_id`; session sampling settings determine how many correlated samples are retained.
4. Poll `GetGraphicsStateCollectionStatus()` or `perfmeter.graphics.state_collection.status` until `Completed`, then stop the session if desired. Stopping while the trace is active cancels it and can leave `IsBusy`/`is_busy` true while owned cleanup is retried. The owned `.graphicsstate` artifact is project-relative below `Temp/PerfMeter/GraphicsStateCollections` and is limited to 64 MiB.
5. Pass the reported owned relative path to `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` or the MCP prewarm command. Prewarm is synchronous, preserves the artifact, and reports completed warmups and `IsWarmedUp`; progressive warmup can finish with an explicit incomplete warning.

The graphics-state coordinator allows one flight at a time and also rejects overlap with active external GPU capture, memory snapshot, or alert-capture work. A repeated active trace ID is `AlreadyActive`; another ID is `RejectedOverlap`. `CancelGraphicsStateTrace` only cancels a matching active/preparing trace and cleans its pending artifact. A failed owned-artifact deletion leaves `HasPendingCleanup`/`has_pending_cleanup` true, persists an adjacent `.delete-pending` sidecar, and is restored and retried after domain reload; `IsBusy`/`is_busy` and the warning remain visible until cleanup succeeds. The Unity backend does not support cache-miss tracing, so no cache-miss evidence is available.
