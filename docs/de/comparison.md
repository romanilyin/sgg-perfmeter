# Vergleich Mit Advanced FPS Counter Und Graphy

Dies ist ein Produkt- und Architekturvergleich, kein gemessener Runtime-Benchmark.

## Kurzfassung

Advanced FPS Counter und Graphy sind starke allgemeine visuelle Overlays. Sie sind nuetzlich, wenn ein schneller FPS/memory/device HUD mit breiter Unterstuetzung alter Unity-Versionen und visueller Anpassung gebraucht wird.

SGG PerfMeter ist enger und diagnostischer fokussiert: Unity `6000.4+`, URP `17.4+`, Render Graph, strukturierte Snapshots, Session-Export, overdraw diagnostics, reproduzierbare camera/device metadata und MCP/API automation.

## Vergleichstabelle

| Bereich | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Positionierung | 🔵 URP Render Graph diagnostics + automation-ready profiling API | ⚠️ Flexibler FPS/memory/device counter | ⚠️ Visueller FPS/memory/audio monitor + debugger |
| Unity-Ziel | ⚠️ Unity `6000.4+`, URP `17.4+` | 🔵 Breite Unterstuetzung alter Unity-Versionen | 🔵 Breite Unterstuetzung alter Unity-Versionen |
| UI backend | 🔵 UI Toolkit overlay | ⚠️ uGUI Canvas/Text labels | ⚠️ uGUI Text/Image modules |
| Timing-Quelle | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Runtime frame/update sampling | ⚠️ `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | 🔵 CPU frame, main thread, render thread, present wait, GPU wenn verfuegbar | 🛑 Kein vergleichbarer split | 🛑 Kein vergleichbarer split |
| Bottleneck classification | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 Kein Aequivalent | 🛑 Kein Aequivalent |
| Render counters | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 Kein URP/SRP counter set | 🛑 Kein URP/SRP counter set |
| Sessions | 🔵 Bounded recorder, warm-up, scene scope, worst frames, JSON/CSV export | 🛑 Nicht primaer | ⚠️ Roadmap-aehnliche Idee |
| Automation | 🔵 MCP command surface und public snapshots | 🛑 Nein | 🛑 Nein |

## Was SGG PerfMeter Besser Macht

- Erklaert wahrscheinliche Frame-Bottlenecks mit CPU frame, main thread, render thread, present wait, GPU timing und Frame-Budget-Daten.
- Stellt URP-orientierte Render-Counter und Render-Graph-Diagnostik bereit.
- Erzeugt reproduzierbare Performance-Reports mit scene, device, camera, settings, samples, summaries und worst-frame metadata.
- Gibt Tools und Automation strukturierte Daten ueber public API und MCP commands.

## Was Andere Besser Machen

- Beide Konkurrenten unterstuetzen mehr alte Unity-Versionen.
- Advanced FPS Counter hat sehr direkte Drop-in-Counter-UX und reife Inspector-Anpassung.
- Graphy hat starke oeffentliche Materialien, klare Modulzustaende, visuelle Anpassung und breite Bekanntheit.
