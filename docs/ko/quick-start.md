# 빠른 시작

이 경로는 code 작성 없이 보이는 overlay를 실행합니다.

## 1. Setup Window 열기

Unity에서 다음을 엽니다.

```text
SGG/Perfmeter/Setup
```

## 2. 권장 Setup 실행

setup window에서 다음을 수행합니다.

- Frame Timing Stats 활성화;
- editable active URP renderer에 `PerfMeterRenderGraphFeature` 설치;
- project-owned 기본 JSON settings 생성;
- overlay visibility, corner, target FPS, visual preset, collection mode 설정.

zero-code settings file은 다음 위치에 저장됩니다.

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

`enabled`와 `autoStart`가 true이면 PerfMeter는 runtime에서 이 JSON으로 시작합니다.

## 3. Play Mode 진입

기본 overlay가 선택한 corner에 표시되어야 합니다. 표시되지 않으면 다음을 확인합니다.

- JSON settings file이 Resources path에 있는지 확인합니다.
- setup window에서 overlay가 visible인지 확인합니다.
- runtime collection mode가 `Overlay`인지 확인합니다.
- Render Graph diagnostics 또는 overdraw를 테스트할 때 active URP renderer에 `PerfMeterRenderGraphFeature`가 있는지 확인합니다.

## 완료 기준

다음 상태이면 완료입니다.

- overlay가 선택한 corner에 표시됩니다.
- FPS 및 CPU timing이 설정된 refresh interval에 맞춰 갱신됩니다.
- `PerformanceMeter.GetStatus().CollectionMode`가 `Overlay`를 보고합니다.

## 선택 사항: 수동 Bootstrap

명시적인 startup control이 필요할 때 code를 사용합니다.

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

## 처음 확인할 유용한 값

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
