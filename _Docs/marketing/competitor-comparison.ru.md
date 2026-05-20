# SGG PerfMeter vs Advanced FPS Counter vs Graphy

Scope: product/architecture comparison с Advanced FPS Counter и Graphy, не измеренный runtime benchmark. Использовать поведение конкурентов только как UX reference; не копировать proprietary assets/code и не импортировать Graphy как dependency.

## Короткий вывод

**SGG PerfMeter уже вышел за рамки классического FPS-counter.** В нем есть базовые поверхности, ожидаемые от visual overlays - FPS, graphs, memory, device info, presets, settings и samples - но главное отличие в structured URP diagnostics: CPU/GPU timing, render-thread и present-wait visibility, ProfilerRecorder counters, bottleneck classification, overdraw measurement, session export, rule alerts, camera/device snapshots, custom metrics, Render Graph diagnostics и MCP command metadata.

Advanced FPS Counter остается хорошим reference по drop-in usability: uGUI labels, hotkey/circle gesture toggles, FPS/memory/device counters, min/max/average controls, scene reset patterns, cached number formatting и VR/world-space examples.

Graphy остается сильным reference по visual product UX: module states, presets, graphs, configurable colors/layouts, hotkeys, background mode, advanced device info, debugger packets и зрелая структура публичного README.

SGG PerfMeter не должен копировать их uGUI-архитектуры. Фокус продукта должен оставаться на **Unity 6000+ URP Render Graph diagnostics и agent-readable automation**.

## Comparison Table

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary positioning | URP Render Graph diagnostics + agent-readable profiling API | Flexible in-game FPS/memory/device counter | Visual FPS/memory/audio stats monitor + debugger |
| Unity target | Unity `6000.4+`, URP `17.4+` | Broad older Unity support | Broad older Unity support |
| Render pipeline focus | URP-specific, Render Graph renderer feature | General Unity overlay | General Unity uGUI overlay |
| UI backend | UI Toolkit overlay | uGUI Canvas/Text labels | uGUI Text/Image modules |
| FPS source | `FrameTimingManager` timing data + rolling stats | Runtime frame/update sampling | `Time.unscaledDeltaTime` history sampling |
| CPU/GPU timing | CPU frame, main thread, render thread, present wait, GPU frame when available | FPS/ms and approximate render helper | FPS/ms graph history; no CPU main/render/GPU split |
| Bottleneck classification | Yes: GPU, CPU main, CPU render, present/VSync, balanced/unknown | No equivalent | No equivalent |
| Render counters | Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes | No URP/SRP counter set | No URP/SRP counter set |
| Device/environment info | Structured Unity/platform/CPU/GPU/API/display/monitor/window/support snapshot | Visual device counter | Advanced device panel |
| Camera reproducibility | Structured camera snapshot with scene, transform, projection, clipping, target display, URP settings | No equivalent | No equivalent |
| Render Graph diagnostics | Observed feature/pass/resource diagnostics with graceful degraded counts | No | No |
| Overdraw | Numerical measurement + visual heatmap through URP Render Graph | No | No |
| Session recording | Bounded recorder, warm-up, scene scope, worst-frame summaries, JSON/CSV export | Not a primary feature | Roadmap idea |
| Rule alerts | Metrics alerts, structured logs, callback, Editor warning cooldowns, MCP alert commands | FPS level callback | Debug packets with conditions/callbacks |
| Custom metrics | Public provider interface and overlay/session/MCP output | No direct equivalent | Roadmap-like concept |
| Overlay presets/modules | `Minimal`, `Timing`, `Rendering`, `Memory`, `Overdraw`, `FullDiagnostics`, `AgentDebug` + module flags | Per-counter inspector settings | Module presets/states |
| Settings | Project-owned JSON in `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` | Inspector-driven component settings | Serialized prefab/inspector settings |
| Player hotkeys | Вне текущего scope; управление через API/MCP/setup | Yes | Yes |
| Audio monitoring | Намеренно out of scope | No | Yes |
| MCP / automation | First-class differentiator | No | No |

## Что SGG PerfMeter делает лучше

- Объясняет, почему кадр медленный: CPU frame, main thread, render thread, present wait, GPU timing, frame budget и bottleneck classification.
- Видит современные URP signals: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes, GPU memory и Render Graph feature diagnostics.
- Поддерживает reproducible performance reports через device snapshots, camera snapshots, session samples, scene summaries, worst-frame data и JSON/CSV export.
- Нативно работает с automation через MCP commands для setup, runtime control, metrics, device info, camera snapshot, Render Graph snapshot, overlay, alerts, overdraw и sessions.
- Интегрирует explicit overdraw measurement и heatmap diagnostics через URP Render Graph.

## Что конкуренты все еще делают лучше

- Advanced FPS Counter имеет очень прямой drop-in visual counter UX, зрелую inspector customization, hotkey/circle gesture toggles, min/max/average UI patterns, VR/world-space examples и полезные cached-formatting patterns.
- Graphy имеет сильные public marketing materials, понятные module states/presets, visual customization, hotkeys/background mode, зрелый debugger packet UX и широкую public awareness.

## Что не копировать

- Не копировать AFPS source code или assets без явного license permission.
- Не импортировать Graphy как dependency.
- Не заменять UI Toolkit на uGUI только потому, что конкуренты используют uGUI.
- Не добавлять audio/spectrum modules.
- Не расширяться на старые Unity versions или все render pipelines для первого публичного релиза.
- Не добавлять player hotkeys, screenshot actions или debug-break alert actions по умолчанию без явного product approval.
- Не утверждать zero-overhead или profiler replacement.

## Интерпретация для roadmap

Конкуренты полезны как references для usability, visual clarity, cached formatting, examples и README presentation. Текущее направление SGG PerfMeter держит hotkeys и audio вне scope, а приоритет отдает Render Graph diagnostics, structured telemetry, sessions, alerts, custom metrics и automation.

## Bottom Line

SGG PerfMeter стоит продвигать как **modern URP diagnostics and automation package**. AFPS и Graphy - visual overlay competitors, но сильнейшая категория SGG - runtime performance telemetry для humans, tools и agents.
