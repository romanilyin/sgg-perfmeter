# Platform Telemetry Decision

Status: implemented for `PM-PLAT-001`; released `2026.8.6-2`.

## Context

PerfMeter needs optional thermal and platform-performance signals for diagnostics, alerts, sessions, and capture evidence. The core runtime must remain usable without `com.unity.adaptiveperformance`, while provider output must distinguish an unsupported field from a real zero value and retain its source identity.

## Decision

PerfMeter exposes one provider seam through `IPerfMeterPlatformTelemetryProvider` and the public `PerformanceMeter.RegisterPlatformTelemetryProvider(...)`, `UnregisterPlatformTelemetryProvider(...)`, and `GetPlatformTelemetry()` methods. A different second provider is rejected. Each collection produces an immutable `PerfMeterPlatformTelemetrySnapshot` with provider ID/version, timestamps, thermal warning, temperature level/trend, CPU/GPU performance levels, adaptive bottleneck, and per-field availability.

The `SGG.PerfMeter.AdaptivePerformance` assembly is optional. It references Unity Adaptive Performance only when the project resolves `com.unity.adaptiveperformance` at `5.1.0+` through an asmdef `versionDefines` entry; the core assembly and package metadata have no hard Adaptive Performance dependency. Core owns a bounded 0.25-second provider cadence/cache and forces one attempt at a capture boundary. Snapshots retain last-attempt/result, last-success age/freshness, and explicit unavailable provenance. The provider feeds the thermal alert metric and records `SGG.PerfMeter.Thermal.Sample` plus `SGG.PerfMeter.Thermal.Available` instrumentation.

Session JSON/CSV and capture samples retain platform telemetry and provider provenance. The MCP read command `perfmeter.platform.telemetry` returns the structured snapshot. Unavailable providers or fields remain explicit: JSON uses `null` for unavailable numeric values and CSV uses empty fields, with availability flags rather than synthetic zero readings.

## Consequences

- Projects without Adaptive Performance keep the core package available and receive an explicit unavailable snapshot.
- Custom platform providers can supply the same contract, but only one provider is active at a time.
- Consumers can correlate thermal/performance state with each collected frame, session exports, capture samples, profiler instrumentation, and the default `thermal.throttling` alert without changing existing core metric meaning.
- Provider identity/version and field availability make degraded or partial platform support visible to API and MCP consumers.

## Validation

Repository contract coverage includes provider registration and failure handling, immutable snapshot normalization, session JSON/CSV serialization, thermal alert gating, MCP output, optional-assembly version-definition checks, capture preservation, and PlayMode frame/session correlation. On Unity `6000.5.6f1`, targeted telemetry EditMode passed `7/7`, capture bundle EditMode passed `13/13`, the telemetry lifecycle PlayMode test passed `1/1`, full EditMode passed `172/172`, and full PlayMode passed `13/13`. The optional assembly also compiled successfully with `com.unity.adaptiveperformance@5.1.6`; this does not claim thermal/performance validation on a real target device.

## Release Gate

Release requires the declared Unity/SRP matrix plus Adaptive Performance `5.1+` package integration and real target-device validation for thermal and performance-level behavior. Device/package results, field support, lifecycle behavior, and exported evidence must be checked on the release matrix; no such real-device validation is claimed by this decision record.
