# Capture Bundle Decision

Status: implemented for `PM-CAP-002`; unreleased.

## Decision

PerfMeter can extend an explicit `PM-CAP-001` request with a correlated, versioned bundle. Capture frames are stored separately from the normal session recorder; capture overhead therefore does not enter baseline samples or the baseline session summary. The bundle freezes the terminal capture status, baseline and capture samples, capture-classified alerts, runtime/settings/device/camera/render context, and an optional end-of-frame runtime screenshot.

The public API and MCP expose request, status, cancel, capabilities, and export operations. Bundle state is explicit: `Recording`, `PendingScreenshot`, `Ready`, `Exported`, `Canceled`, `Unavailable`, or `Error`. Screenshot capture is opt-in and reports `Unavailable` in batch mode, outside Play Mode, or when runtime shutdown interrupts it; that degraded state remains exportable.

## Files And Commit

Schema `sgg.perfmeter.capture-bundle` version `1` contains:

- `manifest.json` written last in staging, with SHA-256 and byte length for every other component.
- `session.json` with baseline evidence and `capture-samples.json` with separately classified capture frames.
- `alerts.json` with an explicit truncation flag for bounded in-memory history.
- `context.json` with settings, runtime, device, camera, and render-integration snapshots.
- `external-capture.json` with requested tool and artifact provenance.
- Optional `screenshot.png`.
- Optional copied external artifact only when the caller supplies a valid project-local file.

All files are first written under a unique sibling staging directory with an exact ownership marker. Export succeeds only through one same-parent directory move to a previously nonexistent final path. Existing destinations are conflicts and are never overwritten. Retention runs only after commit, recognizes marker-backed bounded manifests, and never deletes unknown directories; stale owned staging directories are handled separately.

## Authority And Security

Unity's `ExternalGPUProfiler` does not expose attached-tool identity, version, output path, or a tool-authenticated artifact association. The built-in backend therefore always reports `tool_identity: unknown`, `tool_version: unknown`, and `association: observed` at most. A caller-provided `.rdc` or `.wpix` path is copied and hashed as an observation; neither its extension nor its hash makes it authoritative. `require_authoritative_external_artifact` deterministically fails until a future backend can provide tool-authenticated provenance.

Exports are restricted to `Temp/PerfMeter/CaptureBundles` below the project root. Absolute paths, traversal, invalid path components, reparse points, and external artifact files outside the project are rejected. Components use bounded JSON serialization with redacted project/device identifiers. Per-bundle size, screenshot size, total bundle quota, and retained-bundle count are fixed capabilities. Retention deletes only recognizable finalized PerfMeter bundles and never unknown directories.

## Validation

Automated tests cover bundle state and generation ownership, screenshot degradation, settings context, sample separation, bounded alert history, atomic commit/conflict behavior, manifest hashes, path/size rejection, authority refusal, retention ownership, MCP registration, and Play Mode lifecycle. Unity `6000.5.6f1` passed targeted EditMode `13/13`, targeted PlayMode `8/8`, full EditMode `149/149`, and full PlayMode `12/12`. Real RenderDoc/PIX attachment and tool-side artifact confirmation remain release-candidate gates.
