# MCP и автоматизация агентов

SGG PerfMeter содержит метаданные команд для сценариев Unity MCP и editor-agent в файле пакета:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

Цель - структурированный JSON-вывод для агентов вместо парсинга скриншотов, текста оверлея или Unity Console.

## Группы команд

| Команда | Назначение |
| --- | --- |
| `perfmeter.setup.status` | Прочитать статус настройки. |
| `perfmeter.setup.run` | Запустить действия рекомендованной настройки. |
| `perfmeter.compatibility.status` | Прочитать раздельные import, core runtime и active render-integration compatibility states. |
| `perfmeter.runtime.status` | Прочитать статус во время выполнения. |
| `perfmeter.runtime.ensure` | Запустить PerfMeter во время выполнения при необходимости. |
| `perfmeter.runtime.stop` | Остановить PerfMeter. |
| `perfmeter.runtime.reset_stats` | Сбросить rolling stats, счетчики alerts/оповещений и счетчики активной сессии. |
| `perfmeter.runtime.mode.set` | Переключить `Stopped`, `Background`, `Overlay` или `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Прочитать latest metrics, включая пользовательские метрики. |
| `perfmeter.profiler.capabilities` | Прочитать кэшированные capabilities и provenance Profiler-метрик без запуска runtime и discovery. |
| `perfmeter.profiler.lease.capabilities` | Прочитать process-local ресурсы profiler lease и семантику reload. |
| `perfmeter.profiler.lease.status` | Прочитать текущее или соответствующее process-local состояние profiler lease. |
| `perfmeter.alerts.latest` | Прочитать активные alerts/оповещения, счетчики и состояние Editor warnings. |
| `perfmeter.alerts.clear` | Очистить активные alerts/оповещения, счетчики и состояние cooldown. |
| `perfmeter.alerts.capture.begin` | Начать ограниченную классификацию внешнего capture. |
| `perfmeter.alerts.capture.end` | Завершить соответствующую классификацию внешнего capture. |
| `perfmeter.device.info` | Прочитать информацию об устройстве, graphics, display, monitor, pipeline и Unity environment. |
| `perfmeter.camera.snapshot` | Прочитать transform/projection камеры и настройки URP/HDRP camera. |
| `perfmeter.rendergraph.snapshot` | Прочитать последние наблюдаемые diagnostics render integration для URP Render Graph или HDRP Custom Pass. |
| `perfmeter.render.snapshot` | Прочитать neutral render integration snapshot: freshness, camera/pass context, GRD/VRS и legacy Render Graph facade. |
| `perfmeter.overlay.set` | Показать/скрыть оверлей и задать preset, modules, corner, mode и целевой FPS. |
| `perfmeter.overdraw.start` | Запустить ограниченное измерение overdraw. |
| `perfmeter.overdraw.cancel` | Отменить активное измерение overdraw. |
| `perfmeter.overdraw.heatmap.set` | Показать или скрыть визуальную heatmap overdraw. |
| `perfmeter.session.start` | Запустить ограниченную запись сессии. |
| `perfmeter.session.stop` | Остановить запись и вернуть summary. |
| `perfmeter.session.summary` | Прочитать summary текущей сессии. |
| `perfmeter.session.export` | Экспортировать текущую сессию в project-local JSON или CSV. |
| `perfmeter.capture.request` | Запросить bounded GPU capture; optional `backend_mode`: `GenericUnity`, `NativePreferred` или `NativeRequired`. Native storage mode выбирается только через C# API. |
| `perfmeter.capture.status` | Прочитать состояние capture и bundle. |
| `perfmeter.capture.cancel` | Отменить matching active capture. |
| `perfmeter.capture.export` | Атомарно экспортировать ready bundle в project-local bundle root. |
| `perfmeter.capture.export.request` | Поставить single-flight export в очередь и вернуть export ID и прогресс. |
| `perfmeter.capture.export.status` | Прочитать фазу, прогресс, отмену, retry и authority артефакта. |
| `perfmeter.capture.export.cancel` | Запросить отмену соответствующего активного export. |
| `perfmeter.capture.capabilities` | Прочитать schema, quota, retention, screenshot и provenance capabilities. |

Предпочитайте `perfmeter.capture.export.request`, затем опрашивайте `perfmeter.capture.export.status` и при необходимости вызывайте `perfmeter.capture.export.cancel`. Legacy-команда `perfmeter.capture.export` блокируется для совместимости. Ответы включают универсальный envelope `external_artifact` с association, authority, finalization, content, политикой privacy/share, размером, а также source- и post-copy-хешами. Read-only команды lease предоставляют process-local состояние конфликтов без получения lease.

## Self-overhead в runtime status

`perfmeter.runtime.status` содержит additive-объект `self_overhead`; это не отдельная команда. Верхний уровень: `state`, `cpu_timing_available`, `gpu_timing_availability` и `has_budget_violation`.

Объекты компонентов: `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration` и `hdrp_render_integration`. Каждый содержит `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state` и `allocation_budget_state`.

Значения описывают фиксированные окна CPU callbacks по 120 кадров со средними на один вызов. GPU attribution имеет значение `Unavailable`; неактивная render integration — `Unsupported`, а поддерживаемый компонент без вызовов — `NotMeasured`. Схемы session JSON/CSV не меняются, существующие CPU/GPU-метрики не корректируются.

## Типичный прогон профилирования

```text
perfmeter.profiler.capabilities {}
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Используйте `OverdrawDiagnostic` только для ограниченных URP диагностических окон, потому что числовой overdraw и рендеринг heatmap добавляют дополнительную работу GPU. HDRP возвращает unsupported для overdraw/heatmap, но остальные diagnostics остаются доступны.

## Команды снимков памяти

| Команда | Назначение и основные входы |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Запросить ручной снимок с `capture_id`, необязательными boolean-флагами захвата, `minimum_free_disk_mb` и `cooldown_seconds`. |
| `perfmeter.memory.snapshot.status` | Прочитать состояние снимка и связанного bundle без запуска runtime и без раскрытия временного source path. |
| `perfmeter.memory.snapshot.capabilities` | Прочитать provenance backend, поддерживаемые flags, лимит снимка 512 MiB и принадлежащий PerfMeter temporary root. |
| `perfmeter.memory.snapshot.triggers.configure` | Явно включить или выключить триггеры system-memory threshold и bounded leak-growth, их frame window, flags, free-space guard и cooldown. |

Команды запроса и настройки trigger требуют Play Mode. Automation по умолчанию выключена. Типичная последовательность:

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Ожидайте export-ready состояния, затем используйте существующую команду `perfmeter.capture.export`. Memory-only bundle содержит `requested_tool: MemoryProfiler`, `memory-snapshot.json` и provenance в manifest, но не внешний GPU artifact. Успешный export выполняется один раз и удаляет принадлежащий staging source.

## Команды диагностики графики и GraphicsStateCollection

Следующие шесть команд образуют поверхность PM-GFX-001:

| Команда | Назначение и основные входы |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Прочитать последние shader GPU-program и graphics-pipeline marker values, динамическую capability provenance, catalog revision и graphics API context. Входов нет. |
| `perfmeter.graphics.state_collection.request` | Запустить bounded trace. Требуются Play Mode и активная PerfMeter session; обязателен `capture_id`, `trace_frames` — 1–600 (по умолчанию 60), `minimum_free_disk_mb` по умолчанию 1024. |
| `perfmeter.graphics.state_collection.status` | Прочитать availability, state, progress, backend identity, counts, `is_busy`, `has_pending_cleanup`, warnings и project-relative path owned artifact. Входов нет. |
| `perfmeter.graphics.state_collection.capabilities` | Прочитать backend provenance, trace/prewarm support, cache-miss и parallel-PSO support, session requirement, лимиты 600 frames и 64 MiB и owned artifact root. Входов нет. |
| `perfmeter.graphics.state_collection.cancel` | Отменить совпадающий active или preparing trace и очистить pending artifact. Требуется `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Загрузить и синхронно prewarm один owned project-relative artifact в Play Mode. Обязателен `relative_path`; `max_state_count` — 0–1 000 000, по умолчанию 0. |

`perfmeter.graphics.diagnostics` возвращает `shader_gpu_program_creation_value` и `graphics_pipeline_creation_value`, а также для каждой capability `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count` и `sampled_component_count`. `perfmeter.metrics.latest` и session exports предоставляют те же marker metadata. Значения сохраняют discovered recorder unit и не являются универсальными shader или PSO counts; используйте `sample_state`, а не трактуйте ноль как unavailable.

Ответ state command содержит `result`, `availability`, `state`, `capture_id`, requested/completed trace frames, backend ID/version, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup` и `warning`. `is_busy` остаётся true во время подготовки, trace, завершения, prewarm, cleanup или persisted cleanup; `has_pending_cleanup` указывает на owned artifact, ожидающий retry. Неудачное удаление сохраняется через owned sidecar `.delete-pending`, который восстанавливается и повторно обрабатывается после domain reload. `StopSession` отменяет активный trace, поэтому session должна оставаться active до завершения. Trace переходит в terminal state после tick запрошенных frames в конце кадра; batch mode использует fallback на следующий кадр. Samples, принятые активной session, получают `graphics_state_trace_id`, равный `capture_id`.

Типичная последовательность trace и prewarm:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

Допускается только один graphics-state flight. Повторный active ID возвращает `AlreadyActive`, а другой overlapping trace/prewarm — `RejectedOverlap`. Cancel работает только для matching active/preparing ID. Unity backend сообщает `supports_cache_miss_tracing: false`: cache-miss evidence не поддерживается, и MCP prewarm schema не содержит такого input. Artifacts принадлежат PerfMeter, находятся под `Temp/PerfMeter/GraphicsStateCollections` и ограничены 64 MiB.

## Snapshot интеграции рендеринга

`perfmeter.render.snapshot {}` — read-only команда без inputs. Она не запускает runtime. Ответ содержит `schema_version: 1` и `render_integration` с current pipeline/source, observation frame и age, `observation_matches_current_pipeline`, observed camera identity, integration/pass/injection metadata, scheduled PerfMeter pass count, effective rendering mode (если доступен), вложенные `gpu_resident_drawer` и `variable_rate_shading`, а также `legacy_render_graph`.

`gpu_resident_drawer` содержит project/compute support, public global activity с `activity_source`, URP Forward+/clustered compatibility, `degraded_reason` и вложенный BRG `effectiveness`. Значения равны `null`, пока capability не имеет `AvailableSampled`; recorder names, exact/alias resolution и component counts сохраняют provenance. `scope: "brg_aggregate"` не доказывает использование GRD каждым renderer.

Это MCP-эквивалент `PerformanceMeter.GetRenderIntegrationSnapshot()` и `TryGetRenderIntegrationSnapshot(...)`. Stale observation отмечается явным non-match и warning, а не выдаётся за current. `perfmeter.rendergraph.snapshot` сохраняется для legacy facade. Команда не добавляет Editor navigation: стабильные Unity API не раскрывают RenderGraph/CustomPass viewer или pass targets.
