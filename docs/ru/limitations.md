# Ограничения

SGG PerfMeter сделан как low-overhead runtime diagnostics layer, а не замена deep capture инструментам Unity Profiler, RenderDoc, Profile Analyzer или Frame Debugger.

## Platform И Pipeline Scope

- Поддерживаемая runtime цель: Unity `6000.4+` с URP `17.4+` и Render Graph path.
- Built-in Render Pipeline и HDRP не являются first-class targets.
- Unity `2022.3` through `6000.3` могут импортироваться для compile-safety, но runtime behavior и support target требуют Unity `6000.4+`.

## Timing Availability

- GPU timing может быть недоступен, задержан или ненадежен в зависимости от platform и graphics API.
- `CollectionFrame` - это Unity frame, на котором PerfMeter собрал snapshot, а не обязательно точный hardware frame из `FrameTimingManager`.
- На Android предпочтителен Vulkan, если важен GPU frame timing.
- OpenGL/OpenGLES стоит считать degraded mode для GPU timing и overdraw instrumentation.

## Counter Availability

Profiler counters зависят от platform, Unity version, render pipeline settings и graphics API. Используйте `AvailableCounters`, `UnavailableCounters` и warnings, а не предполагайте, что каждый counter существует везде.

## Overdraw Cost И Support

Numerical overdraw и visual heatmap - diagnostic modes. Они добавляют rendering work и должны использоваться в bounded windows, а не как постоянный gameplay UI.

Numerical overdraw требует:

- `PerfMeterRenderGraphFeature` в активном URP renderer;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- supported graphics API;
- async GPU readback support.

Unsupported targets возвращают `OverdrawState.Unsupported` с warnings.

## Overlay Cost

Overlay allocation-conscious и throttled, но изменившиеся numeric values и graph labels все еще могут materialize managed strings на refresh interval. Heavy visual diagnostics и graph modes нужно валидировать на target devices.

## Validation Status

Текущая validation включает automated EditMode и PlayMode coverage плюс Android S23 Vulkan/GLES smoke validation. Более широкая player-build и device coverage все еще полезна перед использованием данных как release-signoff evidence.
