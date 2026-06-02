# MCP E Automazione Agent

SGG PerfMeter espone metadati dei comandi per workflow Unity MCP/editor-agent nel percorso pacchetto:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

L'obiettivo e un output JSON strutturato per agent, invece di parsing di screenshot, parsing del testo dell'overlay o scraping della Unity Console.

## Gruppi Di Comandi

| Command | Scopo |
| --- | --- |
| `perfmeter.setup.status` | Legge lo stato del setup. |
| `perfmeter.setup.run` | Esegue le azioni di setup consigliate. |
| `perfmeter.runtime.status` | Legge lo stato runtime. |
| `perfmeter.runtime.ensure` | Avvia il runtime se necessario. |
| `perfmeter.runtime.stop` | Ferma il runtime. |
| `perfmeter.runtime.reset_stats` | Reimposta rolling stats, contatori alert e contatori della sessione attiva. |
| `perfmeter.runtime.mode.set` | Passa a `Stopped`, `Background`, `Overlay` o `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Legge le metriche piu recenti, incluse le custom metrics. |
| `perfmeter.alerts.latest` | Legge alert attivi, contatori e stato degli avvisi Editor. |
| `perfmeter.alerts.clear` | Cancella alert attivi, contatori e stato cooldown. |
| `perfmeter.device.info` | Legge informazioni su device, graphics, display, monitor, pipeline e ambiente Unity. |
| `perfmeter.camera.snapshot` | Legge transform/projection/impostazioni URP della camera. |
| `perfmeter.rendergraph.snapshot` | Legge l'ultima diagnostica PerfMeter Render Graph osservata. |
| `perfmeter.overlay.set` | Mostra/nasconde l'overlay e imposta preset, modules, corner, mode e target FPS. |
| `perfmeter.overdraw.start` | Avvia una misurazione overdraw limitata. |
| `perfmeter.overdraw.cancel` | Annulla la misurazione overdraw attiva. |
| `perfmeter.overdraw.heatmap.set` | Mostra o nasconde la overdraw heatmap visiva. |
| `perfmeter.session.start` | Avvia la registrazione di una sessione limitata. |
| `perfmeter.session.stop` | Ferma la registrazione e restituisce il riepilogo. |
| `perfmeter.session.summary` | Legge il riepilogo della sessione corrente. |
| `perfmeter.session.export` | Esporta la sessione corrente in JSON o CSV locale al progetto. |

## Esecuzione Di Profiling Tipica

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Usa `OverdrawDiagnostic` solo per finestre diagnostiche limitate perche numerical overdraw e rendering heatmap aggiungono lavoro GPU extra.
