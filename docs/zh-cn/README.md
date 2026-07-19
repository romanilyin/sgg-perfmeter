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
- 通过 URP Render Graph 显式启用的 overdraw measurement 和 visual overdraw heatmap；HDRP overdraw/heatmap unsupported，但 core diagnostics 仍可用。
- 面向代码和 MCP automation 的 device、URP/HDRP camera、render integration、status、metrics、alerts、sessions 和 custom metrics snapshots。

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
    "com.sungeargames.perfmeter": "2026.7.19-1"
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
