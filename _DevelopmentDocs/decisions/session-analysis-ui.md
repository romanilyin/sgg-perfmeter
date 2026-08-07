# Session Analysis UI Decision

Status: implemented for `PM-SESSION-001`; released `2026.8.7-1`.

## Decision

PerfMeter provides a separate read-only Editor UI Toolkit window at `SGG/Perfmeter/Session Analysis`. It is not part of the runtime overlay and adds no runtime API, retention, or serialization schema. The window reads only `PerformanceMeter.GetSessionSummary()` and `PerformanceMeter.GetSessionSamples()`; opening or refreshing it never starts the runtime.

The four views are a retained-sample timeline, an authoritative worst-frame inspector, derived budget violations, and authoritative scene scopes. Timeline, violation, and scope tables use virtualized lists with dedicated horizontal viewports. Polling is bounded, and retained samples are copied only when session identity, state, recording-window identity, or sample count changes, or after an explicit refresh. Summary and two scope rows can update without rebuilding sample-derived rows.

## Evidence Boundary

- Timeline values come only from retained `PerfMeterSessionSampleSnapshot` records. Spike columns are recorded cumulative counters, not reconstructed per-frame spike events.
- Worst-frame identity comes from `PerfMeterSessionSummarySnapshot.WorstFrame`. Per-sample timing, custom metrics, platform telemetry, and trace correlation appear only when a matching retained frame is available.
- Budget violations are derived against each sample's positive finite `FrameBudgetMs`. CPU-main work is `max(0, main - present wait)`; CPU-main, CPU-render, and GPU comparisons use strict `>`. GPU evaluation is independent of CPU availability and requires explicit valid GPU timing.
- Scene scopes are limited to `WholeRun` and `CurrentScene`. The UI does not fabricate historical scene durations from retained sample spans.
- Timing with missing availability is rendered as `Unavailable`, not numeric zero. Render and memory counters without per-sample availability are deliberately absent from the worst-frame inspector.

## Lifecycle And Localization

The window supports idle, recording, and stopped in-memory sessions. `PerformanceMeter.Stop()`, domain reload, or Play Mode teardown can discard the session because no persistence layer was added. Domain reload rebuilds the window without static snapshot caches. Source-only localization is applied to static UI labels; scene names, custom metric names, IDs, and measured values never pass through localization.

## Validation State

Unity `6000.4.12f1` compile passed. Targeted `PerfMeterSessionAnalysisTests` passed `11/11`, `PerformanceMeterApiTests` passed `58/58`, `PerfMeterSessionCorrelationTests` passed `5/5`, and `PerfMeterCaptureBundleTests` passed `15/15`. Final full EditMode passed `238/238`, and full PlayMode passed `16/16`. An isolated package consumer also compiled on Unity `6000.5.6f1`. Focused review found no remaining P1/P2 defects. Release build and release-player/device validation are deferred until `PM-SETUP-001`; this record makes no release claim.
