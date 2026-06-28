# 문제 해결

PerfMeter가 예상한 data를 표시하지 않을 때 이 checklist를 사용합니다.

## Overlay가 표시되지 않음

- `SGG/Perfmeter/Setup`을 열고 overlay visibility가 enabled인지 확인합니다.
- collection mode가 `Background` 또는 `Stopped`가 아니라 `Overlay`인지 확인합니다.
- zero-code setup을 사용한다면 settings file이 `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`에 있는지 확인합니다.
- manual bootstrap을 사용한다면 scene load 이후 `PerformanceMeter.EnsureRunning()`이 호출되는지 확인합니다.
- Play Mode에 진입합니다. Edit Mode API call은 안전하지만 runtime overlay를 만들지 않습니다.

## Frame Timing 또는 GPU Timing이 없음

- Player Settings -> Rendering -> Frame Timing Stats를 활성화합니다.
- GPU frame timing이 중요하다면 Android에서는 Vulkan을 우선 사용합니다.
- OpenGL/OpenGLES는 GPU timing에서 degraded mode로 취급합니다.
- counter가 있다고 가정하기 전에 `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters`, `Warning`을 확인합니다.

## Overdraw Measurement가 진행되지 않음

- URP에서는 active URP renderer에 `PerfMeterRenderGraphFeature`를 설치합니다.
- HDRP에서는 overdraw와 heatmap이 by design unsupported입니다. core diagnostics를 사용하세요.
- active camera가 해당 feature가 포함된 renderer를 사용하는지 확인합니다.
- target backend가 fragment UAV/storage buffers, compute shaders, async GPU readback을 지원하는지 확인합니다.
- bounded measurement window에는 `PerformanceMeter.RequestOverdrawMeasurement(frameCount)`를 사용합니다.
- target이 unsupported이면 PerfMeter는 pass를 schedule하지 않고 `OverdrawState.Unsupported`를 보고합니다.

## Session Export 실패

- project-local path로 export합니다.
- workflow에서 먼저 명시적으로 제거하지 않는 한 existing export를 overwrite하지 않습니다.
- 긴 run에는 `MaxSamples`를 bounded로 유지합니다.
- summary에서 startup spike를 피하려면 warm-up frames/seconds를 사용합니다.

## Alerts가 너무 많음

- JSON settings에서 threshold 및 consecutive-frame window를 조정합니다.
- Editor warning cooldown을 늘립니다.
- callback 또는 structured log만으로 충분하면 Editor warning log를 비활성화합니다.

## Device마다 Data가 다르게 보임

이는 예상되는 동작입니다. GPU timings, profiler counters, display information, async readback, overdraw support는 graphics API, platform, Unity version, device에 따라 달라집니다. exported session의 device snapshot과 warning을 사용해 차이를 설명합니다.
