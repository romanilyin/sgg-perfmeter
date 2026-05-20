# SGG PerfMeter Positioning

## One-line positioning

**SGG PerfMeter is a Unity 6000+ URP Render Graph diagnostics layer and agent-readable profiling API, not just an FPS counter.**

## Public pitch

SGG PerfMeter gives Unity URP teams a lightweight runtime diagnostics layer for builds, Play Mode, smoke tests, and editor/AI automation. It combines FrameTimingManager timings, ProfilerRecorder counters, bottleneck classification, overdraw diagnostics, device and camera snapshots, session export, rule alerts, custom metrics, UI Toolkit overlay, and MCP commands in one package.

Most FPS counters answer: **"What is the FPS right now?"**

SGG PerfMeter answers: **"Why is this frame slow, what changed, how can an agent read it, and how do we reproduce the capture?"**

## Target users

- Unity URP developers building PC, Android, iOS/macOS, and internal test builds.
- Technical artists and rendering engineers who need draw-call, SetPass, upload, memory, SRP Batcher, BRG, overdraw, and frame-timing visibility.
- Tooling developers who need stable runtime API snapshots instead of a visual HUD only.
- Teams using Unity MCP or editor agents for automated profiling, smoke tests, and regression checks.
- Solo developers who want a more diagnostic alternative to a basic FPS counter.

## Not the target

- Users who only need a tiny drop-in FPS label with old Unity support.
- Users who need a generic uGUI dashboard framework.
- Users who need audio spectrum monitoring.
- Users who need Built-in Render Pipeline or HDRP support as first-class targets.
- Users who need deep frame capture tooling that belongs in Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.

## Differentiators

- URP-specific diagnostic depth: Unity 6000.4+, URP 17.4+, FrameTimingManager, ProfilerRecorder, Render Graph, SRP Batcher, BRG/GRD, upload counters, and Render Graph diagnostics.
- Bottleneck classification: GPU-bound, CPU main-thread-bound, CPU render-thread-bound, present-limited, balanced, or unknown.
- Structured snapshots: status, metrics, device info, camera state, Render Graph diagnostics, alerts, session summaries, and custom metrics are exposed to code and MCP, not only to an overlay.
- Reproducible captures: session exports include scene, settings, device, camera, and worst-frame metadata.
- Agent-readable workflow: MCP commands can start/stop runtime collection, switch overlay modes, export sessions, inspect alerts, read camera/device snapshots, and control overdraw without screenshots or console scraping.
- Explicit overdraw diagnostics: numerical overdraw measurement and visual heatmap are opt-in, bounded URP Render Graph diagnostic modes.

## Messaging pillars

- Fast diagnosis: see the likely bottleneck and suspicious counters while the game is running.
- Reproducible profiling: export session summaries with scene, device, camera, and worst-frame metadata.
- Agent-ready telemetry: give Unity MCP and editor/AI agents structured JSON instead of screenshots.
- Modern URP focus: built for Unity 6000+ URP Render Graph instead of old all-pipeline compatibility.
- Safe diagnostics: expensive diagnostics such as overdraw measurement and heatmap are explicit, bounded, and visible in runtime state.

## README tagline

Runtime performance diagnostics and agent-readable profiling API for Unity 6000+ URP Render Graph projects.

## Short description

SGG PerfMeter is a low-overhead Unity URP diagnostics package that exposes CPU/GPU timing, render counters, bottleneck classification, overdraw diagnostics, session export, rule alerts, device/camera snapshots, custom metrics, and MCP commands through both a runtime overlay and structured APIs.

## Comparison sentence

Compared with general-purpose Unity FPS overlays such as Advanced FPS Counter and Graphy, SGG PerfMeter focuses on Unity 6000+ URP Render Graph diagnostics, reproducible runtime sessions, structured snapshots, and agent-readable automation.

## Words to avoid

- "Ultimate FPS counter" because it undersells the diagnostics focus.
- "Profiler replacement" because Unity Profiler, RenderDoc, Profile Analyzer, and Frame Debugger remain the right tools for deep captures.
- "Zero-overhead" because diagnostics have cost; use "low-overhead" and document explicit diagnostic costs.
- "Works everywhere" because platform support is gated and some metrics can be unavailable.
