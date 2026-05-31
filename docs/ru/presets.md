# Визуальные пресеты

Визуальные пресеты - это проектные JSON-файлы, которые задают layout overlay, стиль, включенные виджеты и их порядок. Они редактируются во вкладке `Presets` окна `SGG/Perfmeter/Setup` и запекаются в Resources JSON для билдов, поэтому runtime не зависит от `AssetDatabase`.

Скриншоты ниже - fullscreen captures из capture-lab scene после 1000 warmup frames. Runtime overlay не локализуется, поэтому русская и английская документация используют одни и те же изображения пресетов.

## Default

Default zero-code diagnostics preset. Он использует layout `MetricBars`, поэтому нижний текстовый блок рендерится как компактные metric bars, а не как обычный текст.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Однострочный FPS preset с текущим FPS, средним FPS, 1% low, 0.1% low и render-thread time. Значения FPS-family окрашиваются относительно выбранного target FPS.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Компактный timing preset с FPS, CPU/GPU timing cards и CPU/GPU budget bars.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Card-focused preset для FPS, CPU, GPU, frame spikes, rendering и memory без graphs.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Timing-focused preset с CPU/GPU history graphs и базовыми FPS/timing cards.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Wide diagnostic preset со всеми основными high-level виджетами PerfMeter.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Цветовая шкала FPS

Preset `FPS Only` окрашивает значения FPS-family относительно выбранного target FPS:

| Ratio к target | Цвет |
| --- | --- |
| `> 2.0x` | Синий |
| `>= 1.0x` | Зеленый |
| `0.75x` до `< 1.0x` | Желтый |
| `0.25x` до `< 0.75x` | Оранжевый |
| `< 0.25x` | Красный |
