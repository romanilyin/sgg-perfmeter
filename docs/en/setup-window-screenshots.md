# Setup Window

Open the Editor window from `SGG/Perfmeter/Setup`.

## Current behavior

- **Setup** and **Presets** expose the persisted PerfMeter project settings and overlay-preset data, including read-only schema/version, legacy-compatibility, and reserved-metadata rows, widget composition, and numeric values normalized on focus loss.
- **Runtime** shows read-only session, memory, graphics-state, render-integration, and GRD/BRG diagnostics, including capability/status for optional integrations. `Unavailable`, `unknown`, and no-sample states remain explicit. `Measure Overdraw (project default)` uses the project-default sentinel.
- Actions include `Session Analysis`, `Profile Analyzer`, and `Refresh`. `Start Session` and `Stop Session` are available only in Play Mode. Opening or refreshing Setup never starts runtime collection.
- Memory-snapshot and graphics-state trace/prewarm request parameters are runtime-only inputs, not project settings.

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
