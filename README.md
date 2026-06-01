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

Unlike familiar FPS overlays, SGG PerfMeter does more than show the current FPS. It helps explain whether a frame is limited by CPU, GPU, render thread, present/VSync, overdraw, or unavailable platform counters, and lets you save that state for later analysis.

## Why It Helps

- See why a frame is slow while the game is running, not just the current FPS.
- Switch between visual presets, graphs, metric bars, compact layouts, and custom metric rows for different debugging situations.
- Record reproducible profiling sessions with warm-up, scene scope, worst-frame summaries, JSON/CSV export, device metadata, and camera metadata.
- Use alerts, structured logs, callbacks, and Editor warning cooldowns to catch regressions without watching the overlay all the time.
- Give tools and agents structured data for comparisons, A/B tests, and hotspot search instead of relying on screenshots or Console scraping.

## How It Exposes The Data

- **Runtime overlay**: visual presets, compact layouts, graphs, metric bars, and custom metric rows for live inspection.
- **Public C# API**: immutable snapshots for status, metrics, device, camera, Render Graph, alerts, sessions, and custom metrics.
- **Session recording**: bounded captures with warm-up, scene scope, worst frames, device/camera metadata, and JSON/CSV export.
- **Alerts**: structured logs, callbacks, Editor warning cooldowns, and latest-alert snapshots.
- **Agent layer**: MCP command metadata lets agents inspect the project, compare runs, perform A/B tests, and search for hotspots through structured data.

## What It Measures

- Unity `6000.4+` / URP `17.4+` Render Graph runtime state.
- FrameTimingManager CPU/GPU timing: CPU frame, main thread, render thread, present wait, and GPU frame time when available.
- ProfilerRecorder render counters: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes, memory, and GPU memory when available.
- Bottleneck classification for GPU, CPU main thread, CPU render thread, present/VSync, balanced, or unknown frames.
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
