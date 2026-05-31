# SGG PerfMeter Package Notes

This package-local documentation is intentionally short. The main user documentation is maintained at the repository level so GitHub can present one organized documentation set by language.

## Main Docs

- English: `../../../../docs/en/README.md`
- Russian: `../../../../docs/ru/README.md`
- Installation: `../../../../docs/en/installation.md`
- Quick Start: `../../../../docs/en/quick-start.md`
- API: `../../../../docs/en/api.md`
- MCP: `../../../../docs/en/mcp.md`
- Comparison: `../../../../docs/en/comparison.md`

## What This Package Provides

- UI Toolkit runtime performance overlay.
- FrameTimingManager CPU/GPU timing and ProfilerRecorder counters.
- Bottleneck classification and target-FPS budget visualization.
- Session recording with JSON/CSV export.
- Device, camera, Render Graph, status, metrics, alerts, and custom metric snapshots.
- Rule alerts with callback/log/Editor warning cooldowns.
- Opt-in overdraw measurement and visual heatmap through URP Render Graph.
- MCP command metadata for editor/agent automation.

## Supported Target

- Unity `6000.4+`.
- URP `17.4+`.
- Render Graph path.

Older Unity versions may import for compile-safety only and are not the supported runtime target.

## First Setup

Open `SGG/Perfmeter/Setup`, run the recommended setup, save JSON settings for zero-code startup, and enter Play Mode.
