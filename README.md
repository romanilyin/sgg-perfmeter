# SGG PerfMeter

**SGG PerfMeter** is a low-overhead runtime performance meter and agent-readable profiling API for **Unity 6000.4+** projects that use **Universal Render Pipeline**.

It is designed for builds and Play Mode where you need fast answers to practical questions:

- Is the frame CPU-bound, GPU-bound, render-thread-bound, or only waiting for presentation/VSync?
- Are draw calls, SetPass calls, SRP Batcher, BRG/GRD, buffer uploads, memory, or overdraw suspicious?
- Can an AI agent or editor tool read profiler state without scraping UI text or Unity Console logs?

The package is intentionally focused on runtime metrics. It does not replace Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.

Target private release candidate: `2026.5.18-1`. The repository remains private until the release preparation is reviewed; the release plan is tracked in `docs/release-2026.5.18-1.md`, and release notes are drafted in `docs/release-notes-2026.5.18-1.md`.

## Status

Current state: **private release candidate / internal validation**.

Implemented:

- runtime public API for status and latest metrics snapshots;
- `FrameTimingManager`-based CPU/GPU timing collection;
- `ProfilerRecorder`-based render, memory, SRP Batcher, BRG/GRD, and upload counters;
- UI Toolkit overlay with compact, graph, and full modes;
- URP Render Graph renderer feature;
- bounded numerical overdraw measurement using a hidden replacement shader and `AsyncGPUReadback`;
- visual overdraw heatmap using an additive replacement shader;
- editor setup window;
- MCP command definitions for setup, runtime control, metrics, overlay, overdraw measurement, and overdraw heatmap;
- English and Russian package documentation;
- release readiness docs, changelog, security policy, contributing policy, and a manual-only release workflow;
- edit-mode API safety tests and Play Mode runtime smoke tests.

Still pending / needs validation:

- broader target-device validation for GPU timings and counter availability;
- broader Play Mode coverage and player-build tests;
- CSV/JSON session export;
- fully validated self-overhead subtraction;
- full zero-allocation overlay refresh path with field labels, cached enum strings, custom numeric formatting, and no `StringBuilder` text rebuilds;
- CI workflow for Unity batchmode compile/tests.

## Requirements

- Unity `6000.4+`.
- URP `17.4+`.
- Render Graph path.
- UI Toolkit runtime support.
- `Frame Timing Stats` enabled for reliable frame timing in builds.

Recommended graphics APIs:

- Windows: D3D12, D3D11, or Vulkan.
- Android: Vulkan preferred.
- iOS/macOS: Metal.

Known platform caveats:

- GPU timing can be unavailable, delayed, or unreliable on some platforms and graphics APIs.
- OpenGL/OpenGLES should be treated as degraded mode for GPU timing and overdraw instrumentation.
- Numerical overdraw requires fragment-stage UAV/storage-buffer support, compute shader support, a supported graphics API, and async GPU readback support; unsupported targets report `OverdrawState = Unsupported` instead of entering measurement.
- WebGL/XR/mobile backends may return partial metrics or mark GPU timing as unavailable.

## Installation

### Option A: install as a Git UPM package

The package lives inside this repository under:

```text
Assets/Scripts/SGG.PerfMeter
```

While the repository is private, use SSH or an authenticated HTTPS Git dependency. Add the package to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

For a private repository over SSH:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

For a fixed version, pin a tag or commit:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.18-1"
  }
}
```

### Option B: copy into Assets

Copy this folder into your Unity project:

```text
Assets/Scripts/SGG.PerfMeter
```

This is useful while developing the package locally or when you do not want to use Git dependencies.

## Quick setup

Open the setup window:

```text
SGG/Perfmeter/Setup
```

Run the recommended setup:

1. Enable **Frame Timing Stats**.
2. Review active and discovered URP renderer assets, then install `PerfMeterRenderGraphFeature` into all editable missing renderers or only the selected ones.
3. Copy or add the initialization snippet.
4. Enter Play Mode and verify the overlay.

Minimal runtime bootstrap:

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

The setup window can also be driven from editor scripts:

```csharp
using SGG.PerfMeter.Editor.Setup;
using UnityEngine;

Debug.Log(PerfMeterSetupActions.GetStatusReport());
Debug.Log(PerfMeterSetupActions.RunRecommendedSetup());
Debug.Log(PerfMeterSetupActions.CopyInitializationSnippetToClipboard());
```

## Runtime API

Namespace:

```csharp
using SGG.PerfMeter;
```

Lifecycle:

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
```

Status:

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    Debug.Log($"PerfMeter state: {safeStatus.State}, GPU: {safeStatus.GraphicsDeviceType}");
}
```

Metrics:

```csharp
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

Debug.Log(
    $"CPU {metrics.CpuFrameTimeMs:0.00} ms, " +
    $"GPU {(metrics.GpuFrameTimeAvailable ? metrics.GpuFrameTimeMs.ToString("0.00") : "N/A")} ms, " +
    $"Draws {metrics.DrawCalls}, SetPass {metrics.SetPassCalls}, " +
    $"Bottleneck {metrics.Bottleneck}");
```

Overlay:

```csharp
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Overdraw:

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);

PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
Debug.Log($"Overdraw: {status.OverdrawState}, {status.OverdrawProgress:P0}, ratio {status.OverdrawRatio:0.00}, heatmap {status.OverdrawHeatmapVisible}");
```

Cancel overdraw measurement:

```csharp
PerformanceMeter.CancelOverdrawMeasurement();
```

All read APIs are safe before the runtime starts. A read before startup returns a stopped snapshot instead of throwing.

## Metrics

### Snapshot frame identity

| Field | Meaning |
| --- | --- |
| `CollectionFrame` | Unity `Time.frameCount` when PerfMeter collected the snapshot. This is not guaranteed to be the exact frame represented by `FrameTimingManager`, because Unity frame timings can arrive delayed by a few frames. |

### Timing

| Field | Meaning |
| --- | --- |
| `CpuFrameTimeMs` | Total CPU frame time from Unity frame timing data. |
| `CpuMainThreadFrameTimeMs` | Main thread frame time. Useful for scripts, physics, animation, jobs, and game logic bottlenecks. |
| `CpuRenderThreadFrameTimeMs` | Render thread frame time. Useful for draw submission and render-state bottlenecks. |
| `CpuMainThreadPresentWaitTimeMs` | Main thread wait time around present/VSync/GPU synchronization. |
| `GpuFrameTimeMs` | GPU frame time when available. |
| `GpuFrameTimeAvailable` | `false` when GPU timing returned no valid value. |

### FPS statistics

| Field | Meaning |
| --- | --- |
| `AverageFps` | Average FPS over the internal frame window. |
| `OnePercentLowFps` | 1% low FPS estimate from slow frames. |
| `PointOnePercentLowFps` | 0.1% low FPS estimate from slow frames. |
| `FrameSpikeCount` | Number of frames above the spike threshold in the history window. |
| `SevereFrameSpikeCount` | Number of frames above the severe spike threshold in the history window. |

The source for these values is frame timing data, not `Time.deltaTime`.

### Rendering counters

| Field | Meaning |
| --- | --- |
| `DrawCalls` | Aggregated draw-call count. |
| `SetPassCalls` | Render-state/shader pass changes. Often more important than raw draw-call count. |
| `Batches` | Dynamic/static/instanced batch count when counters are available. |
| `Vertices` | Submitted vertex count. |
| `SrpBatcherInstances` | SRP Batcher instance count when available. |
| `BrgDrawCalls` | BatchRendererGroup / GPU Resident Drawer draw calls when available. |
| `BrgInstances` | BatchRendererGroup / GPU Resident Drawer instances when available. |
| `IndexBufferUploadInFrameBytes` | Index-buffer upload traffic during the frame. |

Counter availability is exposed through:

```csharp
PerfMeterStatusSnapshot.AvailableCounters
PerfMeterStatusSnapshot.UnavailableCounters
```

### Memory counters

| Field | Meaning |
| --- | --- |
| `SystemUsedMemoryBytes` | System/app memory counter when available. |
| `GcReservedMemoryBytes` | Reserved managed heap memory. |
| `GpuMemoryBytes` | GPU/graphics memory counter when available. |

### Bottleneck classification

Current enum:

```csharp
PerfMeterBottleneck.Unknown
PerfMeterBottleneck.Balanced
PerfMeterBottleneck.GpuBound
PerfMeterBottleneck.CpuMainThreadBound
PerfMeterBottleneck.CpuRenderThreadBound
PerfMeterBottleneck.PresentLimited
```

`PresentLimited` means present/VSync/frame pacing wait is significant while CPU main work, render-thread work, and available GPU work are below the target frame budget.

The frame budget comes from:

```csharp
PerfMeterTargetFps.Fps15
PerfMeterTargetFps.Fps30
PerfMeterTargetFps.Fps60
PerfMeterTargetFps.Fps90
PerfMeterTargetFps.Fps120
PerfMeterTargetFps.Fps144
PerfMeterTargetFps.Fps240
```

## Overlay modes

| Mode | Description |
| --- | --- |
| `FpsOnly` | One compact FPS / 1% low / 0.1% low line. |
| `TextCompact` | Compact text summary with timings, counters, memory, and warnings. |
| `Graphs` | CPU/GPU graphs plus small text summary. |
| `Full` | Full text diagnostics plus CPU/GPU graphs. |

Corners:

```csharp
TopLeft
TopRight
BottomLeft
BottomRight
```

Default:

```csharp
TopRight + Full + Fps60
```

## URP Render Graph feature

The package includes `PerfMeterRenderGraphFeature`.

Recommended installation:

```text
SGG/Perfmeter/Setup -> URP Renderer Features -> Install All Missing / Install Selected
```

The setup window checks active URP assets from Graphics and Quality settings first, then falls back to renderer assets found under `Assets`. Renderer assets inside `Packages` are listed as not editable and must be copied or configured manually.

Manual installation:

1. Open your active URP renderer asset.
2. Add renderer feature: `PerfMeterRenderGraphFeature`.
3. Keep it enabled.
4. Keep the default render pass event unless you have a specific ordering reason to move it.

The feature currently provides:

- an opt-in diagnostic Render Graph marker pass for future self-overhead measurement;
- optional overdraw instrumentation pass while overdraw measurement is active;
- optional visual overdraw heatmap pass while heatmap visibility is enabled;
- overdraw camera filtering, defaulting to Game cameras only, with optional camera-name filtering in the renderer feature settings;
- hidden replacement shader lookup through `Shader.Find` / `Resources.Load`;
- async readback of the numeric overdraw counter.

## Numerical overdraw

Numerical overdraw measurement is opt-in and bounded.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(60);
```

The Render Graph feature redraws the scene with a hidden replacement shader that uses:

- `ZTest Always`;
- `ZWrite Off`;
- `ColorMask 0`;
- fragment-stage atomic increment into a GPU buffer;
- `AsyncGPUReadback` for result collection.

The ratio is:

```text
OverdrawRatio = TotalFragmentCount / RenderedCameraPixelCount
```

Notes:

- This is a diagnostic measurement, not a steady-state metric.
- It can be expensive and should not be left on permanently.
- It depends on graphics API and device support; unsupported targets return `OverdrawState = Unsupported` with a warning before scheduling the Render Graph pass.
- Transparent objects, particles, UI, renderer queues, replacement-shader compatibility, and camera selection can affect the result.
- Readbacks are tied to a measurement session id, so stale `AsyncGPUReadback` callbacks from a canceled or restarted measurement are ignored.

## Visual overdraw heatmap

Visual overdraw heatmap is controlled separately from numerical measurement:

```csharp
PerformanceMeter.SetOverdrawHeatmapVisible(true);
PerformanceMeter.SetOverdrawHeatmapVisible(false);
```

The heatmap pass redraws the scene renderer list with `Hidden/SGG/PerfMeter/OverdrawHeatmap` using `ZTest Always`, `ZWrite Off`, and additive blending over the active camera color target. Brighter red/orange areas indicate more repeated fragment coverage.

Notes:

- Heatmap visibility is exposed through `PerformanceMeter.IsOverdrawHeatmapVisible`, `PerfMeterStatusSnapshot.OverdrawHeatmapVisible`, overlay text, setup runtime controls, and MCP status JSON.
- The heatmap is visual only; it does not update `OverdrawRatio`.
- It uses the same renderer feature camera filtering and replacement-shader compatibility constraints as numerical overdraw.

## MCP commands

The package includes MCP command metadata under:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

Available command IDs:

| Command | Purpose |
| --- | --- |
| `perfmeter.setup.status` | Read setup status. |
| `perfmeter.setup.run` | Run recommended setup actions. |
| `perfmeter.runtime.status` | Read runtime status. |
| `perfmeter.runtime.ensure` | Start runtime if needed. |
| `perfmeter.runtime.stop` | Stop runtime. |
| `perfmeter.metrics.latest` | Read latest metrics snapshot. |
| `perfmeter.overlay.set` | Show/hide overlay and set corner/mode/target FPS. |
| `perfmeter.overdraw.start` | Start bounded overdraw measurement. |
| `perfmeter.overdraw.cancel` | Cancel active overdraw measurement. |
| `perfmeter.overdraw.heatmap.set` | Show or hide visual overdraw heatmap. |

These commands are intended for Unity MCP / editor automation workflows where an agent needs structured JSON output instead of screenshots or log scraping.

## Package layout

```text
Assets/Scripts/SGG.PerfMeter/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    PerformanceMeter.cs
    PerfMeterRuntime.cs
    PerfMeterCollector.cs
    PerfMeterFrameStatsSampler.cs
    PerfMeterOverlay.cs
    PerfMeterOverdrawController.cs
    PerfMeterRenderGraphFeature.cs
    PerfMeterSnapshots.cs
    Resources/
      SGGPerfMeterOverdrawCounter.shader
      SGGPerfMeterOverdrawHeatmap.shader
  Editor/
    Setup/
    UI/
    Mcp/
  Tests/
    EditMode/
    PlayMode/
  .Documentation/
    README.en.md
    README.ru.md
    STATUS.en.md
    STATUS.ru.md
  LICENSE.md
  LICENSE.ru.md
  NOTICE.md
  NOTICE.ru.md
```

## Development and verification

Recommended local compile check:

```bash
Unity.exe -batchmode -quit -projectPath <path-to-project> -logFile <path-to-log>
```

Recommended local Test Runner checks use `-runTests` without `-quit`, because Unity exits after the run and writes the XML results itself:

```bash
Unity.exe -batchmode -projectPath <path-to-project> -runTests -testPlatform EditMode -testResults <path-to-editmode-results.xml> -logFile <path-to-editmode-log>
Unity.exe -batchmode -projectPath <path-to-project> -runTests -testPlatform PlayMode -testResults <path-to-playmode-results.xml> -logFile <path-to-playmode-log>
```

Release-readiness docs:

- `docs/release-2026.5.18-1.md`
- `docs/release-notes-2026.5.18-1.md`
- `docs/release-process.md`
- `docs/manual-checks.md`
- `docs/versioning.md`

Recommended next verification targets:

- Windows Player: D3D11 and D3D12.
- Android Player: Vulkan.
- Android Player: OpenGLES3 degraded-mode behavior.
- macOS/iOS Player: Metal.
- Play Mode overlay stability.
- Release Player metric availability.
- Overdraw measurement and heatmap behavior on real devices.

## Known limitations

- Root project is currently a sample Unity project plus a nested UPM package; install through `?path=/Assets/Scripts/SGG.PerfMeter` when using Git dependencies.
- GPU timings can be delayed or unavailable depending on platform and graphics API; `CollectionFrame` identifies when the snapshot was collected, not the original hardware timing frame.
- Bottleneck classification is heuristic and should be validated against Unity Profiler captures before treating it as authoritative.
- The overlay refresh is throttled, but text assignment still creates managed strings on refresh; full zero-allocation overlay refresh is tracked as a later optimization.
- Self-overhead marker pass is opt-in diagnostic mode, and full overhead subtraction is not finalized.
- Overdraw heatmap is diagnostic and uses an extra scene redraw while visible.
- CSV/JSON session export is not implemented yet.
- CI is not configured yet.

## License

This package is licensed under **Stinger Royalty-Free EULA 1.0**.

- Primary controlling text: `LICENSE.ru.md`.
- English convenience translation: `LICENSE.md`.
- Notices: `NOTICE.md` and `NOTICE.ru.md`.
- SPDX identifier: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.
- Licensor: `ROMAN ILYIN`.

Free for personal, internal, open, and commercial End Products. Royalty-free. Standalone sale, resale, paid redistribution, or standalone commercialization of this asset or derivative assets is prohibited.
