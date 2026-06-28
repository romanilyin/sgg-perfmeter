# 与 Advanced FPS Counter 和 Graphy 的对比

这是产品和架构对比，不是实测 runtime benchmark。

## 简短结论

Advanced FPS Counter 和 Graphy 是成熟的通用 visual overlays。如果主要需求是快速接入 FPS/memory/device HUD、支持较旧 Unity 版本并进行 visual customization，它们很有用。

SGG PerfMeter 的范围更窄且更偏诊断：Unity `6000.4+`、URP `17.4+` Render Graph、HDRP `17.4+` Custom Pass diagnostics、structured snapshots、session export、URP overdraw diagnostics、可复现的 camera/device metadata，以及 MCP/API automation。

## 对比表

| 领域 | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| 主要定位 | 🔵 URP Render Graph / HDRP Custom Pass 诊断 + automation-ready profiling API | ⚠️ 灵活的 in-game FPS/memory/device counter | ⚠️ Visual FPS/memory/audio stats monitor + debugger |
| Unity target | ⚠️ Unity `6000.4+`、URP `17.4+` / HDRP `17.4+` | 🔵 广泛支持较旧 Unity | 🔵 广泛支持较旧 Unity |
| UI backend | 🔵 UI Toolkit overlay | ⚠️ uGUI Canvas/Text labels | ⚠️ uGUI Text/Image modules |
| Timing source | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Runtime frame/update sampling | ⚠️ `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | 🔵 CPU frame、main thread、render thread、present wait、可用时 GPU | 🛑 无等效拆分 | 🛑 无等效拆分 |
| Bottleneck classification | 🔵 GPU、CPU main、CPU render、present-limited、balanced、unknown | 🛑 无等效功能 | 🛑 无等效功能 |
| Render counters | 🔵 Draw calls、SetPass、batches、vertices、SRP Batcher、BRG/GRD、uploads、memory | 🛑 无 URP/SRP counter set | 🛑 无 URP/SRP counter set |
| Device/camera reproducibility | 🔵 Structured device 和 camera snapshots | ⚠️ 仅 device panel | ⚠️ 仅 device panel |
| Sessions | 🔵 有边界 recorder、warm-up、scene scope、worst frames、JSON/CSV export | 🛑 非主要功能 | ⚠️ 类似 roadmap 的想法 |
| Overdraw | 🔵 通过 URP Render Graph 进行 numerical measurement + visual heatmap；HDRP 中为 explicit unsupported state | 🛑 无 | 🛑 无 |
| Automation | 🔵 MCP command surface 和 public snapshots | 🛑 无 | 🛑 无 |

## SGG PerfMeter 的优势

- 通过 CPU frame、main thread、render thread、present wait、GPU timing 和 frame budget data 解释可能的 frame bottlenecks。
- 暴露面向 URP 的 render counters、URP Render Graph diagnostics 和 HDRP Custom Pass observation。
- 生成可复现的 performance reports，包含 scene、device、camera、settings、session samples、summaries 和 worst-frame metadata。
- 通过 public API 和 MCP commands 为工具和自动化提供结构化数据。
- 将 bounded overdraw measurement 和 visual heatmap 作为显式 diagnostics 集成。

## 竞品仍有优势的方面

- 两个竞品都支持更广泛的旧 Unity 版本，这对 legacy projects 是优势。
- Advanced FPS Counter 拥有非常直接的 drop-in visual counter UX、成熟的 inspector customization、hotkeys/circle gesture toggles、min/max/average UI patterns，以及 VR/world-space examples。
- Graphy 有较强的 public marketing material、清晰的 module states、visual customization、hotkeys/background mode、成熟的 debugger packet UX，以及广泛的 public awareness。

## 不应宣称的内容

- 不要宣称 SGG PerfMeter 可以替代 Unity Profiler、RenderDoc、Profile Analyzer 或 Frame Debugger。
- 不要宣称 SGG PerfMeter 是 zero-overhead；应使用 low-overhead，并记录显式 diagnostic costs。
- 不要宣称 SGG PerfMeter 是 all-platform/all-pipeline legacy compatibility package。
