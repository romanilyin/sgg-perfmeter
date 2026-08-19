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

## External GPU Capture

- Coordinator допускает один активный запрос и детерминированно проходит `PreRoll`, `Capturing`, `PostRoll` и `Completed`. Тот же active ID идемпотентен, другой active ID отклоняется как пересечение.
- `GenericUnity` использует экспериментальный `ExternalGPUProfiler` Unity в Editor/Development Build. Его matrix остается RenderDoc на Windows/Linux desktop с D3D11/D3D12/Vulkan и PIX на Windows desktop с D3D12; completion не аутентифицирует tool или artifact.
- Опциональный native-путь поддерживает только RenderDoc в Windows x64 Unity Editor с D3D11, D3D12 или Vulkan. Development Player, Linux native, IL2CPP, mobile и macOS native не поддерживаются.
- UPM-пакет остается без бинарников. Отдельный pinned bridge работает только с уже загруженной `renderdoc.dll` и никогда не устанавливает, не загружает, не запускает и не inject RenderDoc.
- Native MetadataOnly по умолчанию использует `DoNotShare`; Copy/Embed являются sensitive, имеют отдельные квоты и требуют `ReviewBeforeShare`. Generic/caller artifacts остаются observed, не authoritative.
- Native circular timing capture PIX недоступен. Документированный Microsoft Windows timing API поддерживает forward capture, но игнорирует настройки circular storage, memory limit и discard; PerfMeter не заменяет запрошенный pre-alert ring на forward capture без документированной границы хранения или private PIX integration.
- Automated tests используют fake backend. Проверка настоящей external tool и artifact остается release gate.
- Correlated bundles и MCP capture control доступны, но переданный `.rdc`/`.wpix` остается только observed и hashed artifact: Unity не может аутентифицировать attached tool или связь artifact с capture. Проверка real external tool остается release-candidate gate.

## Command annotations RenderDoc

- Command annotations — отдельная optional integration, не равная более широкой матрице capture через `ExternalGPUProfiler`. Начальный transport поддерживает только Windows x64 Editor/D3D12 и требует уже загруженный RenderDoc App API `1.7` и активный capture.
- UPM-пакет остаётся без бинарников. Для аннотаций нужен отдельно установленный, закреплённый пакетом Editor bridge `2026.8.19-1` с новыми annotation exports; более старые capture-only bridges возвращают для аннотаций `BridgeTooOld`. Ни пакет, ни bridge не поставляют, не загружают, не inject и не устанавливают RenderDoc.
- Batch ограничен 32 entries, key — 127 bytes, string — 255 UTF-8 bytes, native pool — 64 pending packets. Исчерпание budget и unavailable-состояния являются явными no-op.
- V1 scopes не должны быть вложенными и обязаны освобождаться. Они очищают свои ключи, но не могут восстановить annotation state, независимо записанный другой библиотекой.
- API-object/resource annotations, D3D11, Vulkan, Development Player, IL2CPP, Linux, mobile и Metal не поддерживаются начальным transport. Для каждого нужен отдельный real-capture gate.
- Real D3D12 `.rdc` smokes прошли на Unity `6000.4.12f1` и `6000.5.6f1` с pinned RenderDoc v1.46: аннотированный красный clear оказался между set/delete calls, а соседний синий clear выполнялся после удаления ключей. Clean external package consumer остается release gate.

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

Оверлей учитывает аллокации и ограничивает частоту обновления, но изменившиеся числовые значения и подписи графиков все еще могут создавать managed strings на интервале обновления. У него два backend-пути UI Toolkit: собственный host `UIDocument` в Unity `6000.4` и собственный host `PanelRenderer` в Unity `6000.5+`. Host сохраняет настройки panel и children чужого UI и пересоздает только container PerfMeter. Числовые значения используют стабильные зарезервированные numeric slots и numeric monospace role; `FpsOnly` использует детерминированный ограниченный двухрядный fallback, если одна строка не помещается, а карточки и полосы переносятся при узкой logical width. Это снижает риск обрезания, но не обещает поддержку любой произвольной resolution или scale; тяжелую визуальную диагностику, режимы графиков и итоговую компоновку нужно валидировать на целевых устройствах.

## Статус валидации

Текущая валидация включает автоматизированное покрытие EditMode, HDRP smoke validation в Unity `6000.4.10f1` и предыдущую smoke-валидацию Android S23 Vulkan/GLES. Более широкое покрытие player-билдов и устройств все еще полезно перед тем, как использовать данные как подтверждение готовности к релизу.

## Ограничения и приватность опциональных снимков памяти

- Функция недоступна без `com.unity.memoryprofiler` `1.1.0+` в Unity `6000.4+`; core package не устанавливает и не требует эту зависимость.
- По умолчанию разрешён только ручной capture. Триггеры system-memory threshold и bounded leak-growth требуют opt-in; каждый запрос проходит single-flight/overlap, cooldown, minimum-free-space, backend и capture-flag guards.
- Owned `.snap` staging находится в `Temp/PerfMeter/MemorySnapshots` и ограничен 512 MiB. Memory-only evidence экспортируется в `Temp/PerfMeter/CaptureBundles`; общий bundle quota — 2 GiB. Успешный export одноразовый и удаляет staging source, при этом cleanup может вернуть явное предупреждение.
- Снимок может содержать чувствительную память процесса. Защитите и проверьте его перед передачей. Bundle содержит `contains_sensitive_memory`, provenance backend/flags, `memory-snapshot.json` и SHA-256 metadata; внешний GPU artifact не создаётся.
- Удаление при OS lock и portable managed-защита от гонок с reparse points выполняются best-effort. Небезопасные или чужие paths отклоняются, а сбой cleanup остаётся видимым как warning.
- Подтверждены memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, optional compile с `com.unity.memoryprofiler@1.1.12`, а также Unity `6000.4.12f1` full EditMode `182/182` и full PlayMode `14/14`. Это не заявление о release-player или device behavior.

## Ограничения graphics diagnostics и GraphicsStateCollection

- Shader GPU-program и graphics-pipeline markers — это динамические capabilities `ProfilerRecorder`. Unity, platform, graphics API и состояние catalog refresh могут менять availability. Используйте `Unavailable`, `AvailableNoSample`, `AvailableSampled` и provenance; не делайте вывод о доступности по numeric zero.
- Marker values сохраняют обнаруженные `Unit` и `DataType` и остаются raw recorder values. Это не универсальные shader или PSO counts, и PerfMeter не переводит их в общую unit. Exact/alias resolution, resolved recorder names, resolved/sampled component counts и catalog revision входят в capability metadata.
- Опциональная сборка `SGG.PerfMeter.GraphicsStateCollection` предназначена для Unity `6000.4+`: в `6000.4` используется `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, в `6000.5+` — `UnityEngine.Rendering.GraphicsStateCollection`. Более ранние версии Unity для этой интеграции не поддерживаются.
- Trace требует active PerfMeter session. В обычном Play Mode trace frames завершаются после end-of-frame, а в batch mode используется fallback следующего кадра. Correlated session samples подчиняются warm-up, interval и max-sample settings session.
- Допускается только один graphics-state flight, включая preparation, trace finalization, prewarm и cleanup. Active external GPU capture, memory snapshot и alert-capture work также вызывают overlap rejection. `IsBusy`/`is_busy` охватывает эти flight и persisted cleanup; `HasPendingCleanup`/`has_pending_cleanup` отдельно сообщает об owned artifact, ожидающем retry. Matching cancellation best effort; cleanup failures остаются видимыми и могут задержать следующий request.
- `StopSession()` отменяет активный trace, поэтому active session нужна на всём протяжении trace. Неудачное удаление owned artifact создаёт соседний sidecar `.delete-pending`; после domain reload он восстанавливается и cleanup повторяется. Warning и busy state сохраняются, пока artifact и marker не удалены.
- Prewarm принимает только owned project-relative artifact, выполняется синхронно, сохраняет artifact и может сообщить incomplete progressive warmup. Unity backend не поддерживает cache-miss tracing: запрос возвращает `Unavailable`, cache-miss evidence не выдаётся.
- Owned `.graphicsstate` artifacts хранятся под `Temp/PerfMeter/GraphicsStateCollections`, должны быть regular non-empty files и ограничены 64 MiB. Trace ограничен 600 frames, progressive prewarm — 1 000 000 states. Действуют guards minimum-free-disk и project-local path.
- Финальные данные: Unity `6000.4.12f1` compile прошёл; targeted GSC EditMode `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208` и full post-fix PlayMode `16/16` прошли. Изолированный optional consumer compile на Unity `6000.5.6f1` также прошёл. Full Unity `6000.5` tests, release-player и target-device behavior остаются release gates и здесь не заявляются.

## Ограничения render integration context

- Public `PerfMeterRenderIntegrationSnapshot` — integration-neutral observation contract, а не глубокий Render Graph или Custom Pass capture. Read не запускает runtime; до первой observation supported current pipeline может быть `Available` с `NotObserved`, а изменение pipeline/configuration помечает прошлую observation как stale через `ObservationMatchesCurrentPipeline: false`, явные frame/age и warning.
- URP использует public current-frame `UniversalRenderingData.renderingMode` и сообщает фактически scheduled PerfMeter passes. HDRP сообщает фактический PerfMeter `CustomPass`, но effective rendering mode остаётся unavailable.
- Private/internal reflection pass/resource удалён. В legacy facade counters `registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count` и `aliased_resource_count` остаются `-1`, поскольку stable public API их не предоставляет.
- GRD activity использует public результат `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` и описывает global runtime state, а не участие GRD для конкретной camera или renderer. URP Forward+ — current-frame observation; для HDRP availability rendering mode/Forward+ остаётся `Unknown`.
- GRD effectiveness использует aggregate BRG draw-call/instance counters с точным capability provenance. Они могут включать других пользователей `BatchRendererGroup` и не доказывают GRD participation каждого renderer. Unavailable или ещё не sampled значения сериализуются как `null`.
- VRS сообщает authoritative hardware support из `SystemInfo`/`ShadingRateInfo`. Configuration и activity остаются `Unknown`, пока будущий typed adapter не сможет их доказать; этот snapshot не утверждает VRS activity.
- Unity не предоставляет stable public RenderGraph/CustomPass viewer или pass-target API, поэтому PerfMeter не добавляет Editor navigation и не обещает её.
- Capture context schema v1 сохраняет `render` и добавляет `render_integration`; session JSON/CSV schemas не изменяются. External capture context фиксируется на первом `Capturing` sample и не заменяется последующими read-операциями.
- Финальные evidence PM-REN-001: Unity `6000.4.12f1` main compile прошёл; targeted `PerformanceMeterApiTests` `53/53`, `PerfMeterCaptureBundleTests` `15/15` и `PerformanceMeterPlayModeSmokeTests` `12/12`; final full EditMode `215/215` и full PlayMode `16/16` прошли. Focused review P1/P2 resolved. Isolated compile matrix прошёл для Unity `6000.4.12f1` URP `17.4` и HDRP `17.4`, а также Unity `6000.5.6f1` URP `17.5` и HDRP `17.5`. Release-player/device validation остаются pending; release claim не делается.
- Финальные evidence PM-GRD-001: Unity `6000.4.12f1` compile прошёл; targeted API `58/58`, capture-bundle `15/15` и PlayMode smoke `12/12`; full EditMode `220/220` и PlayMode `16/16` прошли. Focused review P1/P2 resolved; compile matrix Unity `6000.4`/`6000.5` с URP `17.4`/`17.5` и HDRP `17.4`/`17.5` прошёл. Release-player/device behavior остаётся pending.
