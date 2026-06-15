# HDRP Support Backlog

Статус: MVP реализован в `2026.6.11-1`; остаются future tasks для HDRP overdraw/heatmap и расширенной device validation.

Текущий release candidate поддерживает Unity `6000.4+` с URP `17.4+` Render Graph integration и HDRP `17.4+` Custom Pass integration. Core package не имеет жесткой зависимости от HDRP; HDRP adapter живет в optional assembly.

## Текущее состояние

- Core package не имеет жесткой `package.json` dependency на URP или HDRP.
- Core runtime asmdef не ссылается на URP или HDRP.
- URP render integration вынесена в `Runtime/URP/SGG.PerfMeter.URP.asmdef`.
- HDRP render integration вынесена в `Runtime/HDRP/SGG.PerfMeter.HDRP.asmdef`.
- Editor setup устанавливает URP `PerfMeterRenderGraphFeature` в URP проектах и показывает HDRP Custom Pass availability в HDRP проектах.
- Camera snapshot читает URP additional camera data и HDRP `HDAdditionalCameraData` через reflection.

## MVP HDRP

1. [x] Определить active render pipeline как `URP`, `HDRP`, `BuiltIn` или `Unknown` без жестких package dependencies.
2. [x] Для HDRP показать понятный setup/status: pipeline detected, Custom Pass availability, overdraw unsupported.
3. [x] Добавить HDRP camera snapshot через `HDAdditionalCameraData` reflection.
4. [x] Проверить, что FPS/CPU/GPU timings, memory/render counters, sessions, alerts, settings, overlay и MCP работают в HDRP-проекте без URP package.
5. [x] Для overdraw в HDRP возвращать explicit unsupported state с actionable warning.

## HDRP Render Integration

HDRP не использует URP `ScriptableRendererFeature`. Для render integration нужен отдельный adapter:

- [x] `PerfMeterHdrpCustomPass : CustomPass` в optional HDRP assembly.
- [x] Global Custom Pass registration/unregistration без изменения пользовательских сцен.
- [ ] Проверка, включены ли Custom Passes в HDRP Frame Settings.
- [ ] Profiling marker для PerfMeter pass.
- [x] Наблюдение camera/injection point/pass execution для status/MCP.

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

- [x] HDRP-проект импортирует package без URP dependency.
- [x] Basic metrics, overlay, sessions, alerts, settings and MCP run in HDRP.
- [x] Setup/status явно различает URP supported, HDRP Custom Pass available, Built-in unsupported.
- [x] HDRP docs не обещают overdraw/heatmap до реализации и device validation.
