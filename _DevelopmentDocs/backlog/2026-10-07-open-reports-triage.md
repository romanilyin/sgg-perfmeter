# PerfMeter: разбор открытых репортов и план стабилизации

Дата: `2026-10-07`.
База: опубликованный `2026.10.7-1`, release commit `d4bf372`; исследованный `main f0f31fb` отличается от тега только документацией.
Источник: `C:\Work\Unity\!alex\perfmeter-open-reports-2026-10-07.md` — семь исходных репортов, объединённых в шесть проблем.

Это initial triage для baseline `2026.10.7-1`, не итоговый список закрытий. Реализованные increments, новые Editor/GPU gates и остающиеся Player/Gateway/original-workload проверки фиксируются в [общем release record](../release/2026.10.7-mcpreports-release.md).

## Границы выводов

- Выполнено сопоставление репортов с текущими исходниками, тестами, документацией и историей изменений. Новое воспроизведение в исходном consuming project, Gateway или IL2CPP Player не проводилось.
- Release matrix подтверждает по 557 EditMode и 20 PlayMode tests на пяти runtime-версиях; она не закрывает конкретные внешние GPU/IL2CPP/teardown сценарии.
- Приоритеты ниже относятся к предложенному плану PerfMeter. Внешние Gateway ledger/status и исторические `PM-MCP-001..007` не изменяются.
- Закрывать исходные репорты следует только после проверок их acceptance, с точной версией пакета и артефактами. Наличие подходящего API или похожего теста для закрытия недостаточно.

## Решения по шести проблемам

| Проблема | Вывод для `2026.10.7-1` | Делать | Приоритет |
| --- | --- | --- | --- |
| `MCP-PR-240`: orphan infrastructure | Актуальный пробел: при отсутствующем singleton `StopRunning()` не ищет оставшиеся owned roots. Очистка дубликатов при запуске уже есть. | Безопасный stopped-runtime cleanup и проверка реального post-test состояния. | P1, первым: свежий повторяемый блокер соседних workflows. |
| `MCP-PR-171`: runtime target FPS | Порядок setter/bootstrap остаётся неоднозначным. Поздний Resources bootstrap может перезаписать ранний `SetTargetFps`; полный explicit JSON уже имеет приоритет. Причина всех трёх Player-вариантов ещё требует воспроизведения. | Детерминированный контракт частичного override и настоящий IL2CPP Player regression. | P1. |
| `MCP-PR-195`: export path policy | Подтверждён кодом: containment failure выдаётся как `schema_validation_failed`. | Package-owned typed policy error, recovery и уточнение descriptor. | P1, небольшой независимый fix. |
| `MCP-PR-172`: GPU sampler ownership | Generic custom provider есть; специализированного exact-sampler adapter нет. Named-recorder путь из consuming project нельзя автоматически приписать пакету. | Ограниченный optional URP adapter поверх существующего provider contract. | P1, отдельная функциональная итерация после core fixes. |
| Custom GPU series отсутствуют в export | Custom JSON export уже реализован до репорта. Историческое выпадение конкретных series не опровергнуто. | End-to-end проверка той же сессии; чинить только найденный участок. | P2 verification; P1 при подтверждённой потере retained данных. |
| Self-overhead: `PassNotEnqueued` | Активный overlay/providers не доказывает enqueue. Текущее состояние может быть штатным; объяснение конкретной причины недостаточно точное. | Уточнение diagnostics и реальный dormant/active integration test. | P2. |

P0 по имеющимся свидетельствам не назначается: нет подтверждённой новой потери безопасности, массового crash или блокера уже завершённого release pass.

## P1: ближайшая стабилизация

### 1. Owned cleanup при Stopped и потерянном singleton — MCP-PR-240

**Почему делать:** два свежих репорта `problem_20261006T100355_d0a94a6200e9` и `problem_20261007T060029_11e127a70151` описывают объекты при Stopped/Idle и блокировку pre-Play admission. `TryStop()` определяет результат по singleton, а `StopRunning()` в null-ветке возвращается без поиска объектов.

**Что уже есть:** re-election в `OnEnable`, pruning дубликатов в `EnsureRunning`, очистка orphan overlay children при восстановлении overlay, disposal известного UI host. Повторно проектировать panel host или runtime ownership целиком не требуется. Эти механизмы уже присутствовали в `2026.9.6-2`, указанном в свежем репорте.

**Доработка:**

- Добавить package-owned recovery path для transient runtime/overlay/UI-host объектов при отсутствующей или потерянной ссылке. Discovery выполнять в явном cleanup/lifecycle действии, не на каждом кадре.
- Удалять только доказуемо owned объекты: ownership/type, transient/persistence, отношения owner/child и состояние операций. Одного совпадения имени недостаточно; чужой `PanelRenderer`/`UIDocument`, authored/persistent объекты и чужие UI trees сохраняются.
- `TryStop` должен возвращать правдивый результат: cleaned/applied либо явный pending/orphan/unsafe-to-clean state. Отсутствующий singleton сам по себе не доказывает отсутствие pending ресурсов.
- Сохранить shutdown/finalization контракт capture, memory и graphics operations; занятые native ресурсы не уничтожать ради пустого hierarchy.
- Teardown должен использовать тот же owned cleanup и утверждать отсутствие инфраструктуры после tests.

**Acceptance:** normal Stop, повторный Stop, потерянный singleton, disabled runtime, lost overlay reference, post-domain-reload/Play transition и post-EditMode suite. При Stopped/Idle нет owned transient roots, включая invalid/unloaded-scene/DontSave объекты. Проверять через enumeration, видящую inactive/hidden объекты, а не только `GameObject.Find`. При pending operations публиковать отказ/ожидание; foreign UI остаётся неизменным. Исходный pre-Play/native admission проходит без ручного удаления ID и без ослабления ownership guards.

**Код:** [PerformanceMeter.cs:579](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerformanceMeter.cs#L579), [PerfMeterRuntime.cs:442–535](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterRuntime.cs#L442), [PerfMeterRuntime.cs:2371–2457](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterRuntime.cs#L2371), [PerfMeterOverlayPanelHost.cs:63](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterOverlayPanelHost.cs#L63).

### 2. Детерминированный target-FPS override — MCP-PR-171

**Почему делать:** `SetTargetFps()` не подавляет последующий Resources bootstrap. `AfterSceneLoad` bootstrap применяет snapshot и снова вызывает setter. Перезапись раннего вызова возможна и без IL2CPP; конкретное поведение persistent driver из репорта этим review не объяснено полностью.

**Что уже есть:** setter обновляет runtime budget, alert rules и overlay; успешный `TryApplySettingsJson` делает полный explicit snapshot авторитетным и подавляет Resources auto-start. Есть Editor setter/explicit-JSON tests и обычный PlayMode budget test; исходный IL2CPP startup/override сценарий они не покрывают.

**Доработка:**

- Определить precedence для Resources defaults, частичных runtime overrides и явного повторного применения settings/preset. Ранний явный override должен переживать автоматический bootstrap, не теряя остальные Resources настройки.
- По итогам воспроизведения выбрать небольшой механизм: tracked per-field override либо гарантированный этап завершения bootstrap. Callback без определения precedence сам по себе гонку не закрывает.
- Рассмотреть additive `TrySetTargetFps` с requested/effective result и причиной rejection; существующий `void SetTargetFps` сохранить как compatibility wrapper. Не считать silent no-op успешным применением.
- Согласовать sample bootstrap и user docs с новым контрактом. Текущий sample вызывает setter в `Awake`, раньше `AfterSceneLoad` Resources callback.

**Acceptance:** Unity `6000.5.9f1`, Windows x64/D3D12 IL2CPP Development Player, Resources target=60 и runtime target=120. До/после auto-bootstrap, после scene transition и visual-tree rebuild status, CPU/GPU budget, graph thresholds и default alerts используют `8.33 ms`. Custom providers остаются зарегистрированными. Проверить также поздний override, explicit JSON и отключённый auto-start. JSON перед build не переписывать ради прохождения проверки. Target FPS здесь — диагностический бюджет; управление VSync/`Application.targetFrameRate` не подменяет этот контракт.

**Код:** [PerformanceMeter.cs:533–551](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerformanceMeter.cs#L533), [PerformanceMeter.cs:1174](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerformanceMeter.cs#L1174), [PerfMeterSettings.cs:657–691](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSettings.cs#L657), [PerfMeterSettings.cs:1252–1279](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSettings.cs#L1252), [PerfMeterMinimalBootstrap.cs:13](../../Assets/Scripts/SGG.PerfMeter/Samples~/BootstrapAndSettings/PerfMeterMinimalBootstrap.cs#L13).

### 3. Стабильный отказ export path policy — MCP-PR-195

**Почему делать:** `ResolveProjectLocalPath` явно выбрасывает `schema_validation_failed` при выходе за project root. Поэтому типовая ошибка применения policy воспринимается как устаревший describe/schema.

**Уточнение репорта:** absolute path внутри проекта сейчас принимается. Запрещён выход за project root; нельзя объявлять все absolute paths недопустимыми задним числом.

**Доработка:**

- Стабильный package-owned failure code, например `path_policy_violation`, и понятный recovery с новым project-relative destination. Детали точного result envelope согласовать с существующим MCP контрактом.
- Descriptor должен явно описывать containment, допускаемый формат и поведение in-project absolute. Сохранить существующую совместимость, если отдельная security причина не требует её изменения.
- Ошибки типа аргумента остаются schema errors; корректный string вне policy получает policy error. Existing-file conflict сохраняет `file_exists` и исходные bytes.
- Сейчас единственный consumer этого resolver — session export. Если при доработке resolver станет общим, проверить всех добавленных consumers и сохранить их semantics.

**Acceptance:** через реальный Gateway describe/call проверить актуальные descriptor/hash и результат для absolute outside, traversal, sibling-prefix и malformed path. Successful relative JSON/CSV создаёт ожидаемые bytes. Неверный тип остаётся schema error; unsafe export автоматически не повторяется. Containment/security ограничения сохраняются.

**Код:** [PerfMeterMcpCommands.cs:556–593](../../Assets/Scripts/SGG.PerfMeter/Editor/Mcp/PerfMeterMcpCommands.cs#L556), [PerfMeterMcpCommands.cs:2051–2062](../../Assets/Scripts/SGG.PerfMeter/Editor/Mcp/PerfMeterMcpCommands.cs#L2051), [mcp.commands.json:881](../../Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json#L881).

### 4. Exact-sampler GPU metrics adapter — MCP-PR-172

**Почему делать:** полезный first-class contract для RenderGraph timing отсутствует. Общий `IPerfMeterCustomMetricProvider` уже позволяет consuming project хранить нужный sampler; package helper сократит ошибки владения, availability и freshness.

**Доработка:**

- Optional URP adapter/provider поверх существующего registry. Принимать точный `ProfilingSampler`, который передаётся `AddComputePass`/`AddRasterRenderPass`, с stable metric ID; не создавать новый sampler по совпадению имени.
- Определить recording/lifetime ownership, включение/освобождение recording и взаимодействие с другим владельцем sampler. Избежать blanket enable для всех проходов.
- Различать unsupported named lookup, отсутствие свидетельств исполнения pass, warm-up/no-samples, stale/delayed GPU result и sampled zero. Сам по себе `sampleCount=0` не доказывает отсутствие pass; конкретный reason требует evidence от producer.
- GPU results поступают с задержкой: явно определить freshness и reset на disable/re-enable. Не добавлять блокирующее GPU ожидание.
- Добавить sample с compute/raster passes и guidance по FTUE: renderer installation count подтверждает установку feature, а не наличие произвольных GPU timings. Переписывать уже корректный смысл FTUE не требуется.

**Acceptance:** исходный workload или минимальный аналог на Unity `6000.5.9f1`, Windows D3D12. После warm-up работающие pass instances имеют `gpuSampleCount>0` и положительный elapsed time; отсутствующий/неисполненный SSMS остаётся unavailable с объяснением. Валидное sampled `0` сохраняет available state. Проверить delayed results, отключение/re-enable, несколько камер и отсутствие stale samples. Если заявляется поддержка adapter в IL2CPP Player, этот путь получает отдельный Player gate.

**Код:** [PerfMeterSnapshots.cs:474–499](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSnapshots.cs#L474), [PerfMeterRenderGraphFeature.cs:269–319](../../Assets/Scripts/SGG.PerfMeter/Runtime/URP/PerfMeterRenderGraphFeature.cs#L269). GPU annotations/RenderDoc scopes имеют другой контракт и timing provider не заменяют.

## P2: адресная проверка и точность диагностики

### 5. Custom series: provider → retained session → JSON bytes

**Устарела постановка «добавить custom JSON export»:** `samples[].custom_metrics[]` уже содержит `id/name/category/unit/value/available/warning`. Реализация появилась в `566f215` от `2026-05-19`, до внешнего репорта. Runtime передаёт одну collection в overlay и session recorder; recorder копирует все reported metrics, независимо от лимита отображаемых графиков.

**Что остаётся делать:**

- Уточнить фактический формат исходного и нового artifact: JSON должен содержать custom series; для текущего CSV их отсутствие соответствует ограниченному контракту.
- Проверить исходные четыре `sgg.sky.*` IDs через provider → `GetSessionSamples()` → MCP artifact UTF-8 bytes, сопоставляя точный `session_id`, frame/time и raw value.
- Учитывать session warm-up/interval, retention, capture/baseline separation и `TryCollect=false`. Session samples и полная покадровая overlay history имеют разные интервалы.
- Для ожидаемой series отсутствие measurement желательно возвращать как `TryCollect=true` + unavailable snapshot с причиной; `TryCollect=false` означает отсутствие metric в текущей collection.
- Подтверждённое присутствие metric в retained sample при отсутствии в JSON повышает задачу до P1 data-loss fix. Если metric пропадает раньше — чинить producer/collection/sampling участок, не serializer без доказательств.
- Per-series presence summary добавлять только при доказанной диагностической необходимости, additively. CSV сейчас custom series не обещает; новая CSV-схема для закрытия этого репорта не требуется.

**Acceptance:** 300 retained baseline samples с рабочими series и явно unavailable SSMS; остановить session, сохранив runtime для export. Проверить successful envelope и реальные bytes, доступный ноль, provider exception/false, buffer reuse и отсутствие переноса из предыдущей сессии. Не требовать совпадения всей overlay history с прореженной сессией.

**Код:** [PerfMeterRuntime.cs:677–716](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterRuntime.cs#L677), [PerfMeterSessionRecorder.cs:205–245](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSessionRecorder.cs#L205), [PerfMeterSessionExporter.cs:750–805](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSessionExporter.cs#L750).

### 6. Self-overhead: объяснять фактически наблюдаемое enqueue

**Неподтверждённая часть репорта:** активный overlay и 92 custom metrics не доказывают исполнение package-owned URP pass. `RecordOverlayMarkerPass` по умолчанию выключен; при выключенных marker/overdraw/heatmap installed+enabled feature штатно не enqueue-ит pass. Overlay и custom providers учитываются собственными scopes.

**Что уже есть:** exact session/capture identity, epoch, callback/frame bounds, frozen windows, `PassNotEnqueued` и `NoCameraCallbackObserved`. Повторно реализовывать capture-bound self-observability не требуется.

**Доработка:**

- Warning должен сообщать «package enqueue не наблюдался в этом window», не утверждать конкретную конфигурационную причину без runtime evidence.
- Если нужен точный gate reason, получать его из реального `AddRenderPasses`, отдельно от installation/configuration probe. Учитывать renderer, camera и quality/pipeline selection.
- Добавить настоящий dormant/active URP RenderGraph regression, а не только synthetic вызовы evidence API.

**Acceptance:** две новые session/capture windows. Dormant: feature installed+enabled, marker/overdraw/heatmap off, overlay/providers работают → `enqueue_count=0`, `NotMeasured/PassNotEnqueued`, понятное объяснение. Active: marker включён либо реально активна диагностика → enqueue и `RecordRenderGraph` invocation counts положительны, bounds принадлежат нужному identity/epoch. Ready требует `window_frame_count >= 120`, отсчитываемых от первого measurement callback; 120 кадров от старта session/capture недостаточно. Для завершённого active window при неизменном pipeline ожидать `inactive_reason=None` и `measurement_contained=true`. Enqueue без callback → `NoCameraCallbackObserved`. Новое окно не наследует прошлые counts; GPU self-attribution остаётся Unavailable.

**Код:** [PerfMeterRenderGraphFeature.cs:41–59](../../Assets/Scripts/SGG.PerfMeter/Runtime/URP/PerfMeterRenderGraphFeature.cs#L41), [PerfMeterSelfObservability.cs:505–544](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSelfObservability.cs#L505), [PerfMeterSelfObservability.cs:731–799](../../Assets/Scripts/SGG.PerfMeter/Runtime/PerfMeterSelfObservability.cs#L731).

## Что не включать в доработки

- Переписывание Resources JSON перед build или повышение default FPS как «закрытие» runtime override. Default уже 240 FPS; explicit project settings сохранены.
- Подавление всех Resources настроек из одного `SetTargetFps`: частичный override должен сохранять остальные параметры bootstrap.
- Повторная реализация custom JSON exporter, metric registry, capture-bound windows или owned UI host.
- Гарантия произвольного `Recorder.Get(name)`, reflection по private RenderGraph структурам, фиктивные нули вместо unavailable, либо GPU annotations как источник per-pass времени.
- Автоматическое включение marker/overdraw только ради исчезновения warning; это меняет измеряемый workload.
- Подмена self-overhead whole-frame GPU timing или вычитание overhead из обычных метрик.
- Разрешение out-of-project export, изменение Gateway schema ради обхода package policy, автоматический retry unsafe команды.
- Очистка всех объектов по имени, удаление чужого UI, перенос package-owned cleanup в Gateway, ослабление native admission guards.
- Закрытие внешних репортов только по сегодняшней Editor compatibility matrix. Переоткрытие уже resolved `PM-MCP-001..007` без нового связанного evidence также не требуется.

## Порядок реализации

1. **Короткий preflight:** зафиксировать `2026.10.7-1` в исходном consuming project; собирать version/session/renderer/Player provenance, sanitized результаты и exact acceptance сценарии. Raw/private данные остаются локальными.
2. **Core stabilization:** MCP-PR-240 → MCP-PR-171. MCP-PR-195 можно делать независимо отдельным небольшим PR. Эти исправления не зависят от нового GPU adapter.
3. **GPU correctness slice:** MCP-PR-172, sample и реальный D3D12 gate. Синтетическую same-session export regression из пункта 5 можно добавить раньше; внешний GPU closure выполнить вместе с adapter/workload проверкой.
4. **Diagnostics slice:** закончить пункт 5 по полученным данным и пункт 6 с реальным enqueue, без перепроектирования готовых foundations.
5. **Проверка и closure:** compile → targeted tests изменённых классов и всех consumers общих validators/lifecycle → focused review → full EditMode и PlayMode на основной версии. Отдельно обязательны исходный `6000.5.9f1` D3D12/IL2CPP Player, GPU-pass и post-test/Gateway acceptance gates. Проверить нижнюю runtime границу `6000.4` при изменениях URP/UI APIs. Полную десятиверсионную матрицу повторять при compatibility/API изменениях или отдельном запросе, а не для каждого small fix.

Работу лучше делить на четыре focused PR: owned cleanup; settings/Player override; MCP path diagnostics; GPU adapter с sampling/diagnostic regressions. У каждого PR свои acceptance evidence; финальный общий release pass выполняется после integration review.

## Условия завершения плана

- У каждого исходного report ID есть точная package version и проверка исходного expected behavior; у MCP-PR-240 сохранены оба report ID.
- Изменённые contracts остаются additive, native/path security сохраняется, warmed per-frame collection не получает новых allocations или filesystem work.
- Артефакты проверяются по содержимому/identity; command success, FTUE count и видимый график используются только в пределах их собственного контракта.
- Внутренние roadmap/ledger и внешний ownership ledger синхронизируются после подтверждения результатов, отдельным явным действием; этот triage их закрытие не заявляет.
