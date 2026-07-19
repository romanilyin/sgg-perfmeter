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
- Usar alertas, logs estructurados, callbacks y cooldowns de advertencias del Editor sin vigilar el overlay de forma constante.
- Entregar datos estructurados a herramientas y agentes para comparaciones, pruebas A/B y búsqueda de hotspots.

## Qué Se Mide

- Estado en runtime de Unity `6000.4+` / URP `17.4+` Render Graph y HDRP `17.4+` Custom Pass.
- Timing CPU/GPU de FrameTimingManager: CPU frame, main thread, render thread, present wait y GPU frame time cuando está disponible.
- Contadores de render de ProfilerRecorder: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory y GPU memory cuando están disponibles.
- Clasificación de cuellos de botella para GPU, CPU main, CPU render, present/VSync, balanced o unknown.
- Opt-in overdraw measurement y overdraw heatmap visual mediante URP Render Graph; en HDRP overdraw/heatmap no tienen soporte, pero los core diagnostics siguen disponibles.
- Snapshots de device, URP/HDRP camera, render integration, status, metrics, alerts, sessions y custom metrics para código y automatización MCP.

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
    "com.sungeargames.perfmeter": "2026.7.19-1"
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
