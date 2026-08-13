# Changelog

## Unreleased

## 2026.8.13-2

- Fixed the raw frame-time strip to preserve the selected target-FPS budget across theme and font rebuilds, and simplified its severity palette to four colors by merging orange into yellow.
- Raised every built-in visual preset width cap to `780 px` so five metric cards and two budget bars fit on one row, and disabled CPU-core bars only in the zero-code Default preset.
- Fixed `Presets > Save` to persist the selected visual preset as the active project/build preset instead of saving only its source descriptor; recommended setup and the Android smoke bootstrap now preserve and apply existing project settings.

## 2026.8.13-1

- Added independent RenderDoc analyzer request/result/error JSON Schema v1 contracts with bounded semantic validation, golden fixtures, exact `uint64` strings, raw future unit/type preservation, explicit availability states, and authenticated capture/hash provenance.
- Added Editor-side experimental infrastructure for a package-owned out-of-process `qrenderdoc --python` replay analyzer with isolated marker-owned workspaces, capture hash verification, bounded action/counter output, timeout/cancel/process-tree termination, and clean replay shutdown without loading replay code into Unity; no public UI, API, or MCP entry point is exposed yet.
- Added deterministic explicit and semantic counter selection with vendor/API applicability, stable 16-counter batching and pass provenance, safe generic-counter retries, per-counter failures, fatal replay checks, and strict request/result binding without silent substitution.
- Documented the fail-closed native PIX timing boundary: circular pre-alert capture remains unavailable until Microsoft provides a documented bounded Windows API; generic PIX remains GPU-frame-only and non-authoritative.

## 2026.8.12-1

- Changed the raw CPU frame-time strip to use dark-green, blue, yellow, orange, and red bands at 80%, 100%, 120%, and 200% of the frame budget.

## 2026.8.11-2

- Added stable hysteresis-based bottleneck diagnostics with typed evidence flags, confidence, coverage, freshness, and raw-warning preservation while retaining instantaneous raw metrics.
- Added bounded platform telemetry cadence/cache metadata and forced capture-boundary sampling with explicit unavailable provenance.
- Added typed runtime mutation results and truthful MCP mutation responses for lifecycle, mode, overlay, overdraw, and session operations.
- Added a raw per-collected-frame hitch strip with bounded history, explicit timing gaps, budget severity, and peak-preserving pixel compression independent of throttled overlay text refresh.
- Added up to four visual-preset custom metric graph channels selected by stable metric ID with independent signed ranges, display scales, colors, units, and unavailable gaps.
- Activated bounded visual layout descriptors, module-backed extension widget descriptors, raw-strip height/width/gap controls, responsive graph columns, and public semantic theme manifests without steady-state visual-tree rebuilds.
- Added exact session/capture-bound URP self-overhead provenance with epoch/frame/callback bounds, renderer feature installed/enabled/enqueued evidence, typed inactive reasons, stale-window rejection, and explicit unavailable GPU attribution while keeping whole-frame CPU/GPU/GC as context only.

## 2026.8.11-1

- Added an optional separately distributed, SHA-256-pinned Windows x64 Editor RenderDoc bridge with verified FTUE download, local-file installation, update, and removal while keeping the UPM package binary-free and RenderDoc user-owned.
- Added explicit `GenericUnity`, `NativePreferred`, and `NativeRequired` capture modes with generation-bound native lifecycle, authenticated `.rdc` provenance, safe MetadataOnly/Copy/Embed storage, privacy, quota, cleanup, and export integration.
- Validated the native Windows x64 Unity Editor path with attached RenderDoc on Direct3D 11, Direct3D 12, and Vulkan; Development Player, Linux native, IL2CPP, mobile, and macOS native paths remain unsupported.

## 2026.8.9-1

- Added non-blocking single-flight capture-bundle export request/status/cancel APIs and MCP commands while retaining the blocking compatibility surface.
- Added additive typed missing-sample/capture-boundary timelines, allocation-free warmed custom metric collection, and backward-compatible timeline/schema readers.
- Added a generic external-artifact authority/hash/privacy envelope, process-local profiler conflict leases, and an opt-in hosted PlayMode CI matrix.

## 2026.8.8-1

- Added actionable FTUE continuation workflows for RenderDoc, Memory Profiler, Profile Analyzer, Adaptive Performance, and GraphicsStateCollection instead of terminal installed/available states.
- Added a complete self-contained initialization bootstrap backed by `PerformanceMeter.TryApplySettingsJson(...)`, including overlay, logging, alert, session-default, and overdraw settings with deterministic precedence over Resources auto-start.
- Added localized integration guidance, copyable one-shot/runtime-trigger/trace/prewarm/capture snippets, and owned snapshot/artifact reveal actions.

## 2026.8.7-2

- Added a separate first-time setup tab with live required checks, optional package installation, explicit skips, external capture-tool download actions, and automatic hide/reappearance as setup readiness changes.
- Added independently persisted Editor warning-log and structured-log controls while keeping schema `1` JSON files backward compatible with structured logs enabled by default.
- Added package-version visibility to the Setup window title and header and localized the new FTUE controls in all supported languages.

## 2026.8.7-1

- Added optional Memory Profiler snapshot triggers for manual, threshold, and leak workflows with cooldown, free-space guards, capture flags, and bundle manifest metadata.
- Added shader/PSO and graphics-pipeline diagnostics plus optional `GraphicsStateCollection` trace/prewarm lifecycle handling with capability provenance, session correlation, cancellation, and owned cleanup.
- Added an integration-neutral render context snapshot for camera, SRP/pass, freshness, GRD, and VRS context while keeping URP/HDRP public-API boundaries explicit.
- Added truthful GRD/Forward+/compute support, global activity, and aggregate BRG effectiveness telemetry with provenance and degraded reasons rather than per-renderer claims.
- Added session IDs, Profiler correlation markers, an optional Profile Analyzer opener without a hard dependency, and versioned performance baseline/CI artifact surfaces.
- Added a read-only virtualized UI Toolkit Session Analysis window with retained-sample timeline, worst-frame inspection, derived budget violations, and whole-run/current-scene scopes.
- Completed Setup-window presentation for persisted settings, optional integrations, analysis entry points, and read-only compatibility/schema/diagnostic states with runtime-only boundaries.

## 2026.8.6-2

- Added an opt-in external GPU capture coordinator with deterministic pre/capture/post-roll states, overlap protection, explicit capability failures, fake-backend coverage, and a guarded Unity `ExternalGPUProfiler` backend for attached RenderDoc/PIX tools in Editor or Development Builds.
- Added atomic versioned capture bundles with separately classified baseline/capture samples, bounded alert correlation, context snapshots, optional runtime screenshots, project-local quota/retention/redaction policy, SHA-256 manifests, truthful external-artifact provenance, and MCP request/status/cancel/export/capabilities commands.
- Added explicit Editor compatibility status that independently reports the declared import floor, supported core runtime floor, and active URP/HDRP render-integration compatibility through setup API/reporting and structured MCP output.
- Added an optional `SGG.PerfMeter.AdaptivePerformance` provider for thermal state and temperature trends, CPU/GPU performance levels, provider-provenance alerts, and session/capture telemetry while preserving explicit unavailable states and no core dependency.

## 2026.8.6-1

- Added a startup-cached dynamic Profiler metric catalog with exact/alias resolution provenance and explicit unavailable/no-sample/sampled states.
- Added dependency-free Unity Profiler markers and end-of-frame counters for collection, providers, snapshots, bottleneck classification, captures, exports, timing availability, and runtime state.
- Added fixed 120-frame self-overhead CPU/allocation windows and per-invocation budgets for collector, custom providers, CPU-core sampling, overlay refresh, URP Render Graph, and HDRP Custom Pass callbacks.
- Published additive self-overhead diagnostics through `PerformanceMeter.GetSelfOverhead()`, `PerfMeterStatusSnapshot.SelfOverhead`, and `perfmeter.runtime.status` without subtracting from existing CPU/GPU metrics.
- Preserved the existing public `PerfMeterStatusSnapshot` constructor signature and added deterministic sparse-window, allocation, lifecycle, API, MCP, URP, and HDRP coverage.
- Updated localized API, MCP, and workflow documentation in all supported languages.

## 2026.8.5-2

- Added independent `StructuredLogsEnabled` and `SetStructuredLogsEnabled(bool)` runtime controls without changing callbacks, alert history, overlay warnings, Editor warnings, or sessions.
- Added stable prefix/value/unit cells, a dedicated monospace numeric font role, bounded `FpsOnly` two-row fallback, and wrapping metric cards/budget bars.
- Moved the runtime overlay to an owned UI Toolkit host: `UIDocument` on Unity 6000.4 and `PanelRenderer` on Unity 6000.5+.
- Made overlay rebuilds remove only the PerfMeter-owned container and preserve foreign UI Toolkit settings and children.
- Added PlayMode coverage for numeric-width stability, max-font widget bounds, responsive layout, host ownership, rebuilds, and repeated enable/disable lifecycle.
- Updated localized API, workflow, limitation, widget, troubleshooting, and comparison documentation in all supported languages.

## 2026.7.19-1

- Classified alert firings as lifecycle, steady-state, or explicit external capture events.
- Added fired-history interval, start/reset provenance, classified counters, and latest-fired details to runtime and MCP alert reads.
- Added bounded `perfmeter.alerts.capture.begin/end` commands for authoritative capture attribution.
- Replaced runtime-created UI Toolkit panel/text/theme objects with a serialized package resource containing Unity-assigned ICU data.
- Removed runtime theme discovery and validated the overlay in a Unity 6000.5 Windows Development Player without ICU or text-layout exceptions.
- Updated package metadata and install examples to version `2026.7.19-1`.

## 2026.7.16-1

- Made MCP session exports atomic, added deterministic existing-file conflict semantics, and validated artifact bytes on repeated exports.
- Resolved owning package version provenance and separated configured settings from effective session-start runtime settings in export schema v2.
- Made MCP overlay responses distinguish requested, active-component, deferred, and externally unobserved rendered visibility.
- Preserved MCP overlay visibility across Play Mode transitions for the current Editor session and detached hidden UI before repaint requests.
- Avoided the obsolete `TextSettings.defaultFontAsset` API on Unity 6000.5 and newer while retaining the compatibility path on older editors.
- Updated package metadata and install examples to version `2026.7.16-1`.

## 2026.6.28-1

- Updated user-facing documentation in all languages to describe Unity 6 URP+HDRP support consistently.
- Clarified the support boundary: URP Render Graph supports overdraw/heatmap, HDRP Custom Pass supports core diagnostics, and HDRP overdraw/heatmap remain unsupported.
- Replaced package-local documentation links that pointed outside the package with direct GitHub URLs.
- Updated package metadata and install examples to version `2026.6.28-1`.

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
