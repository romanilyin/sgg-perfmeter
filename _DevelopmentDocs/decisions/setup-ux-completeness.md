# Setup UX Completeness Decision

Status: implemented for `PM-SETUP-001`; released `2026.8.7-1`.

## Decision

`SGG/Perfmeter/Setup` owns the Editor presentation of persisted PerfMeter settings, overlay presets, optional P3 integration status, and analysis entry points. The Setup and Presets tabs expose the complete persisted settings surface represented by the window. Legacy compatibility data, schema/version rows, and reserved preset metadata remain explicit read-only rows; they are not hidden editable aliases.

The Runtime tab exposes read-only P3 session, memory, graphics-state, render-integration, and GRD/BRG diagnostics. The window presents capability, status, and sample availability truthfully rather than converting unavailable data to zero.

## Persisted And Runtime Boundary

Project settings and overlay-preset data are persisted. Memory-snapshot requests and graphics-state trace/prewarm request parameters remain runtime-only API/MCP inputs and are not project settings. `Measure Overdraw (project default)` uses the project-default sentinel. Numeric settings are normalized when their fields lose focus before persistence or application.

## Action Lifecycle

`Session Analysis`, `Profile Analyzer`, and `Refresh` are explicit Editor actions. `Start Session` and `Stop Session` are available only in Play Mode. Opening or refreshing Setup reads and presents state; it never starts runtime collection.

## Localization Boundary

Localization applies to static Setup-window UI text, including labels, buttons, and tooltips. Runtime output, generated snippets, project paths, preset names, IDs, widget/custom-metric names, and measured or diagnostic values remain unchanged.

## Validation State

Unity `6000.4.12f1` compile passed. Targeted `PerfMeterSetupWindowTests` passed `9/9`, `PerfMeterSettingsTests` passed `22/22`, `PerfMeterSessionAnalysisTests` passed `11/11`, `PerfMeterMemorySnapshotTests` passed `9/9`, and `PerfMeterGraphicsStateCollectionTests` passed `25/25`. Final full EditMode passed `247/247`, and PlayMode passed `16/16`. An isolated package consumer compiled on Unity `6000.5.6f1`. Focused review found no remaining P1/P2 defects. Release-player and device validation remain release gates; this record makes no release claim.
