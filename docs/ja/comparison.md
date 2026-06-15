# Advanced FPS Counter および Graphy との比較

これは product と architecture の比較であり、測定済み runtime benchmark ではありません。

## 概要

Advanced FPS Counter と Graphy は、汎用の visual overlays として優れています。幅広い古い Unity support と visual customization を持つ、すぐに導入できる FPS/memory/device HUD が主な要件である場合に有用です。

SGG PerfMeter は、より診断向けに範囲を絞っています。Unity `6000.4+`、URP `17.4+`、Render Graph、structured snapshots、session export、overdraw diagnostics、reproducible camera/device metadata、MCP/API automation を対象にしています。

## 比較表

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Primary positioning | 🔵 URP Render Graph / HDRP Custom Pass diagnostics + automation-ready profiling API | ⚠️ 柔軟な in-game FPS/memory/device counter | ⚠️ visual FPS/memory/audio stats monitor + debugger |
| Unity target | ⚠️ Unity `6000.4+`, URP `17.4+` | 🔵 幅広い古い Unity support | 🔵 幅広い古い Unity support |
| UI backend | 🔵 UI Toolkit overlay | ⚠️ uGUI Canvas/Text labels | ⚠️ uGUI Text/Image modules |
| Timing source | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Runtime frame/update sampling | ⚠️ `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | 🔵 CPU frame、main thread、render thread、present wait、利用可能な場合の GPU | 🛑 同等機能なし | 🛑 同等機能なし |
| Bottleneck classification | 🔵 GPU、CPU main、CPU render、present-limited、balanced、unknown | 🛑 同等機能なし | 🛑 同等機能なし |
| Render counters | 🔵 Draw calls、SetPass、batches、vertices、SRP Batcher、BRG/GRD、uploads、memory | 🛑 URP/SRP counter set なし | 🛑 URP/SRP counter set なし |
| Device/camera reproducibility | 🔵 structured device and camera snapshots | ⚠️ device panel のみ | ⚠️ device panel のみ |
| Sessions | 🔵 bounded recorder、warm-up、scene scope、worst frames、JSON/CSV export | 🛑 primary feature ではない | ⚠️ roadmap-like idea |
| Overdraw | 🔵 URP Render Graph 経由の numerical measurement + visual heatmap | 🛑 なし | 🛑 なし |
| Automation | 🔵 MCP command surface と public snapshots | 🛑 なし | 🛑 なし |

## SGG PerfMeter が得意なこと

- CPU frame、main thread、render thread、present wait、GPU timing、frame budget data で、想定される frame bottleneck を説明します。
- URP-oriented render counters と Render Graph diagnostics を公開します。
- scene、device、camera、settings、session samples、summaries、worst-frame metadata を含む再現可能な performance reports を生成します。
- public API と MCP commands を通じて、tools と automation に structured data を提供します。
- bounded overdraw measurement と visual heatmap を明示的な diagnostics として統合します。

## 競合製品が今も得意なこと

- 両方の競合製品は、より広い範囲の古い Unity versions をサポートしており、legacy projects では利点になります。
- Advanced FPS Counter には、非常に直接的な drop-in visual counter UX、成熟した inspector customization、hotkeys/circle gesture toggles、min/max/average UI patterns、VR/world-space examples があります。
- Graphy には、強い public marketing material、明確な module states、visual customization、hotkeys/background mode、成熟した debugger packet UX、広い public awareness があります。

## 主張しないこと

- SGG PerfMeter は Unity Profiler、RenderDoc、Profile Analyzer、Frame Debugger の replacement ではありません。
- SGG PerfMeter は zero-overhead ではありません。low-overhead と表現し、明示的な diagnostic costs を文書化してください。
- SGG PerfMeter は all-platform/all-pipeline legacy compatibility package ではありません。
