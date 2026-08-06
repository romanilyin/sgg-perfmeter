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
| `perfmeter.compatibility.status` | import, core runtime, active render integration compatibility를 각각 읽습니다. |
| `perfmeter.runtime.status` | runtime status를 읽습니다. |
| `perfmeter.runtime.ensure` | 필요한 경우 runtime을 시작합니다. |
| `perfmeter.runtime.stop` | runtime을 중지합니다. |
| `perfmeter.runtime.reset_stats` | rolling stats, alert counters, active session counters를 reset합니다. |
| `perfmeter.runtime.mode.set` | `Stopped`, `Background`, `Overlay`, `OverdrawDiagnostic` 중 하나로 전환합니다. |
| `perfmeter.metrics.latest` | custom metrics를 포함한 latest metrics를 읽습니다. |
| `perfmeter.profiler.capabilities` | cache된 Profiler metric capabilities와 provenance를 읽으며 runtime이나 discovery를 시작하지 않습니다. |
| `perfmeter.alerts.latest` | active alerts, counters, Editor warning state를 읽습니다. |
| `perfmeter.alerts.clear` | active alerts, counters, cooldown state를 지웁니다. |
| `perfmeter.alerts.capture.begin` | 외부 capture의 bounded classification을 시작합니다. |
| `perfmeter.alerts.capture.end` | 일치하는 외부 capture classification을 종료합니다. |
| `perfmeter.device.info` | device, graphics, display, monitor, pipeline, Unity environment info를 읽습니다. |
| `perfmeter.camera.snapshot` | camera transform/projection 및 URP/HDRP camera settings를 읽습니다. |
| `perfmeter.rendergraph.snapshot` | URP Render Graph 또는 HDRP Custom Pass의 최신 observed render integration diagnostics를 읽습니다. |
| `perfmeter.overlay.set` | overlay 표시/숨김 및 preset, modules, corner, mode, target FPS를 설정합니다. |
| `perfmeter.overdraw.start` | bounded overdraw measurement를 시작합니다. |
| `perfmeter.overdraw.cancel` | active overdraw measurement를 취소합니다. |
| `perfmeter.overdraw.heatmap.set` | visual overdraw heatmap을 표시하거나 숨깁니다. |
| `perfmeter.session.start` | bounded session recording을 시작합니다. |
| `perfmeter.session.stop` | recording을 중지하고 summary를 반환합니다. |
| `perfmeter.session.summary` | current session summary를 읽습니다. |
| `perfmeter.session.export` | current session을 project-local JSON 또는 CSV로 export합니다. |
| `perfmeter.capture.request` | bounded external GPU capture와 correlated bundle을 request합니다. |
| `perfmeter.capture.status` | capture와 bundle state를 읽습니다. |
| `perfmeter.capture.cancel` | 일치하는 active capture를 cancel합니다. |
| `perfmeter.capture.export` | ready bundle을 project-local root 아래에 atomic export합니다. |
| `perfmeter.capture.capabilities` | schema, quota, retention, screenshot, provenance capabilities를 읽습니다. |

## Runtime Self-Overhead Payload

`perfmeter.runtime.status`는 additive `self_overhead` object를 포함하며 별도 command가 아닙니다. Top-level key는 `state`, `cpu_timing_available`, `gpu_timing_availability`, `has_budget_violation`입니다.

Component object는 `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration`, `hdrp_render_integration`입니다. 각각 `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state`, `allocation_budget_state`를 포함합니다.

값은 고정 120-frame CPU callback window와 invocation 기준 average를 나타냅니다. GPU attribution은 `Unavailable`, inactive render integration은 `Unsupported`, 호출되지 않은 supported component는 `NotMeasured`입니다. Session JSON/CSV schema는 변경되지 않으며 기존 CPU/GPU metrics도 조정하지 않습니다.

## 일반적인 Profiling Run

```text
perfmeter.profiler.capabilities {}
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Numerical overdraw와 heatmap rendering은 추가 GPU work를 만들기 때문에 `OverdrawDiagnostic`은 bounded URP diagnostic window에만 사용합니다. HDRP는 overdraw/heatmap을 unsupported로 보고하지만 나머지 diagnostics는 계속 사용할 수 있습니다.

## 메모리 스냅샷 command

| Command | 목적 및 주요 입력 |
| --- | --- |
| `perfmeter.memory.snapshot.request` | `capture_id`, 선택적 capture-flag boolean, `minimum_free_disk_mb`, `cooldown_seconds`로 manual snapshot을 request합니다. |
| `perfmeter.memory.snapshot.status` | runtime을 시작하거나 임시 source path를 노출하지 않고 snapshot 및 correlated bundle state를 읽습니다. |
| `perfmeter.memory.snapshot.capabilities` | backend provenance, 지원 flags, 512 MiB snapshot limit, owned temporary root를 읽습니다. |
| `perfmeter.memory.snapshot.triggers.configure` | system-memory threshold와 bounded leak-growth trigger, frame window, flags, free-space guard, cooldown을 명시적으로 enable/disable합니다. |

request와 trigger configuration command에는 Play Mode가 필요합니다. automation은 기본적으로 disabled입니다. 일반적인 순서는 다음과 같습니다.

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

bundle이 export-ready가 될 때까지 status를 확인한 후 기존 `perfmeter.capture.export` command를 사용합니다. memory-only bundle은 `requested_tool: MemoryProfiler`, `memory-snapshot.json`, manifest provenance를 포함하고 external GPU artifact는 만들지 않습니다. 성공한 export는 one-shot이며 owned staging source를 삭제합니다.
