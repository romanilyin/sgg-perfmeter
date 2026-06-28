# Installation

SGG PerfMeter is distributed as a Unity package named `com.sungeargames.perfmeter`. The current public npm registry package is `2026.6.28-1`; Git UPM and local-copy installs remain available.

## Requirements

- Unity `6000.4+` for supported runtime usage.
- URP `17.4+` with Render Graph path or HDRP `17.4+` with Custom Pass integration.
- UI Toolkit runtime support.
- Frame Timing Stats enabled before relying on FrameTimingManager in builds.

Package metadata keeps Unity `2022.3` as an import-safety floor for import and compile checks. The current supported runtime target is Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.

## npm Scoped Registry Install

Add the npm registry as a Unity Package Manager scoped registry in your Unity project's `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.6.28-1"
  }
}
```

If your manifest already has `scopedRegistries`, merge the `npmjs` entry into the existing array.

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

If your environment uses SSH for Git dependencies:

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
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.28-1"
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
2. Install `PerfMeterRenderGraphFeature` into editable active URP renderer assets. HDRP projects skip URP renderer changes; the package HDRP Custom Pass is registered at runtime when HDRP `17.4+` is installed.
3. Save JSON settings to `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` for zero-code setup, or copy the initialization snippet.
4. Enter Play Mode and verify the overlay.

## Samples

Import package samples from the Package Manager details panel:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
