# Advanced FPS Counter 및 Graphy와의 비교

이는 product 및 architecture 비교이며, 측정된 runtime benchmark가 아닙니다.

## 짧은 요약

Advanced FPS Counter와 Graphy는 범용 visual overlay로 강점이 있습니다. 넓은 older-Unity support와 visual customization을 갖춘 빠른 drop-in FPS/memory/device HUD가 필요할 때 유용합니다.

SGG PerfMeter는 의도적으로 더 좁고 진단 중심입니다. Unity `6000.4+`, URP `17.4+` Render Graph, HDRP `17.4+` Custom Pass diagnostics, structured snapshots, session export, URP overdraw diagnostics, 재현 가능한 camera/device metadata, MCP/API automation에 초점을 둡니다.

## 비교표

| 영역 | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| 주요 포지셔닝 | 🔵 URP Render Graph / HDRP Custom Pass diagnostics + automation-ready profiling API | ⚠️ flexible in-game FPS/memory/device counter | ⚠️ visual FPS/memory/audio stats monitor + debugger |
| Unity target | ⚠️ Unity `6000.4+`, URP `17.4+` / HDRP `17.4+` | 🔵 넓은 older Unity support | 🔵 넓은 older Unity support |
| UI backend | 🔵 UI Toolkit overlay | ⚠️ uGUI Canvas/Text labels | ⚠️ uGUI Text/Image modules |
| Timing source | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Runtime frame/update sampling | ⚠️ `Time.unscaledDeltaTime` history sampling |
| CPU/GPU split | 🔵 사용 가능한 경우 CPU frame, main thread, render thread, present wait, GPU | 🛑 동등한 split 없음 | 🛑 동등한 split 없음 |
| Bottleneck classification | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 동등한 기능 없음 | 🛑 동등한 기능 없음 |
| Render counters | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 URP/SRP counter set 없음 | 🛑 URP/SRP counter set 없음 |
| Device/camera reproducibility | 🔵 structured device 및 camera snapshots | ⚠️ device panel 중심 | ⚠️ device panel 중심 |
| Sessions | 🔵 bounded recorder, warm-up, scene scope, worst frames, JSON/CSV export | 🛑 주요 기능 아님 | ⚠️ roadmap-like idea |
| Overdraw | 🔵 URP Render Graph를 통한 numerical measurement + visual heatmap. HDRP에서는 explicit unsupported state | 🛑 없음 | 🛑 없음 |
| Automation | 🔵 MCP command surface 및 public snapshots | 🛑 없음 | 🛑 없음 |

## SGG PerfMeter의 강점

- CPU frame, main thread, render thread, present wait, GPU timing, frame budget data로 가능한 frame bottleneck을 설명합니다.
- URP-oriented render counters, URP Render Graph diagnostics, HDRP Custom Pass observation을 노출합니다.
- scene, device, camera, settings, session samples, summaries, worst-frame metadata가 포함된 재현 가능한 performance report를 생성합니다.
- public API 및 MCP commands를 통해 도구와 automation에 structured data를 제공합니다.
- bounded overdraw measurement와 visual heatmap을 명시적인 diagnostics로 통합합니다.

## 경쟁 도구의 강점

- 두 경쟁 도구는 더 넓은 older Unity version을 지원하므로 legacy project에 유리합니다.
- Advanced FPS Counter는 매우 직접적인 drop-in visual counter UX, 성숙한 inspector customization, hotkeys/circle gesture toggles, min/max/average UI patterns, VR/world-space examples를 갖습니다.
- Graphy는 강한 public marketing material, 명확한 module states, visual customization, hotkeys/background mode, 성숙한 debugger packet UX, 넓은 public awareness를 갖습니다.

## 주장하지 말아야 할 내용

- SGG PerfMeter는 Unity Profiler, RenderDoc, Profile Analyzer, Frame Debugger를 대체하지 않습니다.
- SGG PerfMeter는 zero-overhead가 아닙니다. low-overhead라고 표현하고 명시적인 diagnostic cost를 문서화합니다.
- SGG PerfMeter는 all-platform/all-pipeline legacy compatibility package가 아닙니다.
