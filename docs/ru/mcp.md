# MCP И Agent Automation

SGG PerfMeter содержит command metadata для Unity MCP/editor-agent workflows по package path:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

Цель - structured JSON output для агентов вместо парсинга screenshots, overlay text или Unity Console.

## Command Groups

| Command | Purpose |
| --- | --- |
| `perfmeter.setup.status` | Прочитать setup status. |
| `perfmeter.setup.run` | Запустить recommended setup actions. |
| `perfmeter.runtime.status` | Прочитать runtime status. |
| `perfmeter.runtime.ensure` | Запустить runtime при необходимости. |
| `perfmeter.runtime.stop` | Остановить runtime. |
| `perfmeter.runtime.reset_stats` | Сбросить rolling stats, alert counters и active session counters. |
| `perfmeter.runtime.mode.set` | Переключить `Stopped`, `Background`, `Overlay` или `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Прочитать latest metrics, включая custom metrics. |
| `perfmeter.alerts.latest` | Прочитать active alerts, counters и Editor warning state. |
| `perfmeter.alerts.clear` | Очистить active alerts, counters и cooldown state. |
| `perfmeter.device.info` | Прочитать device, graphics, display, monitor, pipeline и Unity environment info. |
| `perfmeter.camera.snapshot` | Прочитать camera transform/projection/URP settings. |
| `perfmeter.rendergraph.snapshot` | Прочитать latest observed PerfMeter Render Graph diagnostics. |
| `perfmeter.overlay.set` | Показать/скрыть overlay и задать preset, modules, corner, mode и target FPS. |
| `perfmeter.overdraw.start` | Запустить bounded overdraw measurement. |
| `perfmeter.overdraw.cancel` | Отменить active overdraw measurement. |
| `perfmeter.overdraw.heatmap.set` | Показать или скрыть visual overdraw heatmap. |
| `perfmeter.session.start` | Запустить bounded session recording. |
| `perfmeter.session.stop` | Остановить recording и вернуть summary. |
| `perfmeter.session.summary` | Прочитать current session summary. |
| `perfmeter.session.export` | Экспортировать current session в project-local JSON или CSV. |

## Типичный Profiling Run

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Используйте `OverdrawDiagnostic` только для bounded diagnostic windows, потому что numerical overdraw и heatmap rendering добавляют extra GPU work.
