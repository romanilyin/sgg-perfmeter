# Profile Analyzer And Benchmark CI Decision

Status: implemented for `PM-CI-001`; pending release.

## Decision

Each PerfMeter recording receives an additive lowercase 32-character hexadecimal session ID. The ID is created on every `StartSession`, remains stable after `StopSession`, and is cleared when the recorder returns to idle. Restarting an active recording emits the previous end boundary before creating the next ID. `Reset` also closes an active boundary before clearing state.

`PerfMeterSessionSummarySnapshot.SessionId`, `perfmeter.session.summary.session_id`, and the top-level session JSON `session_id` expose the same value. Existing summary constructors remain source-compatible and produce an empty ID. JSON keeps schema version `2`. CSV preserves every existing positional column and appends `session_id` as the final column.

## Profiler And Editor Boundary

Profiler-enabled builds emit immediate `SGG.PerfMeter.Session.<sessionId>.Begin` and `.End` samples. Dynamic markers are cached for the current ID so their warmed emission path remains allocation-free. Non-profiler builds retain the existing no-op instrumentation semantics.

`PerfMeterProfileAnalyzerIntegration` opens only the public `Window/Analysis/Profile Analyzer` menu and copies the current ID to the clipboard. It does not depend on, reflect into, install, load data into, or automatically filter the Profile Analyzer package. Missing sessions and missing menu availability return `false` with an explicit warning.

## Performance And CI Boundary

The `SGG.PerfMeter.Tests.Performance` assembly is enabled only when `com.unity.test-framework.performance` `3.5.0+` is present. The package manifest keeps no hard dependency; CI adds version `3.5.0` only to its ephemeral checkout. Versioned thresholds live in `Tests/Performance/performance-baselines.json` outside `Resources` and cover warmed profiler counters plus the cached session-boundary pair. Both allocation limits are exact zero.

`.github/workflows/performance-ci.yml` runs the full EditMode correctness assembly and isolated performance class on Unity `6000.4.12f1` and `6000.5.6f1`. Same-repository pull requests, pushes to `main`, and manual dispatches are supported. Fork pull requests are skipped because GitHub withholds Unity license secrets; `pull_request_target` is deliberately not used for untrusted code. Required outputs are raw Unity NUnit XML, converted JUnit XML, performance JSON, and logs. Missing or empty required artifacts fail the job.

## Validation State

Unity `6000.4.12f1` compile passed. Targeted `PerfMeterSessionCorrelationTests` passed `5/5`, `PerformanceMeterApiTests` passed `58/58`, and `PerfMeterPerformanceTests` passed `2/2`; both warmed allocation measurements were `0 B`. Final full EditMode passed `227/227`, and full PlayMode passed `16/16`. An isolated Unity `6000.5.6f1` consumer with performance package `3.2.0` compiled successfully with the `3.5.0` performance test assembly disabled. Workflow YAML parsing and local NUnit-to-JUnit conversion passed. Focused review found no remaining P1/P2 product-code defects. GitHub-hosted matrix execution, release-player behavior, and device validation remain pending; this record makes no release claim.
