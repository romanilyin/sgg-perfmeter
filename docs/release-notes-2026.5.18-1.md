# SGG PerfMeter 2026.5.18-1

Private release candidate for SGG PerfMeter, a low-overhead runtime performance meter and agent-readable profiling API for Unity 6000.4+ URP projects.

The repository remains private. Do not publish these notes as a public GitHub Release until the public switch is explicitly approved.

## What Is Included

- Unity Git UPM package `com.sungeargames.perfmeter` under `Assets/Scripts/SGG.PerfMeter`.
- Public runtime API `SGG.PerfMeter.PerformanceMeter` for lifecycle, status, metrics, overlay controls, target FPS, overdraw measurement, and heatmap visibility.
- `FrameTimingManager` timing collection and `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, index uploads, and GPU memory.
- UI Toolkit runtime overlay with FPS/lows/spikes, compact/text/graph/full modes, configurable corner, target FPS line, and CPU/GPU graphs.
- URP 17 Render Graph feature with opt-in overlay marker, numerical overdraw measurement pass, and visual overdraw heatmap pass.
- Bounded numerical overdraw measurement using a hidden replacement shader, fragment atomic counter, capability gating, and `AsyncGPUReadback`.
- Visual overdraw heatmap using `Hidden/SGG/PerfMeter/OverdrawHeatmap` and additive blending over the active camera color target.
- Editor setup/runtime window for Frame Timing Stats, renderer feature installation, initialization snippets, runtime controls, overdraw measurement, and heatmap toggles.
- MCP command metadata and handlers for setup, runtime status/control, latest metrics, overlay control, overdraw measurement, and heatmap visibility.
- EditMode API/classifier tests and PlayMode runtime smoke tests.
- Android Development APK smoke-build helper and Android runtime smoke bootstrap.

## Coordinates

- Source tag, planned: `2026.5.18-1`.
- Unity package: `com.sungeargames.perfmeter@2026.5.18-1`.
- Private Git UPM: `git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.18-1`.
- License: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.

## Verification

Baseline already completed before release-prep docs:

- Compile passed in `Logs/opencode-overdraw-heatmap-compile.log`.
- EditMode passed `18/18` in `Logs/opencode-overdraw-heatmap-editmode-results.xml`.
- PlayMode passed `2/2` in `Logs/opencode-overdraw-heatmap-playmode-results.xml`.
- Release docs compile passed in `Logs/opencode-release-docs-compile.log`.
- Release docs EditMode passed `18/18` in `Logs/opencode-release-docs-editmode-results.xml`.
- Release docs PlayMode passed `2/2` in `Logs/opencode-release-docs-playmode-results.xml`.
- Android S23 Vulkan smoke validation passed with GPU timing available and `overdraw_state=Completed`.
- Android S23 OpenGLES3 fallback validation passed with expected `overdraw_state=Unsupported`.

Release tagging should re-run the local gates listed in `docs/release-2026.5.18-1.md`.

## Known Limitations

- GPU timings can be delayed, unavailable, or unreliable depending on platform and graphics API.
- OpenGL/OpenGLES is treated as degraded mode for numerical overdraw instrumentation.
- Overdraw measurement and heatmap are diagnostic and add extra scene redraw work while active/visible.
- Full self-overhead subtraction is not finalized.
- Full zero-allocation overlay refresh is not implemented yet; overlay text refresh is throttled.
- Render Graph pass/aliasing/merge analytics and CSV/JSON session export are not implemented yet.
- Broader device validation is still needed beyond the current Galaxy S23 Vulkan/GLES smoke coverage.
