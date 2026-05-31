# SGG PerfMeter Documentation

SGG PerfMeter is a runtime performance diagnostics layer and agent-readable profiling API for Unity `6000.4+` URP `17.4+` Render Graph projects.

It combines a UI Toolkit overlay with structured snapshots, session export, overdraw diagnostics, alerts, custom metrics, and MCP command metadata. The overlay is only one output surface; the same data is available through public C# APIs and automation-friendly snapshots.

## Start Here

- [Installation](./installation.md): add the Unity package from Git or local source.
- [Quick Start](./quick-start.md): get the first overlay running through `SGG/Perfmeter/Setup`.
- [Workflows](./workflows.md): overlay, sessions, alerts, overdraw, custom metrics, and agent automation.
- [Implemented Widgets](./widgets.md): current runtime overlay widget inventory.
- [Visual Presets](./presets.md): default visual presets and fullscreen captures.
- [API](./api.md): runtime C# API and snapshot types.
- [MCP](./mcp.md): command surface for Unity MCP/editor agents.
- [Comparison](./comparison.md): SGG PerfMeter vs Advanced FPS Counter and Graphy.
- [Limitations](./limitations.md): platform, timing, overdraw, and validation notes.
- [Screenshots](./screenshots.md): screenshot index and asset paths.
- [Setup Window Screenshots](./setup-window-screenshots.md): setup window pages in English.

## What It Solves

Most FPS counters tell you the current FPS. SGG PerfMeter is built to help answer more diagnostic questions:

- Is the frame CPU-bound, GPU-bound, render-thread-bound, or present/VSync-limited?
- Are draw calls, SetPass calls, SRP Batcher, BRG/GRD, upload bytes, memory, or overdraw suspicious?
- Is GPU timing available and trustworthy on this platform and graphics API?
- Which device, display, camera, scene, and settings produced this capture?
- Can an editor tool or AI agent read profiler state without parsing UI text or Unity Console logs?

## Main Features

- `FrameTimingManager` timing collection for CPU frame, main thread, render thread, present wait, and GPU frame time when available.
- `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, upload bytes, and GPU memory when available.
- Bottleneck classification against a selected target FPS budget.
- UI Toolkit overlay with layouts, graph modes, visual presets, themes, module filters, and custom metric rows.
- Bounded session recording with warm-up, scene scope, worst frames, JSON/CSV export, device snapshots, camera snapshots, and settings metadata.
- Rule alerts with callbacks, structured logs, Editor warning cooldowns, and MCP access.
- Opt-in numerical overdraw measurement and visual heatmap through URP Render Graph.
- Device, camera, Render Graph, status, metrics, alerts, session, and custom metric snapshots for code and automation.

## First Run Checklist

- Unity `6000.4+`.
- URP `17.4+`.
- Frame Timing Stats enabled in Player Settings.
- `PerfMeterRenderGraphFeature` installed into the active URP renderer when using Render Graph markers, overdraw measurement, or heatmap.
- Vulkan preferred on Android when GPU frame timing matters.

## Documentation Policy

The GitHub `docs/` directory is the main user-facing documentation source. Package-local documentation under `Assets/Scripts/SGG.PerfMeter/` is intentionally shorter and points back here.

Internal development documents are kept under `_DevelopmentDocs/` and are not the primary user entry point.
