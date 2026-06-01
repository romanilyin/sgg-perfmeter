# Comparison With Advanced FPS Counter And Graphy

This is a product and architecture comparison, not a measured runtime benchmark.

## Short Version

Advanced FPS Counter and Graphy are strong general-purpose visual overlays. They are useful when the main need is a quick drop-in FPS/memory/device HUD with broad older-Unity support and visual customization.

SGG PerfMeter is intentionally narrower and more diagnostic: Unity `6000.4+`, URP `17.4+`, Render Graph, structured snapshots, session export, overdraw diagnostics, reproducible camera/device metadata, and MCP/API automation.

## Comparison Table

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary positioning | URP Render Graph diagnostics + agent-readable profiling API | Flexible in-game FPS/memory/device counter | Visual FPS/memory/audio stats monitor + debugger |
| Unity target | Unity `6000.4+`, URP `17.4+` | Broad older Unity support | Broad older Unity support |
| UI backend | UI Toolkit overlay | uGUI Canvas/Text labels | uGUI Text/Image modules |
| Timing source | `FrameTimingManager` + rolling stats | Runtime frame/update sampling | `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | CPU frame, main thread, render thread, present wait, GPU when available | No equivalent split | No equivalent split |
| Bottleneck classification | GPU, CPU main, CPU render, present-limited, balanced, unknown | No equivalent | No equivalent |
| Render counters | Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | No URP/SRP counter set | No URP/SRP counter set |
| Device/camera reproducibility | Structured device and camera snapshots | Device panel only | Device panel only |
| Sessions | Bounded recorder, warm-up, scene scope, worst frames, JSON/CSV export | Not a primary feature | Roadmap-like idea |
| Overdraw | Numerical measurement + visual heatmap through URP Render Graph | No | No |
| Automation | MCP command surface and public snapshots | No | No |

## What SGG PerfMeter Does Better

- Explains likely frame bottlenecks with CPU frame, main thread, render thread, present wait, GPU timing, and frame budget data.
- Exposes URP-oriented render counters and Render Graph diagnostics.
- Produces reproducible performance reports with scene, device, camera, settings, session samples, summaries, and worst-frame metadata.
- Gives tools and agents structured data through public API and MCP commands.
- Integrates bounded overdraw measurement and a visual heatmap as explicit diagnostics.

## What Competitors Still Do Better

- Advanced FPS Counter has very direct drop-in visual counter UX, mature inspector customization, hotkeys/circle gesture toggles, min/max/average UI patterns, and VR/world-space examples.
- Graphy has strong public marketing material, clear module states, visual customization, hotkeys/background mode, mature debugger packet UX, and broad public awareness.

## What Not To Claim

- SGG PerfMeter is not a replacement for Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.
- SGG PerfMeter is not zero-overhead; use low-overhead and document explicit diagnostic costs.
- SGG PerfMeter is not an all-platform/all-pipeline legacy compatibility package.
