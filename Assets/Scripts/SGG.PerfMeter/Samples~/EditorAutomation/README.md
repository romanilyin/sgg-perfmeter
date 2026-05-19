# Editor and MCP Automation

This sample demonstrates setup automation without opening the setup window.

Import the sample, then use the menu items under `SGG/PerfMeter Samples`:

- `Print Setup Status` logs Frame Timing Stats, JSON settings, and renderer feature state.
- `Run Recommended Setup` enables Frame Timing Stats, installs editable renderer features, and creates default JSON settings.
- `Apply JSON Settings To Runtime` reloads project JSON settings into the Play Mode runtime.

Typical MCP command sequence for an agent-driven profiling run:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Use `OverdrawDiagnostic` only for bounded diagnostic windows because numerical overdraw and heatmap rendering add extra GPU work.
