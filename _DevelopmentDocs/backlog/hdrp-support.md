# HDRP Support Backlog

Статус: planned, не реализовано.

Текущий release candidate поддерживает Unity `6000.4+` с URP `17.4+` Render Graph integration. HDRP-поддержка в коде отсутствует: нет `UnityEngine.Rendering.HighDefinition`, `HDAdditionalCameraData`, HDRP `CustomPass`, HDRP shaders или HDRP asmdef.

## Текущее состояние

- Core package не имеет жесткой `package.json` dependency на URP.
- Core runtime asmdef не ссылается на URP.
- URP render integration вынесена в `Runtime/URP/SGG.PerfMeter.URP.asmdef`.
- Editor setup сейчас умеет устанавливать только URP `PerfMeterRenderGraphFeature`.
- Camera snapshot читает URP additional camera data через reflection, но HDRP camera data пока не читает.

## MVP HDRP

1. Определить active render pipeline как `URP`, `HDRP`, `BuiltIn` или `Unknown` без жестких package dependencies.
2. Для HDRP показать понятный setup/status: pipeline detected, render integration missing, overdraw unsupported.
3. Добавить HDRP camera snapshot через `HDAdditionalCameraData` reflection или отдельный optional HDRP asmdef.
4. Проверить, что FPS/CPU/GPU timings, memory/render counters, sessions, alerts, settings, overlay и MCP работают в HDRP-проекте без URP package.
5. Для overdraw в HDRP временно возвращать explicit unsupported state с actionable warning.

## HDRP Render Integration

HDRP не использует URP `ScriptableRendererFeature`. Для render integration нужен отдельный adapter:

- `PerfMeterHdrpCustomPass : CustomPass` в optional HDRP assembly.
- Global Custom Pass registration/unregistration без изменения пользовательских сцен.
- Проверка, включены ли Custom Passes в HDRP Frame Settings.
- Profiling marker для PerfMeter pass.
- Наблюдение camera/injection point/pass execution для status/MCP.

Рекомендуемый первый injection point: `BeforePostProcess`. Для heatmap позже можно добавить настройку `BeforePostProcess` / `AfterPostProcess`.

## HDRP Overdraw

URP overdraw сейчас основан на replacement shader, fragment atomic counter и `AsyncGPUReadback`. Для HDRP понадобится отдельная реализация:

- HDRP-compatible counter shader с `HDRenderPipeline` tag.
- HDRP-compatible heatmap shader.
- `CustomPassUtils.DrawRenderers` или эквивалентный HDRP path с override material.
- Проверка D3D11/D3D12/Vulkan/Metal.
- Сохранить текущие gates: no OpenGL/OpenGLES, required async readback, required compute/storage support.

## Render Graph Analytics в HDRP

Не пытаться в первом проходе повторять URP reflection по внутренностям HDRP Render Graph. Для HDRP достаточно честного статуса:

- HDRP integration observed.
- Injection point observed.
- Camera observed.
- Overdraw unsupported/experimental.
- Internal Render Graph counters unavailable.

## Definition Of Done

- HDRP-проект импортирует package без URP dependency.
- Basic metrics, overlay, sessions, alerts, settings and MCP run in HDRP.
- Setup/status явно различает URP supported, HDRP planned/experimental, Built-in unsupported.
- HDRP docs не обещают overdraw/heatmap до реализации и device validation.
