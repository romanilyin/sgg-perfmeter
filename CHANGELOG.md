# Changelog

## Unreleased

- Added project-owned JSON settings, a setup-window `Presets` tab, and zero-code runtime auto-start through `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Added device/environment snapshots with display layout, monitor names, and `perfmeter.device.info` MCP output.
- Added camera snapshots with transform/projection/URP camera settings and `perfmeter.camera.snapshot` MCP output.
- Added session recorder APIs with bounded in-memory samples, safe sample-copy access, session summaries, status fields, settings/device/camera metadata capture, JSON/CSV export, and MCP session start/stop/summary/export commands.
- Added rule-based alerts with active/fired alert status, separate structured-log/callback/Editor-warning cooldowns, callback events, and MCP `perfmeter.alerts.latest/clear` commands.
- Added session warm-up seconds, runtime `ResetStats`, scene-load reset/ignore options, whole-run/current-scene summaries, worst-frame summaries, and MCP `perfmeter.runtime.reset_stats`.
- Added explicit collection modes (`Stopped`, `Background`, `Overlay`, `OverdrawDiagnostic`) with JSON/setup/runtime/MCP controls.
- Improved overlay text refresh with stable UI Toolkit field rows, cached enum text, reusable numeric formatting buffers, and dirty value-label assignment.
- Added Package Manager samples for bootstrap/zero-code settings, runtime workflows, editor automation, MCP command examples, session export, alerts, overdraw/heatmap, and camera snapshot replay.
- Added JSON tunables for overlay scale/opacity/font/refresh/history, alert thresholds/consecutive frames, session defaults, and overdraw default/max frame counts.
- Added custom metric providers for project-specific counters with session JSON export and MCP latest-metrics output.

## 2026.5.18-1

- Prepared the private release candidate for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Added runtime status and immutable metrics snapshots through `PerformanceMeter`.
- Added `FrameTimingManager` timing collection and zero-allocation `ProfilerRecorder` render, memory, SRP Batcher, BRG/GRD, index upload, and GPU memory counters.
- Added UI Toolkit runtime overlay with FPS, lows, spikes, compact/text/graph/full modes, configurable corner, target FPS line, and CPU/GPU graphs.
- Added URP 17 Render Graph renderer feature with opt-in overlay marker, numerical overdraw measurement, and visual overdraw heatmap passes.
- Added bounded numerical overdraw measurement with replacement shader instrumentation, fragment atomic counter, capability gating, and `AsyncGPUReadback` session safety.
- Added visual overdraw heatmap with additive replacement shader and runtime/API/MCP/setup controls.
- Added Editor setup/runtime window for Frame Timing Stats, renderer feature installation, initialization snippets, overlay controls, target FPS, overdraw measurement, and heatmap toggles.
- Added MCP command metadata and handlers for setup, runtime status/control, latest metrics, overlay control, overdraw measurement, and heatmap visibility.
- Added EditMode API/classifier/safety tests, PlayMode runtime smoke tests, Android S23 Vulkan smoke validation, and Android OpenGLES3 degraded-mode validation.
- Added release-readiness documentation, package metadata URLs, package-local README/CHANGELOG, security policy, contributing policy, CODEOWNERS, PR template, issue routing, and manual-only release workflow.
