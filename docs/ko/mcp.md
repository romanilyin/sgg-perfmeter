# MCP 및 Agent Automation

SGG PerfMeter는 Unity MCP/editor-agent workflow를 위한 command metadata를 package path 아래에 노출합니다.

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

목표는 screenshot parsing, overlay text parsing, Unity Console scraping 대신 Agent가 사용할 수 있는 structured JSON output을 제공하는 것입니다.

## Command Groups

| Command | 목적 |
| --- | --- |
| `perfmeter.setup.status` | setup status를 읽습니다. |
| `perfmeter.setup.run` | 권장 setup action을 실행합니다. |
| `perfmeter.runtime.status` | runtime status를 읽습니다. |
| `perfmeter.runtime.ensure` | 필요한 경우 runtime을 시작합니다. |
| `perfmeter.runtime.stop` | runtime을 중지합니다. |
| `perfmeter.runtime.reset_stats` | rolling stats, alert counters, active session counters를 reset합니다. |
| `perfmeter.runtime.mode.set` | `Stopped`, `Background`, `Overlay`, `OverdrawDiagnostic` 중 하나로 전환합니다. |
| `perfmeter.metrics.latest` | custom metrics를 포함한 latest metrics를 읽습니다. |
| `perfmeter.alerts.latest` | active alerts, counters, Editor warning state를 읽습니다. |
| `perfmeter.alerts.clear` | active alerts, counters, cooldown state를 지웁니다. |
| `perfmeter.device.info` | device, graphics, display, monitor, pipeline, Unity environment info를 읽습니다. |
| `perfmeter.camera.snapshot` | Read camera transform/projection and URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Read latest observed PerfMeter render integration diagnostics for URP Render Graph or HDRP Custom Pass. |
| `perfmeter.overlay.set` | overlay 표시/숨김 및 preset, modules, corner, mode, target FPS를 설정합니다. |
| `perfmeter.overdraw.start` | bounded overdraw measurement를 시작합니다. |
| `perfmeter.overdraw.cancel` | active overdraw measurement를 취소합니다. |
| `perfmeter.overdraw.heatmap.set` | visual overdraw heatmap을 표시하거나 숨깁니다. |
| `perfmeter.session.start` | bounded session recording을 시작합니다. |
| `perfmeter.session.stop` | recording을 중지하고 summary를 반환합니다. |
| `perfmeter.session.summary` | current session summary를 읽습니다. |
| `perfmeter.session.export` | current session을 project-local JSON 또는 CSV로 export합니다. |

## 일반적인 Profiling Run

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Numerical overdraw와 heatmap rendering은 추가 GPU work를 만들기 때문에 `OverdrawDiagnostic`은 bounded diagnostic window에만 사용합니다.
