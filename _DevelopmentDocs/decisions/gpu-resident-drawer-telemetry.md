# GPU Resident Drawer Telemetry Decision

Status: implemented for `PM-GRD-001`; released `2026.8.7-1`.

## Decision

PerfMeter extends the existing `PerfMeterGpuResidentDrawerContextSnapshot` nested in `PerfMeterRenderIntegrationSnapshot`. It does not create a parallel API or MCP command. `PerformanceMeter.GetRenderIntegrationSnapshot()`, `perfmeter.render.snapshot`, and capture-context `render_integration.gpu_resident_drawer` expose the same additive fields while schema version `1`, the legacy render facade, and session JSON/CSV remain unchanged.

## Public Evidence Boundary

- Configured mode and SRP support come from the typed `IGPUResidentRenderPipeline` asset.
- Project support comes from `IsGPUResidentDrawerSupportedByProjectConfiguration(false)`; compute support comes from `SystemInfo.supportsComputeShaders`.
- Actual activity comes from public `IsGPUResidentDrawerEnabled()`. This is global Unity runtime state, not camera- or renderer-specific participation.
- URP reports current-frame Forward+ and clustered rendering compatibility from `UniversalRenderingData.renderingMode`. HDRP does not expose an equivalent effective-mode API, so those fields remain `Unknown`.
- `PerfMeterGpuResidentDrawerReason` provides explicit disabled, unsupported, incompatible, inactive, and query-failed states instead of inferring success from configuration.

## Effectiveness

`PerfMeterGpuResidentDrawerEffectivenessSnapshot` reuses the dynamic profiler catalog's BRG draw-call and instance semantics. Every value carries sample state, exact/alias resolution, resolved recorder names, units, data type, and component counts. Values without an `AvailableSampled` capability remain `-1` in C# and serialize as `null`.

The scope is explicitly `brg_aggregate`. Other systems can use `BatchRendererGroup`, so positive counters show aggregate BRG workload only and do not prove that GRD handled a particular renderer. PerfMeter does not expose a fabricated effectiveness ratio or per-renderer fallback result because Unity has no stable public API for either.

## Runtime Cost And Failure Semantics

Static asset support is cached by pipeline asset/mode, while project support, activity, compute support, current URP mode, and the latest already-collected BRG values are composed for each typed observation without catalog-array allocation. Public queries are exception-contained. Query failures produce explicit availability and `QueryFailed` data; unavailable counters never become false zero samples.

## Validation State

Final evidence is Unity `6000.4.12f1` compile passed; targeted `PerformanceMeterApiTests` `58/58`, `PerfMeterCaptureBundleTests` `15/15`, and `PerformanceMeterPlayModeSmokeTests` `12/12`; final full EditMode `220/220` and full PlayMode `16/16` passed. Focused review P1/P2 resolved. The isolated compile matrix passed for Unity `6000.4.12f1` URP `17.4` and HDRP `17.4`, and Unity `6000.5.6f1` URP `17.5` and HDRP `17.5`. Release-player/device validation remains pending; this record makes no release claim.
