# Changelog

## Unreleased

- No unreleased changes.

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
