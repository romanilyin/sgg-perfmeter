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
| `perfmeter.compatibility.status` | 分别读取 import、core runtime 和 active render integration compatibility。 |
| `perfmeter.runtime.status` | 读取 runtime status。 |
| `perfmeter.runtime.ensure` | 在需要时启动 runtime。 |
| `perfmeter.runtime.stop` | 停止 runtime。 |
| `perfmeter.runtime.reset_stats` | 重置 rolling stats、alert counters 和 active session counters。 |
| `perfmeter.runtime.mode.set` | 切换 `Stopped`、`Background`、`Overlay` 或 `OverdrawDiagnostic`。 |
| `perfmeter.metrics.latest` | 读取 latest metrics，包括 custom metrics。 |
| `perfmeter.profiler.capabilities` | 读取缓存的 Profiler metric capabilities 和 provenance，不启动 runtime 或 discovery。 |
| `perfmeter.profiler.lease.capabilities` | 读取 process-local profiler lease resource 和 reload semantics。 |
| `perfmeter.profiler.lease.status` | 读取 current 或 matching process-local profiler lease state。 |
| `perfmeter.alerts.latest` | 读取 active alerts、counters 和 Editor warning state。 |
| `perfmeter.alerts.clear` | 清除 active alerts、counters 和 cooldown state。 |
| `perfmeter.alerts.capture.begin` | 开始外部 capture 的 bounded classification。 |
| `perfmeter.alerts.capture.end` | 结束对应的外部 capture classification。 |
| `perfmeter.device.info` | 读取 device、graphics、display、monitor、pipeline 和 Unity environment info。 |
| `perfmeter.camera.snapshot` | 读取 camera transform/projection 和 URP/HDRP camera settings。 |
| `perfmeter.rendergraph.snapshot` | 读取 URP Render Graph 或 HDRP Custom Pass 的最新 observed render integration diagnostics。 |
| `perfmeter.render.snapshot` | 读取包含 freshness、camera/pass context、GRD/VRS 和 legacy Render Graph facade 的 neutral render integration snapshot。 |
| `perfmeter.overlay.set` | 显示/隐藏 overlay，并设置 preset、modules、corner、mode 和 target FPS。 |
| `perfmeter.overdraw.start` | 启动有边界的 overdraw measurement。 |
| `perfmeter.overdraw.cancel` | 取消 active overdraw measurement。 |
| `perfmeter.overdraw.heatmap.set` | 显示或隐藏 visual overdraw heatmap。 |
| `perfmeter.session.start` | 启动有边界的 session recording。 |
| `perfmeter.session.stop` | 停止 recording 并返回 summary。 |
| `perfmeter.session.summary` | 读取当前 session summary。 |
| `perfmeter.session.export` | 将当前 session 导出到项目本地 JSON 或 CSV。 |
| `perfmeter.capture.request` | 请求有边界的 external GPU capture 和 correlated bundle。 |
| `perfmeter.capture.status` | 读取 capture 和 bundle state。 |
| `perfmeter.capture.cancel` | 取消匹配的 active capture。 |
| `perfmeter.capture.export` | 将 ready bundle 原子导出到 project-local bundle root。 |
| `perfmeter.capture.export.request` | 将 single-flight export 加入队列，并返回 export ID 和 progress。 |
| `perfmeter.capture.export.status` | 读取 phase、progress、cancellation、retry 和 artifact authority。 |
| `perfmeter.capture.export.cancel` | 请求取消 matching active export。 |
| `perfmeter.capture.capabilities` | 读取 schema、quota、retention、screenshot 和 provenance capabilities。 |

优先使用 `perfmeter.capture.export.request`，然后轮询 `perfmeter.capture.export.status`，并可按需调用 `perfmeter.capture.export.cancel`。Legacy `perfmeter.capture.export` command 为保持兼容性会阻塞。Export response 包含通用 `external_artifact` envelope，其中含有 association、authority、finalization、content、privacy/share policy、size，以及 source hash 和 post-copy hash。Read-only lease commands 可在不获取 lease 的情况下公开 process-local conflict state。

## Runtime Self-Overhead Payload

`perfmeter.runtime.status` 包含 additive `self_overhead` object；它不是新 command。Top-level keys 为 `state`、`cpu_timing_available`、`gpu_timing_availability` 和 `has_budget_violation`。

Component objects 为 `collector`、`custom_metric_providers`、`cpu_core_provider`、`overlay`、`urp_render_integration` 和 `hdrp_render_integration`。每个对象包含 `component`、`state`、`window_frame_count`、`invocation_count`、`average_cpu_time_ms`、`max_cpu_time_ms`、`allocated_bytes`、`average_allocated_bytes`、`cpu_budget_ms`、`allocation_budget_bytes`、`cpu_budget_state` 和 `allocation_budget_state`。

这些值描述固定 120-frame CPU callback window，并按 invocation 计算 average。GPU attribution 为 `Unavailable`；inactive render integration 为 `Unsupported`，未调用的 supported component 为 `NotMeasured`。Session JSON/CSV schema 不变，现有 CPU/GPU metrics 也不会调整。

## 典型 Profiling Run

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

仅在有边界的 URP diagnostic windows 中使用 `OverdrawDiagnostic`，因为 numerical overdraw 和 heatmap rendering 会增加额外 GPU work。HDRP 会将 overdraw/heatmap 报告为 unsupported，但其他 diagnostics 仍可用。

## 内存快照 commands

| Command | 用途和主要输入 |
| --- | --- |
| `perfmeter.memory.snapshot.request` | 使用 `capture_id`、可选 capture-flag boolean、`minimum_free_disk_mb` 和 `cooldown_seconds` 请求 manual snapshot。 |
| `perfmeter.memory.snapshot.status` | 在不启动 runtime 且不暴露临时 source path 的情况下读取 snapshot 与 correlated bundle state。 |
| `perfmeter.memory.snapshot.capabilities` | 读取 backend provenance、支持的 flags、512 MiB snapshot limit 和 owned temporary root。 |
| `perfmeter.memory.snapshot.triggers.configure` | 显式 enable/disable system-memory threshold 与 bounded leak-growth trigger、frame window、flags、free-space guard 和 cooldown。 |

request 和 trigger configuration command 需要 Play Mode。automation 默认关闭。典型顺序如下：

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

等待 bundle 进入 export-ready 后，使用现有的 `perfmeter.capture.export` command。memory-only bundle 使用 `requested_tool: MemoryProfiler`，包含 `memory-snapshot.json` 和 manifest provenance，不会创建 external GPU artifact。成功的 export 是 one-shot，并删除 owned staging source。

## 图形诊断与 GraphicsStateCollection commands

下面 6 个 command 构成 PM-GFX-001 surface：

| Command | 用途和主要输入 |
| --- | --- |
| `perfmeter.graphics.diagnostics` | 读取最新的 shader GPU-program 和 graphics-pipeline creation marker value、dynamic capability provenance、catalog revision、graphics API context。无输入。 |
| `perfmeter.graphics.state_collection.request` | 启动 bounded trace。需要 Play Mode 和 active PerfMeter session；`capture_id` 必填，`trace_frames` 为 1–600（默认 60），`minimum_free_disk_mb` 默认 1024。 |
| `perfmeter.graphics.state_collection.status` | 读取 availability、state、progress、backend identity、counts、`is_busy`、`has_pending_cleanup`、warning 以及 owned artifact 的 project-relative path。无输入。 |
| `perfmeter.graphics.state_collection.capabilities` | 读取 backend provenance、trace/prewarm support、cache-miss 与 parallel-PSO support、session requirement、600-frame/64 MiB limit 和 owned artifact root。无输入。 |
| `perfmeter.graphics.state_collection.cancel` | 取消匹配的 active/preparing trace，并清理 pending artifact。需要 `capture_id`。 |
| `perfmeter.graphics.state_collection.prewarm` | 在 Play Mode 中加载并同步 prewarm 一个 owned project-relative artifact。`relative_path` 必填；`max_state_count` 为 0–1,000,000，默认 0。 |

`perfmeter.graphics.diagnostics` 返回 `shader_gpu_program_creation_value`、`graphics_pipeline_creation_value` 以及每个 capability 的 `sample_state`、`resolution`、`resolved_recorder_names`、`unit`、`data_type`、`resolved_component_count`、`sampled_component_count`。`perfmeter.metrics.latest` 和 session export 也暴露相同的 marker metadata。值保留 discovered recorder unit，并不一定是 shader/PSO count；请使用 `sample_state`，不要把 0 解释为 unavailable。

state response 包含 `result`、`availability`、`state`、`capture_id`、requested/completed trace frames、backend ID/version、`artifact_relative_path`、`artifact_size_bytes`、`total_graphics_state_count`、`variant_count`、`completed_warmup_count`、`is_warmed_up`、`is_busy`、`has_pending_cleanup` 和 `warning`。`is_busy` 在 preparation、trace、结束、prewarm、cleanup 或 persisted cleanup 期间保持 true；`has_pending_cleanup` 表示等待 retry 的 owned artifact。删除失败会通过 owned `.delete-pending` sidecar 持久化，并在 domain reload 后恢复和重试。`StopSession` 会取消 active trace，因此 session 必须保持 active 到完成。trace 在 end-of-frame tick 完请求的 frames 后进入 terminal state；batch mode 使用 next-frame fallback。active session 接纳的 sample 会带有等于 `capture_id` 的 `graphics_state_trace_id`。

典型的 trace 与 prewarm 顺序：

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

只允许一个 graphics-state flight。重复的 active ID 返回 `AlreadyActive`；其他 overlapping trace/prewarm 返回 `RejectedOverlap`。cancel 只匹配 active/preparing ID。Unity backend 报告 `supports_cache_miss_tracing: false`，因此 cache-miss evidence 不受支持，MCP prewarm schema 也不提供该 input。artifact 由 PerfMeter owned，位于 `Temp/PerfMeter/GraphicsStateCollections` 下，最大 64 MiB。

## Render integration snapshot

`perfmeter.render.snapshot {}` 是无 input 的 read-only command，不会启动 runtime。response 使用 `schema_version: 1`，并在 `render_integration` 中返回 current pipeline/source、observation frame/age、`observation_matches_current_pipeline`、observed camera identity、integration/pass/injection metadata、实际 schedule 的 PerfMeter pass count、可用时的 effective rendering mode、嵌套的 `gpu_resident_drawer` 与 `variable_rate_shading`，以及 `legacy_render_graph`。

`gpu_resident_drawer` 包含 project/compute support、带 `activity_source` 的 public global activity、URP Forward+/clustered compatibility、`degraded_reason` 和嵌套 BRG `effectiveness`。capability 不是 `AvailableSampled` 时值为 `null`；recorder names、exact/alias resolution 和 component counts 保留 provenance。`scope: "brg_aggregate"` 不证明逐 renderer 的 GRD 使用。

该 command 对应 `PerformanceMeter.GetRenderIntegrationSnapshot()` 和 `TryGetRenderIntegrationSnapshot(...)`。stale observation 会通过明确的 non-match 和 warning 报告，而不会伪装成 current。`perfmeter.rendergraph.snapshot` 作为 legacy facade 保留。稳定的 Unity API 不提供 RenderGraph/CustomPass viewer 或 pass-target 信息，因此不会增加 Editor navigation。
