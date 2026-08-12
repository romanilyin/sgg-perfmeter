# Capture Bundle Decision

Status: implemented for `PM-CAP-002`; released `2026.8.6-2`.

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

Unity's generic `ExternalGPUProfiler` does not expose attached-tool identity, version, output path, or an authenticated association, so generic and caller-supplied `.rdc`/`.wpix` data remains observed. The native RenderDoc descriptor is separate, generation- and bundle-bound, and may satisfy `require_authoritative_external_artifact` only after bridge-authenticated finalization, stable identity, source hash, and required post-copy hash gates pass.

Exports are restricted to `Temp/PerfMeter/CaptureBundles` below the project root. Absolute paths, traversal, invalid path components, reparse points, and external artifact files outside the project are rejected. Components use bounded JSON serialization with redacted project/device identifiers. Per-bundle size, screenshot size, total bundle quota, and retained-bundle count are fixed capabilities. Retention deletes only recognizable finalized PerfMeter bundles and never unknown directories.

## Validation

Automated tests cover bundle state/generation ownership, screenshot degradation, sample separation, atomic commit, hashes, path/size rejection, generic authority refusal, native descriptor authority, retained Copy/Embed, retention, MCP registration, and Play Mode lifecycle. Real RenderDoc D3D11/D3D12/Vulkan confirmation passed for the initial Windows Editor rows. The accepted [PIX timing boundary](pix-native-timing-boundary.md) leaves circular native PIX waiting for a documented bounded Windows API; generic `.wpix` evidence remains observed.
