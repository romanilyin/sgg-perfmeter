# 快速开始

此路径无需编写代码即可显示 overlay。

## 1. 打开 Setup Window

在 Unity 中打开：

```text
SGG/Perfmeter/Setup
```

## 2. 运行 Recommended Setup

使用 setup window：

- 启用 Frame Timing Stats；
- 将 `PerfMeterRenderGraphFeature` 安装到可编辑的 active URP renderers；
- 创建项目持有的默认 JSON settings；
- 配置 overlay visibility、corner、target FPS、visual preset 和 collection mode。

zero-code settings 文件保存到：

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

当 `enabled` 和 `autoStart` 为 true 时，PerfMeter 会在运行时从此 JSON 启动。

## 3. 进入 Play Mode

默认 overlay 应显示在所选角落。如果没有显示：

- 确认 JSON settings 文件存在于 Resources path；
- 确认 setup window 中 overlay 可见；
- 确认 runtime collection mode 为 `Overlay`；
- 测试 Render Graph diagnostics 或 overdraw 时，确认 active URP renderer 具有 `PerfMeterRenderGraphFeature`。

## 完成标准

满足以下条件即可：

- overlay 出现在所选角落；
- FPS 和 CPU timing 按配置的 refresh interval 更新；
- `PerformanceMeter.GetStatus().CollectionMode` 返回 `Overlay`。

## 可选 Manual Bootstrap

需要显式启动控制时使用代码：

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
    {
        PerformanceMeter.EnsureRunning();
        PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
        PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
        PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
        PerformanceMeter.SetOverlayVisible(true);
    }
}
```

## 首次有用读取

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
