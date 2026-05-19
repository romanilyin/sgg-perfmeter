# Changelog

## Unreleased

- Added project-owned JSON settings, a setup-window `Presets` tab, and zero-code runtime auto-start through `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.

## 2026.5.18-1

- Prepared the private release candidate for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Added `PerformanceMeter` runtime API, immutable status/metrics snapshots, overlay controls, target FPS, overdraw measurement, and heatmap visibility controls.
- Added `FrameTimingManager` timings and `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, index uploads, and GPU memory.
- Added UI Toolkit overlay modes and CPU/GPU graphs.
- Added URP Render Graph feature with opt-in overlay marker, numerical overdraw measurement, and visual overdraw heatmap passes.
- Added Editor setup/runtime controls and MCP command metadata for agent workflows.
- Added EditMode and PlayMode tests plus Android Vulkan/GLES smoke-validation helpers.
