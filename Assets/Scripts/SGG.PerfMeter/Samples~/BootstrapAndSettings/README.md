# Bootstrap and Zero-Code Settings

This sample contains two setup paths:

- `PerfMeterMinimalBootstrap.cs` is a small component for projects that prefer explicit runtime bootstrap code.
- `Resources/SGG.PerfMeter/perfmeter-settings.json` is a zero-code settings example loaded by `Resources.Load<TextAsset>("SGG.PerfMeter/perfmeter-settings")`.

Use the component when you want scene-local control over collection mode, overlay preset, corner, and target FPS. Use the JSON file when you want PerfMeter to auto-start from project-owned settings without writing bootstrap code.

If your project already has `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`, keep only one settings file on the final Resources load path to avoid ambiguous `Resources.Load` results.
