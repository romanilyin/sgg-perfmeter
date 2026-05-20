# SGG PerfMeter 2026.5.18-1

Private release candidate for SGG PerfMeter, a low-overhead runtime performance diagnostics layer and agent-readable profiling API for Unity 6000.4+ URP Render Graph projects.

The repository remains private. Do not publish these notes as a public GitHub Release until the public switch is explicitly approved.

## What Is Included

- Unity Git UPM package `com.sungeargames.perfmeter` under `Assets/Scripts/SGG.PerfMeter`.
- Public runtime API `SGG.PerfMeter.PerformanceMeter` for lifecycle, collection modes, status, metrics, overlay controls, target FPS, sessions, alerts, custom metrics, device/camera snapshots, Render Graph snapshots, overdraw measurement, and heatmap visibility.
- `FrameTimingManager` timing collection and `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, index uploads, and GPU memory.
- UI Toolkit runtime overlay with FPS/lows/spikes, compact/text/graph/full modes, configurable corner, target FPS line, CPU/GPU graphs, presets/modules, JSON-tunable presentation, allocation-conscious text refresh, and bounded custom metric rows.
- Project-owned JSON settings for zero-code setup, collection mode, overlay presets/modules/tunables, rule defaults, session defaults, and overdraw limits.
- Session recorder with warm-up, scene scope, worst-frame summaries, JSON/CSV export, settings/device/camera metadata, and MCP session commands.
- Rule alerts with structured logs, callbacks, Editor warning cooldowns, JSON-tunable thresholds, and MCP alert commands.
- Custom metric providers with API, session JSON export, MCP latest-metrics output, and optional overlay rows.
- Device/environment snapshots and camera snapshots for reproducible performance reports.
- URP 17 Render Graph feature with opt-in overlay marker, numerical overdraw measurement pass, visual overdraw heatmap pass, and safe Render Graph analytics snapshot.
- Bounded numerical overdraw measurement using a hidden replacement shader, fragment atomic counter, capability gating, and `AsyncGPUReadback`.
- Visual overdraw heatmap using `Hidden/SGG/PerfMeter/OverdrawHeatmap` and additive blending over the active camera color target.
- Editor setup/runtime/presets window for Frame Timing Stats, renderer feature installation, zero-code JSON settings, initialization snippets, runtime controls, overdraw measurement, and heatmap toggles.
- MCP command metadata and handlers for setup, runtime status/control/reset/mode, latest metrics, device info, camera snapshot, Render Graph snapshot, overlay control, sessions, alerts, overdraw measurement, and heatmap visibility.
- Package Manager samples for bootstrap/zero-code settings, runtime workflows, editor automation, MCP examples, session export, alerts, overdraw/heatmap, and camera snapshot replay.
- EditMode API/classifier/session/export/MCP/overlay/package-samples tests and PlayMode runtime smoke tests.
- Android Development APK smoke-build helper and Android runtime smoke bootstrap.

## Coordinates

- Source tag, planned: `2026.5.18-1`.
- Unity package: `com.sungeargames.perfmeter@2026.5.18-1`.
- Private Git UPM: `git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.18-1`.
- License: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.

## Verification

Latest baseline already completed before this documentation sync:

- Iteration 43 Render Graph analytics compile passed in `Logs/opencode-iter43-rendergraph-compile.log`.
- Iteration 43 EditMode passed `54/54` in `Logs/opencode-iter43-rendergraph-editmode-results.xml`.
- Iteration 43 PlayMode passed `5/5` in `Logs/opencode-iter43-rendergraph-playmode-results.xml`.
- Iteration 43 `git diff --check` passed.
- Android S23 Vulkan smoke validation passed with GPU timing available and `overdraw_state=Completed`.
- Android S23 OpenGLES3 fallback validation passed with expected `overdraw_state=Unsupported`.

Release tagging should re-run the local gates listed in `docs/release-2026.5.18-1.md`.

## Known Limitations

- GPU timings can be delayed, unavailable, or unreliable depending on platform and graphics API.
- OpenGL/OpenGLES is treated as degraded mode for numerical overdraw instrumentation.
- Overdraw measurement and heatmap are diagnostic and add extra scene redraw work while active/visible.
- Full self-overhead subtraction is not finalized.
- Overlay refresh is allocation-conscious and throttled, but changed numeric values and graph legend labels can still materialize managed strings at the refresh interval.
- Render Graph pass/resource/aliasing/merge analytics are conservative; internal Unity counters can report `-1` with a warning when URP does not expose them safely.
- Broader device validation is still needed beyond the current Galaxy S23 Vulkan/GLES smoke coverage.
