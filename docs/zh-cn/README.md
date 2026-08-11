# SGG PerfMeter

**面向 Unity 6 URP+HDRP (FPS meter) 的轻量级运行时诊断与 agent 可读性能分析。**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](../es/README.md) | [Français](../fr/README.md) | [Italiano](../it/README.md) | [日本語](../ja/README.md) | [한국어](../ko/README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](./README.md)

[安装](./installation.md) | [快速开始](./quick-start.md) | [工作流](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [对比](./comparison.md) | [限制](./limitations.md) | [故障排查](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter 可识别帧瓶颈、对比性能变化、记录可复现的会话，并为工具和 AI agents 提供结构化性能分析数据。

## 用途

- 在游戏运行时直接查看瓶颈上下文。
- 在 presets、graphs、metric bars、compact layouts 和 custom metric rows 之间切换。
- 使用 warm-up、scene scope、worst-frame summary 和 JSON/CSV export 记录可复现的性能分析会话。
- 使用 alerts、structured logs、callbacks 和 Editor warning cooldowns，减少持续盯着 overlay 的需要。
- 为工具和 agents 提供结构化数据，用于对比、A/B 测试和热点定位。

## 测量内容

- Unity `6000.4+` / URP `17.4+` Render Graph 和 HDRP `17.4+` Custom Pass 运行时状态。
- FrameTimingManager CPU/GPU timing：CPU frame、main thread、render thread、present wait，以及可用时的 GPU frame time。
- ProfilerRecorder render counters：draw calls、SetPass、batches、vertices、SRP Batcher、BRG/GRD、uploads、memory，以及可用时的 GPU memory。
- GPU、CPU main、CPU render、present/VSync、balanced 或 unknown 的瓶颈分类。
- 通过 Unity generic path 协调 RenderDoc/PIX，或选择使用单独分发并经过 SHA-256 验证的 RenderDoc bridge，在 Windows x64 Editor 的 D3D11/D3D12/Vulkan 上生成 authenticated `.rdc` artifact。UPM package 保持 binary-free，也不会安装 RenderDoc。
- 通过 URP Render Graph 显式启用的 overdraw measurement 和 visual overdraw heatmap；HDRP overdraw/heatmap unsupported，但 core diagnostics 仍可用。
- 面向代码和 MCP automation 的 device、URP/HDRP camera、render integration、status、metrics、alerts、sessions 和 custom metrics snapshots。

## 可选平台遥测

PerfMeter 可以通过一个 provider 可选地收集 thermal 和 Adaptive Performance 信号。core assembly 不包含对 `com.unity.adaptiveperformance` 的 hard dependency；当 `com.unity.adaptiveperformance` 为 `5.1.0+` 时，可为 Unity `6000.4+` 启用可选的 `SGG.PerfMeter.AdaptivePerformance` assembly。

- **Provider API**：`PerformanceMeter.RegisterPlatformTelemetryProvider(...)`、`UnregisterPlatformTelemetryProvider(...)` 和 `GetPlatformTelemetry()` 最多管理一个 active provider，并返回不可变的 `PerfMeterPlatformTelemetrySnapshot`，其中包含 provider ID/version、sample/change time、thermal warning、temperature level/trend、CPU/GPU performance level、adaptive bottleneck 以及每个字段的 availability。注册不同的第二个 provider 会被拒绝。
- **采集与导出**：core 使用 0.25 秒的 bounded cadence 和 cache，并公开 last attempt/result、last success age 与 freshness。capture boundary 会强制采样，同时不会隐藏 `Unavailable`。session JSON/CSV 和 capture samples 会保留 snapshot 及 provider provenance。MCP command `perfmeter.platform.telemetry` 提供当前 structured snapshot。
- **Profiler 与 alerts**：`SGG.PerfMeter.Thermal.Sample` 和 `SGG.PerfMeter.Thermal.Available` 提供 thermal collection marker 与 availability counter。默认 `thermal.throttling` alert 在 imminent 或 active throttling level 可用时使用 `ThermalWarningLevel` metric。
- **Unavailable 状态是明确的**：不支持的 provider 和字段保持 unavailable；JSON 将不可用数值序列化为 `null`，CSV 使用空字段，并通过 availability flags 区分 fake zero。真实 Adaptive Performance package 和 target device validation 仍属于 release matrix/release-candidate gate；本文档不宣称已经获得 device result。

## 快速开始

1. 通过 npm registry 或 Git UPM 安装 Unity package。
2. 在 Unity 中打开 `SGG/Perfmeter/Setup`。
3. 运行 recommended setup，进入 Play Mode，并确认 overlay 出现。

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
    "com.sungeargames.perfmeter": "2026.8.11-2"
  }
}
```

## 文档

- [安装](./installation.md)
- [快速开始](./quick-start.md)
- [工作流](./workflows.md)
- [API](./api.md)
- [MCP 和 Agent 自动化](./mcp.md)
- [Visual Presets](./presets.md)
- [已实现 Widgets](./widgets.md)
- [截图](./screenshots.md)
- [Setup Window 截图](./setup-window-screenshots.md)
- [限制](./limitations.md)
- [故障排查](./troubleshooting.md)
- [对比](./comparison.md)
- [Brand Usage Policy](./brand.md)

## 许可证

此 package 使用 **Stinger Royalty-Free EULA 1.0** 授权。

- 权威俄文许可证文本：[LICENSE.ru.md](../../LICENSE.ru.md)
- 英文辅助翻译：[LICENSE.md](../../LICENSE.md)
- 声明：[NOTICE.md](../../NOTICE.md) 和 [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy：[brand.md](./brand.md)
