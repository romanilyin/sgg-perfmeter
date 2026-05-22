# Changelog

## Unreleased

- Replaced Unity 6000.4+ camera snapshot runtime IDs with `CameraEntityId` / MCP `camera_entity_id` based on `Object.GetEntityId()` while keeping `CameraInstanceId` fallback for older Unity versions.
- Updated the UI Toolkit overlay scale path to `VisualElement.style.scale` to avoid Unity 6000.4 obsolete API warnings.

## 2026.5.20-1

- Prepared the private release candidate for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Added `PerformanceMeter` runtime API, immutable status/metrics snapshots, collection modes, overlay controls, target FPS, session recording/export, alerts, custom metrics, device/camera snapshots, Render Graph snapshots, overdraw measurement, and heatmap visibility controls.
- Added `FrameTimingManager` timings and `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, index uploads, and GPU memory.
- Added UI Toolkit overlay modes, CPU/GPU graphs, presets/modules, JSON-tunable presentation, allocation-conscious text refresh, and bounded custom metric rows.
- Added URP Render Graph feature with opt-in overlay marker, numerical overdraw measurement, visual overdraw heatmap passes, and safe Render Graph analytics snapshot.
- Added project-owned JSON settings, a setup-window `Presets` tab, JSON tunables, and zero-code runtime auto-start through `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Added device/environment snapshots, camera snapshots, session recorder JSON/CSV export, rule alerts, custom metric providers, and MCP commands for agent-driven workflows.
- Added Package Manager samples for bootstrap/zero-code settings, runtime workflows, editor automation, MCP command examples, session export, alerts, overdraw/heatmap, and camera snapshot replay.
- Added EditMode and PlayMode tests plus Android Vulkan/GLES smoke-validation helpers.
- Added package documentation, package metadata URLs, license/notice files, release-readiness docs, and marketing/positioning docs.
