# MCP E Automacao De Agents

SGG PerfMeter expoe metadados de comandos para workflows Unity MCP/editor-agent no caminho do pacote:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

O objetivo e saida JSON estruturada para agents em vez de parsing de screenshots, parsing de texto do overlay ou scraping do Unity Console.

## Grupos De Comandos

| Comando | Finalidade |
| --- | --- |
| `perfmeter.setup.status` | Ler o status de setup. |
| `perfmeter.setup.run` | Executar as acoes de setup recomendadas. |
| `perfmeter.compatibility.status` | Ler separadamente compatibility de import, core runtime e render integration ativa. |
| `perfmeter.runtime.status` | Ler o status runtime. |
| `perfmeter.runtime.ensure` | Iniciar o runtime se necessario. |
| `perfmeter.runtime.stop` | Parar o runtime. |
| `perfmeter.runtime.reset_stats` | Resetar rolling stats, contadores de alerts e contadores da sessao ativa. |
| `perfmeter.runtime.mode.set` | Alternar entre `Stopped`, `Background`, `Overlay` ou `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Ler as metricas mais recentes, incluindo custom metrics. |
| `perfmeter.profiler.capabilities` | Ler capabilities e proveniencia de resolucao das metricas Profiler em cache sem iniciar o runtime ou a discovery. |
| `perfmeter.alerts.latest` | Ler alerts ativos, contadores e estado de avisos do Editor. |
| `perfmeter.alerts.clear` | Limpar alerts ativos, contadores e estado de cooldown. |
| `perfmeter.alerts.capture.begin` | Iniciar a classificacao limitada de uma captura externa. |
| `perfmeter.alerts.capture.end` | Encerrar a classificacao da captura externa correspondente. |
| `perfmeter.device.info` | Ler informacoes de device, graficos, display, monitor, pipeline e ambiente Unity. |
| `perfmeter.camera.snapshot` | Ler transform/projection da camera e URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Ler os diagnostics de render integration mais recentes para URP Render Graph ou HDRP Custom Pass. |
| `perfmeter.render.snapshot` | Ler o snapshot neutro de render integration, com freshness, contexto de camera/pass, GRD/VRS e facade legacy do Render Graph. |
| `perfmeter.overlay.set` | Mostrar/ocultar o overlay e definir preset, modules, corner, mode e target FPS. |
| `perfmeter.overdraw.start` | Iniciar medicao limitada de overdraw. |
| `perfmeter.overdraw.cancel` | Cancelar medicao de overdraw ativa. |
| `perfmeter.overdraw.heatmap.set` | Mostrar ou ocultar o overdraw heatmap visual. |
| `perfmeter.session.start` | Iniciar gravacao de sessao limitada. |
| `perfmeter.session.stop` | Parar a gravacao e retornar um resumo. |
| `perfmeter.session.summary` | Ler o resumo atual da sessao. |
| `perfmeter.session.export` | Exportar a sessao atual para JSON ou CSV local ao projeto. |
| `perfmeter.capture.request` | Solicitar um capture GPU externo limitado e um bundle correlacionado. |
| `perfmeter.capture.status` | Ler o estado do capture e do bundle. |
| `perfmeter.capture.cancel` | Cancelar o capture ativo correspondente. |
| `perfmeter.capture.export` | Exportar atomicamente um bundle pronto sob a raiz local do projeto. |
| `perfmeter.capture.capabilities` | Ler capabilities de schema, quota, retention, screenshot e provenance. |

## Self-Overhead No Status Runtime

`perfmeter.runtime.status` inclui o objeto aditivo `self_overhead`; nao e um comando separado. As chaves principais sao `state`, `cpu_timing_available`, `gpu_timing_availability` e `has_budget_violation`.

Os objetos de componente sao `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration` e `hdrp_render_integration`. Cada um contem `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state` e `allocation_budget_state`.

Os valores descrevem janelas fixas de 120 frames para callbacks CPU com medias por invocacao. A atribuicao GPU e `Unavailable`; a render integration inativa e `Unsupported`, e um componente suportado sem chamadas e `NotMeasured`. Os schemas JSON/CSV de sessao nao mudam e as metricas CPU/GPU existentes nao sao ajustadas.

## Execucao Tipica De Profiling

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

Use `OverdrawDiagnostic` apenas em janelas diagnosticas URP limitadas porque overdraw numerico e renderizacao de heatmap adicionam trabalho extra de GPU. HDRP reporta overdraw/heatmap como unsupported, enquanto os demais diagnostics continuam disponiveis.

## Comandos de snapshot de memoria

| Comando | Objetivo e principais entradas |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Solicitar um snapshot manual com `capture_id`, booleanos opcionais de capture flags, `minimum_free_disk_mb` e `cooldown_seconds`. |
| `perfmeter.memory.snapshot.status` | Ler o estado do snapshot e do bundle correlacionado sem iniciar o runtime nem expor o source path temporario. |
| `perfmeter.memory.snapshot.capabilities` | Ler a proveniencia do backend, flags suportadas, limite de 512 MiB e a raiz temporaria pertencente ao PerfMeter. |
| `perfmeter.memory.snapshot.triggers.configure` | Habilitar ou desabilitar explicitamente triggers de limite de memoria do sistema e crescimento limitado de vazamento, janela de frames, flags, guard de espaco livre e cooldown. |

Os comandos de solicitacao e configuracao de triggers exigem Play Mode. A automacao fica desabilitada por padrao. Sequencia tipica:

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Aguarde o bundle ficar pronto para exportacao e use o comando existente `perfmeter.capture.export`. Um bundle somente de memoria usa `requested_tool: MemoryProfiler`, inclui `memory-snapshot.json` e proveniencia no manifest e nao produz artefato GPU externo. Uma exportacao bem-sucedida e de uso unico e remove a origem de staging pertencente ao PerfMeter.

## Comandos de diagnostico grafico e GraphicsStateCollection

Os seis comandos a seguir formam a superficie do PM-GFX-001:

| Comando | Objetivo e principais entradas |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Ler os valores mais recentes dos markers de criacao de programas GPU de shaders e graphics pipelines, a provenance dinamica das capabilities, a revisao do catalogo e o contexto da graphics API. Sem entradas. |
| `perfmeter.graphics.state_collection.request` | Iniciar um trace limitado. Requer Play Mode e uma sessao PerfMeter ativa; `capture_id` e obrigatorio, `trace_frames` e 1–600 (padrao 60) e `minimum_free_disk_mb` tem padrao 1024. |
| `perfmeter.graphics.state_collection.status` | Ler availability, state, progresso, identidade do backend, counts, `is_busy`, `has_pending_cleanup`, warnings e o path relativo ao projeto do artifact owned. Sem entradas. |
| `perfmeter.graphics.state_collection.capabilities` | Ler provenance do backend, suporte a trace/prewarm, cache-miss e PSO paralelo, requisito de sessao, limites de 600 frames/64 MiB e a raiz dos artifacts owned. Sem entradas. |
| `perfmeter.graphics.state_collection.cancel` | Cancelar o trace ativo ou em preparacao correspondente e limpar seu artifact pendente. Requer `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Carregar e executar prewarm sincrono de um artifact owned relativo ao projeto em Play Mode. `relative_path` e obrigatorio; `max_state_count` e 0–1.000.000, padrao 0. |

`perfmeter.graphics.diagnostics` retorna `shader_gpu_program_creation_value` e `graphics_pipeline_creation_value`, alem de `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count` e `sampled_component_count` para cada capability. `perfmeter.metrics.latest` e os exports de sessao expoem a mesma metadata dos markers. Os valores mantem a unidade descoberta do recorder e nao sao universalmente counts de shaders ou PSO; use `sample_state` em vez de interpretar zero como unavailable.

A resposta de state inclui `result`, `availability`, `state`, `capture_id`, trace frames solicitados/concluidos, ID/versao do backend, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup` e `warning`. `is_busy` permanece true durante preparacao, trace, finalizacao, prewarm, cleanup ou cleanup persistente; `has_pending_cleanup` indica um artifact owned aguardando retry. Uma exclusao falha e persistida com um sidecar owned `.delete-pending`, restaurado e tentado novamente apos um domain reload. `StopSession` cancela um trace ativo, portanto a sessao deve permanecer ativa ate a conclusao. O trace chega ao estado terminal depois de tickar os frames solicitados no fim do frame; em batch mode usa um fallback no frame seguinte. Samples admitidos por uma sessao ativa incluem `graphics_state_trace_id` igual a `capture_id`.

Sequencia tipica de trace e prewarm:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

Apenas um graphics-state flight e admitido. Um ID ativo repetido retorna `AlreadyActive`; outro trace/prewarm em overlap retorna `RejectedOverlap`. Cancel corresponde somente ao ID ativo/em preparacao. O backend do Unity informa `supports_cache_miss_tracing: false`: evidencia de cache-miss nao e suportada e o schema MCP de prewarm nao oferece esse input. Os artifacts pertencem ao PerfMeter, ficam em `Temp/PerfMeter/GraphicsStateCollections` e sao limitados a 64 MiB.

## Snapshot de render integration

`perfmeter.render.snapshot {}` e um comando read-only sem inputs. Ele nao inicia o runtime. A resposta usa `schema_version: 1` e retorna `render_integration` com pipeline/source atuais, frame e age da observation, `observation_matches_current_pipeline`, identidade da camera observada, metadata de integration/pass/injection, quantidade de passes do PerfMeter realmente agendados, effective rendering mode quando disponivel, os contextos aninhados `gpu_resident_drawer` e `variable_rate_shading` e `legacy_render_graph`.

O comando e o equivalente MCP de `PerformanceMeter.GetRenderIntegrationSnapshot()` e `TryGetRenderIntegrationSnapshot(...)`. Uma observation stale e reportada com non-match e warning explicitos, nao como atual. `perfmeter.rendergraph.snapshot` continua disponivel como facade legacy. O comando nao adiciona navegacao no Editor: as APIs estaveis do Unity nao expoem viewer de RenderGraph/CustomPass nem informacao de pass targets.
