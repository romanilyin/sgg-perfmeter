Да, HDRP поддержка реальна, но это **не “добавить ещё один define”**. Сейчас PerfMeter архитектурно заточен под URP: package.json прямо говорит про URP-проекты и тянет `com.unity.render-pipelines.universal`, runtime/editor asmdef тоже напрямую ссылаются на Universal Runtime, а рендер-интеграция сделана через URP `ScriptableRendererFeature` + RenderGraph.

## Коротко: что нужно сделать

Нужно разделить PerfMeter на **core** и **render-pipeline adapters**:

```text
SGG.PerfMeter.Core
  FPS / CPU / GPU timing
  ProfilerRecorder counters
  overlay
  sessions/export
  alerts
  settings
  MCP/status API

SGG.PerfMeter.URP
  текущий PerfMeterRenderGraphFeature
  URP camera snapshot
  URP setup utility
  URP shaders

SGG.PerfMeter.HDRP
  HDRP CustomPass integration
  HDRP camera snapshot
  HDRP setup/status utility
  HDRP shaders
```

Большая часть PerfMeter останется общей: сбор FPS/CPU/GPU timing и render counters уже сидит на `FrameTimingManager` и `ProfilerRecorder`, а не на URP-типах. Текущий `PerfMeterCollector` использует `FrameTimingManager`, `ProfilerRecorder`, `ProfilerCategory.Render`, `ProfilerCategory.Memory` и только общие `UnityEngine.Rendering` типы.

---

## Главная проблема сейчас

### 1. Package dependency

Сейчас пакет принудительно зависит от URP:

```json
"dependencies": {
  "com.unity.render-pipelines.universal": "17.4.0"
}
```

И описание/keywords тоже позиционируют пакет как URP-only.

Для HDRP это плохо: HDRP-проекту не надо тащить URP только ради PerfMeter. Лучший вариант — **не делать один пакет, который зависит и от URP, и от HDRP**. Лучше:

```text
com.sungeargames.perfmeter          // core, без URP/HDRP dependency
com.sungeargames.perfmeter.urp      // depends on core + URP
com.sungeargames.perfmeter.hdrp     // depends on core + HDRP
```

Или монорепо с одним UPM package, но с optional pipeline assemblies. Однако для чистой UPM-дистрибуции отдельные пакеты проще, честнее и меньше ломают чужие проекты.

### 2. asmdef

Сейчас runtime asmdef напрямую ссылается на:

```json
"Unity.RenderPipelines.Core.Runtime",
"Unity.RenderPipelines.Universal.Runtime"
```

Editor asmdef тоже ссылается на Universal Runtime.

Для HDRP надо вынести URP-зависимый код в отдельный asmdef:

```text
SGG.PerfMeter.Core.asmdef
  references:
    Unity.RenderPipelines.Core.Runtime

SGG.PerfMeter.URP.asmdef
  references:
    SGG.PerfMeter.Core
    Unity.RenderPipelines.Universal.Runtime

SGG.PerfMeter.HDRP.asmdef
  references:
    SGG.PerfMeter.Core
    Unity.RenderPipelines.HighDefinition.Runtime
```

Editor тоже аналогично:

```text
SGG.PerfMeter.Editor.Core
SGG.PerfMeter.Editor.URP
SGG.PerfMeter.Editor.HDRP
```

---

## Что переписывать под HDRP

### 1. `PerfMeterRenderGraphFeature.cs`

Это главный URP-only файл. Он наследуется от `ScriptableRendererFeature`, использует `ScriptableRenderPass`, `RenderPassEvent`, `UniversalResourceData`, `UniversalCameraData`, `UniversalRenderingData`, `UniversalLightData`, `RenderingUtils.CreateDrawingSettings`, URP shader tags и URP RenderGraph path.

Для HDRP вместо этого нужен новый файл примерно такого типа:

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SGG.PerfMeter.HDRP
{
    internal sealed class PerfMeterHdrpCustomPass : CustomPass
    {
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // Create materials / buffers if needed.
        }

        protected override void Execute(CustomPassContext ctx)
        {
            // Marker pass.
            // Overdraw counter pass.
            // Heatmap pass.
        }

        protected override void Cleanup()
        {
            // Destroy materials / release buffers.
        }
    }
}
```

HDRP официально поддерживает `CustomPass`: он даёт `Setup`, `Execute(CustomPassContext ctx)` и `Cleanup`, а `CustomPassContext` содержит command buffer, render context, buffers и другие данные pass-а. ([Unity документация][1])

---

## Как лучше подключать HDRP pass

Для HDRP я бы **не создавал сценовый GameObject и не заставлял пользователя добавлять Custom Pass Volume руками**. Лучше использовать Global Custom Pass API.

Unity прямо даёт API для регистрации custom pass без GameObject/Volume в сцене: `CustomPassVolume.RegisterGlobalCustomPass` и `UnregisterGlobalCustomPass`. Это позволяет не менять пользовательские сцены и не спавнить скрытые volume-объекты. ([Unity документация][2])

Примерно:

```csharp
CustomPassVolume.RegisterUniqueGlobalCustomPass(
    CustomPassInjectionPoint.BeforePostProcess,
    _perfMeterHdrpPass,
    priority: 0f);
```

И при остановке:

```csharp
CustomPassVolume.UnregisterGlobalCustomPass(_perfMeterHdrpPass);
```

Но есть важный нюанс: HDRP global custom pass выполняется для камер, у которых включены custom passes во frame settings. Unity API указывает, что global pass будет выполнен каждой камерой с включёнными custom pass frame settings. ([Unity документация][3])

Значит setup/status должен уметь писать предупреждение:

```text
HDRP detected, but Custom Passes are disabled in HDRP Frame Settings.
PerfMeter HDRP render integration will not execute.
```

---

## Какие HDRP injection points использовать

Для PerfMeter нужны разные задачи:

| Задача                      |                       HDRP injection point | Почему                                                                                         |
| --------------------------- | -----------------------------------------: | ---------------------------------------------------------------------------------------------- |
| Диагностический marker pass | `BeforePostProcess` или `AfterPostProcess` | Просто профайлерный маркер, можно почти где угодно                                             |
| Overdraw counter            |                        `BeforePostProcess` | Все основные opaque/transparent уже есть, HDR color доступен, до финального post-process       |
| Heatmap overlay             | `BeforePostProcess` или `AfterPostProcess` | До post-process heatmap попадёт под тонмаппинг, после post-process будет ближе к debug overlay |

HDRP имеет семь injection points, и `BeforePostProcess` содержит HDR-геометрию кадра, а `AfterPostProcess` содержит уже финальный результат после постпроцесса. ([Unity документация][4])

Для первого релиза я бы сделал так:

```text
Overdraw counter: BeforePostProcess
Heatmap: BeforePostProcess by default
Optional setting: heatmapInjectionPoint = BeforePostProcess / AfterPostProcess
```

`AfterPostProcess` может быть визуально удобнее для heatmap, но Unity предупреждает, что при этом injection point объекты, использующие depth buffer, могут давать jitter artifacts. Для heatmap с `ZTest Always` это может быть терпимо, но лучше не делать это дефолтом. ([Unity документация][4])

---

## Overdraw в HDRP

Текущий URP overdraw работает так:

1. Создаёт override material.
2. Рендерит renderer list.
3. В fragment shader делает `InterlockedAdd` в `RWStructuredBuffer<uint>`.
4. Делает `AsyncGPUReadback`.
5. Считает ratio: fragments / screen pixels.

Эту идею можно сохранить. Для HDRP вместо URP `RendererList` проще использовать:

```csharp
CustomPassUtils.DrawRenderers(
    ctx,
    layerMask,
    CustomPass.RenderQueueType.All,
    overrideMaterial,
    overrideMaterialIndex: 0,
    sorting: SortingCriteria.None);
```

HDRP `CustomPassUtils.DrawRenderers` официально поддерживает override material, render queue filter, sorting и rendering layer mask. ([Unity документация][5])

Нужно будет сделать HDRP-версии шейдеров.

Сейчас шейдеры жёстко URP-only:

```shader
Tags { "RenderPipeline" = "UniversalPipeline" }
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
```

Это есть и в counter, и в heatmap shader.

Для HDRP нужны отдельные:

```text
SGGPerfMeterOverdrawCounterHDRP.shader
SGGPerfMeterOverdrawHeatmapHDRP.shader
```

С тегом:

```shader
Tags { "RenderPipeline" = "HDRenderPipeline" }
```

И HDRP include-ами. Для custom pass fullscreen/utility shaders Unity использует HDRP `CustomPassCommon.hlsl`; пример документации включает:

```hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
```

([Unity документация][1])

Для counter shader надо сохранить ограничения текущей реализации: `RWStructuredBuffer`, `InterlockedAdd`, `AsyncGPUReadback`, отсутствие OpenGL/OpenGLES. Текущий код уже проверяет `supportsAsyncGPUReadback`, `supportsComputeShaders` и исключает OpenGL/OpenGLES.

---

## Camera snapshot под HDRP

Сейчас `PerfMeterCameraSnapshotProvider` импортирует `UnityEngine.Rendering.Universal` и читает `UniversalAdditionalCameraData`: `renderType`, `renderPostProcessing`, `antialiasing`, `antialiasingQuality`, `stopNaN`, `renderShadows`, `requiresDepthTexture`, `requiresColorTexture` и т.п.

Для HDRP нужен аналог через `HDAdditionalCameraData`. У HDRP есть `HDAdditionalCameraData`, который хранит HDRP-specific параметры камеры; в API видны, например, `antialiasing`, `SMAAQuality`, `TAAQuality`, `allowDynamicResolution`, `allowDeepLearningSuperSampling`, `allowFidelityFX2SuperResolution`, `clearColorMode`, `clearDepth`, `backgroundColorHDR`, `customRenderingSettings`. ([Unity документация][6])

Я бы поменял модель snapshot так, чтобы она не была URP-only:

```csharp
public enum PerfMeterRenderPipelineKind
{
    Unknown,
    BuiltIn,
    URP,
    HDRP
}

public readonly struct PerfMeterCameraSnapshot
{
    public PerfMeterRenderPipelineKind RenderPipeline { get; }
    public bool HasPipelineCameraData { get; }

    // Common camera fields.
    public string CameraName { get; }
    public CameraType CameraType { get; }
    public bool AllowHDR { get; }
    public bool AllowMSAA { get; }

    // Pipeline-specific summary, not strongly URP-only.
    public string PipelineCameraMode { get; }
    public string AntiAliasing { get; }
    public string AntiAliasingQuality { get; }
    public bool PostProcessingEnabled { get; }
    public bool DynamicResolutionEnabled { get; }
    public string PipelineWarnings { get; }
}
```

И внутри адаптеров:

```text
URP adapter:
  reads UniversalAdditionalCameraData

HDRP adapter:
  reads HDAdditionalCameraData
```

---

## Setup window / auto install

Сейчас setup utility делает URP-specific работу:

* ищет `UniversalRenderPipelineAsset`;
* ищет `UniversalRendererData`;
* добавляет `PerfMeterRenderGraphFeature` в `m_RendererFeatures`;
* статус пишет “URP renderer assets”.

Для HDRP это надо заменить не на “добавить renderer feature”, а на:

```text
HDRP detected
Frame Timing Stats enabled/disabled
HDRP Custom Pass integration registered/unregistered
Custom Passes enabled in HDRP Frame Settings: yes/no/unknown
HDRP overdraw shaders available: yes/no
HDRP camera data available: yes/no
```

В UI можно сделать секцию:

```text
Render Pipeline Integration

Detected pipeline: HDRP
Runtime integration: Global Custom Pass
Status: Registered
Injection point: BeforePostProcess
Overdraw: Supported / Unsupported / Waiting for pass
```

Для URP оставить текущую установку renderer feature.

---

## RenderGraph analytics

Текущий `PerfMeterRenderGraphAnalytics` в `PerfMeterRenderGraphFeature.cs` читает внутренние поля RenderGraph reflection-ом: pass count, native/merged passes, transient/imported/aliased resources.

В HDRP я бы **не пытался сразу повторить это reflection-ом**. HDRP CustomPass API не даёт тебе такой же прямой `RenderGraph` object в `Execute(CustomPassContext)`. На первом этапе лучше:

```text
URP:
  RenderGraph analytics available

HDRP:
  Render integration observed
  Injection point observed
  Camera observed
  Overdraw pass observed
  RenderGraph internal counters unavailable
```

И в статусе честно писать:

```text
HDRP RenderGraph internal counters are not exposed through the HDRP Custom Pass API.
PerfMeter reports Custom Pass observation instead.
```

Это лучше, чем ломкий reflection по HDRP internals.

---

## Что почти не нужно менять

### FPS / CPU / GPU timing

Оставить почти как есть. Но надо протестировать HDRP на D3D11/D3D12/Vulkan/Metal, потому что GPU timing и profiler counters зависят не только от render pipeline, но и от platform/backend. Текущий код уже умеет предупреждать, если GPU frame timing недоступен или counters отсутствуют.

### Overlay

Если overlay сейчас рисуется поверх игры вне SRP-specific pass-а, его трогать почти не надо. Главное — не привязывать видимость overlay к наличию URP RenderGraphFeature. Для HDRP render pass нужен только для marker/overdraw/heatmap.

### Sessions / export / alerts / settings

Оставить в core. В settings добавить pipeline-specific блок:

```json
{
  "renderPipeline": {
    "preferredIntegration": "Auto",
    "hdrp": {
      "enabled": true,
      "injectionPoint": "BeforePostProcess",
      "heatmapInjectionPoint": "BeforePostProcess"
    },
    "urp": {
      "autoInstallRendererFeature": true
    }
  }
}
```

---

## Минимальный MVP HDRP

Я бы делал в таком порядке.

### Этап 1 — компиляция и core split

Цель: PerfMeter ставится в HDRP-проект и не тянет URP.

Сделать:

```text
[ ] Вынести core runtime из URP asmdef.
[ ] Вынести core editor/setup из URP editor asmdef.
[ ] Убрать URP dependency из core package.
[ ] Обновить README: URP supported, HDRP planned/experimental.
[ ] Добавить RenderPipelineKind detection.
```

После этого PerfMeter должен собираться без URP.

### Этап 2 — basic HDRP metrics

Цель: FPS/CPU/GPU/render counters + overlay работают в HDRP без overdraw.

Сделать:

```text
[ ] HDRP adapter без custom pass.
[ ] HDRP camera snapshot через HDAdditionalCameraData.
[ ] Setup/status для HDRP.
[ ] MCP/status API показывает pipeline = HDRP.
[ ] Overdraw в HDRP временно возвращает Unsupported с понятным warning.
```

Это уже полезная HDRP-поддержка.

### Этап 3 — HDRP CustomPass marker

Цель: PerfMeter виден в frame debugger/profiler как HDRP custom pass.

Сделать:

```text
[ ] PerfMeterHdrpCustomPass : CustomPass.
[ ] RegisterUniqueGlobalCustomPass.
[ ] Unregister on shutdown/domain reload.
[ ] Injection point setting.
[ ] ProfilingSampler / ProfilingScope marker.
[ ] Status: pass observed this frame / waiting.
```

### Этап 4 — HDRP overdraw counter

Цель: overdraw ratio работает в HDRP.

Сделать:

```text
[ ] HDRP OverdrawCounter shader.
[ ] HDRP OverdrawHeatmap shader.
[ ] CustomPassUtils.DrawRenderers with override material.
[ ] GraphicsBuffer + AsyncGPUReadback reused from current controller.
[ ] Camera/layer/renderQueue filtering.
[ ] Validation on D3D11/D3D12/Vulkan/Metal.
```

### Этап 5 — docs/release polish

```text
[ ] README: URP/HDRP support matrix.
[ ] Comparison docs: “URP RenderGraph + HDRP CustomPass”.
[ ] Troubleshooting:
    - HDRP Custom Passes disabled
    - GPU timing unavailable
    - overdraw unsupported on OpenGL/OpenGLES
    - shader stripped/missing
[ ] Samples:
    - HDRP minimal bootstrap
    - HDRP overdraw diagnostic
```

---

## Рекомендуемая структура файлов

```text
Assets/Scripts/SGG.PerfMeter/
  Runtime/
    Core/
      PerformanceMeter.cs
      PerfMeterCollector.cs
      PerfMeterRuntime.cs
      PerfMeterSettings.cs
      PerfMeterSessionRecorder.cs
      PerfMeterSessionExporter.cs
      PerfMeterAlertEngine.cs
      PerfMeterDeviceInfoProvider.cs
      PerfMeterSnapshots.cs
      SGG.PerfMeter.Core.asmdef

    URP/
      PerfMeterUrpRenderGraphFeature.cs
      PerfMeterUrpCameraSnapshotProvider.cs
      Resources/
        SGGPerfMeterOverdrawCounterURP.shader
        SGGPerfMeterOverdrawHeatmapURP.shader
      SGG.PerfMeter.URP.asmdef

    HDRP/
      PerfMeterHdrpCustomPass.cs
      PerfMeterHdrpIntegration.cs
      PerfMeterHdrpCameraSnapshotProvider.cs
      Resources/
        SGGPerfMeterOverdrawCounterHDRP.shader
        SGGPerfMeterOverdrawHeatmapHDRP.shader
      SGG.PerfMeter.HDRP.asmdef

  Editor/
    Core/
      Setup UI common
      MCP common commands
      SGG.PerfMeter.Editor.Core.asmdef

    URP/
      PerfMeterUrpSetupUtility.cs
      SGG.PerfMeter.Editor.URP.asmdef

    HDRP/
      PerfMeterHdrpSetupUtility.cs
      SGG.PerfMeter.Editor.HDRP.asmdef
```

---

## Итоговая оценка сложности

**Basic HDRP support** — несложно: core split + camera snapshot + setup/status. Это в основном архитектурная чистка.

**Full HDRP support с overdraw/heatmap** — средняя сложность. Сам алгоритм можно переиспользовать, но нужно аккуратно заменить URP RenderGraphFeature на HDRP CustomPass, переписать шейдеры и проверить injection points.

Я бы позиционировал релиз так:

```text
v1.0:
  Unity 6000.4+ URP
  Full support: FPS/CPU/GPU/render counters/overlay/sessions/overdraw/heatmap

v1.1:
  Unity 6000.4+ HDRP experimental
  FPS/CPU/GPU/render counters/overlay/sessions/camera snapshot
  HDRP CustomPass marker
  Overdraw: experimental

v1.2:
  HDRP stable
  Overdraw/heatmap validated
  Setup + docs + samples polished
```

Главное — не смешивать URP и HDRP в одном жёстко зависимом runtime assembly. Лучше сделать core + adapters, тогда PerfMeter станет нормальным multi-pipeline инструментом, а не URP-пакетом с прикрученной HDRP-веткой.

[1]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4017.4/manual/Custom-Pass-Scripting.html "Create a Custom Pass in a C# script | High Definition Render Pipeline | 17.4.0 "
[2]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4017.4/manual/Global-Custom-Pass-API.html "Manage a Custom Pass without a GameObject | High Definition Render Pipeline | 17.4.0 "
[3]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4017.4/api/UnityEngine.Rendering.HighDefinition.CustomPassVolume.html "Class CustomPassVolume
 \| High Definition Render Pipeline | 17.4.0 "
[4]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4017.4/manual/Custom-Pass-Injection-Points.html "Injection Points | High Definition Render Pipeline | 17.4.0 "
[5]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4017.4/api/UnityEngine.Rendering.HighDefinition.CustomPassUtils.html "Class CustomPassUtils
 \| High Definition Render Pipeline | 17.4.0 "
[6]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4017.4/api/UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.html "Class HDAdditionalCameraData
 \| High Definition Render Pipeline | 17.4.0 "
