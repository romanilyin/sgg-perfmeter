# Changelog

## Unreleased

- Added project-owned JSON settings, a setup-window `Presets` tab, and zero-code runtime auto-start through `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Added device/environment snapshots with display layout, monitor names, and `perfmeter.device.info` MCP output.
- Added camera snapshots with transform/projection/URP camera settings and `perfmeter.camera.snapshot` MCP output.
- Added JSON-backed overlay presets/modules, module-filtered overlay output, setup-window module toggles, and MCP `overlay_preset` / `overlay_modules` support.
- Added session recorder APIs with bounded in-memory samples, safe sample-copy access, session summaries, status fields, settings/device/camera metadata capture, JSON/CSV export, and MCP session start/stop/summary/export commands.
- Added rule-based alerts with active/fired alert status, separate structured-log/callback/Editor-warning cooldowns, callback events, and MCP `perfmeter.alerts.latest/clear` commands.
- Added session warm-up seconds, runtime `ResetStats`, scene-load reset/ignore options, whole-run/current-scene summaries, worst-frame summaries, and MCP `perfmeter.runtime.reset_stats`.
- Added explicit collection modes (`Stopped`, `Background`, `Overlay`, `OverdrawDiagnostic`) with JSON/setup/runtime/MCP controls.

## 2026.5.18-1

- Prepared the private release candidate for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Added `PerformanceMeter` runtime API, immutable status/metrics snapshots, overlay controls, target FPS, overdraw measurement, and heatmap visibility controls.
- Added `FrameTimingManager` timings and `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, index uploads, and GPU memory.
- Added UI Toolkit overlay modes and CPU/GPU graphs.
- Added URP Render Graph feature with opt-in overlay marker, numerical overdraw measurement, and visual overdraw heatmap passes.
- Added Editor setup/runtime controls and MCP command metadata for agent workflows.
- Added EditMode and PlayMode tests plus Android Vulkan/GLES smoke-validation helpers.
