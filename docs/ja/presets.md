# Visual Presets

Visual presets は、overlay layout、style、enabled widgets、widget order を定義する project JSON files です。`SGG/Perfmeter/Setup` の `Presets` tab で作成し、build 用に Resources JSON へ bake されるため、runtime は `AssetDatabase` に依存しません。

以下の screenshots は、1000 warmup frames 後の capture-lab scene からの fullscreen captures です。runtime overlay text は localized されないため、各言語の docs は同じ preset images を使用します。

## Default

default zero-code diagnostics preset です。`MetricBars` layout を使用するため、下部の text block は plain text block ではなく compact metric bars として表示されます。

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

current FPS、average FPS、1% low、0.1% low、render-thread time を表示する single-line FPS preset です。FPS-family values は selected target FPS に対して color-coded されます。

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

FPS、CPU/GPU timing cards、CPU/GPU budget bars を持つ compact timing preset です。

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

graphs を使わず、FPS、CPU、GPU、frame spikes、rendering、memory に焦点を当てた card-focused preset です。

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

CPU と GPU history graphs に加え、core FPS/timing cards を持つ timing-focused preset です。

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

主要な high-level PerfMeter widgets をすべて有効化した wide diagnostic preset です。

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## FPS Color Scale

`FPS Only` preset は、selected target FPS に対して FPS-family values を色分けします。

| Ratio To Target | Color |
| --- | --- |
| `> 2.0x` | Blue |
| `>= 1.0x` | Green |
| `0.75x` to `< 1.0x` | Yellow |
| `0.25x` to `< 0.75x` | Orange |
| `< 0.25x` | Red |
