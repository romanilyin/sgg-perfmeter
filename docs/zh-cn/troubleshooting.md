# 故障排查

当 PerfMeter 没有显示预期数据时，使用此 checklist。

## Overlay 未出现

- 打开 `SGG/Perfmeter/Setup`，确认 overlay visibility 已启用。
- 确认 collection mode 是 `Overlay`，而不是 `Background` 或 `Stopped`。
- 如果使用 zero-code setup，确认 settings file 存在于 `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`。
- 如果使用 manual bootstrap，确认在 scene load 后调用了 `PerformanceMeter.EnsureRunning()`。
- 进入 Play Mode；Edit Mode API calls 是安全的，但不会创建 runtime overlay。

## Frame Timing 或 GPU Timing 缺失

- 启用 Player Settings -> Rendering -> Frame Timing Stats。
- 当 GPU frame timing 很重要时，Android 优先使用 Vulkan。
- 将 OpenGL/OpenGLES 视为 GPU timing 的 degraded mode。
- 在假定 counter 存在前，检查 `PerfMeterStatusSnapshot.AvailableCounters`、`UnavailableCounters` 和 `Warning`。

## Overdraw Measurement 不推进

- 将 `PerfMeterRenderGraphFeature` 安装到 active URP renderer。
- 确认 active camera 使用包含该 feature 的 renderer。
- 确认 target backend 支持 fragment UAV/storage buffers、compute shaders 和 async GPU readback。
- 使用 `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` 创建有边界的 measurement window。
- 如果 target 不受支持，PerfMeter 会报告 `OverdrawState.Unsupported`，不会 schedule pass。

## Session Export 失败

- 导出到 project-local path。
- 除非 workflow 明确先移除已有文件，否则不要覆盖 existing export。
- 长时间运行时保持 `MaxSamples` 有界。
- 使用 warm-up frames/seconds，避免 summaries 中包含 startup spikes。

## Alerts 过于频繁

- 在 JSON settings 中调整 thresholds 和 consecutive-frame windows。
- 增加 Editor warning cooldowns。
- 当 callbacks 或 structured logs 已足够时，禁用 Editor warning logs。

## 不同设备上的数据看起来不同

这是预期情况。GPU timings、profiler counters、display information、async readback 和 overdraw support 会随 graphics API、platform、Unity version 和 device 变化。使用导出 sessions 中的 device snapshots 和 warnings 解释差异。
