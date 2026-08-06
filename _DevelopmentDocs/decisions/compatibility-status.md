# Compatibility Status Decision

Status: implemented for `PM-COMP-001`; released `2026.8.6-2`.

## Decision

PerfMeter reports three independent Editor compatibility facts instead of one overloaded support boolean:

- `ImportCompatible` means the current Unity version meets the package metadata floor, currently Unity `2022.3`. It promises only import/compile eligibility, not supported runtime behavior.
- `CoreRuntimeCompatible` means the current Unity version meets the supported runtime floor, currently Unity `6000.4`. It does not require URP or HDRP because core timing, memory, session, alert, and device diagnostics are pipeline-independent.
- `RenderIntegrationCompatible` means core runtime is compatible, the active pipeline is URP or HDRP, its registered package version is at least `17.4`, and the corresponding PerfMeter adapter assembly is available. Built-in and unknown pipelines are explicit incompatible states.

Renderer Feature installation, HDRP runtime registration, Frame Timing Stats, and settings assets remain setup/configuration readiness. They are not folded into compatibility.

## Contract

`PerfMeterSetupActions.GetCompatibilityStatus()` returns the additive Editor-only `PerfMeterCompatibilityStatus` snapshot with current versions, declared floors, booleans, and reasons. `perfmeter.compatibility.status` returns the same facts as structured JSON. `perfmeter.setup.status` retains `status_report` and adds a structured `compatibility` object.

The evaluator is deterministic and separately testable with explicit Unity/SRP versions. The declared import-floor test is tied to `package.json` so code and package metadata cannot silently drift.

## Validation

Feature development validation on Unity `6000.5.6f1` passed targeted compatibility EditMode `16/16` and full EditMode `165/165`. Import-only Unity `2022.3`/`6000.3` checks and supported Unity `6000.4` URP/HDRP checks remain release-candidate matrix gates; pure evaluator tests do not replace those Editor runs.
