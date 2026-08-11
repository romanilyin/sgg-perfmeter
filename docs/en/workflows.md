# Workflows

## Setup FTUE And Continuations

Open `SGG/Perfmeter/Setup` and select the **FTUE** tab. Required checks cover compatibility, render integration, Frame Timing Stats, the package path, and a loaded settings JSON. Optional rows can be installed or skipped; an installed row exposes the next action instead of silently claiming that the workflow is complete.

### Memory Profiler

After `com.unity.memoryprofiler` is installed, the **Memory Profiler** row provides **Open Window/Analysis/Memory Profiler**, **Copy RequestMemorySnapshot Snippet**, **Copy Memory Trigger Snippet**, **Open Runtime**, and **Reveal Snapshots** after the owned folder exists. The copied snippets are runtime code that the project must invoke; FTUE does not request a snapshot or configure triggers itself. One-shot `.snap` files are staged below `Temp/PerfMeter/MemorySnapshots`; open or copy the result before a later request or runtime cleanup removes the owned source.

The one-shot snippet is:

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

The opt-in trigger snippet is:

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

Use **Open Runtime** to inspect the capability/status snapshot. Manual capture is the default; trigger thresholds remain disabled until explicitly configured.

### Profile Analyzer

The installed **Profile Analyzer** row provides **Open Profile Analyzer** and **Open Runtime**. Begin recording in Unity Profiler first, then start and stop a PerfMeter session inside that recording. The opener uses `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()` to open Profile Analyzer and copy the session ID; load the recorded Profiler data and search for that ID. It does not install Profile Analyzer, load Profiler data, or apply a filter automatically.

### Adaptive Performance

The installed **Adaptive Performance** row provides **Open Runtime** so the optional telemetry provider's current status can be inspected. The FTUE action does not start a session or capture.

### RenderDoc

RenderDoc is an external tool and is not bundled with PerfMeter. The UPM package also remains binary-free; its optional SGG bridge is a separately published artifact. Use this flow:

1. Install RenderDoc from the official download page: <https://renderdoc.org/builds>.
2. Save project changes, then use **Load RenderDoc** from the Game View or Scene View tab menu. Alternatively, launch the Unity Editor or a Development Build through RenderDoc; restart Unity if Unity does not expose the attachment after installation. The official Unity guide is <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html>.
3. On a Windows x64 Editor project, optionally click **Download Verified Bridge** or **Install Local Bridge**. The installer accepts only the release-pinned archive/DLL size, SHA-256, and native AMD64 PE contract, configures an Editor-only plugin, and never installs or loads RenderDoc. Restart the Editor after install/update; FTUE also exposes download cancel and managed removal.
4. Click **Check Attachment** in FTUE. This refreshes Unity's shared external-profiler signal only; FTUE cannot detect RenderDoc installation and Unity cannot identify RenderDoc versus PIX from that signal.
5. Click **Copy Capture Snippet**, enter Play Mode, and invoke the copied code from project runtime code:

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions(
           "ftue-renderdoc-capture",
           PerfMeterCaptureTool.RenderDoc,
           captureFrames: 1,
           preRollFrames: 0,
           postRollFrames: 0,
           backendMode: PerfMeterCaptureBackendMode.NativeRequired,
           externalArtifactStorageMode: PerfMeterExternalArtifactStorageMode.Copy));
   ```

6. Use **Open Runtime** for capture status. The native path is limited to Windows x64 Unity Editor with Direct3D 11, Direct3D 12, or Vulkan. `NativeRequired` fails closed; `NativePreferred` can fall back only before native begin; `GenericUnity` preserves the previous broader compatibility path. Native MetadataOnly defaults to `DoNotShare`; Copy/Embed data is sensitive and requires explicit review before sharing.

### GraphicsStateCollection

The bundled optional **GraphicsStateCollection** row needs no package install. It provides **Open Runtime**, **Copy Trace Snippet**, **Copy Prewarm Snippet**, and **Reveal Artifacts**. FTUE does not request a trace or prewarm automatically. Use this sequence:

1. In Play Mode, start and keep a PerfMeter session recording with `PerformanceMeter.StartSession(...)`.
2. Invoke the copied trace code from project runtime code:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. Poll `PerformanceMeter.GetGraphicsStateCollectionStatus()` until `State == PerfMeterGraphicsStateCollectionState.Completed`. Use its `ArtifactRelativePath`, which points below `Temp/PerfMeter/GraphicsStateCollections`, as the input to prewarm. Stopping the session while tracing cancels the trace.
4. Replace `<trace-artifact-file>` in the copied prewarm snippet with that returned path:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. Click **Reveal Artifacts** after a trace to reveal the project-local artifact folder. Prewarm is synchronous, preserves the artifact, and can report an incomplete progressive warmup. Trace length is limited to 600 frames and owned artifacts to 64 MiB; the Unity backend does not provide cache-miss evidence.

## Full Initialization Bootstrap

In **Setup > Initialization Code**, click **Refresh from Project Settings**, then **Copy Init Code**. The generated `PerfMeterBootstrap` embeds the complete normalized project settings snapshot and calls `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)` after scene load. It carries overlay, logging, alert, session-default, and overdraw settings, honors `enabled` and `collectionMode: Stopped`, and performs no `StartSession` or capture request.

Use this explicit bootstrap instead of the Resources zero-code settings path when code-owned startup is preferred. If both are present, a successfully parsed explicit call suppresses the Resources auto-start callback for the current domain; if Resources already started first, the explicit snapshot is applied afterward and becomes authoritative. Invalid explicit JSON leaves the current runtime unchanged and does not suppress a later Resources auto-start. Session and default overdraw operations use the active explicit runtime snapshot.

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

Use the capture coordinator for a bounded RenderDoc or PIX request when the tool is already attached. The five-argument constructor remains `GenericUnity`; select a native mode explicitly:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions(
        "gpu-spike",
        PerfMeterCaptureTool.RenderDoc,
        1,
        30,
        30,
        PerfMeterCaptureBackendMode.NativeRequired,
        PerfMeterExternalArtifactStorageMode.Copy),
    new PerfMeterCaptureBundleOptions(includeScreenshot: true));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
PerfMeterCaptureBundleStatusSnapshot bundle = PerformanceMeter.GetCaptureBundleStatus("gpu-spike");
```

Only one request can own the coordinator. `GenericUnity` invokes Unity's experimental `ExternalGPUProfiler` and retains its existing Editor/Development Build matrix: requested RenderDoc on Windows/Linux desktop with Direct3D 11, Direct3D 12, or Vulkan, and PIX on Windows desktop with Direct3D 12. The native path is separate and supports only the Windows x64 Unity Editor on Direct3D 11, Direct3D 12, or Vulkan.

For `GenericUnity`, `Completed` means only that the guarded begin/end lifecycle finished; Unity does not expose attached-tool identity or an authoritative artifact path. A successful native result reports `RequestedBackendMode`, `EffectiveBackendKind == RenderDocNative`, and generation-bound `NativePhase`, then authenticates the selected finalized `.rdc` through bridge index/time and stable file identity.

The bundle overload excludes capture frames from normal baseline session evidence and correlates both sample sets with capture alerts and context. When `bundle.IsExportReady`, `PerformanceMeter.ExportCaptureBundle("gpu-spike")` atomically creates a project-local versioned bundle under `Temp/PerfMeter/CaptureBundles`. Native MetadataOnly records authenticated metadata and defaults to `DoNotShare`; Copy retains a separately quota-managed project-local payload; Embed stages the payload into the atomic bundle. Copy/Embed require `ReviewBeforeShare`. Caller-supplied and generic artifacts remain observed and non-authoritative. Equivalent MCP commands are `perfmeter.capture.request/status/cancel/export/capabilities`.

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

## Profile Analyzer Session Correlation

While profiling, each session emits instantaneous `SGG.PerfMeter.Session.<sessionId>.Begin` and `.End` samples. Use `SGG/Perfmeter/Open Profile Analyzer For Session` to open the optional Profile Analyzer window and copy the current session ID to the clipboard. The command does not install Profile Analyzer, load Profiler data, or apply a filter automatically; search for the copied ID after loading the relevant capture.

## Session Analysis Window

Open `SGG/Perfmeter/Session Analysis` for a read-only Editor view of the current in-memory session. Its virtualized tabs show the retained sample timeline, the authoritative worst frame with retained-sample details when available, derived CPU-main/CPU-render/GPU budget violations, and the authoritative whole-run/current-scene scopes. CPU-main violations exclude present wait; GPU values and violations require explicit GPU timing availability.

The window reads only `GetSessionSummary()` and `GetSessionSamples()` and never starts the runtime. Unavailable timing is shown as `Unavailable`, not numeric zero. A stopped session remains visible while its runtime instance exists; `PerformanceMeter.Stop()`, a domain reload, or closing Play Mode can discard that in-memory session.

## Graphics-State Trace And Prewarm

1. On Unity `6000.4+`, ensure the optional `SGG.PerfMeter.GraphicsStateCollection` assembly is available. It uses the experimental `UnityEngine.Experimental.Rendering.GraphicsStateCollection` namespace on Unity `6000.4` and the `UnityEngine.Rendering.GraphicsStateCollection` namespace on Unity `6000.5+`.
2. Start a PerfMeter session before requesting the trace. Use `PerformanceMeter.StartSession(...)`, then call `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` or the matching MCP request. The request is rejected without an active session, and the session must remain recording through trace completion; `PerformanceMeter.StopSession()` cancels an active trace.
3. Keep the scenario running while the bounded trace advances. In normal Play Mode each trace frame is ticked after `WaitForEndOfFrame`; in batch mode the coordinator uses a next-frame fallback. Session samples admitted during this interval carry `GraphicsStateTraceId`/`graphics_state_trace_id`; session sampling settings determine how many correlated samples are retained.
4. Poll `GetGraphicsStateCollectionStatus()` or `perfmeter.graphics.state_collection.status` until `Completed`, then stop the session if desired. Stopping while the trace is active cancels it and can leave `IsBusy`/`is_busy` true while owned cleanup is retried. The owned `.graphicsstate` artifact is project-relative below `Temp/PerfMeter/GraphicsStateCollections` and is limited to 64 MiB.
5. Pass the reported owned relative path to `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` or the MCP prewarm command. Prewarm is synchronous, preserves the artifact, and reports completed warmups and `IsWarmedUp`; progressive warmup can finish with an explicit incomplete warning.

The graphics-state coordinator allows one flight at a time and also rejects overlap with active external GPU capture, memory snapshot, or alert-capture work. A repeated active trace ID is `AlreadyActive`; another ID is `RejectedOverlap`. `CancelGraphicsStateTrace` only cancels a matching active/preparing trace and cleans its pending artifact. A failed owned-artifact deletion leaves `HasPendingCleanup`/`has_pending_cleanup` true, persists an adjacent `.delete-pending` sidecar, and is restored and retried after domain reload; `IsBusy`/`is_busy` and the warning remain visible until cleanup succeeds. The Unity backend does not support cache-miss tracing, so no cache-miss evidence is available.

## Render Integration Context

Use the neutral snapshot when a tool needs one pipeline-independent view of the latest typed render integration:

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

Or read the same data without a C# consumer through:

```text
perfmeter.render.snapshot {}
```

These reads do not start runtime collection. Check `State`, `ObservationAgeFrames`, `LastObservedFrame`, and `ObservationMatchesCurrentPipeline` together. A changed pipeline or asset configuration makes the previous observation stale; keep the explicit warning and do not treat its pass, mode, GRD, or VRS values as current. The legacy `PerformanceMeter.GetRenderGraphSnapshot()` API and `perfmeter.rendergraph.snapshot` command remain available.

For GRD diagnosis, check `DegradedReason` in order with SRP support, project configuration, compute support, URP rendering-mode compatibility, and `ActivityAvailability`. Treat `IsObservedActive` as Unity's global enabled state. Use `Effectiveness` only as aggregate BRG workload context; `AvailableNoSample` and `Unavailable` are not zero workload, and positive BRG counters do not prove that a specific renderer used GRD.

For a capture bundle, schema `sgg.perfmeter.capture-context` version `1` preserves the existing `render` object and adds `render_integration`. External GPU capture freezes that context on the first `Capturing` sample; a Memory Profiler bundle records it when the memory request completes. Session JSON/CSV schemas are unchanged. The public API provides no stable RenderGraph/CustomPass viewer or pass targets, so this workflow does not promise Editor navigation.
