# SGG PerfMeter vs Advanced FPS Counter vs Graphy

Scope: product and architecture comparison against Advanced FPS Counter and Graphy, not a measured runtime benchmark. Use competitor behavior as UX reference only; do not copy proprietary assets/code or import Graphy as a dependency.

## Executive Summary

**SGG PerfMeter has moved beyond the classic FPS-counter category.** It now has the core surfaces expected from visual overlays - FPS, graphs, memory, device info, presets, settings, and samples - but its strongest differentiator is structured URP diagnostics: CPU/GPU timing, render-thread and present-wait visibility, ProfilerRecorder counters, bottleneck classification, overdraw measurement, session export, rule alerts, camera/device snapshots, custom metrics, Render Graph diagnostics, and MCP command metadata.

Advanced FPS Counter remains a good reference for drop-in usability: uGUI labels, hotkey/circle gesture toggles, FPS/memory/device counters, min/max/average controls, scene reset patterns, cached number formatting, and VR/world-space examples.

Graphy remains a strong reference for visual product UX: module states, presets, graphs, customizable colors/layouts, hotkeys, background mode, advanced device info, debugger packets, and mature public README structure.

SGG PerfMeter should not clone their uGUI overlay architectures. Keep the product focus on **Unity 6000+ URP Render Graph diagnostics and agent-readable automation**.

## Comparison Table

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary positioning | URP Render Graph diagnostics + agent-readable profiling API | Flexible in-game FPS/memory/device counter | Visual FPS/memory/audio stats monitor + debugger |
| Unity target | Unity `6000.4+`, URP `17.4+` | Broad older Unity support | Broad older Unity support |
| Render pipeline focus | URP-specific, Render Graph renderer feature | General Unity overlay | General Unity uGUI overlay |
| UI backend | UI Toolkit overlay | uGUI Canvas/Text labels | uGUI Text/Image modules |
| FPS source | `FrameTimingManager` timing data + rolling stats | Runtime frame/update sampling | `Time.unscaledDeltaTime` history sampling |
| CPU/GPU timing | CPU frame, main thread, render thread, present wait, GPU frame when available | FPS/ms and approximate render helper | FPS/ms graph history; no CPU main/render/GPU split |
| Bottleneck classification | Yes: GPU, CPU main, CPU render, present/VSync, balanced/unknown | No equivalent | No equivalent |
| Render counters | Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes | No URP/SRP counter set | No URP/SRP counter set |
| Device/environment info | Structured Unity/platform/CPU/GPU/API/display/monitor/window/support snapshot | Visual device counter | Advanced device panel |
| Camera reproducibility | Structured camera snapshot with scene, transform, projection, clipping, target display, URP settings | No equivalent | No equivalent |
| Render Graph diagnostics | Observed feature/pass/resource diagnostics with graceful degraded counts | No | No |
| Overdraw | Numerical measurement + visual heatmap through URP Render Graph | No | No |
| Session recording | Bounded recorder, warm-up, scene scope, worst-frame summaries, JSON/CSV export | Not a primary feature | Roadmap idea |
| Rule alerts | Metrics alerts, structured logs, callback, Editor warning cooldowns, MCP alert commands | FPS level callback | Debug packets with conditions/callbacks |
| Custom metrics | Public provider interface and overlay/session/MCP output | No direct equivalent | Roadmap-like concept |
| Overlay presets/modules | `Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug` + module flags | Per-counter inspector settings | Module presets/states |
| Settings | Project-owned JSON in `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` | Inspector-driven component settings | Serialized prefab/inspector settings |
| Player hotkeys | Out of current scope; control through API/MCP/setup | Yes | Yes |
| Audio monitoring | Intentionally out of scope | No | Yes |
| MCP / automation | First-class differentiator | No | No |

## What SGG PerfMeter Does Better

- Diagnoses why a frame is slow with CPU frame, main thread, render thread, present wait, GPU timing, frame budget, and bottleneck classification.
- Exposes modern URP signals: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes, GPU memory, and Render Graph feature diagnostics.
- Supports reproducible performance reports through device snapshots, camera snapshots, session samples, scene summaries, worst-frame data, and JSON/CSV export.
- Speaks automation natively through MCP commands for setup, runtime control, metrics, device info, camera snapshot, Render Graph snapshot, overlay, alerts, overdraw, and sessions.
- Integrates explicit overdraw measurement and heatmap diagnostics through URP Render Graph.

## What Competitors Still Do Better

- Advanced FPS Counter has very direct drop-in visual counter UX, mature inspector customization, hotkey/circle gesture toggles, min/max/average UI patterns, VR/world-space examples, and useful cached-formatting patterns.
- Graphy has strong public marketing material, clear module states/presets, visual customization, hotkeys/background mode, mature debugger packet UX, and broad public awareness.

## What Not To Copy

- Do not copy AFPS source code or assets without explicit license permission.
- Do not import Graphy as a dependency.
- Do not replace UI Toolkit with uGUI just because competitors use uGUI.
- Do not add audio/spectrum modules.
- Do not expand to old Unity versions or all render pipelines for the first public release.
- Do not add player hotkeys, screenshot actions, or debug-break alert actions by default without explicit product approval.
- Do not claim zero-overhead or profiler replacement.

## Roadmap Interpretation

Competitors are useful references for usability, visual clarity, cached formatting, examples, and README presentation. Current SGG PerfMeter product direction keeps hotkeys and audio out of scope, prioritizes Render Graph diagnostics, structured telemetry, sessions, alerts, custom metrics, and automation.

## Bottom Line

SGG PerfMeter should be marketed as a **modern URP diagnostics and automation package**. AFPS and Graphy are visual overlay competitors, but SGG's strongest category is runtime performance telemetry for humans, tools, and agents.
