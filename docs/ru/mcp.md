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
| `perfmeter.alerts.latest` | Прочитать активные alerts/оповещения, счетчики и состояние Editor warnings. |
| `perfmeter.alerts.clear` | Очистить активные alerts/оповещения, счетчики и состояние cooldown. |
| `perfmeter.alerts.capture.begin` | Начать ограниченную классификацию внешнего capture. |
| `perfmeter.alerts.capture.end` | Завершить соответствующую классификацию внешнего capture. |
| `perfmeter.device.info` | Прочитать информацию об устройстве, graphics, display, monitor, pipeline и Unity environment. |
| `perfmeter.camera.snapshot` | Прочитать transform/projection камеры и настройки URP/HDRP camera. |
| `perfmeter.rendergraph.snapshot` | Прочитать последние наблюдаемые diagnostics render integration для URP Render Graph или HDRP Custom Pass. |
| `perfmeter.overlay.set` | Показать/скрыть оверлей и задать preset, modules, corner, mode и целевой FPS. |
| `perfmeter.overdraw.start` | Запустить ограниченное измерение overdraw. |
| `perfmeter.overdraw.cancel` | Отменить активное измерение overdraw. |
| `perfmeter.overdraw.heatmap.set` | Показать или скрыть визуальную heatmap overdraw. |
| `perfmeter.session.start` | Запустить ограниченную запись сессии. |
| `perfmeter.session.stop` | Остановить запись и вернуть summary. |
| `perfmeter.session.summary` | Прочитать summary текущей сессии. |
| `perfmeter.session.export` | Экспортировать текущую сессию в project-local JSON или CSV. |
| `perfmeter.capture.request` | Запросить ограниченный внешний GPU capture и correlated bundle. |
| `perfmeter.capture.status` | Прочитать состояние capture и bundle. |
| `perfmeter.capture.cancel` | Отменить matching active capture. |
| `perfmeter.capture.export` | Атомарно экспортировать ready bundle в project-local bundle root. |
| `perfmeter.capture.capabilities` | Прочитать schema, quota, retention, screenshot и provenance capabilities. |

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
