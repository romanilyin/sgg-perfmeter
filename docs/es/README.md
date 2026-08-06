# SGG PerfMeter

**Diagnóstico ligero en runtime y profiling legible por agentes para Unity 6 URP+HDRP (FPS meter).**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](./README.md) | [Français](../fr/README.md) | [Italiano](../it/README.md) | [日本語](../ja/README.md) | [한국어](../ko/README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](../zh-cn/README.md)

[Instalación](./installation.md) | [Inicio rápido](./quick-start.md) | [Flujos de trabajo](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [Comparación](./comparison.md) | [Limitaciones](./limitations.md) | [Solución de problemas](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter detecta cuellos de botella de frames, compara cambios de rendimiento, graba sesiones reproducibles y ofrece datos de profiling estructurados para herramientas y AI agents.

## Por Qué Ayuda

- Ver contexto de cuellos de botella directamente durante el juego.
- Cambiar entre presets, gráficos, barras de métricas, layouts compactos y filas de métricas personalizadas.
- Grabar sesiones de profiling reproducibles con warm-up, alcance por escena, resumen de peores frames y exportación JSON/CSV.
- Coordinar una solicitud explícita y acotada de RenderDoc/PIX con estados deterministas de pre-roll, captura y post-roll cuando ya hay un profiler GPU externo conectado; la coordinación está limitada a Editor/Development Build y no afirma un path de artefacto autoritativo.
- Usar alertas, logs estructurados, callbacks y cooldowns de advertencias del Editor sin vigilar el overlay de forma constante.
- Entregar datos estructurados a herramientas y agentes para comparaciones, pruebas A/B y búsqueda de hotspots.

## Qué Se Mide

- Estado en runtime de Unity `6000.4+` / URP `17.4+` Render Graph y HDRP `17.4+` Custom Pass.
- Timing CPU/GPU de FrameTimingManager: CPU frame, main thread, render thread, present wait y GPU frame time cuando está disponible.
- Contadores de render de ProfilerRecorder: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory y GPU memory cuando están disponibles.
- Clasificación de cuellos de botella para GPU, CPU main, CPU render, present/VSync, balanced o unknown.
- Opt-in overdraw measurement y overdraw heatmap visual mediante URP Render Graph; en HDRP overdraw/heatmap no tienen soporte, pero los core diagnostics siguen disponibles.
- Snapshots de device, URP/HDRP camera, render integration, status, metrics, alerts, sessions y custom metrics para código y automatización MCP.

## Telemetría de Plataforma Opcional

PerfMeter puede recopilar señales opcionales de thermal y Adaptive Performance mediante un provider. La assembly core no tiene una dependencia obligatoria de `com.unity.adaptiveperformance`; la assembly opcional `SGG.PerfMeter.AdaptivePerformance` se activa para Unity `6000.4+` cuando `com.unity.adaptiveperformance` es `5.1.0+`.

- **API del provider**: `PerformanceMeter.RegisterPlatformTelemetryProvider(...)`, `UnregisterPlatformTelemetryProvider(...)` y `GetPlatformTelemetry()` permiten como máximo un provider activo y devuelven el `PerfMeterPlatformTelemetrySnapshot` inmutable con ID/versión del provider, tiempos de sample/cambio, thermal warning, temperature level/trend, CPU/GPU performance levels, adaptive bottleneck y disponibilidad por campo. Se rechaza un segundo provider diferente.
- **Recopilación y exportación**: el runtime toma una muestra del provider una vez por cada frame recopilado. El JSON/CSV de sesión y los capture samples conservan el snapshot y la procedencia del provider. El comando MCP `perfmeter.platform.telemetry` expone el snapshot estructurado actual.
- **Profiler y alertas**: `SGG.PerfMeter.Thermal.Sample` y `SGG.PerfMeter.Thermal.Available` exponen el marker de recopilación térmica y el counter de disponibilidad. La alerta predeterminada `thermal.throttling` usa la métrica `ThermalWarningLevel` cuando está disponible un nivel de throttling inminente o activo.
- **La falta de disponibilidad es explícita**: los providers y campos no compatibles permanecen unavailable; JSON serializa los valores numéricos no disponibles como `null` y CSV usa campos vacíos, con flags de disponibilidad en lugar de ceros ficticios. La validación del paquete Adaptive Performance real y de dispositivos objetivo sigue siendo un gate de la release matrix/release candidate; este README no afirma ningún resultado de dispositivo.

## Inicio Rápido

1. Instala el paquete Unity desde npm registry o Git UPM.
2. Abre `SGG/Perfmeter/Setup` en Unity.
3. Ejecuta la configuración recomendada, entra en Play Mode y comprueba que aparece el overlay.

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
    "com.sungeargames.perfmeter": "2026.8.6-2"
  }
}
```

## Documentación

- [Instalación](./installation.md)
- [Inicio rápido](./quick-start.md)
- [Flujos de trabajo](./workflows.md)
- [API](./api.md)
- [MCP y automatización con agentes](./mcp.md)
- [Visual Presets](./presets.md)
- [Widgets implementados](./widgets.md)
- [Screenshots](./screenshots.md)
- [Screenshots de la ventana de setup](./setup-window-screenshots.md)
- [Limitaciones](./limitations.md)
- [Solución de problemas](./troubleshooting.md)
- [Comparación](./comparison.md)
- [Brand Usage Policy](./brand.md)

## Licencia

El paquete está licenciado bajo **Stinger Royalty-Free EULA 1.0**.

- Texto ruso autoritativo de la licencia: [LICENSE.ru.md](../../LICENSE.ru.md)
- Traducción auxiliar al inglés: [LICENSE.md](../../LICENSE.md)
- Avisos: [NOTICE.md](../../NOTICE.md) y [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy: [brand.md](./brand.md)
