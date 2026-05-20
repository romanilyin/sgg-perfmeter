# Changelog

## Unreleased

- No changes yet.

## 2026.5.20-1

- Prepared the private release candidate for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Added runtime status and immutable metrics snapshots through `PerformanceMeter`.
- Added `FrameTimingManager` timing collection and zero-allocation `ProfilerRecorder` render, memory, SRP Batcher, BRG/GRD, index upload, and GPU memory counters.
- Added UI Toolkit runtime overlay with FPS, lows, spikes, compact/text/graph/full modes, configurable corner, target FPS line, CPU/GPU graphs, presets/modules, JSON-tunable presentation, and allocation-conscious text refresh.
- Added URP 17 Render Graph renderer feature with opt-in overlay marker, numerical overdraw measurement, visual overdraw heatmap passes, and safe Render Graph analytics snapshot.
- Added bounded numerical overdraw measurement with replacement shader instrumentation, fragment atomic counter, capability gating, and `AsyncGPUReadback` session safety.
- Added visual overdraw heatmap with additive replacement shader and runtime/API/MCP/setup controls.
- Added project-owned JSON settings, a setup-window `Presets` tab, JSON tunables, and zero-code runtime auto-start through `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Added device/environment snapshots with display layout, monitor names, and `perfmeter.device.info` MCP output.
- Added camera snapshots with transform/projection/URP camera settings and `perfmeter.camera.snapshot` MCP output.
- Added session recorder APIs with bounded in-memory samples, warm-up, scene scope, worst-frame summaries, settings/device/camera metadata capture, JSON/CSV export, and MCP session start/stop/summary/export commands.
- Added rule-based alerts with active/fired alert status, separate structured-log/callback/Editor-warning cooldowns, callback events, JSON-tunable thresholds, and MCP `perfmeter.alerts.latest/clear` commands.
- Added explicit collection modes (`Stopped`, `Background`, `Overlay`, `OverdrawDiagnostic`) and runtime `ResetStats` with JSON/setup/runtime/MCP controls.
- Added custom metric providers for project-specific counters with session JSON export, MCP latest-metrics output, and bounded overlay rows behind the `CustomMetrics` module.
- Added Package Manager samples for bootstrap/zero-code settings, runtime workflows, editor automation, MCP command examples, session export, alerts, overdraw/heatmap, and camera snapshot replay.
- Added EditMode API/classifier/session/export/MCP/overlay/package-samples tests, PlayMode runtime smoke tests, Android S23 Vulkan smoke validation, and Android OpenGLES3 degraded-mode validation.
- Added release-readiness documentation, package metadata URLs, package-local README/CHANGELOG, security policy, contributing policy, CODEOWNERS, PR template, issue routing, manual-only release workflow, and marketing/positioning docs.
