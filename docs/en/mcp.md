# MCP And Agent Automation

SGG PerfMeter exposes command metadata for Unity MCP/editor-agent workflows under the package path:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

The goal is structured JSON output for agents instead of screenshot parsing, overlay text parsing, or Unity Console scraping.

## Command Groups

| Command | Purpose |
| --- | --- |
| `perfmeter.setup.status` | Read setup status. |
| `perfmeter.setup.run` | Run recommended setup actions. |
| `perfmeter.runtime.status` | Read runtime status. |
| `perfmeter.runtime.ensure` | Start runtime if needed. |
| `perfmeter.runtime.stop` | Stop runtime. |
| `perfmeter.runtime.reset_stats` | Reset rolling stats, alert counters, and active session counters. |
| `perfmeter.runtime.mode.set` | Switch `Stopped`, `Background`, `Overlay`, or `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Read latest metrics, including custom metrics. |
| `perfmeter.alerts.latest` | Read active alerts, counters, and Editor warning state. |
| `perfmeter.alerts.clear` | Clear active alerts, counters, and cooldown state. |
| `perfmeter.device.info` | Read device, graphics, display, monitor, pipeline, and Unity environment info. |
| `perfmeter.camera.snapshot` | Read camera transform/projection and URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Read latest observed PerfMeter render integration diagnostics for URP Render Graph or HDRP Custom Pass. |
| `perfmeter.overlay.set` | Show/hide overlay and set preset, modules, corner, mode, and target FPS. |
| `perfmeter.overdraw.start` | Start bounded overdraw measurement. |
| `perfmeter.overdraw.cancel` | Cancel active overdraw measurement. |
| `perfmeter.overdraw.heatmap.set` | Show or hide visual overdraw heatmap. |
| `perfmeter.session.start` | Start bounded session recording. |
| `perfmeter.session.stop` | Stop recording and return summary. |
| `perfmeter.session.summary` | Read current session summary. |
| `perfmeter.session.export` | Export current session to project-local JSON or CSV. |

## Typical Profiling Run

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Use `OverdrawDiagnostic` only for bounded URP diagnostic windows because numerical overdraw and heatmap rendering add extra GPU work. HDRP reports overdraw and heatmap as unsupported while the rest of the diagnostics stay available.
