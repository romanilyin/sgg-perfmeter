# 설치

SGG PerfMeter는 `com.sungeargames.perfmeter`라는 Unity package로 배포됩니다. 현재 public npm version은 `2026.8.19-1`이며 Git UPM과 local copy install도 사용할 수 있습니다.

## 요구 사항

- 지원되는 런타임 사용을 위한 Unity `6000.4+`.
- Render Graph path를 사용하는 URP `17.4+` 또는 HDRP `17.4+` Custom Pass integration.
- UI Toolkit runtime support.
- build에서 FrameTimingManager에 의존하기 전에 Frame Timing Stats 활성화.
- Optional native RenderDoc capture는 Windows x64 Unity Editor의 Direct3D 11, Direct3D 12, Vulkan만 지원합니다. Development Player, Linux native, IL2CPP, mobile, macOS native는 지원하지 않습니다.
- UPM package는 binary-free이며 RenderDoc 자체를 설치하지 않습니다. FTUE는 별도 배포 artifact의 byte length, SHA-256, AMD64 native PE contract가 정확히 일치하는 bridge만 download 또는 local install하며 이후 Editor restart가 필요합니다.

Package metadata는 import 및 compile check를 위한 import-safety floor로 Unity `2022.3`을 유지합니다. 현재 지원되는 런타임 target은 Unity `6000.4+` 및 URP `17.4+` Render Graph 또는 HDRP `17.4+` Custom Pass integration입니다.

이는 서로 다른 compatibility level입니다. `ImportCompatible`는 supported runtime behavior를 보장하지 않고, `CoreRuntimeCompatible`는 Unity `6000.4+`가 필요하지만 특정 pipeline은 요구하지 않습니다. `RenderIntegrationCompatible`는 active URP/HDRP `17.4+`와 PerfMeter adapter도 필요합니다. `PerfMeterSetupActions.GetCompatibilityStatus()` 또는 MCP `perfmeter.compatibility.status`로 조회하며 configuration readiness는 별도입니다.

## npm Scoped Registry Install

Unity project의 `Packages/manifest.json`에 npm registry를 Unity Package Manager scoped registry로 추가합니다.

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
    "com.sungeargames.perfmeter": "2026.8.19-1"
  }
}
```

manifest에 기존 `scopedRegistries`가 있으면 `npmjs` entry를 기존 array에 추가하세요.

## Git UPM 설치

package는 이 저장소 안의 다음 경로에 있습니다.

```text
Assets/Scripts/SGG.PerfMeter
```

Unity project의 `Packages/manifest.json`에 추가합니다.

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

환경에서 Git dependency에 SSH를 사용한다면 다음을 사용합니다.

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

반복 가능한 설치를 위해 tag 또는 commit을 고정합니다.

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.8.19-1"
  }
}
```

## 로컬 복사 설치

다음 folder를 Unity project로 복사합니다.

```text
Assets/Scripts/SGG.PerfMeter
```

이는 local package development 또는 Git dependency를 원하지 않는 경우에 유용합니다.

## 초기 Project Setup

다음을 엽니다.

```text
SGG/Perfmeter/Setup
```

첫 실행 설정 탭은 필수 준비 상태를 실시간으로 추적합니다. 선택 사항으로 명확히 표시된 각 통합을 설치하거나 건너뛰십시오. 모든 단계가 해결되면 탭이 숨겨지고 필수 검사가 나중에 실패하면 다시 표시됩니다.

그다음 권장 setup을 실행합니다.

1. Frame Timing Stats를 활성화합니다.
2. editable active URP renderer asset에 `PerfMeterRenderGraphFeature`를 설치합니다. HDRP projects는 URP renderer changes를 건너뛰며, package HDRP Custom Pass는 HDRP `17.4+`가 설치된 경우 runtime에 등록됩니다.
3. zero-code setup을 위해 JSON settings를 `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`에 저장하거나 initialization snippet을 복사합니다.
4. Play Mode에 진입하고 overlay를 확인합니다.

## Samples

Package Manager details panel에서 package samples를 import합니다.

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
