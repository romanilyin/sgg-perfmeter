# Visual Presets

Visual presets 是项目 JSON 文件，用于定义 overlay layout、style、enabled widgets 和 widget order。它们在 `SGG/Perfmeter/Setup` 的 `Presets` tab 中编辑，并 baked into Resources JSON 以用于 builds，因此 runtime 不依赖 `AssetDatabase`。

以下截图来自 capture-lab scene，在 1000 warmup frames 后进行 fullscreen capture。Runtime overlay text 未本地化，因此中文文档使用相同的 preset images。

## Default

默认 zero-code diagnostics preset。它使用 `MetricBars` layout，因此下方 text block 会以 compact metric bars 渲染，而不是 plain text block。

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

单行 FPS preset，包含 current FPS、average FPS、1% low、0.1% low 和 render-thread time。FPS-family values 会根据选定 target FPS 进行颜色编码。

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Compact timing preset，包含 FPS、CPU/GPU timing cards 和 CPU/GPU budget bars。

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

面向 cards 的 preset，显示 FPS、CPU、GPU、frame spikes、rendering 和 memory，不显示 graphs。

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

面向 timing 的 preset，包含 CPU 和 GPU history graphs，以及核心 FPS/timing cards。

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

宽幅 diagnostics preset，启用所有主要 high-level PerfMeter widgets。

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## FPS Color Scale

`FPS Only` preset 会根据选定 target FPS 为 FPS-family values 着色：

| Ratio To Target | Color |
| --- | --- |
| `> 2.0x` | Blue |
| `>= 1.0x` | Green |
| `0.75x` to `< 1.0x` | Yellow |
| `0.25x` to `< 0.75x` | Orange |
| `< 0.25x` | Red |
