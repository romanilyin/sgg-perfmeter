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
| `perfmeter.compatibility.status` | import、core runtime、active render integration の compatibility を個別に読み取ります。 |
| `perfmeter.runtime.status` | runtime status を読み取ります。 |
| `perfmeter.runtime.ensure` | 必要に応じて runtime を開始します。 |
| `perfmeter.runtime.stop` | runtime を停止します。 |
| `perfmeter.runtime.reset_stats` | rolling stats、alert counters、active session counters をリセットします。 |
| `perfmeter.runtime.mode.set` | `Stopped`、`Background`、`Overlay`、`OverdrawDiagnostic` を切り替えます。 |
| `perfmeter.metrics.latest` | custom metrics を含む latest metrics を読み取ります。 |
| `perfmeter.profiler.capabilities` | cache 済み Profiler metric capabilities と resolution provenance を、runtime や discovery を開始せずに読み取ります。 |
| `perfmeter.profiler.lease.capabilities` | process-local profiler lease resource と reload semantics を読み取ります。 |
| `perfmeter.profiler.lease.status` | current または matching process-local profiler lease state を読み取ります。 |
| `perfmeter.alerts.latest` | active alerts、counters、Editor warning state を読み取ります。 |
| `perfmeter.alerts.clear` | active alerts、counters、cooldown state をクリアします。 |
| `perfmeter.alerts.capture.begin` | 外部 capture の bounded classification を開始します。 |
| `perfmeter.alerts.capture.end` | 対応する外部 capture classification を終了します。 |
| `perfmeter.device.info` | device、graphics、display、monitor、pipeline、Unity environment info を読み取ります。 |
| `perfmeter.camera.snapshot` | camera transform/projection と URP/HDRP camera settings を読み取ります。 |
| `perfmeter.rendergraph.snapshot` | URP Render Graph または HDRP Custom Pass の最新 observed render integration diagnostics を読み取ります。 |
| `perfmeter.render.snapshot` | freshness、camera/pass context、GRD/VRS、legacy Render Graph facade を含む neutral render integration snapshot を読み取ります。 |
| `perfmeter.overlay.set` | overlay の show/hide と preset、modules、corner、mode、target FPS を設定します。 |
| `perfmeter.overdraw.start` | bounded overdraw measurement を開始します。 |
| `perfmeter.overdraw.cancel` | active overdraw measurement をキャンセルします。 |
| `perfmeter.overdraw.heatmap.set` | visual overdraw heatmap を表示または非表示にします。 |
| `perfmeter.session.start` | bounded session recording を開始します。 |
| `perfmeter.session.stop` | recording を停止して summary を返します。 |
| `perfmeter.session.summary` | current session summary を読み取ります。 |
| `perfmeter.session.export` | current session を project-local JSON または CSV に export します。 |
| `perfmeter.capture.request` | bounded GPU capture を request。optional `backend_mode`: `GenericUnity`、`NativePreferred`、`NativeRequired`。Native storage mode は C# API だけで選択します。 |
| `perfmeter.capture.status` | capture と bundle の state を読み取ります。 |
| `perfmeter.capture.cancel` | 一致する active capture を cancel します。 |
| `perfmeter.capture.export` | ready bundle を project-local root に atomic export します。 |
| `perfmeter.capture.export.request` | single-flight export を queue し、export ID と progress を返します。 |
| `perfmeter.capture.export.status` | phase、progress、cancellation、retry、artifact authority を読み取ります。 |
| `perfmeter.capture.export.cancel` | matching active export の cancellation を request します。 |
| `perfmeter.capture.capabilities` | schema、quota、retention、screenshot、provenance capabilities を読み取ります。 |

`perfmeter.capture.export.request` を優先し、その後 `perfmeter.capture.export.status` を polling し、必要に応じて `perfmeter.capture.export.cancel` を呼び出してください。Legacy の `perfmeter.capture.export` command は互換性のため blocking 動作を維持します。Export response には association、authority、finalization、content、privacy/share policy、size、source hash、post-copy hash を持つ汎用 `external_artifact` envelope が含まれます。Read-only lease commands は lease を取得せずに process-local conflict state を公開します。

## Runtime Self-Overhead Payload

`perfmeter.runtime.status` は additive な `self_overhead` object を含みます。これは別の command ではありません。Top-level keys は `state`、`cpu_timing_available`、`gpu_timing_availability`、`has_budget_violation` です。

Component objects は `collector`、`custom_metric_providers`、`cpu_core_provider`、`overlay`、`urp_render_integration`、`hdrp_render_integration` です。それぞれ `component`、`state`、`window_frame_count`、`invocation_count`、`average_cpu_time_ms`、`max_cpu_time_ms`、`allocated_bytes`、`average_allocated_bytes`、`cpu_budget_ms`、`allocation_budget_bytes`、`cpu_budget_state`、`allocation_budget_state` を含みます。

値は固定 120-frame CPU callback window と invocation 単位の average を表します。GPU attribution は `Unavailable`、inactive render integration は `Unsupported`、呼び出されていない supported component は `NotMeasured` です。Session JSON/CSV schema は変更されず、既存の CPU/GPU metrics も補正されません。

## Typical Profiling Run

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

numerical overdraw と heatmap rendering は追加の GPU work を発生させるため、`OverdrawDiagnostic` は bounded URP diagnostic windows でのみ使用してください。HDRP は overdraw/heatmap を unsupported として報告しますが、その他の diagnostics は利用できます。

## メモリスナップショットの command

| Command | 目的と主な入力 |
| --- | --- |
| `perfmeter.memory.snapshot.request` | `capture_id`、任意の capture-flag boolean、`minimum_free_disk_mb`、`cooldown_seconds` で manual snapshot を request します。 |
| `perfmeter.memory.snapshot.status` | runtime を起動せず、一時 source path を公開せずに snapshot と correlated bundle の state を読み取ります。 |
| `perfmeter.memory.snapshot.capabilities` | backend provenance、対応 flags、512 MiB の snapshot limit、owned temporary root を読み取ります。 |
| `perfmeter.memory.snapshot.triggers.configure` | system-memory threshold と bounded leak-growth trigger、frame window、flags、空き容量 guard、cooldown を明示的に enable/disable します。 |

request と trigger configuration の command には Play Mode が必要です。automation は既定で無効です。典型的な順序は次のとおりです。

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

bundle が export-ready になるまで status を読み、その後既存の `perfmeter.capture.export` を使います。memory-only bundle は `requested_tool: MemoryProfiler`、`memory-snapshot.json`、manifest provenance を含み、external GPU artifact を持ちません。成功した export は one-shot で、owned staging source を削除します。

## Graphics diagnostics と GraphicsStateCollection command

次の 6 command が PM-GFX-001 の surface です。

| Command | 目的と主な入力 |
| --- | --- |
| `perfmeter.graphics.diagnostics` | 最新の shader GPU-program と graphics-pipeline creation marker value、dynamic capability provenance、catalog revision、graphics API context を読む。入力なし。 |
| `perfmeter.graphics.state_collection.request` | bounded trace を開始する。Play Mode と active PerfMeter session が必要。`capture_id` は必須、`trace_frames` は 1–600（既定 60）、`minimum_free_disk_mb` の既定値は 1024。 |
| `perfmeter.graphics.state_collection.status` | availability、state、progress、backend identity、counts、`is_busy`、`has_pending_cleanup`、warning、owned artifact の project-relative path を読む。入力なし。 |
| `perfmeter.graphics.state_collection.capabilities` | backend provenance、trace/prewarm support、cache-miss と parallel-PSO support、session requirement、600-frame/64 MiB limit、owned artifact root を読む。入力なし。 |
| `perfmeter.graphics.state_collection.cancel` | 一致する active/preparing trace を cancel し、pending artifact を cleanup する。`capture_id` が必要。 |
| `perfmeter.graphics.state_collection.prewarm` | Play Mode で owned project-relative artifact を load し、synchronous に prewarm する。`relative_path` は必須、`max_state_count` は 0–1,000,000（既定 0）。 |

`perfmeter.graphics.diagnostics` は `shader_gpu_program_creation_value`、`graphics_pipeline_creation_value` と、各 capability の `sample_state`、`resolution`、`resolved_recorder_names`、`unit`、`data_type`、`resolved_component_count`、`sampled_component_count` を返します。`perfmeter.metrics.latest` と session export も同じ marker metadata を公開します。値は recorder の discovered unit を保持し、shader/PSO count とは限りません。zero を unavailable と判断せず `sample_state` を使ってください。

state response には `result`、`availability`、`state`、`capture_id`、requested/completed trace frames、backend ID/version、`artifact_relative_path`、`artifact_size_bytes`、`total_graphics_state_count`、`variant_count`、`completed_warmup_count`、`is_warmed_up`、`is_busy`、`has_pending_cleanup`、`warning` が含まれます。`is_busy` は preparation、trace、終了、prewarm、cleanup、または persisted cleanup の間 true で、`has_pending_cleanup` は retry 待ちの owned artifact を示します。削除失敗は owned `.delete-pending` sidecar に保存され、domain reload 後に復元・再試行されます。`StopSession` は active trace を cancel するため、完了まで session を active に保つ必要があります。trace は end-of-frame で requested frame を tick した後に terminal state へ進み、batch mode では next-frame fallback を使います。active session に admitted された sample には `capture_id` と同じ `graphics_state_trace_id` が入ります。

trace と prewarm の典型的な順序:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

graphics-state flight は一つだけです。同じ active ID は `AlreadyActive`、別の overlapping trace/prewarm は `RejectedOverlap` を返します。cancel は matching active/preparing ID にだけ適用されます。Unity backend の `supports_cache_miss_tracing: false` のため cache-miss evidence は未対応で、MCP prewarm schema にもその input はありません。artifact は PerfMeter が所有し、`Temp/PerfMeter/GraphicsStateCollections` 以下に置かれ、64 MiB に制限されます。

## Render integration snapshot

`perfmeter.render.snapshot {}` は input のない read-only command です。runtime は起動しません。response は `schema_version: 1` を使用し、`render_integration` に current pipeline/source、observation frame/age、`observation_matches_current_pipeline`、observed camera identity、integration/pass/injection metadata、実際に schedule された PerfMeter pass count、利用可能な場合の effective rendering mode、nested `gpu_resident_drawer` と `variable_rate_shading`、`legacy_render_graph` を含めます。

`gpu_resident_drawer` は project/compute support、`activity_source` 付き public global activity、URP Forward+/clustered compatibility、`degraded_reason`、nested BRG `effectiveness` を含みます。capability が `AvailableSampled` でない値は `null` で、recorder name、exact/alias resolution、component count が provenance を保持します。`scope: "brg_aggregate"` は renderer ごとの GRD use を証明しません。

これは `PerformanceMeter.GetRenderIntegrationSnapshot()` と `TryGetRenderIntegrationSnapshot(...)` に対応する MCP command です。stale observation は current として返さず、明示的な non-match と warning で示します。`perfmeter.rendergraph.snapshot` は legacy facade として残ります。安定した Unity API に RenderGraph/CustomPass viewer や pass-target 情報がないため、Editor navigation は追加されません。
