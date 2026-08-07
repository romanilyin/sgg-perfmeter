# Setup 창

Editor 창은 `SGG/Perfmeter/Setup`에서 열 수 있습니다.

## 현재 동작

- **Setup**과 **Presets**는 저장된 PerfMeter 프로젝트 설정과 오버레이 프리셋 데이터를 보여 줍니다. 스키마/버전, `legacy` 호환성, 예약된 메타데이터 행은 모두 읽기 전용이며, 위젯 구성과 포커스를 잃을 때 정규화되는 숫자 값도 포함됩니다.
- **Runtime**은 세션, 메모리, graphics-state, render integration, GRD/BRG 진단과 선택적 통합의 기능/상태를 읽기 전용으로 표시합니다. `Unavailable`, `unknown`, 샘플 없음 상태를 명시적으로 유지합니다. `Measure Overdraw (project default)`는 프로젝트 기본 sentinel 값을 사용합니다.
- `Session Analysis`, `Profile Analyzer`, `Refresh` 작업을 제공합니다. `Start Session`과 `Stop Session`은 Play Mode에서만 사용할 수 있습니다. Setup을 열거나 `Refresh`해도 런타임 수집은 시작되지 않습니다.
- memory snapshot과 graphics-state trace/prewarm 요청 파라미터는 런타임 전용 입력이며 프로젝트 설정이 아닙니다.

## 참고용 스크린샷

> 아래 스크린샷은 P3.5 이전에 촬영되었습니다. 시각적 참고로만 유지하며, 완료된 Setup UX의 현재 증거가 아닙니다.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-ko-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-ko-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-ko-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-ko-debug.png)
