# MCP Und Agent-Automation

SGG PerfMeter stellt Command-Metadaten fuer Unity MCP/editor-agent workflows bereit unter:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

Ziel ist strukturierte JSON-Ausgabe fuer Agents statt Screenshot-, Overlay-Text- oder Unity-Console-Parsing.

## Command Groups

| Command | Zweck |
| --- | --- |
| `perfmeter.setup.status` | Setup-Status lesen. |
| `perfmeter.setup.run` | Empfohlene Setup-Aktionen ausfuehren. |
| `perfmeter.runtime.status` | Runtime-Status lesen. |
| `perfmeter.runtime.ensure` | Runtime starten, falls noetig. |
| `perfmeter.runtime.stop` | Runtime stoppen. |
| `perfmeter.runtime.reset_stats` | Rolling stats, alert counters und aktive Session-Counter zuruecksetzen. |
| `perfmeter.runtime.mode.set` | `Stopped`, `Background`, `Overlay` oder `OverdrawDiagnostic` setzen. |
| `perfmeter.metrics.latest` | Latest metrics inklusive custom metrics lesen. |
| `perfmeter.alerts.latest` | Aktive alerts, counters und Editor warning state lesen. |
| `perfmeter.alerts.clear` | Aktive alerts, counters und cooldown state loeschen. |
| `perfmeter.device.info` | Device, graphics, display, monitor, pipeline und Unity environment info lesen. |
| `perfmeter.camera.snapshot` | Kamera transform/projection und URP/HDRP camera settings lesen. |
| `perfmeter.rendergraph.snapshot` | Zuletzt beobachtete PerfMeter render integration diagnostics fuer URP Render Graph oder HDRP Custom Pass lesen. |
| `perfmeter.overlay.set` | Overlay anzeigen/verbergen und preset, modules, corner, mode und target FPS setzen. |
| `perfmeter.overdraw.start` | Begrenzte overdraw measurement starten. |
| `perfmeter.overdraw.cancel` | Aktive overdraw measurement abbrechen. |
| `perfmeter.overdraw.heatmap.set` | Visual overdraw heatmap anzeigen oder verbergen. |
| `perfmeter.session.start` | Begrenzte Session-Aufzeichnung starten. |
| `perfmeter.session.stop` | Aufzeichnung stoppen und summary zurueckgeben. |
| `perfmeter.session.summary` | Aktuelle Session-summary lesen. |
| `perfmeter.session.export` | Aktuelle Session als projektlokales JSON oder CSV exportieren. |

## Typischer Profiling-Run

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Nutze `OverdrawDiagnostic` nur fuer begrenzte URP-Diagnosefenster, weil numerical overdraw und heatmap rendering zusaetzliche GPU-Arbeit erzeugen. HDRP meldet overdraw/heatmap als unsupported, waehrend die restlichen diagnostics verfuegbar bleiben.
