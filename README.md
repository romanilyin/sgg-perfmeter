<div align="center">

# SGG PerfMeter

**Lightweight runtime performance diagnostics and agent-readable profiling for Unity 6 URP+HDRP (FPS meter).**

[English](./README.md) |
[Русский](./docs/ru/README.md) |
[Deutsch](./docs/de/README.md) |
[Español](./docs/es/README.md) |
[Français](./docs/fr/README.md) |
[Italiano](./docs/it/README.md) |
[日本語](./docs/ja/README.md) |
[한국어](./docs/ko/README.md) |
[Português (Brasil)](./docs/pt-br/README.md) |
[简体中文](./docs/zh-cn/README.md)

—

[Installation](./docs/en/installation.md) |
[Quick Start](./docs/en/quick-start.md) |
[Workflows](./docs/en/workflows.md) |
[Visual Presets](./docs/en/presets.md) |
[Implemented Widgets](./docs/en/widgets.md) |
[API](./docs/en/api.md) |
[MCP](./docs/en/mcp.md) |
[Comparison](./docs/en/comparison.md) |
[Limitations](./docs/en/limitations.md) |
[Troubleshooting](./docs/en/troubleshooting.md) |
[Screenshots](./docs/en/screenshots.md) |
[Setup Window Screenshots](./docs/en/setup-window-screenshots.md) |
[Contributor Checks](./docs/en/contributor-checks.md) |
[Changelog](./CHANGELOG.md)

<p>
  <a href="./docs/en/installation.md"><img src="./docs/assets/readme/cards/unity.svg" alt="Unity" height="48"></a>
  <a href="./docs/en/installation.md"><img src="./docs/assets/readme/cards/urp.svg" alt="URP" height="48"></a>
  <a href="./docs/en/workflows.md#runtime-overlay"><img src="./docs/assets/readme/cards/uitk.svg" alt="UI Toolkit" height="48"></a>
  <a href="./docs/en/api.md"><img src="./docs/assets/readme/cards/csharp.svg" alt="C#" height="48"></a>
  <a href="./docs/en/limitations.md"><img src="./docs/assets/readme/cards/android.svg" alt="Android" height="48"></a>
  <a href="./docs/en/limitations.md"><img src="./docs/assets/readme/cards/ios.svg" alt="iOS" height="48"></a>
  <a href="./docs/en/quick-start.md"><img src="./docs/assets/readme/cards/docs.svg" alt="Docs" height="48"></a>
</p>

<p>
  <a href="./docs/en/presets.md#default"><img src="./docs/assets/screenshots/presets/preset-default-landing.png" alt="SGG PerfMeter landing screenshot" width="960"></a>
</p>

</div>

SGG PerfMeter - lightweight runtime performance diagnostics and agent-readable profiling for Unity 6 URP+HDRP (FPS meter).

Detect frame bottlenecks, compare performance changes, capture reproducible sessions, and expose structured profiling data to tools and AI agents.

SGG PerfMeter explains whether a frame is limited by CPU, GPU, render thread, present/VSync, overdraw, or unavailable platform counters, and lets you save that state for later analysis.

## Why It Helps

- See frame bottleneck context while the game is running.
- Switch between visual presets, graphs, metric bars, compact layouts, and custom metric rows for different debugging situations.
- Record reproducible profiling sessions with warm-up, scene scope, worst-frame summaries, JSON/CSV export, device metadata, and camera metadata.
- Preserve explicit missing-sample and capture-boundary events in additive session/capture JSON timelines instead of turning unavailable timing into numeric zero.
- Coordinate one explicit bounded RenderDoc/PIX request with deterministic pre-roll, capture, and post-roll states when an external GPU profiler is already attached.
- Export a versioned project-local capture bundle that correlates baseline and capture samples, alerts, context, an optional runtime screenshot, and external-artifact provenance. Generic Unity observations remain non-authoritative; the optional native RenderDoc path can authenticate a generation-bound `.rdc`.
- Queue, poll, and cancel single-flight capture-bundle exports while serialization, streaming copy/hash, retention, and atomic commit run off the caller thread; the existing blocking API remains available for compatibility.
- Optionally capture sensitive memory snapshots through the separate Memory Profiler integration and correlate them with the existing evidence-bundle surface.
- Inspect dynamic shader GPU-program and graphics-pipeline creation markers with their discovered units and provenance, then correlate an optional GraphicsStateCollection trace with session samples.
- Use alerts, structured logs, callbacks, and Editor warning cooldowns to catch regressions without watching the overlay all the time.
- Give tools and agents structured data for comparisons, A/B tests, and hotspot search instead of relying on screenshots or Console scraping.

## How It Exposes The Data

- **Runtime overlay**: visual presets, compact layouts, graphs, metric bars, and custom metric rows for live inspection.
- **Public C# API**: immutable snapshots for status, metrics, device, camera, integration-neutral render context (with a legacy Render Graph facade), alerts, sessions, timelines, external artifacts, profiler leases, and custom metrics.
- **Frame-accurate graph channels**: a full-width raw frame-time strip advances on every collected frame, preserves one-frame peaks at narrow widths, and accepts up to four stable-ID custom metric channels with independent signed ranges, display scales, colors, units, and unavailable gaps.
- **External GPU capture**: guarded generic Editor/Development Build coordination for attached RenderDoc or PIX, plus an optional Windows x64 Editor native RenderDoc path with authenticated artifacts on Direct3D 11, Direct3D 12, and Vulkan.
- **Session recording**: bounded captures with warm-up, scene scope, worst frames, device/camera metadata, and JSON/CSV export.
- **Alerts**: structured logs, callbacks, Editor warning cooldowns, and latest-alert snapshots.
- **Agent layer**: MCP command metadata lets agents inspect the project, compare runs, perform A/B tests, search for hotspots, queue/status/cancel exports, and read external-artifact authority plus profiler lease conflicts through structured data.

## What It Measures

- Unity `6000.4+` / URP `17.4+` Render Graph and HDRP `17.4+` Custom Pass runtime state.
- FrameTimingManager CPU/GPU timing: CPU frame, main thread, render thread, present wait, and GPU frame time when available.
- ProfilerRecorder render counters: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes, memory, and GPU memory when available.
- Bottleneck classification for GPU, CPU main thread, CPU render thread, present/VSync, balanced, or unknown frames.
- Opt-in numerical overdraw measurement and visual overdraw heatmap through URP Render Graph; HDRP overdraw and heatmap are reported as unsupported while core diagnostics remain available.
- Dynamic `ProfilerRecorder` shader GPU-program and graphics-pipeline creation markers when Unity exposes them; values keep their discovered units and are not assumed to be shader or PSO counts.
- Device, URP/HDRP camera, render-integration, status, metrics, alerts, session, and custom metric snapshots for code and MCP automation.
- Integration-neutral render context: current pipeline/source, observed camera and frame freshness, integration/pass/injection details, actual PerfMeter pass count, effective mode where Unity exposes it, typed GRD support/activity/effectiveness, and explicit VRS availability.

## Optional Platform Telemetry

PerfMeter can collect optional thermal and Adaptive Performance signals through one provider. The core assembly has no hard `com.unity.adaptiveperformance` dependency; the optional `SGG.PerfMeter.AdaptivePerformance` assembly is enabled for Unity `6000.4+` when `com.unity.adaptiveperformance` is `5.1.0+`.

- **Provider API**: `PerformanceMeter.RegisterPlatformTelemetryProvider(...)`, `UnregisterPlatformTelemetryProvider(...)`, and `GetPlatformTelemetry()` allow at most one active provider and return the immutable `PerfMeterPlatformTelemetrySnapshot` with provider ID/version, sample/change timestamps, thermal warning, temperature level/trend, CPU/GPU performance levels, adaptive bottleneck, and per-field availability. A different second provider is rejected.
- **Collection and exports**: the core owns a bounded 0.25-second provider cadence and cached snapshot, with last-attempt/result, last-success age, and freshness metadata. Capture boundaries force one sample without hiding an `Unavailable` provider result. Session JSON/CSV and capture samples preserve the snapshot and provider provenance. MCP command `perfmeter.platform.telemetry` exposes the current structured snapshot.
- **Profiler and alerts**: `SGG.PerfMeter.Thermal.Sample` and `SGG.PerfMeter.Thermal.Available` expose the thermal collection marker and availability counter. The default `thermal.throttling` alert uses the `ThermalWarningLevel` metric when an imminent or active throttling level is available.
- **Unavailable is explicit**: unsupported providers and fields stay unavailable; JSON serializes unavailable numeric values as `null` and CSV uses empty fields, with availability flags instead of fake zero readings. Real Adaptive Performance package and target-device validation remain release-matrix/release-candidate gates; no device result is implied here.

## Optional Memory Snapshots

Memory snapshots are an opt-in extension, not a core-package dependency. On Unity `6000.4+`, installing `com.unity.memoryprofiler` `1.1.0+` enables the separate `SGG.PerfMeter.MemoryProfiler` assembly, which auto-registers the Memory Profiler backend. Check `PerformanceMeter.GetMemorySnapshotCapabilities()` before requesting a snapshot.

- Manual requests use `RequestMemorySnapshot(...)`; system-memory threshold and bounded leak-growth triggers are disabled by default and must be explicitly configured.
- `GetMemorySnapshotStatus()` reports single-flight, cooldown, free-space, and capture-flag decisions. Memory-only evidence uses the existing capture-bundle API and is exported below `Temp/PerfMeter/CaptureBundles` with `MemoryProfiler` provenance; it does not create an external GPU artifact.
- A `.snap` source is owned under `Temp/PerfMeter/MemorySnapshots`, limited to 512 MiB, and copied with a streaming SHA-256 into the bundle. The total bundle retention quota is 2 GiB. Treat snapshots as sensitive process-memory data and protect/review them before sharing.

See the localized [API](./docs/en/api.md), [MCP](./docs/en/mcp.md), [Workflows](./docs/en/workflows.md), and [Limitations](./docs/en/limitations.md) pages for the optional integration details.

## Optional Graphics-State Diagnostics

`PerformanceMeter.GetGraphicsDiagnostics()` reports dynamic shader GPU-program and graphics-pipeline creation markers, their exact/alias recorder provenance, discovered units and data types, and graphics API context. Values are raw recorder values; availability is explicit and can change by Unity version, platform, and runtime catalog refresh.

The optional `SGG.PerfMeter.GraphicsStateCollection` assembly supports a bounded trace and synchronous prewarm workflow on Unity `6000.4+`. Start and keep a PerfMeter session recording through the trace; `StopSession()` cancels an active trace. The owned `.graphicsstate` artifact is written below `Temp/PerfMeter/GraphicsStateCollections`, limited to 64 MiB, and correlated session samples carry `graphics_state_trace_id`. Cache-miss evidence is not supported by the Unity backend.

See the localized [API](./docs/en/api.md), [MCP](./docs/en/mcp.md), [Workflows](./docs/en/workflows.md), and [Limitations](./docs/en/limitations.md) pages for the graphics diagnostics and trace workflow.

## Render Integration Context

`PerformanceMeter.GetRenderIntegrationSnapshot()` and `TryGetRenderIntegrationSnapshot(...)` expose the additive `PerfMeterRenderIntegrationSnapshot` for URP Render Graph and HDRP Custom Pass integrations. The snapshot includes the current pipeline and asset source, the observed camera identity, observation frame/age and current-pipeline match, integration/pass/injection metadata, scheduled PerfMeter pass count, effective rendering mode where a stable public API provides it, and nested GRD/VRS context. `perfmeter.render.snapshot` exposes the same read-only data; `perfmeter.rendergraph.snapshot` remains available as the legacy facade.

Reads do not start runtime collection. A stale observation is marked as not matching the current pipeline instead of being presented as current. Capture context schema v1 preserves `render` and adds `render_integration`; session schemas are unchanged. Unity does not expose a stable public RenderGraph/CustomPass viewer or pass-target API for navigation, so PerfMeter does not promise Editor navigation or private pass/resource counters.

The nested GRD context reports public SRP/project/compute support, Unity's global runtime-enabled result, current-frame URP Forward+/clustered compatibility, structured degraded reasons, and provenance-rich BRG effectiveness counters. BRG values are aggregate `BatchRendererGroup` evidence, not proof that a particular renderer used GRD; unavailable or unsampled JSON values are `null`.

## Quick Start

1. Install the Unity package from npm registry or Git UPM.
2. Open `SGG/Perfmeter/Setup` in Unity.
3. Run the recommended setup, enter Play Mode, and confirm that the overlay appears.

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
    "com.sungeargames.perfmeter": "2026.8.11-1"
  }
}
```

For Git UPM and local-copy options, see [Installation](./docs/en/installation.md) and [Quick Start](./docs/en/quick-start.md).

## First-Time Setup And Bootstrap

Open `SGG/Perfmeter/Setup` and use the **FTUE** tab for required setup checks and optional continuations. Optional rows provide focused next actions when their package, backend, or external tool is available:

- **Memory Profiler**: open the Unity window, copy a one-shot `RequestMemorySnapshot(...)` snippet or an explicitly enabled runtime-trigger snippet, and open Runtime.
- **Profile Analyzer**: open the existing session integration or Runtime. It copies a PerfMeter session ID for manual search after the relevant Unity Profiler data is recorded or loaded; it does not load or filter that data automatically.
- **Adaptive Performance**: open Runtime to inspect optional telemetry status.
- **RenderDoc**: keep RenderDoc user-installed, then optionally use **Download Verified Bridge** or **Install Local Bridge** for the separately published SHA-256-pinned Windows x64 Editor bridge. FTUE also checks Unity's shared attachment signal, exposes cancel/remove and restart guidance, and copies a `NativeRequired` + `Copy` capture snippet. The package never installs or loads RenderDoc itself.
- **GraphicsStateCollection**: copy trace/prewarm snippets, use Runtime, and reveal the owned artifact under `Temp/PerfMeter/GraphicsStateCollections`. Keep a PerfMeter session recording through the trace, then prewarm the returned artifact path; FTUE does not request either operation automatically.

The Setup **Initialization Code** section generates a complete normalized project-settings snapshot and applies it after scene load through the public API:

```csharp
public static bool TryApplySettingsJson(string json, out string warning);
```

The generated bootstrap includes overlay, logging, alert, session-default, and overdraw settings, honors `enabled` and `collectionMode: "Stopped"`, and does not start sessions or captures. It is an alternative to the Resources zero-code file at `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`. If both are present, a valid explicit application suppresses Resources auto-start for the current domain and becomes authoritative; invalid explicit JSON leaves the runtime unchanged.

See [Workflows](./docs/en/workflows.md), [API](./docs/en/api.md), and [Setup Window Screenshots](./docs/en/setup-window-screenshots.md) for the exact continuation steps and limitations.

## Common Workflows

- **Zero-code overlay**: create `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` from the setup window and let PerfMeter auto-start.
- **Runtime API**: call `PerformanceMeter.EnsureRunning()`, then read immutable status, metrics, device, camera, and session snapshots.
- **Session export**: record bounded profiling windows and export JSON/CSV with scene, device, camera, settings, counters, warnings, and worst-frame metadata.
- **Overdraw diagnostics**: request a bounded numerical measurement or enable the visual heatmap when the URP renderer feature is installed; HDRP explicitly reports overdraw/heatmap as unsupported.
- **MCP automation**: use MCP command metadata to start collection, switch overlay modes, export sessions, inspect alerts, and read snapshots.

See [Workflows](./docs/en/workflows.md), [API](./docs/en/api.md), and [MCP](./docs/en/mcp.md).

## Screenshots

See the default overlay preset, setup window pages, visual presets, and runtime widgets in the screenshot galleries.

Start with [Visual Presets](./docs/en/presets.md), [Setup Window Screenshots](./docs/en/setup-window-screenshots.md), [Implemented Widgets](./docs/en/widgets.md), and [Screenshots](./docs/en/screenshots.md).

## Compared With FPS Counters

Advanced FPS Counter and Graphy are strong general-purpose drop-in visual overlays. SGG PerfMeter intentionally focuses on modern Unity SRP diagnostics: structured timing and render counters, bottleneck classification, reproducible sessions, device/camera snapshots, URP overdraw diagnostics, URP Render Graph state, HDRP Custom Pass state, and MCP/API automation.

Use [Comparison](./docs/en/comparison.md) as product and architecture context rather than measured runtime benchmark data.

## Requirements

- Unity `6000.4+` for supported runtime usage.
- URP `17.4+` with Render Graph path or HDRP `17.4+` with the package HDRP Custom Pass integration.
- Frame Timing Stats enabled before relying on FrameTimingManager in builds.
- Vulkan is preferred on Android when GPU timing matters.
- Optional native RenderDoc capture is supported only in the Windows x64 Unity Editor on Direct3D 11, Direct3D 12, or Vulkan. Development Player, Linux native, IL2CPP, mobile, and macOS native paths are not supported.

Unity `2022.3` through `6000.3` may be import-safe for compile checks, but runtime overlay, render integration, overdraw passes, and support expectations target Unity `6000.4+` with URP `17.4+` or HDRP `17.4+`. Some features may not work in versions before `6000.4`.

In the Editor, `PerfMeterSetupActions.GetCompatibilityStatus()` reports `ImportCompatible`, `CoreRuntimeCompatible`, and `RenderIntegrationCompatible` independently with the detected Unity/SRP versions and explicit reasons. The same structured state is available through `perfmeter.compatibility.status` and inside `perfmeter.setup.status`; render compatibility does not imply that renderer assets are already configured.

## License

This package is licensed under **Stinger Royalty-Free EULA 1.0**.

- Authoritative license text: [LICENSE.ru.md](./LICENSE.ru.md)
- English convenience translation: [LICENSE.md](./LICENSE.md)
- Notices: [NOTICE.md](./NOTICE.md) and [NOTICE.ru.md](./NOTICE.ru.md)
- Brand usage policy: [English](./docs/en/brand.md), [Russian](./docs/ru/brand.md), [German](./docs/de/brand.md), [Spanish](./docs/es/brand.md), [French](./docs/fr/brand.md), [Italian](./docs/it/brand.md), [Japanese](./docs/ja/brand.md), [Korean](./docs/ko/brand.md), [Brazilian Portuguese](./docs/pt-br/brand.md), and [Simplified Chinese](./docs/zh-cn/brand.md)
