# 限制

SGG PerfMeter 设计为低开销 runtime diagnostics layer。对于深度 capture，仍应使用 Unity Profiler、RenderDoc、Profile Analyzer 或 Frame Debugger。

## Platform And Pipeline Scope

- 支持的 runtime target：Unity `6000.4+`，搭配 URP `17.4+` Render Graph 或 HDRP `17.4+` Custom Pass integration。
- Built-in Render Pipeline unsupported，且不计划支持。
- HDRP overdraw 和 heatmap unsupported。HDRP projects 仍可使用 FPS、CPU、GPU、memory、sessions、alerts、camera、device、setup 和 MCP diagnostics。
- Unity `2022.3` 到 `6000.3` 可能可导入用于 compile-safety，但 runtime behavior 和 support target 是 Unity `6000.4+`。

## Timing Availability

- GPU timing 可能因 platform 和 graphics API 不可用、延迟或不可靠。
- `CollectionFrame` 是 PerfMeter 收集 snapshot 的 Unity frame，不一定是 `FrameTimingManager` 所代表的精确 hardware frame。
- 当 GPU frame timing 很重要时，Android 应优先使用 Vulkan。
- 对 GPU timing 和 overdraw instrumentation，应将 OpenGL/OpenGLES 视为 degraded mode。

## Counter Availability

Profiler counters 会因 platform、Unity version、render pipeline settings 和 graphics API 而异。使用 `AvailableCounters`、`UnavailableCounters` 和 warnings，不要假定每个 counter 在所有地方都存在。

## External GPU Capture

- Coordinator 只允许一个 active request，并以 deterministic 顺序经过 `PreRoll`、`Capturing`、`PostRoll` 和 `Completed`。相同的 active ID 是 idempotent，不同的 active ID 会作为 overlap 被 reject。
- Backend 仅在 Editor 或 Development Builds、external tool 已 attach 时使用 Unity 的 experimental `ExternalGPUProfiler`。`RenderDoc` 限定为 Windows/Linux desktop 的 Direct3D 11、Direct3D 12 或 Vulkan；`PIX` 限定为 Windows desktop 的 Direct3D 12。
- `Completed` 只确认 Unity wrapper lifecycle，不证明 external `.rdc`/`.wpix` artifact 存在，也不提供 artifact path。
- Automated tests 使用 fake backend。Real external tool 和 artifact 的确认仍是 release gate。
- Capture bundles、artifact provenance 和 MCP capture control 不属于本 coordinator，属于独立的 future work。

## Overdraw Cost And Support

Numerical overdraw 和 visual heatmap 属于 diagnostic modes。它们会增加 rendering work，应在有边界的窗口内使用，不应作为稳定运行的 gameplay UI 长期开启。

URP 中的 numerical overdraw 需要：

- 将 `PerfMeterRenderGraphFeature` 安装到 active URP renderer；
- fragment-stage UAV/storage-buffer support；
- compute shader support；
- 受支持的 graphics API；
- async GPU readback support。

不受支持的目标（包括 HDRP）会带 warnings 返回 `OverdrawState.Unsupported`。

## Overlay Cost

Overlay 会注意 allocation 并进行 throttling，但变化的数值和 graph labels 仍可能在 refresh interval 产生 managed strings。它有两条 UI Toolkit backend path：Unity `6000.4` 使用 owned `UIDocument` host，Unity `6000.5+` 使用 owned `PanelRenderer` host。host 会保留 foreign UI 的 panel settings 和 children，只 rebuild PerfMeter-owned container。numeric values 使用 stable reserved numeric slots 和 numeric monospace role；当一行无法容纳时，`FpsOnly` 使用 deterministic bounded two-row fallback，cards 和 bars 会在较窄的 logical widths 下 wrap。这可以降低 clipping 风险，但不保证任意 resolution 或 scale；heavy visual diagnostics、graph modes 和最终 layout 仍必须在目标设备上验证。

## Validation Status

当前验证包含自动化 EditMode coverage、Unity `6000.4.10f1` 中的 HDRP smoke validation，以及之前的 Android S23 Vulkan/GLES smoke validation。在将数据作为 release-signoff evidence 前，更广泛的 player-build 和 device coverage 仍然有价值。
