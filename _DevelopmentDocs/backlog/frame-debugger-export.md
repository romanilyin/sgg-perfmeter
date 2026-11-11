# Frame Debugger Export Plan

Статус: planned, docs-only design; реализация не начата.
Roadmap IDs: `PM-FD-001..004`.
Дата актуализации: 2026-11-11.
Source baseline: `main`, commit `d5520626e9002a6fd841b4ade20e05aa87eb45c4`, package `2026.10.13-1`.

## Цель И Продуктовая Ценность

Добавить явную выгрузку событий Unity Frame Debugger для разбора состава кадра, SRP batching, render targets и изменений между двумя состояниями сцены. Основной workflow работает через package-owned Editor service/UI. Optional MCP adapter вызывает тот же service; наличие Gateway не требуется для экспорта или анализа.

Текущий session CSV содержит агрегированные samples. Отдельный снимок событий даст shader/pass/keyword/render-state context для исследования роста draw calls, дополнительных ShadowCaster/DepthNormals/Blit операций и изменений RT. Производительность оценивается отдельными timing measurements; структура снимка служит диагностическим evidence.

## Основание И Границы Проверки

- [Sumzap: Frame Debugger all events to CSV](https://tech.sumzap.co.jp/entry/frame-debugger-all-events-to-csv) описывает последовательный обход событий через internal Editor APIs, ожидание выбранного индекса и экспорт render state/shader properties. Автор проверял Unity `6000.4`/URP `17`; полный интегрируемый модуль PerfMeter в статье отсутствует.
- [Unity 6000.6: event information](https://docs.unity3d.com/6000.6/Documentation/Manual/frame-debugger-window-event-information.html) определяет свойства события, Draw Calls и SRP Batch cause. [Copy event information](https://docs.unity3d.com/6000.6/Documentation/Manual/FrameDebugger-share-event-information.html) описывает штатное копирование отдельного события/свойств.
- Статическая сверка [UnityCsReference `6000.6.8f1`](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6.8f1/Editor/Mono/PerformanceTools/FrameDebugger.bindings.cs) подтверждает наличие event metadata, `m_FrameEventIndex`, `m_DrawCallCount`, `GetFrameEventData`, `eventsHash` и признаков remote data. Возврат `false` из `GetFrameEventData` означает предыдущие данные; наличие этих членов не подтверждает работоспособность будущего адаптера в Editor.
- В baseline нет Frame Debugger exporter или MCP-команды. [Session exporter](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSessionExporter.cs) сохраняет samples; [RenderDoc analyzer DTO](../../Assets/Scripts/SGG.PerfMeter/Editor/RenderDoc/PerfMeterRenderDocAnalyzerProtocolDtos.cs) содержит action tree/counters. Этот анализатор остаётся экспериментальной инфраструктурой без public UI/API/MCP entry point, согласно [release scope](../release/2026.8.13-1-renderdoc-analyzer-release.md).

Первый verification target: Windows x64 Editor, Unity `6000.6.8f1`, URP `17.6.0`, D3D12. До real capture acceptance этот target считается planned. Package runtime floor `6000.4+` автоматически не распространяет поддержку нового Editor adapter на все версии. Remote Player/iOS/Android, HDRP и другие OS/API/Unity rows требуют отдельных реализационных решений и реальных проверок.

## Архитектурная Граница

- Предлагаемый модуль `SGG.PerfMeter.Editor.FrameDebugger` является optional Editor-only assembly. Название и public API фиксируются при implementation review. Runtime collector не получает reflection, `UnityEditor`, Gateway или RenderDoc dependencies.
- Internal type/member resolution, signature checks и version mapping находятся в одном adapter. Missing members и неподтверждённые signatures дают typed `Unsupported`/`Unavailable`; подстановка ложных нулей и повторная mutation через speculative fallback запрещены.
- Явный экспорт использует уже включённый local Frame Debugger. Первый slice не включает автоматический enable/disable, выбор remote target, continuous recording или alert-triggered capture. Открытие UI/status не изменяет Frame Debugger.
- Editor service владеет обходом и progress/cancel; UI Toolkit window отображает состояние. Unity-owned data копируются в immutable DTO на main thread; serialization, hashing и filesystem I/O выполняются worker слоем по модели `PM-CORE-001`.
- Предусмотреть отдельную session/capture diagnostic boundary для интервала работы Frame Debugger; samples из этого интервала не считать normal baseline. Если отладчик был включён до запроса, нельзя реконструировать начало паузы или приписывать ему прошлые timings без evidence.
- [Public render-integration boundary](../decisions/render-integration-context.md) сохраняется: `PM-RG-001` по-прежнему ждёт стабильных public pass/resource/aliasing/merge APIs. Frame Debugger events не заполняют эти runtime counters.

## PM-FD-001 Editor Export Foundation

Priority: P2, после текущей P1 stabilization lane. Dependencies: `PM-EXT-002`, `PM-CORE-001`, `PM-COMP-002`.

### Первый Рабочий Сценарий

1. Пользователь включает Frame Debugger для local target и явно запускает экспорт в PerfMeter.
2. Service проверяет Editor lifecycle, capability, enabled/local target и отсутствие другой собственной операции; получает shared `active-gpu`/`exclusive-profiling-operation` leases. Ручное окно Unity обходит эти leases, поэтому фактическое состояние проверяется отдельно.
3. Сохраняет target/capture observations, исходный selected limit/index и фиксирует список событий. Обход использует `ChangeFrameEventLimit(i + 1)` только через проверенный adapter.
4. После каждого переключения ждёт подтверждённые данные нужного индекса через `EditorApplication.update`. Учитывает success/readiness API и `m_FrameEventIndex`; фиксированное ожидание двух ticks само по себе недостаточно.
5. На каждом шаге проверяет enabled state, target, ожидаемую selection и доступные list/hash observations. Замена capture, ручной выбор события, Play Mode transition или потеря lease прекращают обход с явной причиной. Hash/list equality не является доказательством exact source-frame identity.
6. Per-event и общий wall-clock timeout обязательны. Неполученное событие сохраняется с собственным status/reason; bounded continuation допустимо только при сохранённой target/capture consistency. Итог публикует requested/exported/missing/truncated counts и completeness.
7. Success/cancel/error снимают update callbacks и освобождают собственные ресурсы. Selection восстанавливается только при сохранённом target/capture и совпадении текущего значения с последней собственной mutation; новый ручной выбор не перезаписывается. Domain reload не восстанавливает ownership по одному сохранённому ID.
8. Detached snapshot проходит bounded export в marker-owned project-local directory с atomic publication, hash, квотой и retention. Численные лимиты events/properties/output bytes/duration/retention фиксируются до реализации и проверяются boundary fixtures. Cleanup удаляет только собственные временные данные.

### Данные И Форматы

| Группа | Planned данные | Семантика |
| --- | --- | --- |
| Capture metadata | Schema/version, export ID, UTC time, Unity/SRP versions, graphics API, local target, доступные scene/camera/context observations, completeness | Источник каждого поля и availability сохраняются; camera/context observation не означает per-event attribution. |
| Event identity | Capture-local index, raw/resolved event type, доступное имя/scope | Индекс действует только внутри конкретной выгрузки; неизвестные типы сохраняются. |
| Shader/pass | Used/original shader, pass/light mode, sorted keyword view и raw keywords | Имя material/object и связь с asset добавляются только при разрешённой достоверной identity; shader properties не доказывают конкретный Material asset. |
| Geometry/compute | Draw calls, instances, vertices/indices, mesh/submesh при доступности, dispatch groups | Event count и draw-call count раздельны; отсутствующий BRG/ECS mesh остаётся unavailable. |
| Render target | Name, size, format, backbuffer, RT count, color/depth load/store, clear, memoryless | Только реально доступные fields/attachments; descriptor не даёт GPU allocation lifetime, aliasing или measured bandwidth. |
| Render state | Depth, raster/cull, blend, stencil, SRP batch cause | Batch cause имеет SRP-семантику; не распространяется на все виды batching. |
| Optional properties | Typed float/int/vector/texture properties с availability; дополнительные типы после adapter validation | Подробные properties выключены по умолчанию и имеют отдельные limits; texture pixels/buffer contents не экспортируются. |

Основной контракт — versioned JSON с вложенными properties и explicit availability. Компактный CSV содержит стабильные основные столбцы; optional properties CSV использует длинную форму `event -> name -> type -> value`. Тысячи property-specific колонок не становятся базовым форматом. Writer фиксирует UTF-8, invariant numeric formatting, quoting/escaping и безопасное текстовое представление значений для spreadsheet import, сохраняя raw typed значения в JSON.

Существующие session JSON/CSV и runtime public API остаются самостоятельными контрактами. New snapshot schema получает golden fixtures, unknown-field handling и bounded import. Invalid/missing numeric values остаются `null`/пустыми с reason; sampled zero сохраняется как zero.

## PM-FD-002 Rendering Summaries

Priority: P3. Dependency: `PM-FD-001`.

- Группировать по shader/pass/event type/RT; хранить ссылки на исходные события, coverage и правила включения. Суммировать draw counts только по применимым известным leaf events, без двойного учёта parent/scope rows.
- Показывать причины SRP batch breaks вместе с доступными различиями соседних keywords/pass/state. Отделять сообщение Unity от предположения анализатора; первая строка, missing data и неподходящие типы не создают выдуманную причину.
- Показывать последовательности render targets, изменения размеров/форматов и clear/load/store. Дополнительные copy/clear и большие RT помечать как наблюдения для проверки; необходимость операции определяется renderer context.
- UI оставляет raw events доступными, использует virtualized list и безопасное текстовое отображение имён. Empty/partial capture публикует coverage, а не полный итог кадра.
- GPU duration по событию, CPU cost, actual memory residency и hardware saturation не выводятся из числа событий/размеров RT. Timing требует независимого источника с собственными units/provenance.

## PM-FD-003 Semantic A/B Comparison

Priority: P3. Dependencies: `PM-FD-001`, `PM-FD-002`.

- Проверять сравнимость Unity/SRP/API, render settings, resolution и доступного scene/camera context; различия и unknown context показывать до результатов.
- Сопоставлять по event type, shader/pass/light mode, target role и доступному scope/order context. Exact matching policy версионируется; capture-local indices, transient IDs и RT names не используются как единственная межкадровая identity.
- Keywords нормализуются для сравнения наборов; raw значения сохраняются. Для повторяющихся одинаковых событий отражать ambiguity/confidence, added/removed/unmatched и partial coverage без произвольного утверждения one-to-one identity.
- Отчёт показывает подтверждённые изменения draw counts, pass inventory, keywords, RT descriptors и render state. Изменение одного event не должно превращать весь хвост sequence в фиктивные изменения.
- A/B report описывает наблюдаемую разницу. Утверждение об улучшении миллисекунд/FPS требует отдельного normal-run measurement; содержимое capture не доказывает причину stutter.

## PM-FD-004 MCP And Bundle Integration

Priority: P3. Dependencies: `PM-FD-001`, `PM-CAP-002`, `PM-CORE-003`, `PM-COMP-002`.

- Optional MCP adapter предоставляет capabilities, request, status/progress, cancel и export над тем же Editor service. Final command IDs определяются implementation review; capability/status остаются read-only, export request явно управляет selection. Host policy применяется адаптером без зависимости service от Gateway process/config/token.
- Публиковать bounded summaries и paging/references для detail data; полный properties dump не отправляется в каждый ответ автоматически. Summary/diff commands требуют готовности `PM-FD-002`/`PM-FD-003` соответственно.
- Добавить отдельное versioned diagnostic attachment и manifest descriptor с checksum, size, schema, completeness, provenance и correlation state. Текущий external GPU artifact path для `.rdc`/`.wpix` не принимает этот JSON/CSV по подменённому tool kind.
- Session ID/time/context разрешают correlation. Exact frame/capture association требует самостоятельного evidence; отсутствие доказательства записывается как unverified/unknown. Постфактум выгруженный кадр не считается ранее зарегистрированным worst frame, RenderDoc event IDs не приравниваются Frame Debugger indices.
- Имена assets/objects, paths и property values могут содержать project data. Применить существующие project-local path validation, bounded copy/hash/retention и explicit export/share policy; автоматическая загрузка и отправка содержимого не входят в workflow.
- Bundle attachment и диагностический интервал имеют additive compatibility fixtures. Текущие baseline/capture samples не дублируются и не получают подменённые timings.

## Acceptance Перед Implementation Release

| Область | Обязательная проверка |
| --- | --- |
| Adapter/readiness | Missing member/signature, `GetFrameEventData == false`, wrong index, delayed data, unknown type/field и null mesh дают явные результаты; предыдущие данные не попадают в новую строку. |
| Lifecycle/ownership | Disabled/remote target, busy lease, manual selection, changed capture/target, cancel, timeout, exception, reload и Play Mode transition завершаются без бесконечного ожидания и перезаписи чужого UI state. |
| Formats/limits | JSON/CSV fixtures, sampled zero vs unavailable, Unicode/quotes/newlines, unknown fields, oversized properties/events/output, cancel/retry, atomic export и owned cleanup; runtime session formats сохраняют прежнее поведение. |
| Summary/diff | Clear/compute/SRP batch не считаются одним draw; группировки не удваивают parent totals; insert/delete, repeated signatures и partial inputs показывают корректный scope/ambiguity. |
| Real Editor | Сверить экспорт с видимыми событиями local Frame Debugger на pinned first target: shadows, opaque/transparent, postprocess/copies, compute и BRG/GRD cases при доступности. Подтвердить event/RT/state values, отсутствие зависания и safe restore. |
| Isolation/overhead | Без модуля и без активного экспорта runtime collector/overlay работают штатно; bounded UI responsiveness и diagnostic interval проверены отдельно от normal performance evidence. |
| Automation/bundle | Работа без Gateway, thin MCP parity, request cancellation, bounded output, manifest/legacy-reader fixtures и unverified frame association покрыты тестами; imported text не исполняется. |

Эти строки задают будущие gates; ни один runtime test или real capture не считается пройденным данным docs-only изменением. HDRP, remote Player/mobile, другие версии Unity и graphics APIs получают собственные evidence rows перед support claim.

## Порядок Реализации

1. После P1 stabilization выполнить `PM-FD-001`: adapter, bounded session, JSON/CSV, ручной UI и pinned local Editor acceptance.
2. Добавить `PM-FD-002` и `PM-FD-003`: сначала сводки с raw evidence, затем semantic comparison.
3. Выполнить `PM-FD-004`: MCP и bundle attachment после стабилизации snapshot schema. Это не является зависимостью ручного экспорта.
4. Отдельно оценить remote Player/HDRP/version expansion по реальным потребностям; сохранять эти rows deferred до отдельного scope и validation.
