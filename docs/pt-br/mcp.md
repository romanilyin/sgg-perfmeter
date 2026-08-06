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
