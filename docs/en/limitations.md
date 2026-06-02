# Limitations

SGG PerfMeter is designed as a low-overhead runtime diagnostics layer, not a deep capture replacement for Unity Profiler, RenderDoc, Profile Analyzer, or Frame Debugger.

## Platform And Pipeline Scope

- Supported runtime target: Unity `6000.4+` with URP `17.4+` and Render Graph path.
- Built-in Render Pipeline is unsupported and not planned.
- HDRP support is planned for future work, but it is not implemented in `2026.6.5-1`.
- Unity `2022.3` through `6000.3` may import for compile-safety, but runtime behavior and support target Unity `6000.4+`.

## Timing Availability

- GPU timing can be unavailable, delayed, or unreliable depending on platform and graphics API.
- `CollectionFrame` is the Unity frame where PerfMeter collected the snapshot, not necessarily the exact hardware frame represented by `FrameTimingManager`.
- Android should prefer Vulkan when GPU frame timing matters.
- OpenGL/OpenGLES should be treated as degraded mode for GPU timing and overdraw instrumentation.

## Counter Availability

Profiler counters vary by platform, Unity version, render pipeline settings, and graphics API. Use `AvailableCounters`, `UnavailableCounters`, and warnings instead of assuming every counter exists everywhere.

## Overdraw Cost And Support

Numerical overdraw and visual heatmap are diagnostic modes. They add rendering work and should be used in bounded windows, not left on as steady-state gameplay UI.

Numerical overdraw requires:

- `PerfMeterRenderGraphFeature` installed into the active URP renderer;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- supported graphics API;
- async GPU readback support.

Unsupported targets report `OverdrawState.Unsupported` with warnings.

## Overlay Cost

The overlay is allocation-conscious and throttled, but changed numeric values and graph labels can still materialize managed strings at the refresh interval. Heavy visual diagnostics and graph modes should be validated on target devices.

## Validation Status

Current validation includes automated EditMode and PlayMode coverage plus Android S23 Vulkan/GLES smoke validation. Broader player-build and device coverage is still useful before treating data as release-signoff evidence.
