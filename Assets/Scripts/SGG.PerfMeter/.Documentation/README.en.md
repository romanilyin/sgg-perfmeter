# SGG PerfMeter

SGG PerfMeter exposes a public runtime API for safely reading profiler status and the latest performance metrics without scraping UI or Unity Console output.

## Agent API

- Namespace: `SGG.PerfMeter`
- Status: `PerfMeter.GetStatus()` or `PerfMeter.TryGetStatus(out PerfMeterStatusSnapshot status)`
- Metrics: `PerfMeter.GetLatestMetrics()` or `PerfMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)`
- Lifecycle: `PerfMeter.EnsureRunning()` and `PerfMeter.Stop()`
- Overlay: `PerfMeter.SetOverlayVisible(bool visible)`, `PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerfMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerfMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerfMeter.IsOverlayVisible`, `PerfMeter.OverlayCorner`, `PerfMeter.OverlayMode`, `PerfMeter.TargetFps`, and status snapshot fields `OverlayVisible` / `OverlayCorner` / `OverlayMode` / `TargetFps`
- Overdraw: `PerfMeter.RequestOverdrawMeasurement(int frameCount = 60)` and `PerfMeter.CancelOverdrawMeasurement()`

Queries are safe before the runtime is started: normal reads return a snapshot with `State = Stopped` and should not throw exceptions.

## Setup Window

Open `SGG/Perfmeter/Setup` to prepare the project without editing URP renderer assets by hand.

- `Project Settings` shows `Frame Timing Stats` status and can enable the Player Setting with `Enable Frame Timing`.
- `URP Renderer Features` lists discovered URP renderer assets with installed/missing status and can add `PerfMeterRenderGraphFeature` to all missing renderers or only selected renderers without creating duplicates.
- `Initialization Code` shows the runtime overlay bootstrap; `Overlay Visible`, `Target FPS`, `Overlay Corner`, and `Overlay Mode` options immediately update the code copied by `Copy Init Code`.
- The `Runtime` tab is for Play Mode: buttons are disabled in Edit Mode, and in Play Mode they switch target FPS, overlay mode/corner, show or hide the overlay, and start a short overdraw measurement.

The same actions are available to agents and Editor scripts without opening the window through the public Editor API:

```csharp
using SGG.PerfMeter.Editor.Setup;
using UnityEngine;

Debug.Log(PerfMeterSetupActions.GetStatusReport());
Debug.Log(PerfMeterSetupActions.RunRecommendedSetup());
Debug.Log(PerfMeterSetupActions.CopyInitializationSnippetToClipboard());
```

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
	{
		PerfMeter.EnsureRunning();
		PerfMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
		PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
		PerfMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
		PerfMeter.SetOverlayVisible(true);
	}
}
```

## Runtime Metrics

The runtime singleton updates snapshots in `Update()` with real values from `FrameTimingManager` and `ProfilerRecorder`. The metric collection path avoids PerfMeter-side per-frame allocations; the overlay text refresh is throttled and still rebuilds managed strings at the refresh interval.

- Timings: `CpuFrameTimeMs`, `CpuMainThreadFrameTimeMs`, `CpuRenderThreadFrameTimeMs`, `CpuMainThreadPresentWaitTimeMs`, `GpuFrameTimeMs`.
- FPS stats: `AverageFps`, `OnePercentLowFps`, `PointOnePercentLowFps`, `FrameSampleCount`, `GpuValidSampleCount`, `FrameSpikeCount`, and `SevereFrameSpikeCount`; the source is `FrameTiming.cpuFrameTime`, not `Time.deltaTime`.
- Render counters: `DrawCalls`, `SetPassCalls`, `Batches`, `Vertices`, `SrpBatcherInstances`.
- BRG/GRD counters when available: `BrgDrawCalls`, `BrgInstances`, `IndexBufferUploadInFrameBytes` through `PerfMeterStatusSnapshot.AvailableCounters` / `UnavailableCounters`.
- Memory counters: `SystemUsedMemoryBytes`, `GcReservedMemoryBytes`, `GpuMemoryBytes`.
- Classification: `Bottleneck` (`GpuBound`, `CpuMainThreadBound`, `CpuRenderThreadBound`, `PresentLimited`, `Balanced`, `Unknown`) with the current `FrameBudgetMs`, derived from `PerfMeterTargetFps`; `PresentLimited` indicates significant present/VSync/frame pacing wait while CPU/GPU work is below budget.
- Numerical overdraw measurement: `OverdrawState`, `OverdrawProgress`, and `OverdrawRatio` are available in status/metrics snapshots for agent-readable measurement control.

On OpenGL/OpenGLES, hardware GPU timing may be unavailable or unreliable. In that case `GpuFrameTimeAvailable` is `false` and `PerfMeterStatusSnapshot.Warning` contains a warning; Vulkan is preferred on Android.

## Overlay

The runtime overlay is built programmatically with UI Toolkit (`UIDocument`, `PanelSettings`, `VisualElement`, `Label`). It is created only while the application is playing; EditMode API calls remain safe and do not create a visible UI document.

- Layout is absolute with `PickingMode.Ignore`, so it does not intercept game input; in graph modes the overlay is split into a wider graph block on top and a narrower text block below.
- The overlay defaults to the top-right corner (`TopRight`) and `Full` mode; available corners are `TopLeft`, `TopRight`, `BottomLeft`, and `BottomRight`.
- Modes: `FpsOnly` shows one FPS/1%/0.1% line, `TextCompact` shows a compact text summary, `Graphs` shows FPS plus CPU/GPU graphs, and `Full` adds render/memory/overdraw counters.
- The label is refreshed at most 4 times per second from the latest runtime snapshots, not every frame.
- Full zero-allocation overlay refresh is backlog work: split the text block into stable field labels, cache enum strings, use custom numeric formatting into reusable buffers, and update only changed labels instead of rebuilding text with `StringBuilder`.
- Graphs are drawn through UI Toolkit `generateVisualContent`; the CPU graph uses stacked areas for `render`, `main`, and the remainder up to `frame`, while `frame` is drawn as the upper boundary without summing `frame + main + render`.
- The right side of the graphs shows colored label badges for `frame`, `other`, `main`, `render`, and `gpu` with current, average, worst 1%, and worst 0.1% timings; numbers use fixed width relative to the current graph scale/maximum.
- If GPU timing is temporarily unavailable, the GPU badge turns gray, the current value uses an underscore placeholder, and averages/history use valid samples only.
- Target FPS is selected from `15/30/60/90/120/144/240`; the matching `FrameBudgetMs` drives the red target line shown left of the graph and the scale formula `max(averageTimeMs * 1.1, FrameBudgetMs * 1.2)`.
- Text modes show current/min/max over the overlay's internal history window for timings, render counters, and memory.
- Warnings are held briefly so transient GPU timing gaps do not blink on every refresh.
- In `Full`, visible fields include state, bottleneck, FPS/lows/spikes, CPU/GPU timings, draw calls, SetPass, batches, vertices, SRP/BRG counters, index uploads, overdraw state/progress/ratio, memory, and warning text when present.
- `PerfMeter.SetOverlayVisible(false)` hides the retained UI without stopping metric collection; `PerfMeter.SetOverlayVisible(true)` ensures the runtime is running and shows the overlay in Play Mode.
- `PerfMeter.IsOverlayVisible` and `PerfMeterStatusSnapshot.OverlayVisible` report the actual visible overlay state.

## URP Render Graph Renderer Feature

`PerfMeterRenderGraphFeature` adds a URP 17 Render Graph marker pass with a dedicated profiling sampler named `SGG.PerfMeter.Overlay` and an opt-in pass for numerical overdraw measurement. The overlay marker does not render heavy graphics; it is reserved for future `ProfilerRecorder`-based measurement and subtraction of the tool cost.

- Preferred installation: `SGG/Perfmeter/Setup` -> select missing renderers or use `Install All Missing`.
- Manual installation: add the feature to the renderer asset, for example `Assets/Settings/PC_Renderer.asset` for PC or `Assets/Settings/Mobile_Renderer.asset` for Mobile.
- In the renderer asset Inspector, choose `Add Renderer Feature` -> `Perf Meter Render Graph Feature`.
- Keep `Enabled` on, leave `Render Pass Event` at the default `AfterRenderingPostProcessing`, or switch it to `AfterRendering` if the marker must run after the whole frame.
- `Marker Name` defaults to `SGG.PerfMeter.Overlay`; change it only if downstream `ProfilerRecorder` code will look for another marker name.
- The feature uses `RecordRenderGraph(RenderGraph, ContextContainer)` and `AddRasterRenderPass`; no legacy-only path is used.

## Overdraw Measurement

Overdraw measurement is opt-in and bounded. Call `PerfMeter.RequestOverdrawMeasurement()` to request the default 60-frame measurement window, or pass a custom positive frame count. Call `PerfMeter.CancelOverdrawMeasurement()` to stop the request early.

- The runtime is `Off` by default and does not record the overdraw pass until a request is active.
- `PerfMeterRenderGraphFeature` must be added to the active URP renderer. During an active request it records a Render Graph raster pass with the profiling marker `SGG.PerfMeter.Overdraw`.
- The pass redraws the scene renderer list with the hidden replacement shader `Hidden/SGG/PerfMeter/OverdrawCounter`: `ZTest Always`, `ZWrite Off`, `ColorMask 0`.
- The fragment shader atomically increments a GPU `GraphicsBuffer`, then a readback pass uses `AsyncGPUReadback` without a CPU stall.
- `OverdrawRatio` is computed as `TotalFragments / RenderedCameraPixels` and averaged across completed readback samples.
- The shader requires fragment UAV/storage buffer support; on OpenGL ES or other limited backends the measurement can enter `Error` with a warning.
- Visual heatmap output is not implemented yet.
- Progress advances after async readback scheduled from the Render Graph overdraw pass. If progress stays at `0`, verify that the renderer feature is installed on the renderer used by the active camera and that the target backend supports shader instrumentation.

```csharp
using SGG.PerfMeter;

PerfMeter.EnsureRunning();
PerfMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerfMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
PerfMeter.SetOverlayVisible(true);
PerfMeter.RequestOverdrawMeasurement();

if (PerfMeter.TryGetStatus(out PerfMeterStatusSnapshot status))
{
	UnityEngine.Debug.Log($"PerfMeter: {status.State}, overdraw {status.OverdrawState} {status.OverdrawProgress:P0}, {status.Warning}");
}

if (PerfMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics))
{
	UnityEngine.Debug.Log($"GPU {metrics.GpuFrameTimeMs:0.00} ms, Draws {metrics.DrawCalls}, Overdraw {metrics.OverdrawRatio:0.00}");
}
```

## License

This package is licensed under **Stinger Royalty-Free EULA 1.0**.

- Authoritative version: Russian text in [`LICENSE.ru.md`](../LICENSE.ru.md).
- English convenience text: [`LICENSE.md`](../LICENSE.md), provided for readability.
- Project notices: [`NOTICE.md`](../NOTICE.md) and [`NOTICE.ru.md`](../NOTICE.ru.md).
- SPDX identifier: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.
- Licensor: ROMAN ILYIN.
- Canonical repository: https://github.com/romanilyin/sgg-perfmeter.

Free for personal, internal, open, and commercial End Products. Royalty-free. Standalone sale, resale, paid redistribution, or standalone commercialization of this Asset or Derivative Assets is prohibited.

The Russian EULA is the primary and controlling version. If the Russian and English versions conflict, differ, or are interpreted differently, the Russian version controls.
