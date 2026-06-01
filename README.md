<div align="center">

# SGG PerfMeter

**Runtime performance diagnostics and agent-readable profiling API for Unity 6000+ URP Render Graph projects.**

[English](./README.md) |
[Russian](./docs/ru/README.md)

—

[Installation](./docs/en/installation.md) |
[Quick Start](./docs/en/quick-start.md) |
[Workflows](./docs/en/workflows.md) |
[Visual Presets](./docs/en/presets.md) |
[Implemented Widgets](./docs/en/widgets.md) |
[API](./docs/en/api.md) |
[MCP](./docs/en/mcp.md) |
[Comparison](./docs/en/comparison.md) |
[Limitations](./docs/en/limitations.md) |
[Troubleshooting](./docs/en/troubleshooting.md) |
[Screenshots](./docs/en/screenshots.md) |
[Setup Window Screenshots](./docs/en/setup-window-screenshots.md) |
[Contributor Checks](./docs/en/contributor-checks.md) |
[Changelog](./CHANGELOG.md)

<p>
  <a href="./docs/en/installation.md"><img src="./docs/assets/readme/cards/unity.svg" alt="Unity" height="48"></a>
  <a href="./docs/en/installation.md"><img src="./docs/assets/readme/cards/urp.svg" alt="URP" height="48"></a>
  <a href="./docs/en/workflows.md#runtime-overlay"><img src="./docs/assets/readme/cards/uitk.svg" alt="UI Toolkit" height="48"></a>
  <a href="./docs/en/api.md"><img src="./docs/assets/readme/cards/csharp.svg" alt="C#" height="48"></a>
  <a href="./docs/en/limitations.md"><img src="./docs/assets/readme/cards/android.svg" alt="Android" height="48"></a>
  <a href="./docs/en/limitations.md"><img src="./docs/assets/readme/cards/ios.svg" alt="iOS" height="48"></a>
  <a href="./docs/en/quick-start.md"><img src="./docs/assets/readme/cards/docs.svg" alt="Docs" height="48"></a>
</p>

<p>
  <a href="./docs/en/presets.md#default"><img src="./docs/assets/screenshots/presets/preset-default.png" alt="SGG PerfMeter default overlay preset" width="960"></a>
</p>

</div>

SGG PerfMeter is not just an FPS counter. It is a lightweight runtime diagnostics layer for Unity URP projects that need to understand why a frame is slow, what changed, and how to reproduce the capture.

The same performance data is available through several surfaces: the runtime overlay, public C# API snapshots, JSON/CSV session exports, alerts, and MCP command metadata for editor/agent automation.

Most FPS overlays answer: **what is the FPS right now?**

SGG PerfMeter answers: **is this CPU-bound, GPU-bound, render-thread-bound, present-limited, overdraw-heavy, or missing platform counters, and can that state be exported or automated?**

## Highlights

- Unity `6000.4+` and URP `17.4+` focus, with Render Graph renderer feature integration.
- FrameTimingManager CPU/GPU timing, main-thread, render-thread, and present-wait visibility.
- ProfilerRecorder render, SRP Batcher, BRG/GRD, upload, memory, and GPU-memory counters when available.
- Bottleneck classification for GPU, CPU main thread, CPU render thread, present/VSync, balanced, or unknown frames.
- UI Toolkit runtime overlay with presets, layouts, graphs, metric bars, themes, and custom metric rows.
- Session recording with warm-up, scene scope, worst-frame summaries, JSON/CSV export, device metadata, and camera metadata.
- Rule alerts with structured logs, callbacks, Editor warning cooldowns, and MCP alert commands.
- Opt-in numerical overdraw measurement and visual overdraw heatmap through URP Render Graph.
- Device, camera, Render Graph, status, metrics, alerts, session, and custom metric snapshots for code and MCP automation.

## Quick Start

1. Install the Unity package from this repository with the package path `Assets/Scripts/SGG.PerfMeter`.
2. Open `SGG/Perfmeter/Setup` in Unity.
3. Run the recommended setup, enter Play Mode, and confirm that the overlay appears.

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

For the full setup guide, see [Installation](./docs/en/installation.md) and [Quick Start](./docs/en/quick-start.md).

## Who It Is For

- Unity URP developers validating performance in Play Mode, development builds, and device smoke tests.
- Rendering engineers and technical artists who need draw calls, SetPass, upload, memory, SRP Batcher, BRG/GRD, overdraw, and frame timing visibility.
- Tooling developers who need stable runtime snapshots instead of a visual HUD only.
- Teams using Unity MCP or editor agents for profiling automation and regression checks.
- Solo developers who want a more diagnostic alternative to a basic FPS overlay.

## Common Workflows

- **Zero-code overlay**: create `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` from the setup window and let PerfMeter auto-start.
- **Runtime API**: call `PerformanceMeter.EnsureRunning()`, then read immutable status, metrics, device, camera, and session snapshots.
- **Session export**: record bounded profiling windows and export JSON/CSV with scene, device, camera, settings, counters, warnings, and worst-frame metadata.
- **Overdraw diagnostics**: request a bounded numerical measurement or enable the visual heatmap when the URP renderer feature is installed.
- **Agent automation**: use MCP command metadata to start collection, switch overlay modes, export sessions, inspect alerts, and read snapshots.

See [Workflows](./docs/en/workflows.md), [API](./docs/en/api.md), and [MCP](./docs/en/mcp.md).

## Screenshots

See the default overlay preset, setup window pages, visual presets, and runtime widgets in the screenshot galleries.

Start with [Visual Presets](./docs/en/presets.md), [Setup Window Screenshots](./docs/en/setup-window-screenshots.md), [Implemented Widgets](./docs/en/widgets.md), and [Screenshots](./docs/en/screenshots.md).

## Compared With FPS Counters

Advanced FPS Counter and Graphy are strong general-purpose drop-in visual overlays. SGG PerfMeter intentionally focuses on modern Unity URP diagnostics: structured timing and render counters, bottleneck classification, reproducible sessions, device/camera snapshots, overdraw diagnostics, Render Graph state, and agent-readable automation.

This is a product and architecture comparison, not a measured runtime benchmark. See [Comparison](./docs/en/comparison.md).

## Requirements

- Unity `6000.4+` for supported runtime usage.
- URP `17.4+` with Render Graph path.
- Frame Timing Stats enabled before relying on FrameTimingManager in builds.
- Vulkan is preferred on Android when GPU timing matters.

Unity `2022.3` through `6000.3` may be import-safe for compile checks, but runtime overlay, Render Graph features, overdraw passes, and support expectations target Unity `6000.4+` with URP `17.4+`.

## License

This package is licensed under **Stinger Royalty-Free EULA 1.0**.

- Authoritative license text: [LICENSE.ru.md](./LICENSE.ru.md)
- English convenience translation: [LICENSE.md](./LICENSE.md)
- Notices: [NOTICE.md](./NOTICE.md) and [NOTICE.ru.md](./NOTICE.ru.md)
