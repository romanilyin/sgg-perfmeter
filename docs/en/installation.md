# Installation

SGG PerfMeter is currently distributed as a Unity package named `com.sungeargames.perfmeter`. NPM distribution is not documented as a current install path yet.

## Requirements

- Unity `6000.4+` for supported runtime usage.
- URP `17.4+` with Render Graph path.
- UI Toolkit runtime support.
- Frame Timing Stats enabled before relying on FrameTimingManager in builds.

## Git UPM Install

The package lives inside this repository:

```text
Assets/Scripts/SGG.PerfMeter
```

Add it to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

For a private repository over SSH:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Pin a tag or commit for repeatable installs:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.20-1"
  }
}
```

## Local Copy Install

Copy this folder into your Unity project:

```text
Assets/Scripts/SGG.PerfMeter
```

This is useful for local package development or when Git dependencies are not desired.

## Initial Project Setup

Open:

```text
SGG/Perfmeter/Setup
```

Then run the recommended setup:

1. Enable Frame Timing Stats.
2. Install `PerfMeterRenderGraphFeature` into editable active URP renderer assets.
3. Save JSON settings to `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` for zero-code setup, or copy the initialization snippet.
4. Enter Play Mode and verify the overlay.

## Samples

Import package samples from the Package Manager details panel:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
