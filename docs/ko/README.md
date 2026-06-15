# SGG PerfMeter

**Unity 6 URP를 위한 경량 런타임 진단 및 에이전트가 읽을 수 있는 프로파일링.**

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

- Unity `6000.4+` / URP `17.4+` Render Graph 런타임 상태.
- 사용 가능한 경우 FrameTimingManager CPU/GPU timing: CPU frame, main thread, render thread, present wait, GPU frame time.
- 사용 가능한 경우 ProfilerRecorder render counter: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory, GPU memory.
- GPU, CPU main, CPU render, present/VSync, balanced, unknown 병목 분류.
- URP Render Graph를 통한 opt-in overdraw measurement 및 visual overdraw heatmap.
- code 및 MCP automation용 device, camera, Render Graph, status, metrics, alerts, sessions, custom metrics snapshot.

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
    "com.sungeargames.perfmeter": "2026.6.11-1"
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
