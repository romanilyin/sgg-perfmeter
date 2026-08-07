# Memory Snapshot Decision

Status: implemented for `PM-MEM-001`; released `2026.8.7-1`.

## Context

PerfMeter needs an optional way to correlate a memory investigation with its existing session and capture evidence without making Memory Profiler a core dependency. A snapshot can be large and can contain sensitive process memory, so capture, staging, export, cleanup, and provenance must remain explicit and bounded.

## Decision

The core package exposes `RegisterMemorySnapshotBackend`, `UnregisterMemorySnapshotBackend`, `GetMemorySnapshotCapabilities`, `GetMemorySnapshotStatus`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)`, and `GetMemorySnapshotTriggers`. The separate `SGG.PerfMeter.MemoryProfiler` assembly references `com.unity.memoryprofiler` only when Unity `6000.4+` and package version `1.1.0+` resolve; it auto-registers the backend. The core package has no hard Memory Profiler dependency. Custom backends can use `IPerfMeterMemorySnapshotBackend`.

Manual capture is the default. System-memory threshold and bounded leak-growth triggers require explicit opt-in. The coordinator owns one flight at a time and applies overlap, cooldown, minimum-free-space, capture-flag, and backend-availability guards. Owned staging files live under `Temp/PerfMeter/MemorySnapshots`; `.snap` artifacts are capped at 512 MiB. Cleanup rejects unowned paths and reports deletion or reparse-point failures instead of silently treating them as safe.

Memory-only evidence uses the existing capture-bundle API under `Temp/PerfMeter/CaptureBundles`, with `requested_tool: MemoryProfiler`, `memory-snapshot.json`, manifest provenance, and streaming SHA-256. No external GPU artifact is produced. A successful bundle export is one-shot and removes the staging source; OS locks or portable managed reparse races remain best-effort conditions with explicit warnings/rejections. Bundle retention has a total 2 GiB quota.

## Evidence And Privacy

The binary snapshot may contain sensitive process memory. Consumers must protect it, review it, and redact or remove it before sharing. Status and MCP reads avoid exposing the temporary source path; the exported manifest explicitly marks sensitive-memory content and records backend/flag provenance.

## Validation

Targeted evidence is memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, and optional assembly compilation with the real `com.unity.memoryprofiler@1.1.12`. Unity `6000.4.12f1` also passed full EditMode `182/182` and full PlayMode `14/14`; release-player and device behavior are not claimed by this record.

## Release Gate

Release requires the real `com.unity.memoryprofiler` `1.1.0+` package on the declared Unity matrix, release-player validation, and target-device checks for lifecycle, storage, cleanup, privacy, and exported evidence. The targeted checks and optional compile result do not substitute for that gate.
