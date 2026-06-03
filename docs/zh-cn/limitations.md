# 限制

SGG PerfMeter 设计为低开销 runtime diagnostics layer。对于深度 capture，仍应使用 Unity Profiler、RenderDoc、Profile Analyzer 或 Frame Debugger。

## Platform And Pipeline Scope

- 受支持的 runtime target：Unity `6000.4+`，搭配 URP `17.4+` 和 Render Graph path。
- Built-in Render Pipeline 不受支持，也没有计划支持。
- HDRP support 已规划为 future work，但未在 `2026.6.5-2` 中实现。
- Unity `2022.3` 到 `6000.3` 可能可导入用于 compile-safety，但 runtime behavior 和 support target 是 Unity `6000.4+`。

## Timing Availability

- GPU timing 可能因 platform 和 graphics API 不可用、延迟或不可靠。
- `CollectionFrame` 是 PerfMeter 收集 snapshot 的 Unity frame，不一定是 `FrameTimingManager` 所代表的精确 hardware frame。
- 当 GPU frame timing 很重要时，Android 应优先使用 Vulkan。
- 对 GPU timing 和 overdraw instrumentation，应将 OpenGL/OpenGLES 视为 degraded mode。

## Counter Availability

Profiler counters 会因 platform、Unity version、render pipeline settings 和 graphics API 而异。使用 `AvailableCounters`、`UnavailableCounters` 和 warnings，不要假定每个 counter 在所有地方都存在。

## Overdraw Cost And Support

Numerical overdraw 和 visual heatmap 属于 diagnostic modes。它们会增加 rendering work，应在有边界的窗口内使用，不应作为稳定运行的 gameplay UI 长期开启。

Numerical overdraw 需要：

- 将 `PerfMeterRenderGraphFeature` 安装到 active URP renderer；
- fragment-stage UAV/storage-buffer support；
- compute shader support；
- 受支持的 graphics API；
- async GPU readback support。

不受支持的目标会通过 warnings 报告 `OverdrawState.Unsupported`。

## Overlay Cost

Overlay 会注意 allocation 并进行 throttling，但变化的数值和 graph labels 仍可能在 refresh interval 产生 managed strings。Heavy visual diagnostics 和 graph modes 应在目标设备上验证。

## Validation Status

当前验证包含自动化 EditMode 和 PlayMode coverage，以及 Android S23 Vulkan/GLES smoke validation。在将数据作为 release-signoff evidence 前，更广泛的 player-build 和 device coverage 仍然有价值。
