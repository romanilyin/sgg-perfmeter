# Setup 窗口

可从 `SGG/Perfmeter/Setup` 打开 Editor 窗口。

## 当前行为

- **Setup** 和 **Presets** 展示已持久化的 PerfMeter 项目设置和叠加层预设数据，包括架构/版本、`legacy` 兼容和保留元数据行（均为只读）、组件组合，以及失去焦点时会规范化的数值。
- **Runtime** 以只读方式显示会话、内存、graphics-state、render integration 和 GRD/BRG 诊断，以及可选集成的能力/状态。`Unavailable`、`unknown` 和无样本状态会保持明确显示。`Measure Overdraw (project default)` 使用项目默认 sentinel 值。
- 可执行 `Session Analysis`、`Profile Analyzer` 和 `Refresh`。`Start Session` 与 `Stop Session` 仅在 Play Mode 中可用。打开或刷新 Setup 都不会启动运行时采集。
- memory snapshot 以及 graphics-state trace/prewarm 的请求参数仅属于运行时输入，不是项目设置。

## 参考截图

> 以下截图早于 P3.5。它们仅作为视觉参考保留，不是已完成 Setup UX 的当前证据。

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-zh-cn-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-zh-cn-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-zh-cn-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-zh-cn-debug.png)
