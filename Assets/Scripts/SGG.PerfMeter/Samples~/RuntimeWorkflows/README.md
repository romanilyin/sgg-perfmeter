# Runtime Workflows

This sample demonstrates runtime-only workflows through public `SGG.PerfMeter` APIs:

- `PerfMeterRuntimeWorkflowExample.cs` starts overlay collection, switches overlay presets, records a bounded session, exports JSON/CSV to `Application.persistentDataPath`, listens to alert callbacks, and exposes context-menu actions for overdraw and heatmap diagnostics.
- `PerfMeterCameraSnapshotReplayExample.cs` captures the active camera snapshot and restores the same transform/projection state later for repeatable profiling.

Attach the components to a GameObject in a test scene. Use Inspector context menu actions to start/stop sessions or request diagnostics. No player hotkeys are registered by this sample.

For numerical overdraw or heatmap output, install `PerfMeterRenderGraphFeature` into the active URP renderer first through `SGG/Perfmeter/Setup`. HDRP intentionally reports overdraw/heatmap unsupported, but the rest of runtime workflows still apply.
