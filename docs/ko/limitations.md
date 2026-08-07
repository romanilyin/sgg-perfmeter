# 제한 사항

SGG PerfMeter는 low-overhead runtime diagnostics layer로 설계되었습니다. Unity Profiler, RenderDoc, Profile Analyzer, Frame Debugger의 deep capture를 대체하는 용도가 아닙니다.

## Platform 및 Pipeline 범위

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph 또는 HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline은 unsupported이며 planned 상태가 아닙니다.
- HDRP overdraw 및 heatmap은 unsupported입니다. HDRP projects에서도 FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup, MCP diagnostics는 사용할 수 있습니다.
- Unity `2022.3`부터 `6000.3`까지는 compile-safety를 위해 import될 수 있지만, runtime behavior 및 support target은 Unity `6000.4+`입니다.

## Timing Availability

- GPU timing은 platform 및 graphics API에 따라 unavailable, delayed, unreliable 상태일 수 있습니다.
- `CollectionFrame`은 PerfMeter가 snapshot을 수집한 Unity frame이며, `FrameTimingManager`가 나타내는 정확한 hardware frame과 반드시 같지는 않습니다.
- GPU frame timing이 중요하다면 Android에서는 Vulkan을 우선 사용합니다.
- OpenGL/OpenGLES는 GPU timing 및 overdraw instrumentation에서 degraded mode로 취급해야 합니다.

## Counter Availability

Profiler counter는 platform, Unity version, render pipeline settings, graphics API에 따라 달라집니다. 모든 counter가 어디서나 존재한다고 가정하지 말고 `AvailableCounters`, `UnavailableCounters`, warnings를 사용합니다.

## External GPU Capture

- Coordinator는 active request 하나를 허용하며 `PreRoll`, `Capturing`, `PostRoll`, `Completed`를 deterministic하게 진행합니다. 같은 active ID는 idempotent이고 다른 active ID는 overlap으로 reject됩니다.
- Backend는 Unity의 experimental `ExternalGPUProfiler`를 Editor 또는 Development Builds에서 external tool이 이미 attach된 경우에만 사용합니다. `RenderDoc`은 Windows/Linux desktop의 Direct3D 11, Direct3D 12, Vulkan으로 제한되고 `PIX`는 Windows desktop의 Direct3D 12로 제한됩니다.
- `Completed`는 Unity wrapper lifecycle만 확인합니다. external `.rdc`/`.wpix` artifact가 존재한다는 증거가 아니며 artifact path도 제공하지 않습니다.
- Automated tests는 fake backend를 사용합니다. Real external tool 및 artifact 확인은 release gate로 남습니다.
- Correlated bundles와 MCP capture control은 사용할 수 있지만, 전달된 `.rdc`/`.wpix`는 observed/hashed artifact일 뿐입니다. Unity는 attached tool이나 capture association을 인증할 수 없으므로 real external tool 확인은 release-candidate gate로 남습니다.

## Overdraw 비용 및 지원

Numerical overdraw와 visual heatmap은 diagnostic mode입니다. rendering work를 추가하므로 steady-state gameplay UI로 계속 켜 두지 말고 bounded window에서 사용해야 합니다.

URP numerical overdraw에는 다음이 필요합니다.

- active URP renderer에 설치된 `PerfMeterRenderGraphFeature`;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- supported graphics API;
- async GPU readback support.

HDRP를 포함한 지원되지 않는 target은 warnings와 함께 `OverdrawState.Unsupported`를 보고합니다.

## Overlay 비용

overlay에는 두 가지 UI Toolkit backend path가 있습니다. Unity `6000.4`에서는 owned `UIDocument` host를, Unity `6000.5+`에서는 owned `PanelRenderer` host를 사용합니다. host는 foreign UI의 panel settings와 children을 보존하며 PerfMeter가 소유한 container만 rebuild합니다. numeric value는 stable reserved numeric slot과 numeric monospace role을 사용합니다. `FpsOnly`는 한 줄에 들어가지 않을 때 deterministic bounded two-row fallback을 사용하고, card와 bar는 좁은 logical width에서 wrap됩니다. 이는 clipping 위험을 줄이지만 임의의 모든 resolution이나 scale을 보장하지 않으므로, heavy visual diagnostics, graph mode 및 최종 layout은 target device에서 검증해야 합니다.

## Validation Status

현재 validation에는 automated EditMode coverage, Unity `6000.4.10f1`의 HDRP smoke validation, 이전 Android S23 Vulkan/GLES smoke validation이 포함됩니다. 데이터를 release-signoff evidence로 다루기 전에는 더 넓은 player-build 및 device coverage가 여전히 유용합니다.

## 선택적 메모리 스냅샷: 제한 및 개인정보

- Unity `6000.4+`에서 `com.unity.memoryprofiler` `1.1.0+`가 없으면 기능을 사용할 수 없습니다. core package는 이 dependency를 install하거나 요구하지 않습니다.
- 기본값은 manual capture만 허용합니다. system-memory threshold와 bounded leak-growth trigger는 opt-in이며 모든 request에 single-flight/overlap, cooldown, minimum free-space, backend, capture-flag guard가 적용됩니다.
- owned `.snap` staging은 `Temp/PerfMeter/MemorySnapshots` 아래에 있고 512 MiB로 제한됩니다. memory-only evidence는 `Temp/PerfMeter/CaptureBundles` 아래로 export되며 bundle retention total quota는 2 GiB입니다. 성공한 export는 one-shot으로 staging source를 삭제하지만 cleanup warning이 명시될 수 있습니다.
- snapshot에는 민감한 process memory가 포함될 수 있습니다. 공유하기 전에 보호하고 검토하십시오. bundle은 `contains_sensitive_memory`, backend/flag provenance, `memory-snapshot.json`, SHA-256 metadata를 기록하며 external GPU artifact는 만들지 않습니다.
- OS file lock으로 인한 삭제와 portable managed reparse-point race 보호는 best-effort입니다. 안전하지 않거나 소유하지 않은 path는 reject되고 cleanup failure는 warning으로 남습니다.
- evidence에는 memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, `com.unity.memoryprofiler@1.1.12` optional compile, Unity `6000.4.12f1` full EditMode `182/182`와 full PlayMode `14/14`가 포함됩니다. release-player 또는 device behavior를 주장하는 결과는 아닙니다.

## Graphics diagnostics 및 GraphicsStateCollection 제한

- shader GPU-program creation 및 graphics-pipeline creation marker는 동적인 `ProfilerRecorder` capability입니다. Unity, platform, graphics API, catalog refresh 상태에 따라 availability가 달라질 수 있습니다. `Unavailable`, `AvailableNoSample`, `AvailableSampled`와 provenance를 사용하고 numeric 0으로 availability를 추론하지 마십시오.
- marker value는 recorder의 `Unit`과 `DataType`을 유지하는 raw value입니다. 항상 shader/PSO count인 것은 아니며 PerfMeter는 common unit으로 변환하지 않습니다. capability metadata에는 exact/alias resolution, resolved recorder names, resolved/sampled component count, catalog revision이 포함됩니다.
- optional `SGG.PerfMeter.GraphicsStateCollection` assembly는 Unity `6000.4+`를 대상으로 합니다. `6000.4`에서는 `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, `6000.5+`에서는 `UnityEngine.Rendering.GraphicsStateCollection`을 사용하며 더 이전 Unity는 이 integration을 지원하지 않습니다.
- trace에는 active PerfMeter session이 필요합니다. 일반 Play Mode에서는 end-of-frame 후 trace frame이 끝나고 batch mode에서는 next-frame fallback을 사용합니다. correlated session sample은 session의 warm-up, interval, max-sample 설정에 영향을 받습니다.
- preparation, trace finalization, prewarm, cleanup을 포함해 graphics-state flight는 하나만 허용됩니다. active external GPU capture, memory snapshot, alert-capture도 overlap rejection 대상입니다. `IsBusy`/`is_busy`는 이 flight와 persisted cleanup을 포함하고, `HasPendingCleanup`/`has_pending_cleanup`은 retry 대기 중인 owned artifact를 구체적으로 알립니다. matching cancel은 best-effort이며 cleanup failure는 표시된 상태로 남아 다음 request를 늦출 수 있습니다.
- `StopSession()`은 active trace를 cancel하므로 trace 전체에 active session이 필요합니다. owned artifact 삭제가 실패하면 인접한 `.delete-pending` sidecar marker가 생성되고 domain reload 후 복원·재시도됩니다. artifact와 marker가 삭제될 때까지 warning과 busy state가 남습니다.
- prewarm은 owned project-relative artifact만 받아 synchronous하게 실행하고 artifact를 보존합니다. progressive warmup은 incomplete일 수 있습니다. Unity backend는 cache-miss tracing을 지원하지 않으므로 request는 `Unavailable`이고 cache-miss evidence는 노출되지 않습니다.
- owned `.graphicsstate` artifact는 `Temp/PerfMeter/GraphicsStateCollections` 아래에 저장되고 regular non-empty file이어야 하며 64 MiB로 제한됩니다. trace는 600 frames, progressive prewarm은 1,000,000 states로 제한되고 minimum-free-disk 및 project-local path guard가 적용됩니다.
- 최종 evidence는 Unity `6000.4.12f1` compile passed, GSC EditMode targeted `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208`, full post-fix PlayMode `16/16`입니다. Unity `6000.5.6f1` optional consumer compile도 isolated하게 passed했습니다. Unity `6000.5` full tests, release-player 및 target-device behavior는 release gate로 남아 있으며 여기서 검증되었다고 주장하지 않습니다.

## Render integration context 제한

- `PerfMeterRenderIntegrationSnapshot`은 integration-neutral observation contract이며 deep Render Graph/Custom Pass capture가 아닙니다. read는 runtime collection을 시작하지 않습니다. 첫 observation 전에는 지원되는 current pipeline이 `Available`/`NotObserved`일 수 있고, pipeline/configuration 변경 후에는 `ObservationMatchesCurrentPipeline: false`, 명시적인 frame/age와 warning으로 stale observation을 표시합니다.
- URP는 public current-frame `UniversalRenderingData.renderingMode`와 실제로 schedule된 PerfMeter pass를 보고합니다. HDRP는 실제 PerfMeter `CustomPass`를 보고하지만 effective rendering mode는 unavailable입니다.
- private/internal Render Graph pass/resource reflection은 제거되었습니다. stable public API가 없기 때문에 legacy facade의 `registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count`, `aliased_resource_count`는 `-1`로 유지됩니다.
- GRD activity는 public `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` 결과이며 global runtime state를 나타냅니다. 특정 camera/renderer가 GRD를 사용했다는 증거는 아닙니다. URP Forward+는 current-frame observation이고 HDRP rendering-mode/Forward+ availability는 `Unknown`으로 유지됩니다.
- GRD effectiveness는 exact capability provenance가 포함된 aggregate BRG draw-call/instance counter입니다. 다른 `BatchRendererGroup` 사용자를 포함할 수 있으므로 renderer별 GRD participation을 증명하지 않습니다. unavailable 또는 sample 전 값은 `null`로 serialize됩니다.
- VRS는 `SystemInfo`/`ShadingRateInfo`의 authoritative hardware support를 제공합니다. future typed adapter가 증명하지 않는 한 configuration/activity는 `Unknown`이며 VRS activity를 주장하지 않습니다.
- Unity의 stable public API는 RenderGraph/CustomPass viewer나 pass-target API를 공개하지 않습니다. 따라서 PerfMeter는 Editor navigation을 추가하지 않고 약속하지도 않습니다.
- capture context schema v1은 `render`를 유지하고 `render_integration`을 추가하며 session JSON/CSV schema는 변경하지 않습니다. external capture context는 첫 `Capturing` sample에서 freeze되고 이후 read로 교체되지 않습니다.
- PM-REN-001 최종 evidence는 Unity `6000.4.12f1` main compile passed, targeted `PerformanceMeterApiTests` `53/53`, `PerfMeterCaptureBundleTests` `15/15`, `PerformanceMeterPlayModeSmokeTests` `12/12`, final full EditMode `215/215`, full PlayMode `16/16`입니다. Focused review P1/P2 resolved. Isolated compile matrix는 Unity `6000.4.12f1` URP `17.4`/HDRP `17.4` 및 Unity `6000.5.6f1` URP `17.5`/HDRP `17.5`에서 passed했습니다. Release-player/device validation은 pending이며 release를 주장하지 않습니다.
- PM-GRD-001 최종 evidence는 Unity `6000.4.12f1` compile passed, targeted API `58/58`, capture-bundle `15/15`, PlayMode smoke `12/12`, full EditMode `220/220`, PlayMode `16/16`입니다. Focused review P1/P2 resolved. Unity `6000.4`/`6000.5`의 URP `17.4`/`17.5`, HDRP `17.4`/`17.5` compile matrix도 passed했습니다. Release-player/device behavior는 pending입니다.
