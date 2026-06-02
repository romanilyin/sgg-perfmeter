# Inicio Rápido

Esta ruta muestra un overlay visible sin escribir código.

## 1. Abre La Ventana De Setup

En Unity, abre:

```text
SGG/Perfmeter/Setup
```

## 2. Ejecuta La Configuración Recomendada

Usa la ventana de setup para:

- activar Frame Timing Stats;
- instalar `PerfMeterRenderGraphFeature` en los URP renderers activos editables;
- crear la configuración JSON predeterminada propiedad del proyecto;
- configurar la visibilidad del overlay, la esquina, el target FPS, el visual preset y el modo de recolección.

El archivo de configuración sin código se guarda en:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Cuando `enabled` y `autoStart` son true, PerfMeter arranca desde este JSON en runtime.

## 3. Entra En Play Mode

El overlay predeterminado debería aparecer en la esquina seleccionada. Si no aparece:

- confirma que el archivo JSON de configuración existe en la ruta Resources;
- confirma que el overlay está visible en la ventana de setup;
- confirma que el modo de recolección runtime es `Overlay`;
- confirma que el URP renderer activo tiene `PerfMeterRenderGraphFeature` al probar diagnósticos Render Graph u overdraw.

## Criterios De Finalización

Has terminado cuando:

- el overlay aparece en la esquina seleccionada;
- FPS y CPU timing se actualizan en el intervalo de refresco configurado;
- `PerformanceMeter.GetStatus().CollectionMode` informa `Overlay`.

## Bootstrap Manual Opcional

Usa código cuando quieras control explícito del arranque:

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

## Primeras Lecturas Útiles

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
