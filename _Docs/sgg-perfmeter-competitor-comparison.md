# SGG PerfMeter vs Advanced FPS Counter vs Graphy

Scope: this comparison is based on the current `romanilyin/sgg-perfmeter` main branch inspected on 2026-05-19, the uploaded `AdvancedFPSCounter.zip` package (`net.codestage.advanced-fps-counter`, version `1.5.7`), and `Tayx94/graphy` master (`com.tayx.graphy`, version `3.0.5`). This is a product and architecture comparison, not a measured runtime benchmark.

## Short positioning

`SGG PerfMeter` should be documented as a **URP Render Graph performance diagnostics package and agent-readable profiling API**, not as just another FPS counter.

`Advanced FPS Counter` and `Graphy` are strong general-purpose in-game stats overlays. They are optimized for quick visual feedback, customization, hotkeys, device information, and convenient runtime usage. `SGG PerfMeter` has a narrower but deeper target: Unity `6000.4+`, URP `17.4+`, FrameTimingManager / ProfilerRecorder metrics, URP Render Graph integration, bottleneck classification, overdraw diagnostics, and structured API/MCP access for editor automation and AI agents.

Suggested documentation sentence:

> Compared with general-purpose Unity FPS counters such as Advanced FPS Counter and Graphy, SGG PerfMeter focuses on URP Render Graph diagnostics, structured runtime snapshots, bottleneck classification, render counters, overdraw measurement, and agent-readable automation. It is not intended to replace Unity Profiler, RenderDoc, or visual overlay-only FPS tools.

## Summary matrix

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary goal | URP performance diagnostics + structured API for humans and agents | Lightweight configurable in-game FPS/memory/device counter | Visual stats monitor/debugger with graphs, modules, presets |
| Unity target | Unity `6000.4+`, URP `17.4+`, Render Graph path | Unity `2019.3+` package metadata | Unity `2019.4+` package metadata; README also references older support line |
| Render pipeline focus | URP-specific, Render Graph renderer feature | General Unity, includes built-in and SRP render-time helper | General Unity uGUI overlay |
| FPS source | FrameTimingManager timing data, not only `Time.deltaTime` | FPS from runtime frame/update sampling; ms derived from FPS | FPS from `Time.unscaledDeltaTime` and sample history |
| CPU/GPU timing | CPU frame, main thread, render thread, present wait, GPU frame time when available | FPS/ms and approximate camera render time | FPS/ms and graph history; no CPU main/render/GPU breakdown |
| Bottleneck classification | Yes: GPU bound, CPU main thread, CPU render thread, present/VSync limited, balanced/unknown | No equivalent classification | No equivalent classification |
| Rendering counters | Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, index upload, memory counters when available | No URP/SRP counter set | No URP/SRP counter set |
| Overdraw diagnostics | Numerical overdraw measurement and visual heatmap via URP Render Graph | No | No |
| UI approach | UI Toolkit overlay with text/graph/full modes | uGUI Text labels generated under Canvas | uGUI Text/Image modules with shader-based graph rendering |
| Overlay modes | `FpsOnly`, `TextCompact`, `Graphs`, `Full` | Normal/background/disabled operation; per-counter anchors | FULL/TEXT/BASIC/BACKGROUND/OFF per module; presets |
| Device information | Currently basic graphics device info in status; not a full visible module yet | Platform, CPU, GPU, RAM, screen, model | CPU, OS, RAM, GPU/API/VRAM, screen/window/XR information |
| Runtime API | Immutable status and metrics snapshots, safe reads before runtime starts | Singleton and per-counter data API | `GraphyManager` properties/getters and `GraphyDebugger` API |
| Agent/editor automation | MCP command metadata for setup, runtime control, metrics, overlay, overdraw | No | No |
| Alerts/debug rules | Not implemented yet | FPS level change callback | Debug packets: conditions + actions such as log, screenshot, editor break, callbacks/events |
| Export/benchmark sessions | Pending / not implemented yet | Not a main feature in uploaded package | Roadmap mentions storing FPS benchmark data and dumping data |
| Customization | Limited runtime overlay options; many visual values are hard-coded today | Rich inspector customization: font, color, style, background, shadow/outline, anchors, intervals | Rich module states, colors, thresholds, graph resolutions, hotkeys, presets |
| Hotkeys | MCP/runtime calls; no dedicated in-player hotkey layer yet | Hotkey and optional circle gesture | Hotkeys for active toggle and mode cycling |
| License consideration | Stinger Royalty-Free EULA | Uploaded package has no license file; treat as third-party commercial/Asset Store code unless license says otherwise | MIT |

## Key differentiators to emphasize in SGG PerfMeter docs

### 1. SGG PerfMeter is diagnostics-first, not overlay-first

Advanced FPS Counter and Graphy are excellent when the user wants to drop a visual counter into a scene and immediately see FPS, memory, and device information. SGG PerfMeter should instead be positioned as a tool for answering diagnostic questions:

- Is the frame CPU-bound, GPU-bound, render-thread-bound, or present/VSync-limited?
- Are render counters suspicious: draw calls, SetPass calls, batches, vertices, SRP Batcher / BRG / GRD, uploads?
- Are GPU timings unavailable or unreliable on the current platform/API?
- Is overdraw suspicious enough to justify a bounded diagnostic pass?
- Can an editor tool or agent read the state without scraping UI text?

### 2. SGG PerfMeter is more modern and more specific

Graphy and Advanced FPS Counter are general-purpose tools. SGG PerfMeter can be stronger by staying specific:

- Unity `6000.4+` instead of broad legacy support.
- URP `17.4+` / Render Graph instead of all render pipelines.
- `ProfilerRecorder` / `FrameTimingManager` / `AsyncGPUReadback` integration.
- Explicit degraded states when counters or timings are unavailable.
- Structured metrics snapshots instead of UI text as the source of truth.

### 3. SGG PerfMeter already has a stronger automation story

Neither Advanced FPS Counter nor Graphy expose an MCP-oriented command surface. SGG PerfMeter already has commands for setup, runtime status/control, latest metrics, overlay changes, overdraw measurement, and overdraw heatmap control. This should be a primary selling point in docs, especially for AI-agent workflows.

Suggested documentation sentence:

> SGG PerfMeter exposes the same diagnostic data to both the runtime overlay and automation APIs. This makes it suitable for editor tools, CI-style validation, and AI agents that need structured metrics instead of screenshots or console parsing.

## What to add to SGG PerfMeter from these tools

Do not copy Advanced FPS Counter code unless the license explicitly allows it. Treat Advanced FPS Counter as product reference only. Graphy is MIT, but even there it is better to copy design ideas selectively rather than importing old uGUI architecture into a Unity 6000 URP package.

### P0 - high value, aligned with SGG PerfMeter

#### 1. Session recorder and CSV/JSON export

Inspired by Graphy's roadmap idea of benchmark storage/export and by the general usefulness of background monitoring in both competitors.

Add:

- `PerformanceMeter.StartSession(...)`
- `PerformanceMeter.StopSession()`
- `PerformanceMeter.GetSessionSummary()`
- `PerformanceMeter.ExportSessionJson(path)`
- `PerformanceMeter.ExportSessionCsv(path)`
- MCP commands: `perfmeter.session.start`, `perfmeter.session.stop`, `perfmeter.session.export`, `perfmeter.session.summary`

Capture:

- timestamp / frame / scene name / active renderer / graphics API;
- CPU frame, main, render, present wait;
- GPU frame and GPU timing availability;
- average FPS, 1% low, 0.1% low;
- spikes and severe spikes;
- draw calls, SetPass, batches, vertices;
- SRP Batcher, BRG/GRD, uploads;
- system/GC/GPU memory;
- bottleneck classification;
- overdraw result when measured;
- warnings and unavailable counters.

Why this is important: this converts SGG PerfMeter from an overlay into a repeatable profiling/validation tool.

#### 2. Rule/alert system without audio

Take the concept from Graphy's `GraphyDebugger`, but adapt it to SGG's structured metrics.

Add a small rule engine:

```csharp
public readonly struct PerfMeterRule
{
    public string Id;
    public PerfMeterMetric Metric;
    public PerfMeterComparison Comparison;
    public double Threshold;
    public int ConsecutiveFrames;
    public float CooldownSeconds;
}
```

Possible triggers:

- average FPS below target;
- 1% low below threshold;
- GPU frame time over budget;
- CPU main thread over budget;
- render thread over budget;
- present wait high;
- draw calls or SetPass over threshold;
- GPU timing unavailable;
- overdraw ratio above threshold;
- memory growth over session.

Possible actions:

- add alert to status snapshot;
- write structured log entry;
- invoke C# callback;
- optionally create editor warning;
- optionally capture screenshot in Editor/Development builds;
- expose fired alerts through MCP.

Avoid by default:

- sound alerts;
- unconditional `Debug.Break()` in players;
- heavy side effects unless explicitly enabled.

#### 3. Overlay presets and module visibility

SGG already has overlay modes, but Graphy has a stronger module/preset model and Advanced FPS Counter has good per-counter control.

Add presets such as:

- `Minimal`: FPS, 1% low, bottleneck.
- `Timing`: CPU frame/main/render/present/GPU + graph.
- `Rendering`: draw calls, SetPass, batches, vertices, SRP/BRG, uploads.
- `Memory`: system, GC, GPU memory.
- `Overdraw`: overdraw state/ratio/heatmap status.
- `FullDiagnostics`: current full mode.
- `AgentDebug`: compact text with all fields that are most useful for screenshots during agent work.

API example:

```csharp
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.Rendering);
PerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.Memory, false);
```

MCP should mirror this so agents can switch the overlay before taking screenshots.

#### 4. Device/environment snapshot

Both Advanced FPS Counter and Graphy do this well. Add a first-class `PerfMeterDeviceSnapshot`.

Fields:

- Unity version;
- platform and runtime platform;
- graphics API and graphics device name;
- GPU vendor/name/version when available;
- shader model / compute support / async readback support;
- CPU name and core count;
- system memory;
- screen/window size, refresh rate, DPI;
- URP asset/renderer name if discoverable;
- frame timing stats enabled/disabled;
- XR status if applicable.

Expose through:

- `PerformanceMeter.GetDeviceInfo()`;
- status snapshot summary fields;
- overlay optional module;
- session export metadata;
- MCP command `perfmeter.device.info`.

#### 5. JSON settings and setup-window presets

Advanced FPS Counter and Graphy are convenient because users can tune behavior in Inspector. SGG currently leans heavily on hard-coded overlay constants and runtime calls.

Add project-owned JSON settings edited through the setup window `Presets` tab. Do not use a `ScriptableObject` settings asset.

- default overlay visible/mode/corner/preset;
- target FPS;
- overlay scale, opacity, font size, update interval;
- graph history length;
- thresholds for warning colors and rules;
- enabled counters/modules;
- session recorder defaults;
- overdraw safety limits.

The setup window creates or updates `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`; runtime zero-code setup loads it through `Resources.Load<TextAsset>("SGG.PerfMeter/perfmeter-settings")` and auto-starts only when the JSON explicitly enables `enabled` and `autoStart`.

### P1 - valuable, but after core export/rules/config

#### 6. Warm-up, reset, and min/max controls

Advanced FPS Counter has useful ideas around skipping initialization spikes and resetting average/min/max on scene load.

Add:

- configurable warm-up frames/seconds before session recording;
- reset stats on scene load;
- manual `PerformanceMeter.ResetStats()`;
- min/max frame time and FPS in session summary;
- separate tracking for current scene vs whole run.

#### 7. Background/headless mode semantics

SGG can already hide the overlay while keeping runtime collection alive, but it should be documented and exposed as an explicit mode:

- `Overlay`: collect + show UI;
- `Background`: collect only, no UI allocations;
- `Stopped`: no collection;
- `OverdrawDiagnostic`: explicit temporary high-cost diagnostic state.

This is useful for builds, automated tests, and agent runs.

#### 8. Allocation-conscious formatting

Advanced FPS Counter uses cached numeric strings and dirty label updates; Graphy also uses non-alloc numeric helpers. SGG already tracks zero-allocation overlay refresh as pending.

Add:

- cached enum strings;
- cached labels and field names;
- fixed numeric formatting helpers;
- no per-refresh `StringBuilder.ToString()` for the whole overlay if UI Toolkit labels can be split into reusable fields;
- per-field dirty updates;
- allocation tests or profiler validation checklist.

Keep this aligned with SGG's UI Toolkit approach; do not port uGUI labels.

#### 9. Player hotkeys

Do not add player hotkeys yet. Keep overlay/session/overdraw control in runtime API, setup window, and MCP until there is explicit product demand for in-player shortcuts.

#### 10. Example scene and sample bootstrap scripts

Both competitors are easy to understand because they include examples/prefabs. Add small samples:

- minimal bootstrap;
- overlay preset switching;
- session recording/export;
- rules/alerts;
- overdraw measurement;
- MCP/editor automation demo.

### P2 - optional / only if demand appears

#### 11. World-space or XR overlay mode

Advanced FPS Counter has VR/world-space examples, and Graphy has XR-related device info. For SGG, this should be optional because UI Toolkit runtime overlay and URP diagnostics are the core. Add only if the package targets XR users.

#### 12. Alternative graph backend

Graphy's shader-based graph can be considered if UI Toolkit `Painter2D` graph rendering becomes measurable overhead. Do not implement this before profiling SGG's current graph cost.

#### 13. Custom metric providers

Graphy roadmap mentions templates for custom graph/text modules. SGG could provide a structured version:

```csharp
public interface IPerfMeterCustomMetricProvider
{
    string Id { get; }
    bool TryCollect(out PerfMeterCustomMetric metric);
}
```

Expose custom metrics in overlay, session export, and MCP output. Keep it constrained to avoid turning the package into a generic dashboard framework.

## What not to add

- Audio module or audio spectrum. The project does not need it.
- Direct code/assets copy from Advanced FPS Counter unless the license explicitly allows it.
- Full uGUI label system. SGG should keep UI Toolkit unless profiling proves a different backend is needed.
- Very broad legacy Unity support. SGG's advantage is modern Unity/URP specificity.
- All render pipelines. Adding HDRP/built-in support would dilute the URP Render Graph focus.
- Circle gesture toggles. Hotkeys and API/MCP control are enough.
- Heavy visual effects such as shadows/outline as a core feature. They add overhead and do not improve diagnostics.
- Force FPS as a core runtime feature. If added, keep it as an explicit debug utility because it changes VSync / target frame-rate behavior.
- Screenshot/debug-break alert actions enabled by default. They are useful in Editor/Development builds but dangerous as default player behavior.

## Recommended roadmap wording

Add to the roadmap:

1. Implement CSV/JSON session recorder with warm-up, scene metadata, device metadata, and summary stats.
2. Add threshold/rule alerts over structured metrics and expose fired alerts through runtime API and MCP.
3. Add JSON settings and a setup-window `Presets` tab for overlay, thresholds, recorder defaults, and overdraw limits; do not use `ScriptableObject` settings.
4. Add overlay presets and per-module visibility.
5. Add device/environment snapshot and optional overlay module.
6. Finish zero-allocation overlay refresh path and add validation notes/tests.
7. Add sample scenes/scripts for bootstrap, session recording, alerts, and overdraw diagnostics.

## README-ready compact comparison

SGG PerfMeter is intentionally different from general-purpose FPS overlays such as Advanced FPS Counter and Graphy. Those tools are excellent drop-in visual counters with configurable UI, hotkeys, device information, and general FPS/memory monitoring. SGG PerfMeter focuses on Unity `6000.4+` URP Render Graph diagnostics: FrameTimingManager CPU/GPU timing, render-thread and present-wait visibility, ProfilerRecorder render counters, SRP Batcher / BRG / upload metrics, bottleneck classification, bounded overdraw measurement, visual overdraw heatmap, and structured runtime snapshots.

The package is designed for both humans and automation. The runtime overlay is only one output surface; the same data is available through public APIs and MCP command metadata so editor tools and AI agents can read performance state without parsing screenshots, UI text, or Unity Console logs.
