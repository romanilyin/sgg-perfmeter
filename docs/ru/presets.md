# Визуальные пресеты

Визуальные пресеты - это проектные JSON-файлы, которые задают компоновку оверлея, стиль, включенные виджеты и их порядок. Они редактируются во вкладке `Presets` окна `SGG/Perfmeter/Setup` и запекаются в Resources JSON для билдов, поэтому во время выполнения не зависят от `AssetDatabase`.

Скриншоты ниже - полноэкранные захваты из сцены capture-lab после 1000 кадров прогрева. Оверлей во время выполнения не локализуется, поэтому русская и английская документация используют одни и те же изображения пресетов.

## Default

Пресет диагностики по умолчанию без кода. Он использует компоновку `MetricBars`, поэтому нижний текстовый блок рендерится как компактные полосы метрик, а не как обычный текст.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Однострочный FPS-пресет с текущим FPS, средним FPS, 1% low, 0.1% low и временем render thread. Значения семейства FPS окрашиваются относительно выбранного целевого FPS.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Компактный timing-пресет с FPS, карточками CPU/GPU timing и полосами бюджета CPU/GPU.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Пресет с фокусом на карточках для FPS, CPU, GPU, frame spikes, rendering и memory без графиков.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Пресет с фокусом на таймингах: графики истории CPU/GPU и базовые карточки FPS/timing.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Широкий диагностический пресет со всеми основными высокоуровневыми виджетами PerfMeter.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Цветовая шкала FPS

Пресет `FPS Only` окрашивает значения семейства FPS относительно выбранного целевого FPS:

| Соотношение с target | Цвет |
| --- | --- |
| `> 2.0x` | Синий |
| `>= 1.0x` | Зеленый |
| `0.75x` до `< 1.0x` | Желтый |
| `0.25x` до `< 0.75x` | Оранжевый |
| `< 0.25x` | Красный |
