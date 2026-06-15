# 제한 사항

SGG PerfMeter는 low-overhead runtime diagnostics layer로 설계되었습니다. Unity Profiler, RenderDoc, Profile Analyzer, Frame Debugger의 deep capture를 대체하는 용도가 아닙니다.

## Platform 및 Pipeline 범위

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline은 unsupported이며 planned 상태가 아닙니다.
- HDRP overdraw 및 heatmap은 unsupported입니다. HDRP projects에서도 FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup, MCP diagnostics는 사용할 수 있습니다.
- Unity `2022.3`부터 `6000.3`까지는 compile-safety를 위해 import될 수 있지만, runtime behavior 및 support target은 Unity `6000.4+`입니다.

## Timing Availability

- GPU timing은 platform 및 graphics API에 따라 unavailable, delayed, unreliable 상태일 수 있습니다.
- `CollectionFrame`은 PerfMeter가 snapshot을 수집한 Unity frame이며, `FrameTimingManager`가 나타내는 정확한 hardware frame과 반드시 같지는 않습니다.
- GPU frame timing이 중요하다면 Android에서는 Vulkan을 우선 사용합니다.
- OpenGL/OpenGLES는 GPU timing 및 overdraw instrumentation에서 degraded mode로 취급해야 합니다.

## Counter Availability

Profiler counter는 platform, Unity version, render pipeline settings, graphics API에 따라 달라집니다. 모든 counter가 어디서나 존재한다고 가정하지 말고 `AvailableCounters`, `UnavailableCounters`, warnings를 사용합니다.

## Overdraw 비용 및 지원

Numerical overdraw와 visual heatmap은 diagnostic mode입니다. rendering work를 추가하므로 steady-state gameplay UI로 계속 켜 두지 말고 bounded window에서 사용해야 합니다.

Numerical overdraw에는 다음이 필요합니다.

- active URP renderer에 설치된 `PerfMeterRenderGraphFeature`;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- supported graphics API;
- async GPU readback support.

지원되지 않는 target은 warnings와 함께 `OverdrawState.Unsupported`를 보고합니다.

## Overlay 비용

overlay는 allocation-conscious이며 throttled되지만, 변경된 numeric value와 graph label은 refresh interval마다 managed string을 만들 수 있습니다. 무거운 visual diagnostics 및 graph mode는 target device에서 검증해야 합니다.

## Validation Status

현재 validation에는 automated EditMode 및 PlayMode coverage와 Android S23 Vulkan/GLES smoke validation이 포함됩니다. 데이터를 release-signoff evidence로 다루기 전에는 더 넓은 player-build 및 device coverage가 여전히 유용합니다.
