# Visual Presets

Visual presets are project JSON files that define overlay layout, style, enabled widgets, and widget order. They are authored in the `Presets` tab of `SGG/Perfmeter/Setup` and baked into Resources JSON for builds, so runtime does not depend on `AssetDatabase`.

The screenshots below are fullscreen captures from the capture-lab scene after 1000 warmup frames. Runtime overlay text is not localized, so localized docs use the same preset images.

## Default

Default zero-code diagnostics preset. It uses the `MetricBars` layout so the lower text block is rendered as compact metric bars instead of a plain text block.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Single-line FPS preset with current FPS, average FPS, 1% low, 0.1% low, and render-thread time. FPS-family values are color-coded against the selected target FPS.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Compact timing preset with FPS, CPU/GPU timing cards, and CPU/GPU budget bars.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Card-focused preset for FPS, CPU, GPU, frame spikes, rendering, and memory without graphs.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Timing-focused preset with CPU and GPU history graphs, a full-width raw per-frame hitch strip, and core FPS/timing cards. The hitch strip is independent of the throttled text refresh and does not average one-frame spikes away.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Wide diagnostic preset with all major high-level PerfMeter widgets enabled.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Custom Metric Graph Channels

Visual preset JSON can add up to four channels to the raw frame-time strip. Matching is case-sensitive against `PerfMeterCustomMetricSnapshot.Id`. `displayScale` is applied to the provider value first; `min` and `max` are the explicit display-space range for that channel. Rendering clamps only the plotted position and never changes the raw metric. Missing, unavailable, or non-finite values create a gap rather than a zero.

```json
"customMetricGraphs": [
  {
    "metricId": "movement.horizontal-speed",
    "enabled": true,
    "min": -12.0,
    "max": 12.0,
    "displayScale": 1.0,
    "color": "#FF5B78",
    "unit": "m/s"
  },
  {
    "metricId": "movement.vertical-speed",
    "enabled": true,
    "min": -4.0,
    "max": 8.0,
    "displayScale": 1.0,
    "color": "#56C8FF",
    "unit": "m/s"
  }
]
```

Channels are normalized only against their own configured range. PerfMeter does not infer units, normalize one channel against another, or substitute provider values for a missing stable ID. Invalid, duplicate, and excess configurations are reported by preset validation and ignored at runtime.

## FPS Color Scale

The `FPS Only` preset colors FPS-family values against the selected target FPS:

| Ratio To Target | Color |
| --- | --- |
| `> 2.0x` | Blue |
| `>= 1.0x` | Green |
| `0.75x` to `< 1.0x` | Yellow |
| `0.25x` to `< 0.75x` | Orange |
| `< 0.25x` | Red |
