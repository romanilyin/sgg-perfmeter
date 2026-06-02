# Render Pipeline Support Decision

## Решение

- Current release target: Unity `6000.4+` with URP `17.4+` Render Graph.
- Built-in Render Pipeline не поддерживаем и не планируем.
- HDRP в планах, но не реализован в текущем release candidate.
- Core package не должен жестко тянуть URP или HDRP dependency.
- Pipeline-specific integration должна жить в optional adapter assemblies.

## Почему так

PerfMeter строится вокруг runtime diagnostics, `FrameTimingManager`, `ProfilerRecorder`, UI Toolkit overlay, sessions, alerts, MCP и render-pipeline-specific diagnostics. Общая часть может быть pipeline-neutral, но overdraw/heatmap/render markers требуют разных integration paths.

URP path уже реализован через `ScriptableRendererFeature` и Render Graph. HDRP требует отдельный `CustomPass` adapter и отдельные shaders. Built-in не имеет нужного современного SRP/Render Graph integration surface, поэтому поддержка Built-in размоет продукт и усложнит runtime без достаточной пользы.

## Практические правила

- Не добавлять Built-in compatibility code.
- Не делать URP dependency обязательной для core runtime.
- Не обещать HDRP в user-facing docs до реализации и валидации.
- Любой HDRP work начинать с `backlog/hdrp-support.md`.
