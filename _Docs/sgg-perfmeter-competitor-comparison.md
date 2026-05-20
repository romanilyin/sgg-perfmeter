# SGG PerfMeter vs Advanced FPS Counter vs Graphy

This file is the historical competitor-derived roadmap reference kept at its original path. The current marketing comparison lives in `_Docs/marketing/competitor-comparison.en.md` and `_Docs/marketing/competitor-comparison.ru.md`.

## Current Positioning

SGG PerfMeter is a Unity `6000.4+` URP `17.4+` Render Graph diagnostics layer and agent-readable profiling API, not just another FPS counter.

Advanced FPS Counter and Graphy are strong general-purpose visual overlays. They are useful references for drop-in usability, visual clarity, presets, module states, cached formatting, examples, and README presentation.

SGG PerfMeter should keep its own focus: FrameTimingManager CPU/GPU timing, render-thread and present-wait visibility, ProfilerRecorder render counters, SRP Batcher / BRG / upload metrics, bottleneck classification, overdraw diagnostics, structured device and camera snapshots, session export, rule alerts, custom metrics, Render Graph diagnostics, and agent-readable runtime/MCP APIs.

## Current Feature State

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary goal | URP Render Graph diagnostics + structured API for humans and agents | Configurable in-game FPS/memory/device counter | Visual stats monitor/debugger with graphs/modules/presets |
| UI backend | UI Toolkit overlay | uGUI Canvas/Text labels | uGUI Text/Image modules |
| Timing source | `FrameTimingManager` + rolling stats | Runtime frame/update sampling | `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | CPU frame, main thread, render thread, present wait, GPU when available | No equivalent split | No equivalent split |
| Bottleneck classification | GPU, CPU main, CPU render, present-limited, balanced, unknown | No equivalent | No equivalent |
| Render counters | Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | No URP/SRP counter set | No URP/SRP counter set |
| Device info | Structured Unity/platform/CPU/GPU/API/display/monitor/window snapshot | Visual device counter | Advanced device panel |
| Camera reproducibility | Structured camera snapshot with scene/transform/projection/URP settings | No equivalent | No equivalent |
| Render Graph diagnostics | Safe feature/pass/resource snapshot with degraded counts | No | No |
| Overdraw | Numerical measurement + visual heatmap through URP Render Graph | No | No |
| Sessions | Bounded recorder, warm-up, scene scope, worst frames, JSON/CSV export | Not a primary feature | Roadmap idea |
| Alerts | Metrics rules, structured logs, callbacks, Editor warning cooldowns, MCP alerts | FPS level callback | Debug packets concept |
| Custom metrics | Public provider API, session JSON, MCP, bounded overlay rows | No direct equivalent | Roadmap-like concept |
| Settings | Project-owned Resources JSON, no `ScriptableObject` settings | Inspector-driven component settings | Serialized prefab/inspector settings |
| Hotkeys | Out of current scope; use API/MCP/setup controls | Yes | Yes |
| Audio | Intentionally out of scope | No | Yes |
| Automation | MCP command surface is a first-class differentiator | No | No |

## Current Roadmap Decisions

- Session JSON/CSV export is implemented.
- Rule alerts are implemented without sound/audio actions.
- Overlay presets/modules and JSON tunables are implemented.
- Device/environment and camera snapshots are implemented.
- Custom metric providers and bounded custom metric overlay rows are implemented.
- Render Graph analytics snapshot is implemented as a conservative degraded-safe spike.
- Player hotkeys remain out of scope until there is explicit product demand.
- Audio/spectrum modules remain out of scope.
- Screenshot/debug-break alert actions are not default behavior.

## What To Reuse From Competitor Research

- Keep the README visually clear and direct.
- Keep samples easy to import and understand.
- Keep module/preset concepts understandable.
- Keep cached formatting and dirty-update ideas where they fit UI Toolkit.
- Keep AFPS/Graphy as UX references only; do not copy AFPS code/assets and do not import Graphy as a dependency.

## What Not To Claim

- Do not claim SGG PerfMeter replaces Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.
- Do not claim zero-overhead; use low-overhead and document diagnostic costs.
- Do not claim all-platform/all-pipeline support.
