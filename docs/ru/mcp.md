# MCP и автоматизация агентов

SGG PerfMeter содержит метаданные команд для сценариев Unity MCP и editor-agent по пути пакета:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

Цель - структурированный JSON-вывод для агентов вместо парсинга скриншотов, текста оверлея или Unity Console.

## Группы команд

| Команда | Назначение |
| --- | --- |
| `perfmeter.setup.status` | Прочитать статус настройки. |
| `perfmeter.setup.run` | Запустить действия рекомендованной настройки. |
| `perfmeter.runtime.status` | Прочитать runtime-статус. |
| `perfmeter.runtime.ensure` | Запустить runtime при необходимости. |
| `perfmeter.runtime.stop` | Остановить runtime. |
| `perfmeter.runtime.reset_stats` | Сбросить rolling stats, счетчики alerts и счетчики активной сессии. |
| `perfmeter.runtime.mode.set` | Переключить `Stopped`, `Background`, `Overlay` или `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Прочитать latest metrics, включая пользовательские метрики. |
| `perfmeter.alerts.latest` | Прочитать активные alerts, счетчики и состояние Editor warnings. |
| `perfmeter.alerts.clear` | Очистить активные alerts, счетчики и состояние cooldown. |
| `perfmeter.device.info` | Прочитать информацию об устройстве, graphics, display, monitor, pipeline и Unity environment. |
| `perfmeter.camera.snapshot` | Прочитать transform/projection/URP settings камеры. |
| `perfmeter.rendergraph.snapshot` | Прочитать последнюю наблюдаемую диагностику PerfMeter Render Graph. |
| `perfmeter.overlay.set` | Показать/скрыть оверлей и задать preset, modules, corner, mode и target FPS. |
| `perfmeter.overdraw.start` | Запустить ограниченное измерение overdraw. |
| `perfmeter.overdraw.cancel` | Отменить активное измерение overdraw. |
| `perfmeter.overdraw.heatmap.set` | Показать или скрыть визуальную heatmap overdraw. |
| `perfmeter.session.start` | Запустить ограниченную запись сессии. |
| `perfmeter.session.stop` | Остановить запись и вернуть summary. |
| `perfmeter.session.summary` | Прочитать summary текущей сессии. |
| `perfmeter.session.export` | Экспортировать текущую сессию в project-local JSON или CSV. |

## Типичный прогон профилирования

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Используйте `OverdrawDiagnostic` только для ограниченных диагностических окон, потому что числовой overdraw и рендеринг heatmap добавляют дополнительную работу GPU.
