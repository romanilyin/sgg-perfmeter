# 安装

SGG PerfMeter 目前作为名为 `com.sungeargames.perfmeter` 的 Unity package 分发。`2026.6.5-1` release 不提供 npm install 路径。

## 要求

- Unity `6000.4+`，用于受支持的运行时使用。
- URP `17.4+`，使用 Render Graph path。
- UI Toolkit runtime support。
- 在 build 中依赖 FrameTimingManager 之前启用 Frame Timing Stats。

Package metadata 仍将 Unity `2022.3` 保留为 import-safety floor，用于导入和编译检查。当前受支持的运行时目标是 Unity `6000.4+` 搭配 URP `17.4+` Render Graph。

## Git UPM 安装

此 package 位于仓库内：

```text
Assets/Scripts/SGG.PerfMeter
```

将它添加到 Unity 项目的 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

如果你的环境使用 SSH 管理 Git dependencies：

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

固定 tag 或 commit 可获得可重复安装：

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.5-1"
  }
}
```

## 本地拷贝安装

将此文件夹复制到你的 Unity 项目：

```text
Assets/Scripts/SGG.PerfMeter
```

这适合本地 package 开发，或不希望使用 Git dependencies 的环境。

## 初始项目设置

打开：

```text
SGG/Perfmeter/Setup
```

然后运行 recommended setup：

1. 启用 Frame Timing Stats。
2. 将 `PerfMeterRenderGraphFeature` 安装到可编辑的 active URP renderer assets。
3. 将 JSON settings 保存到 `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` 以使用 zero-code setup，或复制 initialization snippet。
4. 进入 Play Mode 并验证 overlay。

## Samples

从 Package Manager details panel 导入 package samples：

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
