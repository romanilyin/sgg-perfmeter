# Сравнение С Advanced FPS Counter И Graphy

Это product/architecture comparison, а не измеренный runtime benchmark.

## Коротко

Advanced FPS Counter и Graphy - сильные general-purpose visual overlays. Они хороши, когда нужен быстрый drop-in FPS/memory/device HUD с широкой поддержкой старых Unity и visual customization.

SGG PerfMeter намеренно уже и диагностичнее: Unity `6000.4+`, URP `17.4+`, Render Graph, structured snapshots, session export, overdraw diagnostics, reproducible camera/device metadata и MCP/API automation.

## Таблица Сравнения

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary positioning | URP Render Graph diagnostics + agent-readable profiling API | Flexible in-game FPS/memory/device counter | Visual FPS/memory/audio stats monitor + debugger |
| Unity target | Unity `6000.4+`, URP `17.4+` | Broad older Unity support | Broad older Unity support |
| UI backend | UI Toolkit overlay | uGUI Canvas/Text labels | uGUI Text/Image modules |
| Timing source | `FrameTimingManager` + rolling stats | Runtime frame/update sampling | `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | CPU frame, main thread, render thread, present wait, GPU when available | No equivalent split | No equivalent split |
| Bottleneck classification | GPU, CPU main, CPU render, present-limited, balanced, unknown | No equivalent | No equivalent |
| Render counters | Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | No URP/SRP counter set | No URP/SRP counter set |
| Device/camera reproducibility | Structured device and camera snapshots | Device panel only | Device panel only |
| Sessions | Bounded recorder, warm-up, scene scope, worst frames, JSON/CSV export | Not a primary feature | Roadmap-like idea |
| Overdraw | Numerical measurement + visual heatmap through URP Render Graph | No | No |
| Automation | MCP command surface and public snapshots | No | No |

## Что SGG PerfMeter Делает Лучше

- Объясняет вероятные bottlenecks через CPU frame, main thread, render thread, present wait, GPU timing и frame budget data.
- Показывает URP-oriented render counters и Render Graph diagnostics.
- Создает reproducible performance reports со scene, device, camera, settings, session samples, summaries и worst-frame metadata.
- Дает tools и agents structured data через public API и MCP commands.
- Включает bounded overdraw measurement и visual heatmap как явные diagnostics.

## Что Конкуренты Все Еще Делают Лучше

- Advanced FPS Counter имеет очень прямой drop-in visual counter UX, зрелую inspector customization, hotkeys/circle gesture toggles, min/max/average UI patterns и VR/world-space examples.
- Graphy имеет сильные public marketing materials, понятные module states, visual customization, hotkeys/background mode, зрелый debugger packet UX и широкую public awareness.

## Что Не Утверждать

- SGG PerfMeter не заменяет Unity Profiler, RenderDoc, Profile Analyzer или Frame Debugger.
- SGG PerfMeter не zero-overhead; используйте low-overhead и явно документируйте diagnostic costs.
- SGG PerfMeter не является all-platform/all-pipeline legacy compatibility package.
