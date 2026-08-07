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
| `perfmeter.render.snapshot` | freshness, camera/pass context, GRD/VRS와 legacy Render Graph facade를 포함한 neutral render integration snapshot을 읽습니다. |
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

## 그래픽 진단 및 GraphicsStateCollection command

다음 6개 command가 PM-GFX-001 surface를 제공합니다.

| Command | 목적 및 주요 입력 |
| --- | --- |
| `perfmeter.graphics.diagnostics` | 최신 shader GPU-program 및 graphics-pipeline creation marker value, dynamic capability provenance, catalog revision, graphics API context를 읽습니다. 입력 없음. |
| `perfmeter.graphics.state_collection.request` | bounded trace를 시작합니다. Play Mode와 active PerfMeter session이 필요하며 `capture_id`는 필수, `trace_frames`는 1–600(기본 60), `minimum_free_disk_mb` 기본값은 1024입니다. |
| `perfmeter.graphics.state_collection.status` | availability, state, progress, backend identity, counts, `is_busy`, `has_pending_cleanup`, warning, owned artifact의 project-relative path를 읽습니다. 입력 없음. |
| `perfmeter.graphics.state_collection.capabilities` | backend provenance, trace/prewarm support, cache-miss 및 parallel-PSO support, session requirement, 600-frame/64 MiB limit, owned artifact root를 읽습니다. 입력 없음. |
| `perfmeter.graphics.state_collection.cancel` | 일치하는 active/preparing trace를 cancel하고 pending artifact를 cleanup합니다. `capture_id` 필요. |
| `perfmeter.graphics.state_collection.prewarm` | Play Mode에서 owned project-relative artifact를 load하고 synchronous prewarm합니다. `relative_path` 필수, `max_state_count`는 0–1,000,000(기본 0)입니다. |

`perfmeter.graphics.diagnostics`는 `shader_gpu_program_creation_value`, `graphics_pipeline_creation_value`와 각 capability의 `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count`, `sampled_component_count`를 반환합니다. `perfmeter.metrics.latest`와 session export도 동일한 marker metadata를 노출합니다. 값은 discovered recorder unit을 유지하며 항상 shader/PSO count인 것은 아닙니다. 0을 unavailable로 해석하지 말고 `sample_state`를 사용하십시오.

state response에는 `result`, `availability`, `state`, `capture_id`, requested/completed trace frames, backend ID/version, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup`, `warning`이 포함됩니다. `is_busy`는 preparation, trace, 종료, prewarm, cleanup 또는 persisted cleanup 동안 true이고 `has_pending_cleanup`은 retry 대기 중인 owned artifact를 나타냅니다. 삭제 실패는 owned `.delete-pending` sidecar에 저장되고 domain reload 후 복원·재시도됩니다. `StopSession`은 active trace를 cancel하므로 완료될 때까지 session을 active로 유지해야 합니다. trace는 end-of-frame에서 requested frame을 tick한 뒤 terminal state가 되며 batch mode에서는 next-frame fallback을 사용합니다. active session이 admitted한 sample에는 `capture_id`와 같은 `graphics_state_trace_id`가 들어갑니다.

trace 및 prewarm의 일반적인 순서:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

graphics-state flight는 하나만 허용됩니다. 동일한 active ID는 `AlreadyActive`, 다른 overlapping trace/prewarm은 `RejectedOverlap`을 반환합니다. cancel은 matching active/preparing ID에만 적용됩니다. Unity backend는 `supports_cache_miss_tracing: false`를 보고하므로 cache-miss evidence는 지원되지 않으며 MCP prewarm schema에도 해당 input이 없습니다. artifact는 PerfMeter 소유이고 `Temp/PerfMeter/GraphicsStateCollections` 아래에 저장되며 64 MiB로 제한됩니다.

## Render integration snapshot

`perfmeter.render.snapshot {}`은 input이 없는 read-only command입니다. runtime을 시작하지 않습니다. response는 `schema_version: 1`을 사용하며 `render_integration`에 current pipeline/source, observation frame/age, `observation_matches_current_pipeline`, observed camera identity, integration/pass/injection metadata, 실제로 schedule된 PerfMeter pass count, 가능한 경우 effective rendering mode, 중첩된 `gpu_resident_drawer`와 `variable_rate_shading`, `legacy_render_graph`를 포함합니다.

이 command는 `PerformanceMeter.GetRenderIntegrationSnapshot()` 및 `TryGetRenderIntegrationSnapshot(...)`에 대응하는 MCP surface입니다. stale observation은 current로 표시하지 않고 명시적인 non-match와 warning으로 보고합니다. `perfmeter.rendergraph.snapshot`은 legacy facade로 유지됩니다. 안정적인 Unity API가 RenderGraph/CustomPass viewer나 pass-target 정보를 공개하지 않으므로 Editor navigation은 추가되지 않습니다.
