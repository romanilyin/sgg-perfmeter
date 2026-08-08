# Setup Window

Open the Editor window from `SGG/Perfmeter/Setup`.

## Current behavior

- **Setup** and **Presets** expose the persisted PerfMeter project settings and overlay-preset data, including read-only schema/version, legacy-compatibility, and reserved-metadata rows, widget composition, and numeric values normalized on focus loss.
- **Runtime** shows read-only session, memory, graphics-state, render-integration, and GRD/BRG diagnostics, including capability/status for optional integrations. `Unavailable`, `unknown`, and no-sample states remain explicit. `Measure Overdraw (project default)` uses the project-default sentinel.
- Actions include `Session Analysis`, `Profile Analyzer`, and `Refresh`. `Start Session` and `Stop Session` are available only in Play Mode. Opening or refreshing Setup never starts runtime collection.
- Memory-snapshot and graphics-state trace/prewarm request parameters are runtime-only inputs, not project settings.

## FTUE and initialization code

The **FTUE** tab contains required setup checks and optional continuation actions. Optional package rows can install or skip `com.unity.memoryprofiler`, `com.unity.performance.profile-analyzer`, and `com.unity.adaptiveperformance`; installed rows expose their next action:

- **Memory Profiler**: open `Window/Analysis/Memory Profiler`, copy the one-shot `PerformanceMeter.RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions(...))` snippet, copy the runtime-only `PerformanceMeter.ConfigureMemorySnapshotTriggers(...)` snippet, open Runtime, or reveal owned `.snap` files below `Temp/PerfMeter/MemorySnapshots` after the folder exists.
- **Profile Analyzer**: begin recording in Unity Profiler, start and stop a PerfMeter session inside that recording, then open the existing session integration. It copies the session ID, but does not load Profiler data or apply a filter.
- **Adaptive Performance**: open Runtime to inspect the optional provider status.
- **GraphicsStateCollection**: open Runtime, copy trace/prewarm snippets, or reveal artifacts below `Temp/PerfMeter/GraphicsStateCollections`. Start and keep a session recording for the trace, wait for `ArtifactRelativePath`, then pass that path to prewarm. FTUE does not request trace or prewarm automatically.
- **RenderDoc**: open the official download page, check the shared external-profiler attachment signal, copy the `RequestCapture` snippet, open Runtime, or open Unity's official integration guide. FTUE cannot detect RenderDoc installation; Unity cannot identify RenderDoc versus PIX from its attachment signal, and capture completion does not provide an external artifact path.

The **Initialization Code** section on Setup has **Refresh from Project Settings** and **Copy Init Code**. The generated `PerfMeterBootstrap` embeds a complete normalized snapshot of overlay, logging, alert, session-default, and overdraw settings and calls:

```csharp
PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)
```

The explicit bootstrap honors `enabled` and `collectionMode: "Stopped"`, but does not start sessions or captures. It is an alternative to the Resources zero-code file at `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`. If both are present, a valid explicit application suppresses Resources auto-start for the current domain and becomes authoritative; invalid explicit JSON leaves the runtime unchanged.

## Reference screenshots

> The screenshots below predate P3.5. They are retained as visual references only and are not current evidence of the completed Setup UX.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-en-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-en-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-en-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-en-debug.png)
