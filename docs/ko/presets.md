# Visual Presets

Visual preset은 overlay layout, style, enabled widgets, widget order를 정의하는 project JSON file입니다. `SGG/Perfmeter/Setup`의 `Presets` tab에서 작성되며 build를 위해 Resources JSON으로 baked되므로 runtime은 `AssetDatabase`에 의존하지 않습니다.

아래 스크린샷은 1000 warmup frames 이후 capture-lab scene에서 찍은 fullscreen capture입니다. Runtime overlay text는 localization되지 않으므로 preset 이미지는 여러 언어 문서에서 공유됩니다.

## Default

기본 zero-code diagnostics preset입니다. `MetricBars` layout을 사용하므로 아래쪽 text block이 plain text block 대신 compact metric bars로 렌더링됩니다.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

current FPS, average FPS, 1% low, 0.1% low, render-thread time을 표시하는 single-line FPS preset입니다. FPS-family value는 선택한 target FPS에 따라 color-coded됩니다.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

FPS, CPU/GPU timing cards, CPU/GPU budget bars를 포함하는 compact timing preset입니다.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

그래프 없이 FPS, CPU, GPU, frame spikes, rendering, memory에 초점을 둔 card-focused preset입니다.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

CPU 및 GPU history graph와 핵심 FPS/timing card를 포함하는 timing-focused preset입니다.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

주요 high-level PerfMeter widget이 모두 활성화된 wide diagnostic preset입니다.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## FPS Color Scale

`FPS Only` preset은 선택한 target FPS 대비 FPS-family value에 색을 적용합니다.

| Ratio To Target | Color |
| --- | --- |
| `> 2.0x` | Blue |
| `>= 1.0x` | Green |
| `0.75x` to `< 1.0x` | Yellow |
| `0.25x` to `< 0.75x` | Orange |
| `< 0.25x` | Red |
