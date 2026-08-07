# Render Integration Context Decision

Status: implemented for `PM-REN-001`; released `2026.8.7-1`.

## Decision

PerfMeter exposes an additive, integration-neutral `PerfMeterRenderIntegrationSnapshot` through `PerformanceMeter.GetRenderIntegrationSnapshot()` and `TryGetRenderIntegrationSnapshot(...)`. It carries the current render pipeline and asset source, the last observed camera entity/name/type, observation frame and age, current-pipeline match, integration identity/version, pass kind/name/injection point, actual PerfMeter-owned pass count, effective rendering mode when available, and nested GPU Resident Drawer, Variable Rate Shading, and legacy Render Graph context.

## Compatibility

The existing `PerformanceMeter.GetRenderGraphSnapshot()` API and `perfmeter.rendergraph.snapshot` command remain as a compatibility facade. The new `render_integration` object is additive; capture-context schema `sgg.perfmeter.capture-context` version `1` preserves the existing `render` object and adds `render_integration`. Session JSON/CSV schemas are unchanged.

## Stable Public API Boundary

- URP reads the public current-frame `UniversalRenderingData.renderingMode` and reports the PerfMeter passes actually scheduled for that frame.
- HDRP reports the actual observed PerfMeter `CustomPass`; effective rendering mode remains unavailable because no stable public API exposes it.
- Private/internal Render Graph pass/resource reflection is not used. Legacy facade counters (`registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count`, and `aliased_resource_count`) remain `-1` because no stable public API exists for them.
- GRD exposes configured mode and public SRP support. `PM-GRD-001` additively extends this nested context with public project/compute support, global runtime activity, URP rendering-mode context, degraded reasons, and aggregate BRG effectiveness provenance; see `gpu-resident-drawer-telemetry.md`.
- VRS exposes authoritative `SystemInfo`/`ShadingRateInfo` hardware support. Configuration and activity remain `Unknown` unless a future typed adapter proves them.
- No Editor navigation is added. Stable public APIs do not expose a RenderGraph/CustomPass viewer or pass targets, so the snapshot does not promise navigation.

## Freshness And Read Semantics

Read methods do not start runtime collection. Each read evaluates the current pipeline and compares it with the most recent typed observation. Before any observation, a supported current pipeline can be `Available` with `State = NotObserved`; an observation from another pipeline configuration is retained only as stale evidence, with `ObservationMatchesCurrentPipeline = false`, `ObservationAgeFrames`, and a warning. Unsupported or unknown pipelines remain explicit degraded states rather than inferred observations.

## Capture Timing

For an external GPU capture, capture context is frozen once on the first sample in the `Capturing` phase. The stored `render_integration` therefore describes that capture-context observation, including its frame age and current-pipeline match at that moment; it is not continuously replaced by the latest read. For a Memory Profiler snapshot, the context is captured when the memory request reaches its completion observation. Both paths preserve the legacy `render` context alongside the additive neutral object.

## Validation And Release State

Final evidence is Unity `6000.4.12f1` main compile passed; targeted `PerformanceMeterApiTests` `53/53`, `PerfMeterCaptureBundleTests` `15/15`, and `PerformanceMeterPlayModeSmokeTests` `12/12`; final full EditMode `215/215` and full PlayMode `16/16` passed. Focused review P1/P2 resolved. The isolated compile matrix passed for Unity `6000.4.12f1` URP `17.4` and HDRP `17.4`, and Unity `6000.5.6f1` URP `17.5` and HDRP `17.5`. Release-player/device validation remains pending; this decision record makes no release claim.
