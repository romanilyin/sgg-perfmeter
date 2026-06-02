# Быстрый старт

Этот путь запускает видимый оверлей без написания кода.

## 1. Откройте окно настройки

В Unity откройте:

```text
SGG/Perfmeter/Setup
```

## 2. Запустите рекомендованную настройку

Через окно настройки можно:

- включить Frame Timing Stats;
- установить `PerfMeterRenderGraphFeature` в активные URP Renderers, доступные для редактирования;
- создать проектные JSON-настройки по умолчанию;
- настроить видимость оверлея, угол, целевой FPS, визуальный пресет и режим сбора.

Настройки запуска без кода сохраняются в:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Если `enabled` и `autoStart` включены, PerfMeter стартует из этого JSON во время выполнения.

## 3. Войдите в Play Mode

Оверлей должен появиться в выбранном углу. Если этого не произошло:

- проверьте, что JSON-файл настроек находится в Resources;
- проверьте, что оверлей включен в окне настройки;
- проверьте, что режим сбора во время выполнения равен `Overlay`;
- проверьте, что активный URP Renderer содержит `PerfMeterRenderGraphFeature`, если тестируется диагностика Render Graph или overdraw.

## Критерии готовности

Настройка завершена, когда:

- оверлей появился в выбранном углу;
- FPS и CPU timing обновляются с настроенным интервалом;
- `PerformanceMeter.GetStatus().CollectionMode` возвращает `Overlay`.

## Опциональный ручной запуск

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

## Первые полезные API-вызовы

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
