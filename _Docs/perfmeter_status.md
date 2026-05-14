# SGG PerfMeter Status

## Package State

- Package metadata is `com.sungeargames.perfmeter` / `SGG PerfMeter`; no `sky` or `weather` package identity remnants were found under `Assets/Scripts/SGG.PerfMeter/`.
- User-facing docs are maintained in English and Russian under `Assets/Scripts/SGG.PerfMeter/.Documentation/`.
- Runtime now includes opt-in numerical overdraw measurement through a hidden replacement shader, fragment-stage atomic counter buffer, and `AsyncGPUReadback`.
- Runtime snapshots expose FPS/lows/spikes, render, SRP Batcher, BRG/GRD, index upload, memory, timing, and overdraw values through API, MCP JSON, and overlay output.
- `DrawCalls`, `Batches`, BRG draw calls, and BRG instances are aggregates of Unity 6000 component `ProfilerRecorder` counters, not single recorder names.
- Runtime overlay corner placement is configurable through `PerfMeter.SetOverlayCorner(...)`; display mode is configurable through `PerfMeter.SetOverlayMode(...)` with `FpsOnly`, `TextCompact`, `Graphs`, and `Full`; target line budget is configurable through `PerfMeter.SetTargetFps(...)` for 15/30/60/90/120/144/240 FPS; default placement is `TopRight`/`Full`/`60 FPS`, with stacked CPU graph layout, softer colored graph legends, fixed-width graph numbers, placeholder formatting for unavailable samples/counters, text min/max history, and generated UI Toolkit theme/text settings for reliable runtime text rendering.
- Editor setup window now has `Setup` and `Runtime` tabs under `SGG/Perfmeter/Setup`: setup options update the copied bootstrap code, while runtime controls are enabled only in Play Mode for target FPS, overlay mode/corner/visibility, and short overdraw capture.

## Verification Notes

- Use Unity 6000.4.5f1 batchmode compile as the reliable local verification path.
- Latest local verification passed with `Logs/opencode-target-fps-compile.log`.
- Do not spend time on the known local `-runTests` issue unless the project verification setup changes.
- Manual target-device validation is still needed for GPU timings, overdraw behavior, and platform-specific ProfilerRecorder counter availability.

## Handoff Notes

- Next meaningful implementation work should focus on overdraw heatmap output, target-device validation, or Render Graph analytics.
- `ProjectSettings/ProjectSettings.asset` currently has `enableFrameTimingStats: 1`; keep it enabled before depending on `FrameTimingManager` in builds.
