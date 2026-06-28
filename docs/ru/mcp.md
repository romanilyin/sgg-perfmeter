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
| `perfmeter.runtime.status` | Прочитать статус во время выполнения. |
| `perfmeter.runtime.ensure` | Запустить PerfMeter во время выполнения при необходимости. |
| `perfmeter.runtime.stop` | Остановить PerfMeter. |
| `perfmeter.runtime.reset_stats` | Сбросить rolling stats, счетчики alerts/оповещений и счетчики активной сессии. |
| `perfmeter.runtime.mode.set` | Переключить `Stopped`, `Background`, `Overlay` или `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Прочитать latest metrics, включая пользовательские метрики. |
| `perfmeter.alerts.latest` | Прочитать активные alerts/оповещения, счетчики и состояние Editor warnings. |
| `perfmeter.alerts.clear` | Очистить активные alerts/оповещения, счетчики и состояние cooldown. |
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

Используйте `OverdrawDiagnostic` только для ограниченных URP диагностических окон, потому что числовой overdraw и рендеринг heatmap добавляют дополнительную работу GPU. HDRP возвращает unsupported для overdraw/heatmap, но остальные diagnostics остаются доступны.
