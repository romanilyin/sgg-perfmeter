# Быстрый Старт

Этот путь запускает видимый overlay без написания кода.

## 1. Откройте Окно Настройки

В Unity откройте:

```text
SGG/Perfmeter/Setup
```

## 2. Запустите Recommended Setup

Через окно настройки можно:

- включить Frame Timing Stats;
- установить `PerfMeterRenderGraphFeature` в editable active URP renderers;
- создать default project-owned JSON settings;
- настроить видимость overlay, corner, target FPS, visual preset и collection mode.

Zero-code settings сохраняются в:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Если `enabled` и `autoStart` включены, PerfMeter стартует из этого JSON в runtime.

## 3. Войдите В Play Mode

Overlay должен появиться в выбранном углу. Если этого не произошло:

- проверьте, что JSON settings file существует на Resources path;
- проверьте, что overlay включен в setup window;
- проверьте, что runtime collection mode равен `Overlay`;
- проверьте, что активный URP renderer содержит `PerfMeterRenderGraphFeature`, если тестируются Render Graph diagnostics или overdraw.

## Опциональный Manual Bootstrap

Используйте код, если нужен явный контроль запуска:

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

## Первые Полезные API-Вызовы

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
