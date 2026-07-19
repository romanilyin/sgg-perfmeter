# MCP と Agent Automation

SGG PerfMeter は Unity MCP/editor-agent workflows 向けに、次の package path で command metadata を公開します。

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

目的は、screenshot parsing、overlay text parsing、Unity Console scraping に頼らず、agents に structured JSON output を提供することです。

## Command Groups

| Command | 目的 |
| --- | --- |
| `perfmeter.setup.status` | setup status を読み取ります。 |
| `perfmeter.setup.run` | recommended setup actions を実行します。 |
| `perfmeter.runtime.status` | runtime status を読み取ります。 |
| `perfmeter.runtime.ensure` | 必要に応じて runtime を開始します。 |
| `perfmeter.runtime.stop` | runtime を停止します。 |
| `perfmeter.runtime.reset_stats` | rolling stats、alert counters、active session counters をリセットします。 |
| `perfmeter.runtime.mode.set` | `Stopped`、`Background`、`Overlay`、`OverdrawDiagnostic` を切り替えます。 |
| `perfmeter.metrics.latest` | custom metrics を含む latest metrics を読み取ります。 |
| `perfmeter.alerts.latest` | active alerts、counters、Editor warning state を読み取ります。 |
| `perfmeter.alerts.clear` | active alerts、counters、cooldown state をクリアします。 |
| `perfmeter.alerts.capture.begin` | 外部 capture の bounded classification を開始します。 |
| `perfmeter.alerts.capture.end` | 対応する外部 capture classification を終了します。 |
| `perfmeter.device.info` | device、graphics、display、monitor、pipeline、Unity environment info を読み取ります。 |
| `perfmeter.camera.snapshot` | camera transform/projection と URP/HDRP camera settings を読み取ります。 |
| `perfmeter.rendergraph.snapshot` | URP Render Graph または HDRP Custom Pass の最新 observed render integration diagnostics を読み取ります。 |
| `perfmeter.overlay.set` | overlay の show/hide と preset、modules、corner、mode、target FPS を設定します。 |
| `perfmeter.overdraw.start` | bounded overdraw measurement を開始します。 |
| `perfmeter.overdraw.cancel` | active overdraw measurement をキャンセルします。 |
| `perfmeter.overdraw.heatmap.set` | visual overdraw heatmap を表示または非表示にします。 |
| `perfmeter.session.start` | bounded session recording を開始します。 |
| `perfmeter.session.stop` | recording を停止して summary を返します。 |
| `perfmeter.session.summary` | current session summary を読み取ります。 |
| `perfmeter.session.export` | current session を project-local JSON または CSV に export します。 |

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

numerical overdraw と heatmap rendering は追加の GPU work を発生させるため、`OverdrawDiagnostic` は bounded URP diagnostic windows でのみ使用してください。HDRP は overdraw/heatmap を unsupported として報告しますが、その他の diagnostics は利用できます。
