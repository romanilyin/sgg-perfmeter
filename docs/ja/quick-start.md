# クイックスタート

この手順では、コードを書かずに表示可能な overlay を起動できます。

## 1. Setup Window を開く

Unity で次を開きます。

```text
SGG/Perfmeter/Setup
```

## 2. Recommended Setup を実行する

setup window で次を行います。

- Frame Timing Stats を有効化します。
- editable active URP renderers に `PerfMeterRenderGraphFeature` をインストールします。
- project-owned の default JSON settings を作成します。
- overlay visibility、corner、target FPS、visual preset、collection mode を設定します。

zero-code settings file は次に保存されます。

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

`enabled` と `autoStart` が true の場合、PerfMeter は runtime にこの JSON から起動します。

## 3. Play Mode に入る

default overlay が選択した corner に表示されるはずです。表示されない場合は次を確認してください。

- JSON settings file が Resources path に存在すること。
- setup window で overlay が visible になっていること。
- runtime collection mode が `Overlay` であること。
- Render Graph diagnostics または overdraw をテストする場合、active URP renderer に `PerfMeterRenderGraphFeature` があること。

## 完了条件

次を満たせば完了です。

- overlay が選択した corner に表示される。
- FPS と CPU timing が configured refresh interval で更新される。
- `PerformanceMeter.GetStatus().CollectionMode` が `Overlay` を返す。

## 任意の手動 Bootstrap

明示的な startup control が必要な場合はコードを使用します。

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

## 最初に読むと有用な値

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
