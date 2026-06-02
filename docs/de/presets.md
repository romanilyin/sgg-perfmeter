# Visual Presets

Visual Presets sind projektbezogene JSON-Dateien, die Overlay-Layout, Stil, aktivierte Widgets und Widget-Reihenfolge definieren. Sie werden im Tab `Presets` von `SGG/Perfmeter/Setup` bearbeitet und fuer Builds in Resources JSON geschrieben, sodass die Runtime nicht von `AssetDatabase` abhaengt.

Die Screenshots unten sind Vollbild-Captures aus der capture-lab-Szene nach 1000 Warm-up-Frames. Der Runtime-Overlay-Text ist nicht lokalisiert, deshalb nutzen alle Sprachen dieselben Preset-Bilder.

## Default

Standarddiagnose ohne Code. Nutzt das Layout `MetricBars`, sodass der untere Textblock als kompakte Metrikleisten gerendert wird.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Einzeiliger FPS-Preset mit aktuellem FPS, average FPS, 1% low, 0.1% low und render-thread time. FPS-Werte werden gegen den Ziel-FPS eingefarbt.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Kompakter Timing-Preset mit FPS, CPU/GPU timing cards und CPU/GPU budget bars.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Kartenfokussierter Preset fuer FPS, CPU, GPU, frame spikes, rendering und memory ohne Graphen.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Timing-fokussierter Preset mit CPU/GPU-History-Graphen plus Kern-FPS/timing cards.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Breiter Diagnose-Preset mit allen wichtigen High-Level-Widgets von PerfMeter.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## FPS-Farbskala

Der Preset `FPS Only` faerbt FPS-Werte relativ zum Ziel-FPS:

| Verhaeltnis Zum Ziel | Farbe |
| --- | --- |
| `> 2.0x` | Blau |
| `>= 1.0x` | Gruen |
| `0.75x` bis `< 1.0x` | Gelb |
| `0.25x` bis `< 0.75x` | Orange |
| `< 0.25x` | Rot |
