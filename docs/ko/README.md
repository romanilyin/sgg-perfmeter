# SGG PerfMeter

**Unity 6 URP+HDRP (FPS meter)를 위한 경량 런타임 진단 및 에이전트가 읽을 수 있는 프로파일링.**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](../es/README.md) | [Français](../fr/README.md) | [Italiano](../it/README.md) | [日本語](../ja/README.md) | [한국어](./README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](../zh-cn/README.md)

[설치](./installation.md) | [빠른 시작](./quick-start.md) | [워크플로](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [비교](./comparison.md) | [제한 사항](./limitations.md) | [문제 해결](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter는 프레임 병목을 식별하고, 성능 변화를 비교하고, 재현 가능한 세션을 기록하며, 도구와 AI Agent가 사용할 수 있는 구조화된 프로파일링 데이터를 제공합니다.

## 도움이 되는 이유

- 게임 실행 중 병목 컨텍스트를 바로 확인할 수 있습니다.
- Preset, 그래프, MetricBars, compact layout, custom metric row를 전환할 수 있습니다.
- warm-up, scene 참조, worst-frame 요약, JSON/CSV export가 포함된 재현 가능한 profiling session을 기록할 수 있습니다.
- overlay를 계속 보고 있지 않아도 alert, structured log, callback, Editor warning cooldown을 사용할 수 있습니다.
- 도구와 Agent에 비교, A/B test, hotspot 탐색용 구조화 데이터를 제공할 수 있습니다.

## 측정 항목

- Unity `6000.4+` / URP `17.4+` Render Graph 및 HDRP `17.4+` Custom Pass 런타임 상태.
- 사용 가능한 경우 FrameTimingManager CPU/GPU timing: CPU frame, main thread, render thread, present wait, GPU frame time.
- 사용 가능한 경우 ProfilerRecorder render counter: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory, GPU memory.
- GPU, CPU main, CPU render, present/VSync, balanced, unknown 병목 분류.
- Unity generic path로 RenderDoc/PIX를 조정하거나, 별도로 배포되고 SHA-256으로 검증된 RenderDoc bridge를 선택적으로 사용하여 Windows x64 Editor의 D3D11/D3D12/Vulkan에서 authenticated `.rdc` artifact를 생성합니다. UPM package는 binary-free이며 RenderDoc 자체를 설치하지 않습니다.
- URP Render Graph를 통한 opt-in overdraw measurement 및 visual overdraw heatmap. HDRP overdraw/heatmap은 unsupported이며 core diagnostics는 계속 사용할 수 있습니다.
- code 및 MCP automation용 device, URP/HDRP camera, render integration, status, metrics, alerts, sessions, custom metrics snapshot.

## 선택적 플랫폼 텔레메트리

PerfMeter는 하나의 provider를 통해 thermal 및 Adaptive Performance 신호를 선택적으로 수집할 수 있습니다. core assembly에는 `com.unity.adaptiveperformance` hard dependency가 없으며, 선택적 `SGG.PerfMeter.AdaptivePerformance` assembly는 `com.unity.adaptiveperformance`가 `5.1.0+`일 때 Unity `6000.4+`용으로 활성화됩니다.

- **Provider API**: `PerformanceMeter.RegisterPlatformTelemetryProvider(...)`, `UnregisterPlatformTelemetryProvider(...)`, `GetPlatformTelemetry()`는 active provider를 최대 하나만 허용하며 provider ID/version, sample/change time, thermal warning, temperature level/trend, CPU/GPU performance level, adaptive bottleneck, field별 availability를 포함한 immutable `PerfMeterPlatformTelemetrySnapshot`을 반환합니다. 서로 다른 두 번째 provider 등록은 거부됩니다.
- **수집 및 export**: runtime은 수집된 각 frame마다 provider를 한 번 sample합니다. session JSON/CSV와 capture samples는 snapshot 및 provider provenance를 보존합니다. MCP command `perfmeter.platform.telemetry`로 현재 structured snapshot을 확인할 수 있습니다.
- **Profiler 및 alert**: `SGG.PerfMeter.Thermal.Sample`과 `SGG.PerfMeter.Thermal.Available`은 thermal collection marker와 availability counter를 노출합니다. 기본 `thermal.throttling` alert는 imminent 또는 active throttling level이 available할 때 `ThermalWarningLevel` metric을 사용합니다.
- **Unavailable 상태는 명시적입니다**: 지원되지 않는 provider와 field는 unavailable로 유지됩니다. JSON은 unavailable numeric value를 `null`로, CSV는 빈 field로 serialize하며 availability flag로 fake zero와 구분합니다. 실제 Adaptive Performance package 및 target device validation은 release matrix/release-candidate gate로 남아 있으며, 이 README는 device result가 실행되었다고 주장하지 않습니다.

## 빠른 시작

1. npm registry 또는 Git UPM으로 Unity package를 설치합니다.
2. Unity에서 `SGG/Perfmeter/Setup`을 엽니다.
3. 권장 setup을 실행하고 Play Mode를 시작한 뒤 overlay가 표시되는지 확인합니다.

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.8.11-1"
  }
}
```

## 문서

- [설치](./installation.md)
- [빠른 시작](./quick-start.md)
- [워크플로](./workflows.md)
- [API](./api.md)
- [MCP 및 Agent Automation](./mcp.md)
- [Visual Presets](./presets.md)
- [구현된 Widgets](./widgets.md)
- [스크린샷](./screenshots.md)
- [Setup Window 스크린샷](./setup-window-screenshots.md)
- [제한 사항](./limitations.md)
- [문제 해결](./troubleshooting.md)
- [비교](./comparison.md)
- [Brand Usage Policy](./brand.md)

## 라이선스

이 package는 **Stinger Royalty-Free EULA 1.0**에 따라 라이선스됩니다.

- 기준 러시아어 라이선스 텍스트: [LICENSE.ru.md](../../LICENSE.ru.md)
- 영어 보조 번역: [LICENSE.md](../../LICENSE.md)
- 고지: [NOTICE.md](../../NOTICE.md) 및 [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy: [brand.md](./brand.md)
