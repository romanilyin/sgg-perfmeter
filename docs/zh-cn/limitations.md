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
- `GenericUnity` 在 Editor/Development Build 中使用 Unity experimental `ExternalGPUProfiler`。其 matrix 仍是 Windows/Linux desktop D3D11/D3D12/Vulkan 上的 RenderDoc，以及 Windows desktop D3D12 上的 PIX；completion 不会 authenticate tool/artifact identity。
- Optional native path 仅支持 Windows x64 Unity Editor 的 D3D11、D3D12 或 Vulkan 上的 RenderDoc。不支持 Development Player、Linux native、IL2CPP、mobile 和 macOS native。
- UPM package 保持 binary-free。单独发布的 pinned bridge 只使用 already-loaded `renderdoc.dll`，绝不会 install/load/launch/inject RenderDoc。
- Native MetadataOnly 默认使用 `DoNotShare`；Copy/Embed 属于 sensitive data，使用 separate quota 并要求 `ReviewBeforeShare`。Generic/caller artifact 仍为 observed，不是 authoritative。
- Automated tests 使用 fake backend。Real external tool 和 artifact 的确认仍是 release gate。
- Correlated bundles 和 MCP capture control 已可用，但传入的 `.rdc`/`.wpix` 仅是 observed/hashed artifact。Unity 无法验证 attached tool 或 capture association，因此 real external tool 验证仍是 release-candidate gate。

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

## 可选内存快照：限制与隐私

- Unity `6000.4+` 中没有 `com.unity.memoryprofiler` `1.1.0+` 时，此功能不可用；core package 不会安装或要求该 dependency。
- 默认只启用 manual capture。system-memory threshold 和 bounded leak-growth trigger 需要 opt-in；每个 request 都受 single-flight/overlap、cooldown、minimum free-space、backend 和 capture-flag guard 约束。
- owned `.snap` staging 位于 `Temp/PerfMeter/MemorySnapshots`，上限为 512 MiB。memory-only evidence 导出到 `Temp/PerfMeter/CaptureBundles`，bundle retention total quota 为 2 GiB。成功 export 是 one-shot 并删除 staging source；清理失败会有明确 warning。
- snapshot 可能包含敏感的 process memory。分享前请保护并检查内容。bundle 会记录 `contains_sensitive_memory`、backend/flag provenance、`memory-snapshot.json` 和 SHA-256 metadata，但不会创建 external GPU artifact。
- OS file lock 导致的删除，以及 portable managed 对 reparse-point race 的保护，均为 best-effort。不安全或非 owned path 会被 reject，cleanup failure 会保留为 warning。
- evidence 包括 memory EditMode `9/9`、capture-bundle EditMode `14/14`、PlayMode threshold `1/1`、使用 `com.unity.memoryprofiler@1.1.12` 的 optional compile，以及 Unity `6000.4.12f1` full EditMode `182/182` 和 full PlayMode `14/14`。这不代表 release-player 或 device behavior 已获验证。

## 图形诊断与 GraphicsStateCollection 的限制

- shader GPU-program creation 和 graphics-pipeline creation marker 是动态的 `ProfilerRecorder` capability。Unity、platform、graphics API 和 catalog refresh 状态都会影响 availability。请使用 `Unavailable`、`AvailableNoSample`、`AvailableSampled` 及 provenance，不要从 numeric 0 推断 availability。
- marker value 保留 recorder 的 `Unit` 和 `DataType`，并保持 raw value。它们不一定是 shader 或 PSO count，PerfMeter 也不会转换到统一 unit。capability metadata 包含 exact/alias resolution、resolved recorder names、resolved/sampled component count 和 catalog revision。
- 可选 `SGG.PerfMeter.GraphicsStateCollection` assembly 面向 Unity `6000.4+`。`6000.4` 使用 `UnityEngine.Experimental.Rendering.GraphicsStateCollection`，`6000.5+` 使用 `UnityEngine.Rendering.GraphicsStateCollection`；更早的 Unity 不支持该 integration。
- trace 需要 active PerfMeter session。普通 Play Mode 在 end-of-frame 后完成 trace frame，batch mode 使用 next-frame fallback。correlated session sample 受 session 的 warm-up、interval 和 max-sample 设置限制。
- graphics-state flight 只允许一个，包括 preparation、trace finalization、prewarm 和 cleanup。active external GPU capture、memory snapshot、alert-capture 也会导致 overlap rejection。`IsBusy`/`is_busy` 覆盖这些 flight 和 persisted cleanup；`HasPendingCleanup`/`has_pending_cleanup` 专门报告等待 retry 的 owned artifact。matching cancel 是 best-effort，cleanup failure 会保持可见并可能延迟下一次 request。
- `StopSession()` 会取消 active trace，因此整个 trace 期间都需要 active session。owned artifact 删除失败会创建旁边的 `.delete-pending` sidecar marker；domain reload 后会恢复并重试。artifact 和 marker 清理完成前，warning 与 busy state 会保持可见。
- prewarm 只接受 owned project-relative artifact，以 synchronous 方式执行并保留 artifact；progressive warmup 可能 incomplete。Unity backend 不支持 cache-miss tracing，因此 request 返回 `Unavailable`，不会暴露 cache-miss evidence。
- owned `.graphicsstate` artifact 存储在 `Temp/PerfMeter/GraphicsStateCollections` 下，必须是 regular non-empty file，最大 64 MiB。trace 上限为 600 frames，progressive prewarm 上限为 1,000,000 states，同时应用 minimum-free-disk 和 project-local path guard。
- 最终 evidence：Unity `6000.4.12f1` compile passed；GSC EditMode targeted `25/25`、`PerformanceMeter` API EditMode `47/47`、capture-bundle EditMode `14/14`、PlayMode smoke `12/12`、full post-fix EditMode `208/208`、full post-fix PlayMode `16/16` 均通过。Unity `6000.5.6f1` optional consumer compile 也已 isolated passed。Unity `6000.5` full tests、release-player 和 target-device behavior 仍是 release gate，本文不声称已验证。

## Render integration context 的限制

- `PerfMeterRenderIntegrationSnapshot` 是 integration-neutral observation contract，不是深度 Render Graph 或 Custom Pass capture。read 不会启动 runtime；第一次 observation 之前，支持的 current pipeline 可能是 `Available`/`NotObserved`。pipeline/configuration 改变后，会通过 `ObservationMatchesCurrentPipeline: false`、明确的 frame/age 和 warning 标记 stale observation。
- URP 使用 public current-frame `UniversalRenderingData.renderingMode`，并报告实际 schedule 的 PerfMeter pass。HDRP 报告实际的 PerfMeter `CustomPass`，但 effective rendering mode 仍 unavailable。
- private/internal Render Graph pass/resource reflection 已移除。由于没有 stable public API，legacy facade 的 `registered_pass_count`、`merged_pass_count`、`transient_resource_count`、`imported_resource_count`、`aliased_resource_count` 保持 `-1`。
- GRD activity 使用 public `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` 结果，表示 global runtime state，不证明某个 camera/renderer 使用了 GRD。URP Forward+ 是 current-frame observation；HDRP 的 rendering-mode/Forward+ availability 保持 `Unknown`。
- GRD effectiveness 使用带 exact capability provenance 的 aggregate BRG draw-call/instance counters。它们可能包含其他 `BatchRendererGroup` 用户，因此不证明逐 renderer 的 GRD participation。unavailable 或尚未 sampled 的值序列化为 `null`。
- VRS 提供 `SystemInfo`/`ShadingRateInfo` 的 authoritative hardware support。除非未来 typed adapter 能证明，否则 configuration/activity 为 `Unknown`；不声称 VRS activity。
- Unity stable public API 不提供 RenderGraph/CustomPass viewer 或 pass-target API。因此 PerfMeter 不增加也不承诺 Editor navigation。
- capture context schema v1 保留 `render` 并添加 `render_integration`；session JSON/CSV schema 不变。external capture context 在第一个 `Capturing` sample 冻结，不会被后续 read 替换。
- PM-REN-001 最终 evidence：Unity `6000.4.12f1` main compile passed；targeted `PerformanceMeterApiTests` `53/53`、`PerfMeterCaptureBundleTests` `15/15`、`PerformanceMeterPlayModeSmokeTests` `12/12`；final full EditMode `215/215` 和 full PlayMode `16/16` passed。Focused review P1/P2 resolved。isolated compile matrix 已通过：Unity `6000.4.12f1` URP `17.4` 和 HDRP `17.4`，以及 Unity `6000.5.6f1` URP `17.5` 和 HDRP `17.5`。release-player/device validation 仍 pending；不声称 release。
- PM-GRD-001 最终 evidence：Unity `6000.4.12f1` compile passed；targeted API `58/58`、capture-bundle `15/15`、PlayMode smoke `12/12`；full EditMode `220/220`、PlayMode `16/16` passed。Focused review P1/P2 resolved；Unity `6000.4`/`6000.5` 的 URP `17.4`/`17.5` 和 HDRP `17.4`/`17.5` compile matrix 也 passed。release-player/device behavior 仍 pending。
