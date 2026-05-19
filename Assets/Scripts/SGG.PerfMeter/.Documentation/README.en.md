# SGG PerfMeter

SGG PerfMeter exposes a public runtime API for safely reading profiler status and the latest performance metrics without scraping UI or Unity Console output.

Current package version: `2026.5.18-1`. This is a private release candidate; the repository remains private until the public switch is explicitly approved.

## Agent API

- Namespace: `SGG.PerfMeter`
- Status: `PerformanceMeter.GetStatus()` or `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)`
- Metrics: `PerformanceMeter.GetLatestMetrics()` or `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)`
- Device/environment: `PerformanceMeter.GetDeviceInfo()` or `PerformanceMeter.TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)` returns a Unity/platform/CPU/GPU/screen/monitor snapshot without starting the runtime.
- Camera snapshot: `PerformanceMeter.GetCameraSnapshot(...)` or `PerformanceMeter.TryGetCameraSnapshot(out PerfMeterCameraSnapshot snapshot, ...)` returns camera position, orientation, and parameters for reproducible performance captures.
- Settings: `PerformanceMeter.GetSettings()` returns the zero-code JSON settings snapshot or safe defaults when the JSON file is missing.
- Lifecycle: `PerformanceMeter.EnsureRunning()` and `PerformanceMeter.Stop()`
- Overlay: `PerformanceMeter.SetOverlayVisible(bool visible)`, `PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset preset)`, `PerformanceMeter.SetOverlayModules(PerfMeterOverlayModule modules)`, `PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)`, `PerformanceMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerformanceMeter.IsOverlayVisible`, `PerformanceMeter.OverlayCorner`, `PerformanceMeter.OverlayMode`, `PerformanceMeter.OverlayPreset`, `PerformanceMeter.OverlayModules`, `PerformanceMeter.TargetFps`, and status snapshot fields `OverlayVisible` / `OverlayCorner` / `OverlayMode` / `OverlayPreset` / `OverlayModules` / `TargetFps`
- Overdraw: `PerformanceMeter.RequestOverdrawMeasurement(int frameCount = 60)`, `PerformanceMeter.CancelOverdrawMeasurement()`, `PerformanceMeter.SetOverdrawHeatmapVisible(bool visible)`, and `PerformanceMeter.IsOverdrawHeatmapVisible`

Queries are safe before the runtime is started: normal reads return a snapshot with `State = Stopped` and should not throw exceptions.

## Setup Window

Open `SGG/Perfmeter/Setup` to prepare the project without editing URP renderer assets by hand.

- `Project Settings` shows `Frame Timing Stats` status and can enable the Player Setting with `Enable Frame Timing`.
- `URP Renderer Features` lists active Graphics/Quality URP renderer assets first, then renderer assets discovered under `Assets`, with installed/missing/not-editable status; it can add `PerfMeterRenderGraphFeature` to all editable missing renderers or only selected renderers without creating duplicates.
- The `Presets` tab creates and edits project-owned JSON settings at `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`; runtime loads it with `Resources.Load<TextAsset>("SGG.PerfMeter/perfmeter-settings")`. `ScriptableObject` settings are intentionally not used. The tab also chooses the active overlay preset (`Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug`, `Custom`) and saves selected overlay modules into JSON.
- Zero-code setup is driven by this JSON: when `enabled` and `autoStart` are enabled, runtime auto-start applies overlay visible/corner/mode/target FPS without handwritten bootstrap code.
- `Initialization Code` shows the runtime overlay bootstrap; `Overlay Visible`, `Target FPS`, `Overlay Corner`, and `Overlay Mode` options immediately update the code copied by `Copy Init Code`.
- The `Runtime` tab is for Play Mode: buttons are disabled in Edit Mode, and in Play Mode they switch target FPS, overlay mode/corner, show or hide the overlay, start a short overdraw measurement, and toggle the overdraw heatmap.

The same actions are available to agents and Editor scripts without opening the window through the public Editor API:

```csharp
using SGG.PerfMeter.Editor.Setup;
using UnityEngine;

Debug.Log(PerfMeterSetupActions.GetStatusReport());
Debug.Log(PerfMeterSetupActions.RunRecommendedSetup());
Debug.Log(PerfMeterSetupActions.CreateDefaultSettings());
Debug.Log(PerfMeterSetupActions.CopyInitializationSnippetToClipboard());
```

`RunRecommendedSetup()` enables `Frame Timing Stats`, installs the renderer feature into editable URP renderers, and saves default JSON settings for zero-code setup. If the project should not auto-start PerfMeter, disable `Auto Start` on the `Presets` tab and save JSON.

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
	{
		PerformanceMeter.EnsureRunning();
		PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
		PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
		PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
		PerformanceMeter.SetOverlayVisible(true);
	}
}
```

## Runtime Metrics

The runtime singleton updates snapshots in `Update()` with real values from `FrameTimingManager` and `ProfilerRecorder`. The metric collection path avoids PerfMeter-side per-frame allocations; the overlay text refresh is throttled and still rebuilds managed strings at the refresh interval.

`CollectionFrame` is the `Time.frameCount` when PerfMeter collected the snapshot. It is not guaranteed to be the exact frame represented by `FrameTimingManager`, because Unity frame timings can arrive delayed by a few frames.

- Timings: `CpuFrameTimeMs`, `CpuMainThreadFrameTimeMs`, `CpuRenderThreadFrameTimeMs`, `CpuMainThreadPresentWaitTimeMs`, `GpuFrameTimeMs`.
- FPS stats: `AverageFps`, `OnePercentLowFps`, `PointOnePercentLowFps`, `FrameSampleCount`, `GpuValidSampleCount`, `FrameSpikeCount`, and `SevereFrameSpikeCount`; the source is `FrameTiming.cpuFrameTime`, not `Time.deltaTime`.
- Render counters: `DrawCalls`, `SetPassCalls`, `Batches`, `Vertices`, `SrpBatcherInstances`.
- BRG/GRD counters when available: `BrgDrawCalls`, `BrgInstances`, `IndexBufferUploadInFrameBytes` through `PerfMeterStatusSnapshot.AvailableCounters` / `UnavailableCounters`.
- Memory counters: `SystemUsedMemoryBytes`, `GcReservedMemoryBytes`, `GpuMemoryBytes`.
- Classification: `Bottleneck` (`GpuBound`, `CpuMainThreadBound`, `CpuRenderThreadBound`, `PresentLimited`, `Balanced`, `Unknown`) with the current `FrameBudgetMs`, derived from `PerfMeterTargetFps`; `PresentLimited` indicates significant present/VSync/frame pacing wait while CPU/GPU work is below budget.
- Overdraw: `OverdrawState`, `OverdrawProgress`, and `OverdrawRatio` are available in status/metrics snapshots; `OverdrawHeatmapVisible` is available in status snapshots for agent-readable visual heatmap control.
- Device/environment snapshot: `PerfMeterDeviceSnapshot` includes Unity version, platform, OS, CPU/RAM, GPU/API/capabilities, screen/current resolution/fullscreen state, main window position, render-safe display layout state, and `PerfMeterDisplaySnapshot` entries with system monitor names from `Screen.GetDisplayLayout(List<DisplayInfo>)`. If layout is unavailable, it falls back to `Screen.currentResolution`.
- Camera snapshot: `PerfMeterCameraSnapshot` includes camera name/id, scene name/path, position, rotation quaternion, Euler angles, forward/up vectors, projection, FOV/orthographic size, clip planes, aspect, pixel rect, target display, depth, clear flags, culling mask, HDR/MSAA flags, and URP `UniversalAdditionalCameraData` fields when that component already exists on the camera.

On OpenGL/OpenGLES, hardware GPU timing may be unavailable or unreliable. In that case `GpuFrameTimeAvailable` is `false` and `PerfMeterStatusSnapshot.Warning` contains a warning; Vulkan is preferred on Android.

## Overlay

The runtime overlay is built programmatically with UI Toolkit (`UIDocument`, `PanelSettings`, `VisualElement`, `Label`). It is created only while the application is playing; EditMode API calls remain safe and do not create a visible UI document.

- Layout is absolute with `PickingMode.Ignore`, so it does not intercept game input; in graph modes the overlay is split into a wider graph block on top and a narrower text block below.
- The overlay defaults to the top-right corner (`TopRight`) and `Full` mode; available corners are `TopLeft`, `TopRight`, `BottomLeft`, and `BottomRight`.
- Modes: `FpsOnly` shows one FPS/1%/0.1% line, `TextCompact` shows a compact text summary, `Graphs` shows FPS plus CPU/GPU graphs, and `Full` adds render/memory/overdraw counters.
- Presets group a display mode and module flags: `Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug`, and `Custom`. Module flags (`Fps`, `Timing`, `Graphs`, `Rendering`, `SrpBatcher`, `Brg`, `Uploads`, `Memory`, `Gc`, `GpuMemory`, `Overdraw`, `Heatmap`, `Warnings`) filter overlay rows and hide the graph block when `Graphs` is disabled.
- The label is refreshed at most 4 times per second from the latest runtime snapshots, not every frame.
- Full zero-allocation overlay refresh is backlog work: split the text block into stable field labels, cache enum strings, use custom numeric formatting into reusable buffers, and update only changed labels instead of rebuilding text with `StringBuilder`.
- Graphs are drawn through UI Toolkit `generateVisualContent`; the CPU graph uses stacked areas for `render`, `main`, and the remainder up to `frame`, while `frame` is drawn as the upper boundary without summing `frame + main + render`.
- The right side of the graphs shows colored label badges for `frame`, `other`, `main`, `render`, and `gpu` with current, average, worst 1%, and worst 0.1% timings; numbers use fixed width relative to the current graph scale/maximum.
- If GPU timing is temporarily unavailable, the GPU badge turns gray, the current value uses an underscore placeholder, and averages/history use valid samples only.
- Target FPS is selected from `15/30/60/90/120/144/240`; the matching `FrameBudgetMs` drives the red target line shown left of the graph and the scale formula `max(averageTimeMs * 1.1, FrameBudgetMs * 1.2)`.
- Text modes show current/min/max over the overlay's internal history window for timings, render counters, and memory.
- Warnings are held briefly so transient GPU timing gaps do not blink on every refresh.
- In `Full`, visible fields include state, bottleneck, FPS/lows/spikes, CPU/GPU timings, draw calls, SetPass, batches, vertices, SRP/BRG counters, index uploads, overdraw state/progress/ratio, memory, and warning text when present.
- `PerformanceMeter.SetOverlayVisible(false)` hides the retained UI without stopping metric collection; `PerformanceMeter.SetOverlayVisible(true)` ensures the runtime is running and shows the overlay in Play Mode.
- `PerformanceMeter.IsOverlayVisible` and `PerfMeterStatusSnapshot.OverlayVisible` report the actual visible overlay state.

## URP Render Graph Renderer Feature

`PerfMeterRenderGraphFeature` adds an opt-in URP 17 Render Graph marker pass with a dedicated profiling sampler named `SGG.PerfMeter.Overlay`, an opt-in pass for numerical overdraw measurement, and a visual overdraw heatmap pass. The overlay marker is disabled by default because it is reserved for diagnostic/self-overhead measurement and future `ProfilerRecorder`-based subtraction of the tool cost.

- Preferred installation: `SGG/Perfmeter/Setup` -> select missing renderers or use `Install All Missing`.
- Renderer assets inside `Packages` are shown as not editable; copy/configure them manually if a package-owned renderer must include the feature.
- Manual installation: add the feature to the renderer asset, for example `Assets/Settings/PC_Renderer.asset` for PC or `Assets/Settings/Mobile_Renderer.asset` for Mobile.
- In the renderer asset Inspector, choose `Add Renderer Feature` -> `Perf Meter Render Graph Feature`.
- Keep `Enabled` on, leave `Render Pass Event` at the default `AfterRenderingPostProcessing`, or switch it to `AfterRendering` if the marker must run after the whole frame.
- `Marker Name` defaults to `SGG.PerfMeter.Overlay`; change it only if downstream `ProfilerRecorder` code will look for another marker name.
- Enable `Record Overlay Marker Pass` only when diagnosing PerfMeter self-overhead; numerical overdraw measurement does not require the empty marker pass.
- Overdraw defaults to `Game Cameras Only`; set `Camera Name Filter` if a multi-camera project needs to restrict measurement to one camera.
- The feature uses `RecordRenderGraph(RenderGraph, ContextContainer)` and `AddRasterRenderPass`; no legacy-only path is used.

## Overdraw Measurement

Overdraw measurement is opt-in and bounded. Call `PerformanceMeter.RequestOverdrawMeasurement()` to request the default 60-frame measurement window, or pass a custom positive frame count. Call `PerformanceMeter.CancelOverdrawMeasurement()` to stop the request early.

- The runtime is `Off` by default and does not record the numerical overdraw pass until a request is active.
- `PerfMeterRenderGraphFeature` must be added to the active URP renderer. During an active request it records a Render Graph raster pass with the profiling marker `SGG.PerfMeter.Overdraw`.
- The pass redraws the scene renderer list with the hidden replacement shader `Hidden/SGG/PerfMeter/OverdrawCounter`: `ZTest Always`, `ZWrite Off`, `ColorMask 0`.
- The fragment shader atomically increments a GPU `GraphicsBuffer`, then a readback pass uses `AsyncGPUReadback` without a CPU stall.
- `OverdrawRatio` is computed as `TotalFragments / RenderedCameraPixels` and averaged across completed readback samples.
- Async readbacks are guarded by a measurement session id, so stale callbacks from canceled/restarted measurements are ignored.
- The shader requires fragment UAV/storage buffer support, compute shader support, a supported graphics API, and `AsyncGPUReadback`; unsupported targets enter `Unsupported` with a warning before scheduling the Render Graph pass.
- Visual heatmap is controlled separately through `PerformanceMeter.SetOverdrawHeatmapVisible(true/false)`. It redraws the scene renderer list with `Hidden/SGG/PerfMeter/OverdrawHeatmap` using `ZTest Always`, `ZWrite Off`, and additive blending directly over the active camera color target. Hotter/brighter pixels indicate more repeated fragment coverage.
- The heatmap pass is visual only: it does not update `OverdrawRatio` and can remain enabled while no numerical measurement is running.
- Progress advances after async readback scheduled from the Render Graph overdraw pass. If progress stays at `0`, verify that the renderer feature is installed on the renderer used by the active camera and that the target backend supports shader instrumentation.

```csharp
using SGG.PerfMeter;

PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
PerformanceMeter.RequestOverdrawMeasurement();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status))
{
	UnityEngine.Debug.Log($"PerfMeter: {status.State}, overdraw {status.OverdrawState} {status.OverdrawProgress:P0}, heatmap {status.OverdrawHeatmapVisible}, {status.Warning}");
}

if (PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics))
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
