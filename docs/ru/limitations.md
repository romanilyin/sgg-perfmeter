# Ограничения

SGG PerfMeter - слой runtime-диагностики с низкими накладными расходами. Для глубокого захвата используйте Unity Profiler, RenderDoc, Profile Analyzer или Frame Debugger.

## Область платформ и рендер-пайплайнов

- Поддерживаемая runtime-цель: Unity `6000.4+` с URP `17.4+` Render Graph или HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline не поддерживается и не планируется.
- HDRP overdraw и heatmap не поддерживаются. В HDRP остаются доступны FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup и MCP diagnostics.
- Unity от `2022.3` до `6000.3` может импортироваться для проверки компиляции, но поведение во время выполнения и поддержка требуют Unity `6000.4+`.

## Доступность таймингов

- GPU timing может быть недоступен, задержан или ненадежен в зависимости от платформы и graphics API.
- `CollectionFrame` - это Unity frame, на котором PerfMeter собрал снимок, а не обязательно точный аппаратный кадр из `FrameTimingManager`.
- На Android предпочтителен Vulkan, если важен GPU frame timing.
- OpenGL/OpenGLES стоит считать режимом с ограничениями для GPU timing и инструментации overdraw.

## Доступность счетчиков

Profiler counters зависят от платформы, версии Unity, настроек render pipeline и graphics API. Используйте `AvailableCounters`, `UnavailableCounters` и warnings, а не предполагайте, что каждый счетчик существует везде.

## Стоимость и поддержка overdraw

Числовой overdraw и визуальная heatmap - диагностические режимы. Они добавляют работу рендера и должны использоваться в ограниченных окнах, а не как постоянный игровой UI.

Числовой overdraw в URP требует:

- наличия `PerfMeterRenderGraphFeature` в активном URP renderer;
- поддержки UAV/storage buffer на fragment stage;
- поддержки compute shaders;
- поддерживаемого graphics API;
- поддержки async GPU readback.

Неподдерживаемые цели, включая HDRP, возвращают `OverdrawState.Unsupported` с warnings.

## Стоимость оверлея

Оверлей учитывает аллокации и ограничивает частоту обновления, но изменившиеся числовые значения и подписи графиков все еще могут создавать managed strings на интервале обновления. Тяжелую визуальную диагностику и режимы графиков нужно валидировать на целевых устройствах.

## Статус валидации

Текущая валидация включает автоматизированное покрытие EditMode, HDRP smoke validation в Unity `6000.4.10f1` и предыдущую smoke-валидацию Android S23 Vulkan/GLES. Более широкое покрытие player-билдов и устройств все еще полезно перед тем, как использовать данные как подтверждение готовности к релизу.
