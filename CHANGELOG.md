# Changelog

## 2026.6.11-1

- Added SRP detection without a hard dependency on HDRP.
- Added HDRP camera snapshots through reflection over `HDAdditionalCameraData`.
- Added optional HDRP assembly and runtime-registered HDRP Custom Pass integration for HDRP `17.4+`.
- Extended `perfmeter.rendergraph.snapshot` with render pipeline, integration name, and observed injection point metadata for URP Render Graph and HDRP Custom Pass diagnostics.
- Updated setup/status flows to understand HDRP, skip URP Renderer Feature installation in HDRP projects, and report HDRP Custom Pass availability.
- Marked HDRP overdraw and heatmap as explicitly unsupported while keeping FPS, CPU, GPU, memory, sessions, alerts, camera, device, and MCP diagnostics available.
- Updated package metadata to version `2026.6.11-1`.

## 2026.6.5-2

- Published `com.sungeargames.perfmeter` to the public npm registry for Unity Package Manager scoped-registry installs.
- Added npm registry installation instructions to localized user documentation.
- Kept Git UPM install examples available with the `2026.6.5-2` tag.

## 2026.6.5-1

- Prepared the first public release for GitHub/Git UPM distribution.
- Replaced Unity 6000.4+ camera snapshot runtime IDs with `CameraEntityId` / MCP `camera_entity_id` based on `Object.GetEntityId()` while keeping `CameraInstanceId` fallback for older Unity versions.
- Updated the UI Toolkit overlay scale path to `VisualElement.style.scale` to avoid Unity 6000.4 obsolete API warnings.
- Cleaned internal development documentation and moved public-facing documentation to repository docs.
- Updated package-local documentation to minimal GitHub documentation links.

## 2026.5.20-1

- Prepared the initial package baseline for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
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
- Added package metadata URLs, package-local README/CHANGELOG, security policy, contributing policy, CODEOWNERS, PR template, issue routing, and repository release metadata.
