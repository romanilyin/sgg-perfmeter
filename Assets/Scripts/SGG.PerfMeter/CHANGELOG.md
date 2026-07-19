# Changelog

## 2026.7.19-1

- Added lifecycle, steady-state, and explicit capture provenance to alert snapshots and history.
- Added alert-history boundaries and bounded MCP capture-scope commands.
- Replaced runtime-created UI Toolkit panel settings with a serialized ICU-enabled package resource.
- Validated the overlay in a Unity 6000.5 Windows Development Player without ICU or text-layout exceptions.

## 2026.7.16-1

- Made MCP session exports atomic with deterministic existing-file conflict results.
- Added package-version provenance and separate configured/effective session settings in export schema v2.
- Added truthful requested/active/deferred overlay status and Editor-session visibility persistence across Play Mode transitions.
- Detached hidden overlay UI before repaint requests and removed the obsolete panel font fallback on Unity 6000.5+.

## 2026.6.28-1

- Updated package and GitHub user documentation in all languages to describe Unity 6 URP+HDRP support consistently.
- Clarified URP Render Graph overdraw/heatmap support and HDRP overdraw/heatmap unsupported state while keeping HDRP core diagnostics available.
- Replaced package-local README links with direct GitHub URLs.
- Updated package metadata and install examples to version `2026.6.28-1`.

## 2026.6.11-1

- Added SRP detection without a hard dependency on HDRP.
- Added HDRP camera snapshots through reflection over `HDAdditionalCameraData`.
- Added optional HDRP assembly and runtime-registered HDRP Custom Pass integration for HDRP `17.4+`.
- Extended `perfmeter.rendergraph.snapshot` with render pipeline, integration name, and observed injection point metadata for URP Render Graph and HDRP Custom Pass diagnostics.
- Updated setup/status flows to understand HDRP, skip URP Renderer Feature installation in HDRP projects, and report HDRP Custom Pass availability.
- Marked HDRP overdraw and heatmap as explicitly unsupported while keeping FPS, CPU, GPU, memory, sessions, alerts, camera, device, and MCP diagnostics available.

## 2026.6.5-2

- Published `com.sungeargames.perfmeter` to the public npm registry for Unity Package Manager scoped-registry installs.
- Added npm registry installation instructions to package and GitHub user documentation.
- Kept Git UPM install examples available with the `2026.6.5-2` tag.

## 2026.6.5-1

- Prepared the first public release for GitHub/Git UPM distribution.
- Replaced Unity 6000.4+ camera snapshot runtime IDs with `CameraEntityId` / MCP `camera_entity_id` based on `Object.GetEntityId()` while keeping `CameraInstanceId` fallback for older Unity versions.
- Updated the UI Toolkit overlay scale path to `VisualElement.style.scale` to avoid Unity 6000.4 obsolete API warnings.
- Updated package-local documentation to minimal GitHub documentation links.

## 2026.5.20-1

- Prepared the initial package baseline for `com.sungeargames.perfmeter` / `SGG PerfMeter`.
- Added `PerformanceMeter` runtime API, immutable status/metrics snapshots, collection modes, overlay controls, target FPS, session recording/export, alerts, custom metrics, device/camera snapshots, Render Graph snapshots, overdraw measurement, and heatmap visibility controls.
- Added `FrameTimingManager` timings and `ProfilerRecorder` counters for render, memory, SRP Batcher, BRG/GRD, index uploads, and GPU memory.
- Added UI Toolkit overlay modes, CPU/GPU graphs, presets/modules, JSON-tunable presentation, allocation-conscious text refresh, and bounded custom metric rows.
- Added URP Render Graph feature with opt-in overlay marker, numerical overdraw measurement, visual overdraw heatmap passes, and safe Render Graph analytics snapshot.
- Added project-owned JSON settings, a setup-window `Presets` tab, JSON tunables, and zero-code runtime auto-start through `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Added device/environment snapshots, camera snapshots, session recorder JSON/CSV export, rule alerts, custom metric providers, and MCP commands for agent-driven workflows.
- Added Package Manager samples for bootstrap/zero-code settings, runtime workflows, editor automation, MCP command examples, session export, alerts, overdraw/heatmap, and camera snapshot replay.
- Added EditMode and PlayMode tests plus Android Vulkan/GLES smoke-validation helpers.
- Added package documentation, package metadata URLs, license/notice files, and repository release metadata.
