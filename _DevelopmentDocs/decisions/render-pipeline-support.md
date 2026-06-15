# Render Pipeline Support Decision

## Решение

- Current release target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline не поддерживаем и не планируем.
- HDRP MVP реализован для core diagnostics, camera snapshot, setup/status, MCP и Custom Pass observation. HDRP overdraw/heatmap остаются unsupported.
- Core package не должен жестко тянуть URP или HDRP dependency.
- Pipeline-specific integration должна жить в optional adapter assemblies.

## Почему так

PerfMeter строится вокруг runtime diagnostics, `FrameTimingManager`, `ProfilerRecorder`, UI Toolkit overlay, sessions, alerts, MCP и render-pipeline-specific diagnostics. Общая часть может быть pipeline-neutral, но overdraw/heatmap/render markers требуют разных integration paths.

URP path реализован через `ScriptableRendererFeature` и Render Graph. HDRP path реализован через optional `CustomPass` adapter без жесткой зависимости core runtime от HDRP. Built-in не имеет нужного современного SRP/Render Graph integration surface, поэтому поддержка Built-in размоет продукт и усложнит runtime без достаточной пользы.

## Практические правила

- Не добавлять Built-in compatibility code.
- Не делать URP dependency обязательной для core runtime.
- Не делать HDRP dependency обязательной для core runtime.
- Не обещать HDRP overdraw/heatmap в user-facing docs до отдельной реализации и device validation.
- Новый HDRP work после MVP вести отдельными backlog entries.
