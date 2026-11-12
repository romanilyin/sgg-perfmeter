# План доделок SGG PerfMeter после 2026.11.12-1

Дата: 13 ноября 2026. Статус: актуальная очередь; P0 реализован для `2026.11.13-1`, базовый ручной экспорт принят. [Статус публикации](../release/2026.11.13-frame-debugger-release.md).

База: `main` / `912baec7209d48aa438442abf92246d11cfeb6a3`, опубликованный пакет `2026.11.12-1`, release commit `eee4ebd`. Полный каталог задач и архитектурные границы: [roadmap.md](roadmap.md). Этот документ задаёт актуальную очередь доделок и критерии завершения для этой базы.

## Что уже готово

- Выпущены runtime collector/overlay, custom providers, session analysis UI, typed mutation results, capture bundles, optional platform/memory/graphics integrations и native RenderDoc Editor capture в пределах их заявленных матриц.
- Октябрьские исправления `MCP-PR-240/171/195/172` реализованы и выпущены в `2026.10.7-6`. Для исходных внешних reports остаются отдельные приёмочные проверки; писать эти исправления заново не требуется.
- В `2026.11.12-1` вошли compact JSON schema 3, выбор формата через API/MCP, JSON/CSV в caller-owned streams, stripping roots bootstrap-сборок, Unity 7 capture guards и исправления ownership/lifecycle тестовых fixtures.
- Сохранённая проверка последнего функционального среза: Unity `6000.6.8f1` — 668 EditMode passed, Unity `7000.0.0a16` — 665 passed; по одному skipped, ноль failed; stream tests — 16/16 на каждой версии. Это Editor evidence, которое не закрывает приёмку исходного Player workload.
- GitHub/npm publication, подписи, provenance и соответствие 426 файлов тегу проверены. Эти результаты относятся к опубликованному `2026.11.12-1`.
- `PM-FD-001` реализован в отдельном Editor-модуле: ручной UI, bounded service, JSON/CSV, optional shader properties, manifest SHA-256 и diagnostic interval. Компиляция нового кода чистая; новые автоматические тесты добавлены, но по указанию пользователя не запускались. Результат ручной приёмки и границы поддержки: [checkpoint](frame-debugger-export.md#текущее-состояние-реализации).

## Решение по текущей итерации

По указанию пользователя от 13 ноября **P0 и первая задача — `PM-FD-001`, ручной экспорт Frame Debugger**. P0 здесь означает выбранную первоочередную фичу; наличие нового блокирующего дефекта этим назначением не утверждается.

Оба прежних P1 — внешняя приёмка октябрьских reports и включение реального CI — **сейчас не выполняются**. Они сохранены ниже как отложенные задачи и не являются предварительным условием Frame Debugger.

Custom metrics уже работали, их сбор и экспорт реализованы. Пункт `PM-OBS-006` относился к историческим жалобам на отдельные Player-сборки; текущая поломка не установлена. Дополнительные проверки custom metrics и optional packages выполняются при новом воспроизводимом случае или конкретном расширении поддержки, без обязательного общего повторного прогона.

## Приоритеты и порядок

| Очередь | Приоритет | Доделка | Критерий готовности |
| --- | --- | --- | --- |
| 1 | **P0** | Ручной Frame Debugger export: `PM-FD-001`, реализован локально | Standalone Editor UI/service сохраняет snapshot JSON/CSV с проверкой свежести событий, limits, cancel/partial status и безопасным восстановлением selection. Базовый ручной экспорт 3/3 принят; широкий URP workload остаётся без qualification. |
| 2 | P2 | Standalone Unity Profiler recording: `PM-PROF-001` | Без Gateway работают capability/start/stop/save, bounded `.data` artifact, ownership и связь с PerfMeter session. |
| 3 | P2 | PIX 2610.28: `PM-PIX-002` | Закреплены tool/SDK identity и реальные generic GPU capture результаты; feasibility timing/API/CLI оформлена отдельным решением. |
| 4 | P3 | Frame Debugger analysis: `PM-FD-002 → 003 → 004` | Сводки, semantic A/B diff, затем optional MCP/bundle attachment после `PM-FD-001`. |
| 5 | P3, условно P2 | Стоимость больших session exports | Сначала установлено превышение согласованного memory/latency бюджета; затем при необходимости меняется serializer/async API с сохранением форматов. |
| 6 | P4 | RenderDoc analysis: `PM-RDA-004 → PM-RDUI-001 → PM-RDRULE-001` | Проверяемая агрегация counters, затем UI, затем evidence-based rules; qualification по API/GPU отдельно. |
| 7 | P4 | Nsight, начиная с `PM-NSYS-001` | Один ограниченный external launcher с finalized artifact; остальные интеграции отдельными задачами. |

Условные проверки `PM-OBS-006` и compatibility/optional packages не входят в обязательную очередь перед P0. Возврат обоих отложенных P1 требует отдельного решения пользователя. Unity imports и verification одного проекта имеют одного владельца.

## 1. P0: `PM-FD-001`, ручной Frame Debugger snapshot

Первая задача текущей итерации по решению пользователя. Отложенные P1 и условные проверки выше не являются её prerequisites.

Первый target: Windows x64 Editor, Unity `6000.6.8f1`, URP `17.6.0`, D3D12. Точный adapter, bounded session, JSON/CSV и ручной UI реализованы. Меню: **SGG > Perfmeter > Frame Debugger Export**; файлы: `Temp/PerfMeter/FrameDebuggerExports/<export-id>/`. [Инструкция](../../docs/ru/frame-debugger-export.md). Базовый ручной экспорт 3/3 принят; широкий URP workload остаётся без qualification.

Минимальная готовность: проверка нужного event index/readiness, stale/previous-data rejection, timeout/cancel/reload, явные partial rows и unknown fields, ограниченные events/properties/output, atomic publication и ownership-safe selection restore. Runtime collector сохраняет public-API boundary и диагностический интервал отделяется от normal performance samples. Сервис работает без Gateway; MCP и bundle attachment идут после него.

Польза первого slice — конкретный состав кадра и raw evidence о passes/RT/state/batching. Draw-call count, число events, Render Graph passes и GPU duration имеют отдельную семантику. Полный контракт: [Frame Debugger Export Plan](frame-debugger-export.md).

## 2. P2: `PM-PROF-001`, Unity Profiler start/stop/save

Service/UI пока не реализованы. Первый slice: проверить точные Editor API capabilities и fake-backend ownership/state machine; затем local Editor recording, bounded worker publication `.data`, standalone UI и session marker correlation. Opening/status остаются read-only. Нужны safe restore, cancel/reload, limits и честные unavailable результаты при недоступном API.

Готовность подтверждается настоящим start/stop/save на закреплённой строке и чистым consuming project без Gateway. Optional MCP вызывает тот же сервис после стабилизации standalone workflow. Deep Profile, remote Player и дополнительные версии расширяют scope отдельно. Полный контракт: [Standalone Unity Profiler Recording Plan](unity-profiler-recording.md).

## 3. P2: PIX 2610.28

`PM-PIX-002` начать с повторной проверки официальных contracts и фактически установленного tool/SDK. Закрепить executable/header/runtime identity, Windows x64/D3D12 Editor/Development Player и capability results. Доказать существующий generic GPU capture и открываемый artifact.

После этого отдельным решением оценить documented API/CLI и timing/circular lifecycle: bounds, end/flush/discard/cancel, conflict ownership и finalized artifact provenance. `PM-PIX-001` остаётся P4 до design и real-tool acceptance; наличие API в документации не делает native timing реализованным. См. [PIX scope](2026-10-29-custom-metrics-and-pix2610-28.md#pm-pix-002--windowsd3d12-и-pix-261028).

## 4–7. Последующие функции

| Направление | Порядок и условия |
| --- | --- |
| Frame Debugger analysis, P3 | `PM-FD-002`: сводки с raw evidence и coverage → `PM-FD-003`: semantic A/B diff с ambiguous/unmatched events → `PM-FD-004`: thin MCP и отдельный versioned bundle attachment. Перестановка строк сама по себе не является изменением кадра. |
| Большие session exports, P3 | На одном представительном наборе сначала измерить string/buffer peak memory, latency и влияние на UI. Stream API уже существует, но строит полную строку. Настоящий incremental writer и/или additive async API добавлять при установленной потребности, сохраняя Full/Compact/CSV semantics, Unicode, unavailable/NaN, cancel и atomic publication. Поднять в P2 при воспроизводимом превышении согласованного бюджета. |
| RenderDoc analysis, P4 | Существующие protocol/replay/counter-selection foundations сохранить. Начать с `PM-RDA-004`: явные sum/min/max/weighted/ratio/non-aggregatable semantics → `PM-RDUI-001`: virtualized importer/timeline → `PM-RDRULE-001`: rules с units, availability и provenance. Self-hosted real-tool matrix `PM-RDPROD-001` оформляется отдельно; ранние локальные smokes не заменяют её. |
| Nsight, P4 | `PM-NSYS-001` external launcher → `PM-NSYS-002` NVTX и `PM-NSYS-003` official analysis/export → отдельный `PM-NSYS-004` GPU Metrics. Graphics и Perf SDK имеют свои последующие capability/validation задачи. У каждого инструмента собственный контракт; device-level counters не объявлять process-attributed. |

## Условный P2: разбор нового сбоя custom metrics в Player

Custom metrics уже работали и остаются реализованной возможностью. Существуют registration cache invalidation, retained custom rows, Full/Compact JSON и stream export. Этот пункт не означает, что их требуется заново реализовать или что текущий релиз сломан.

`PM-OBS-006` сохраняет связь с историческими reports от 28–29 октября: в отдельных Player-сборках наблюдались пустые public/export массивы. С тех пор выпущено сохранение bootstrap assemblies при stripping. Отдельный сквозной результат каждого исторического build не следует из Editor-тестов, однако отсутствие такого результата не доказывает текущий дефект.

Возвращаться к разбору при новом воспроизводимом исчезновении метрик либо конкретном требовании квалифицировать новую integration/Player конфигурацию. Сначала проверить уже имеющиеся результаты. Public read может возвращать cached collection; `TryCollect=false` допускает отсутствие строки, exception даёт unavailable row.

Если разбор действительно требуется, снять одну связанную цепочку:

`точные package/build hashes → исполнение installers → зарегистрированные provider IDs → collection/cache generation → public rows → retained rows → Full/Compact JSON bytes`.

Проверить sampled zero, registered-but-unavailable, отсутствующий provider, exception и Reset/Stop/scene lifecycle. Assembly roots каждого auto-registering integration проверяются у владельца интеграции; core stripping fix не доказывает сохранность всех сторонних installers. CSV остаётся built-in-only; custom-series acceptance проводится по JSON. Stream/gzip output проверяется после завершения compressor, с сохранением caller ownership и отказом от публикации partial output.

Условная задача завершена, когда установлен первый разрыв либо подтверждена вся цепочка. При отсутствии нового случая отдельный повторный прогон не назначается. Исправлять доказанный дефект у его владельца; пустой `custom_metrics=[]` сам по себе не доказывает ошибку serializer. См. [связанные Player reports](2026-10-29-custom-metrics-and-pix2610-28.md#pm-obs-006--custom-metrics-в-development-player) и [integration guidance](../../docs/en/workflows.md).

## Условный P2: конкретные строки поддержки optional packages

Речь о дополнительном покрытии определённых конфигураций. Общая совместимость и сами optional integrations уже реализованы; текущая неисправность этим пунктом не установлена. Обязательная широкая перепроверка релиза не назначается.

Конкретные примеры того, что могли бы проверять при отдельной потребности:

| Интеграция | Уже имеющееся основание | Дополнительная проверка при расширении поддержки |
| --- | --- | --- |
| Adaptive Performance | Optional assembly компилировалась с `5.1.6`, provider/session contracts проверялись. | На конкретном устройстве с соответствующим provider получить реальные thermal state и CPU/GPU performance levels, проверить unavailable/freshness и экспорт. |
| Memory Profiler | Backend/threshold/bundle tests и compile с реальным package `1.1.12` существовали. | Проверить настоящий memory snapshot в выбранном поддержанном Player/device режиме, доступность, сохранение, cleanup и exported artifact. |
| GraphicsStateCollection | Есть trace/prewarm/lifecycle tests и optional consumer compile. | На выбранной Unity/API/Player конфигурации проверить запись trace, warm-up и поддерживаемые capabilities. |

Эти примеры не означают, что перечисленные операции сейчас не работают. Перед запуском нужно установить конкретную недостающую строку и проверить более поздние результаты: старый ADR фиксирует только охват своего прохода. При отсутствии нового target или воспроизводимого сбоя пункт остаётся справочным и не задерживает `PM-FD-001`.

- Разделить import/compile floor `2021.3`, supported core/runtime `6000.4+` и URP/HDRP integration `17.4+`. При изменении shared code выбирать связанные consumers; полностью повторять историческую матрицу только при соответствующей причине.
- Отдельно фиксировать Editor, обычный Development Player, IL2CPP, non-development Player и Dedicated Server. Unity 7 alpha `7000.0.0a16` остаётся проверкой конкретной alpha, а не обещанием stable support.
- Для Adaptive Performance, Memory Profiler и GraphicsStateCollection сохранить отдельные real-package/device/lifecycle результаты. Успешный optional compile не закрывает thermal/device behavior, snapshot или prewarm acceptance.
- Existing RenderDoc native support ограничен ранее проверенными Windows x64 Editor rows. Unity 7 external GPU capture guards требуют отдельного настоящего Player capture перед расширением support claim.
- Синхронизировать текущие статусы документации: import floor в compatibility ADR, уже выпущенный Session Analysis UI в profiler backlog, выпущенные MCP fixes. Исторические counts сохранять как датированные результаты.

Если задача активирована, результат — короткая таблица с exact versions, выполненной операцией, результатом, ссылкой на evidence и явными unsupported/unverified полями. Тестовые receipts `668/665`, `16/16` и старые capture/device результаты повторно использовать только для совпадающих входов.

## Отложенный P1: внешняя приёмка исходных reports

Статус: сейчас не выполняется по решению пользователя. Исправления уже находятся в исходниках; ниже сохранён scope для отдельного возврата к этой задаче. Frame Debugger её завершения не ожидает.

| Report / scope | Ограниченная следующая проверка | Когда задача закрывается |
| --- | --- | --- |
| `MCP-PR-240`, owned orphan cleanup | Повторить исходную admission-ситуацию после suite/перед Play; проверить hidden/inactive и scene-bound infrastructure, сохранение чужого UI и честный `PendingCleanup`. Legacy objects не определять только по имени. | Обе исходные жалобы проверены; собственные объекты освобождаются, чужие сохраняются, pending cleanup не выдаёт ложный успех. |
| `MCP-PR-171`, target-FPS/IL2CPP | В Player проверить Resources target 60 → явный override 120, budget 8.33 ms после bootstrap, scene transition и перестройки UI. | Есть runtime/frame receipt с requested/effective target и budget, а не только успешный build. |
| `MCP-PR-195`, export paths | На установленном Gateway выполнить один describe/hash/call проход: relative JSON/CSV, разрешённый project-local absolute path, outside/traversal/sibling/malformed path, wrong type и existing-file refusal. | Отказы имеют правильный код/recovery, валидный экспорт читается, bytes уже существующего файла сохранены. |
| `MCP-PR-172`, exact sampler | На исходных Sky/RTGI D3D12 passes проверить фактический sampler, отсутствующий SSMS case, disable/re-enable и retained JSON той же session. | IDs, availability, причины unavailable и delayed aggregates согласованы. Caller сохраняет recording ownership; точный source GPU frame из `ProfilingSampler` не обещается. |
| Исходный self-overhead case, `PM-OBS-005` | Зафиксировать renderer/camera/quality и свежие dormant/active окна; для active — минимум 120 measurement frames, для dormant корректны нулевые callbacks/measurement frames. Проверить session/epoch containment и transitions. | Есть результат исходного workload. Сам foundation `PM-OBS-005` остаётся resolved; GPU attribution сохраняет `Unavailable`, пока нет отдельного доказательства. |

Перед Player-проверкой разобрать сохранённые timeout/log/native observations двух прошлых IL2CPP запусков. Если первый runtime receipt снова не появляется, остановить сценарий и получить ограниченное native hang/thread-stack наблюдение; многократные слепые relaunch не использовать. Исходная строка `6000.5.9f1` и текущая `6000.6.8f1` имеют отдельный статус: результат одной версии не закрывает другую автоматически. Матрицу конкретного прогона фиксировать до запуска.

Основание: [release 2026.10.7-6 и оставшиеся gates](../release/2026.10.7-mcpreports-release.md), [исторический triage](2026-10-07-open-reports-triage.md). Runtime ownership/recovery находится в `Runtime/PerfMeterRuntime.cs`, target-FPS — в `PerformanceMeter.cs`/`PerfMeterSettings.cs`, policy diagnostics — в `Editor/Mcp/PerfMeterMcpCommands.cs`.

## Отложенный P1: сделать CI выполняющимся gate

Статус: сейчас не выполняется по решению пользователя. Настройка credentials, запуск CI и изменение required checks не входят в текущую итерацию и не блокируют P0.

`PM-CI-002` уже имеет [workflow](../../.github/workflows/performance-ci.yml); его выполнение зависит от `PERFMETER_UNITY_CI_ENABLED` и Unity credentials. Наблюдавшиеся GitHub runs пропущены; npm publishing при этом работает независимо.

1. Выбрать поддержанный runner и одну закреплённую Unity/SRP конфигурацию. Через существующий безопасный механизм настроить необходимые credentials; не включать сразу непроверенную широкую матрицу.
2. Получить один настоящий PlayMode lifecycle и EditMode performance run с обязательным непустым XML, корректными counts и выгрузкой результатов при failure.
3. После стабильного исполнения выбрать required checks для PR. Изменение branch rules — отдельное действие; docs-only PR не должен провоцировать ненужный Unity full suite.
4. Затем добавить необходимые версии по матрице. Hosted Linux/Core lifecycle не объявлять доказательством Windows/D3D12/native capture.

Готово, когда обязательный тестовый run завершён и его failure/timeout/missing-artifact сценарии дают отказ. Новый общий test harness или дополнительные polling agents для этой задачи не нужны.

## Что сознательно отложено

- `PM-ANDROID-001` Perfetto/AGI и `PM-APPLE-001` thermal/MetricKit/Metal: сохраняется ранее заданный Deferred; это отдельно от приёмки уже существующих optional providers.
- `PM-HDRP-001` overdraw/heatmap parity: отдельный research/prototype/API-device acceptance. Основные HDRP diagnostics уже существуют.
- `PM-RG-001`: ждать стабильных public Unity APIs для pass/resource/aliasing/merge counters; сохранять degraded значения.
- Remote streaming/OpenTelemetry, Aftermath crash evidence, CUDA-only Nsight Compute, XR/world-space overlay: отдельное продуктовое решение и конкретный target.
- Built-in Render Pipeline остаётся вне планов. Нативный replay внутри Unity, bundled/injected profiling tools и automatic upload captures не входят в эту очередь.

## Как выполнять план

Первый исполнительский пакет — `PM-FD-001`: adapter, bounded export, JSON/CSV, ручной UI и приёмка на закреплённом local Editor target. Оба P1 сейчас не выполняются; условные Player/optional-package проверки не подставляются вместо них и не блокируют Frame Debugger. Далее — `PM-PROF-001` и отдельная итерация PIX; большие native/Nsight задачи следуют после этих результатов.

Каждая задача получает свой короткий scope/PR и заранее определённый результат. Для runtime изменений: один compile refresh → точные targeted consumers → focused review → один финальный full EditMode. PlayMode/Player/device/real-tool checks выбираются по изменённому lifecycle и заявляемому support scope. Для docs-only достаточно link/status review и `git diff --check`; Unity full suite не требуется.

Закрывать раздельно четыре факта: код реализован, автоматические проверки пройдены, исходный workload принят, конкретная строка поддержки квалифицирована. Новый цикл проверки нужен при изменившихся входах, текущем failure или недостающем результате; существующие immutable receipts сохраняются и переиспользуются.

Основание текущего состояния: [релиз 2026.11.12-1](../release/2026.11.12-session-export-release.md), [полный roadmap](roadmap.md), [октябрьский triage](2026-10-07-open-reports-triage.md), [Player/PIX backlog](2026-10-29-custom-metrics-and-pix2610-28.md). Внешние tool/SDK contracts перепроверяются при начале соответствующей задачи; этот план не добавляет новых support claims.
