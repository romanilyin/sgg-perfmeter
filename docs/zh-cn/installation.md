# 安装

SGG PerfMeter 作为名为 `com.sungeargames.perfmeter` 的 Unity package 分发。当前 public npm version 是 `2026.6.28-1`，Git UPM 和 local copy install 也可用。

## 要求

- Unity `6000.4+`，用于受支持的运行时使用。
- URP `17.4+` with Render Graph path 或 HDRP `17.4+` with Custom Pass integration.
- UI Toolkit runtime support。
- 在 build 中依赖 FrameTimingManager 之前启用 Frame Timing Stats。

Package metadata 仍将 Unity `2022.3` 保留为 import-safety floor，用于导入和编译检查。当前受支持的运行时目标是 Unity `6000.4+` 搭配 URP `17.4+` Render Graph 或 HDRP `17.4+` Custom Pass integration。

## npm Scoped Registry Install

在 Unity project 的 `Packages/manifest.json` 中将 npm registry 添加为 Unity Package Manager scoped registry：

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
    "com.sungeargames.perfmeter": "2026.6.28-1"
  }
}
```

如果 manifest 已有 `scopedRegistries`，请将 `npmjs` entry 合并到现有 array 中。

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
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.28-1"
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
2. 将 `PerfMeterRenderGraphFeature` 安装到可编辑的 active URP renderer assets。HDRP projects 会跳过 URP renderer changes；package HDRP Custom Pass 会在安装 HDRP `17.4+` 时于 runtime 注册。
3. 将 JSON settings 保存到 `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` 以使用 zero-code setup，或复制 initialization snippet。
4. 进入 Play Mode 并验证 overlay。

## Samples

从 Package Manager details panel 导入 package samples：

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
