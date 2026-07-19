# MCP 和 Agent 自动化

SGG PerfMeter 在 package path 下为 Unity MCP/editor-agent workflows 暴露 command metadata：

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

目标是为 agents 提供结构化 JSON 输出，避免依赖 screenshot parsing、overlay text parsing 或 Unity Console scraping。

## Command Groups

| Command | 用途 |
| --- | --- |
| `perfmeter.setup.status` | 读取 setup status。 |
| `perfmeter.setup.run` | 运行 recommended setup actions。 |
| `perfmeter.runtime.status` | 读取 runtime status。 |
| `perfmeter.runtime.ensure` | 在需要时启动 runtime。 |
| `perfmeter.runtime.stop` | 停止 runtime。 |
| `perfmeter.runtime.reset_stats` | 重置 rolling stats、alert counters 和 active session counters。 |
| `perfmeter.runtime.mode.set` | 切换 `Stopped`、`Background`、`Overlay` 或 `OverdrawDiagnostic`。 |
| `perfmeter.metrics.latest` | 读取 latest metrics，包括 custom metrics。 |
| `perfmeter.alerts.latest` | 读取 active alerts、counters 和 Editor warning state。 |
| `perfmeter.alerts.clear` | 清除 active alerts、counters 和 cooldown state。 |
| `perfmeter.alerts.capture.begin` | 开始外部 capture 的 bounded classification。 |
| `perfmeter.alerts.capture.end` | 结束对应的外部 capture classification。 |
| `perfmeter.device.info` | 读取 device、graphics、display、monitor、pipeline 和 Unity environment info。 |
| `perfmeter.camera.snapshot` | 读取 camera transform/projection 和 URP/HDRP camera settings。 |
| `perfmeter.rendergraph.snapshot` | 读取 URP Render Graph 或 HDRP Custom Pass 的最新 observed render integration diagnostics。 |
| `perfmeter.overlay.set` | 显示/隐藏 overlay，并设置 preset、modules、corner、mode 和 target FPS。 |
| `perfmeter.overdraw.start` | 启动有边界的 overdraw measurement。 |
| `perfmeter.overdraw.cancel` | 取消 active overdraw measurement。 |
| `perfmeter.overdraw.heatmap.set` | 显示或隐藏 visual overdraw heatmap。 |
| `perfmeter.session.start` | 启动有边界的 session recording。 |
| `perfmeter.session.stop` | 停止 recording 并返回 summary。 |
| `perfmeter.session.summary` | 读取当前 session summary。 |
| `perfmeter.session.export` | 将当前 session 导出到项目本地 JSON 或 CSV。 |

## 典型 Profiling Run

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

仅在有边界的 URP diagnostic windows 中使用 `OverdrawDiagnostic`，因为 numerical overdraw 和 heatmap rendering 会增加额外 GPU work。HDRP 会将 overdraw/heatmap 报告为 unsupported，但其他 diagnostics 仍可用。
