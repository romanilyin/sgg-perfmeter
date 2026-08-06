# Limitations

SGG PerfMeter is designed as a low-overhead runtime diagnostics layer, not a deep capture replacement for Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.

## Platform And Pipeline Scope

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline is unsupported and not planned.
- HDRP overdraw and heatmap are unsupported. HDRP projects still get FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup, and MCP diagnostics.
- Unity `2022.3` through `6000.3` may import for compile-safety, but runtime behavior and support target Unity `6000.4+`.

## Timing Availability

- GPU timing can be unavailable, delayed, or unreliable depending on platform and graphics API.
- `CollectionFrame` is the Unity frame where PerfMeter collected the snapshot, not necessarily the exact hardware frame represented by `FrameTimingManager`.
- Android should prefer Vulkan when GPU frame timing matters.
- OpenGL/OpenGLES should be treated as degraded mode for GPU timing and overdraw instrumentation.

## Counter Availability

Profiler counters vary by platform, Unity version, render pipeline settings, and graphics API. Use `AvailableCounters`, `UnavailableCounters`, and warnings instead of assuming every counter exists everywhere.

## External GPU Capture

- The coordinator uses Unity's experimental `ExternalGPUProfiler`; it does not ship, inject, launch, or identify RenderDoc or PIX.
- Capture is available only in the Editor or Development Builds when the requested external profiler is already attached.
- RenderDoc support is limited to Windows/Linux desktop with Direct3D 11, Direct3D 12, or Vulkan. PIX support is limited to Windows desktop with Direct3D 12.
- One capture request can be active at a time. Heavy capture is always explicit and opt-in.
- `Completed` confirms the Unity begin/end wrapper lifecycle only. It does not prove that an external `.rdc`/`.wpix` artifact exists and does not provide an artifact path.
- Automated tests use a fake backend. Real external-tool and artifact confirmation remains a release-candidate gate; MCP orchestration and correlated artifact bundles are separate future work.

## Overdraw Cost And Support

Numerical overdraw and visual heatmap are diagnostic modes. They add rendering work and should be used in bounded windows, not left on as steady-state gameplay UI.

Numerical overdraw requires URP and:

- `PerfMeterRenderGraphFeature` installed into the active URP renderer;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- supported graphics API;
- async GPU readback support.

Unsupported targets, including HDRP, report `OverdrawState.Unsupported` with warnings.

## Overlay Cost

The overlay is allocation-conscious and throttled, but changed numeric values and graph labels can still materialize managed strings at the refresh interval. It has two UI Toolkit backend paths: an owned `UIDocument` host on Unity `6000.4` and an owned `PanelRenderer` host on Unity `6000.5+`. The host preserves foreign UI panel settings and children and rebuilds only the PerfMeter-owned container. Numeric values use stable reserved slots and a numeric monospace role; `FpsOnly` uses a deterministic bounded two-row fallback when one row does not fit, while cards and bars wrap at narrow logical widths. These bounds do not promise every arbitrary resolution or scale, so heavy visual diagnostics, graph modes, and the resulting layout should be validated on target devices.

## Validation Status

Current validation includes automated EditMode coverage, HDRP smoke validation in Unity `6000.4.10f1`, and previous Android S23 Vulkan/GLES smoke validation. Broader player-build and device coverage is still useful before treating data as release-signoff evidence.
