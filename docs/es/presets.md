# Visual Presets

Los visual presets son archivos JSON del proyecto que definen layout del overlay, estilo, widgets activados y orden de widgets. Se editan en la pestaña `Presets` de `SGG/Perfmeter/Setup` y se hornean en JSON de Resources para builds, de modo que runtime no depende de `AssetDatabase`.

Los screenshots siguientes son capturas fullscreen de la escena capture-lab después de 1000 warmup frames. El texto del overlay runtime no está localizado, por lo que la documentación en español usa las mismas imágenes de presets compartidas.

## Default

Preset de diagnóstico predeterminado sin código. Usa el layout `MetricBars`, de modo que el bloque de texto inferior se renderiza como barras de métricas compactas en vez de como un bloque de texto plano.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Preset FPS de una sola línea con FPS actual, FPS promedio, 1% low, 0.1% low y render-thread time. Los valores de la familia FPS se colorean respecto al target FPS seleccionado.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Preset compacto de timing con FPS, tarjetas de CPU/GPU timing y barras de budget CPU/GPU.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Preset centrado en tarjetas para FPS, CPU, GPU, frame spikes, rendering y memory sin gráficos.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Preset centrado en timing con gráficos de historial CPU y GPU más tarjetas principales de FPS/timing.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Preset diagnóstico ancho con todos los widgets principales de alto nivel de PerfMeter activados.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Escala De Color FPS

El preset `FPS Only` colorea los valores de la familia FPS respecto al target FPS seleccionado:

| Ratio Respecto Al Target | Color |
| --- | --- |
| `> 2.0x` | Azul |
| `>= 1.0x` | Verde |
| `0.75x` to `< 1.0x` | Amarillo |
| `0.25x` to `< 0.75x` | Naranja |
| `< 0.25x` | Rojo |
