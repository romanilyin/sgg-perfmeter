# Workflows

## Overlay Runtime

Use o overlay quando precisar de visibilidade imediata dentro do jogo.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

O overlay usa UI Toolkit e nao intercepta input de gameplay. Ele suporta FPS-only, texto compacto, grafico, diagnosticos completos, barras de metrica, visual themes, filtros de modulo, graficos de CPU/GPU, widgets de nucleos de CPU e um conjunto limitado de linhas de custom metrics.

O PerfMeter cria e possui um host versionado de UI Toolkit para o overlay: Unity `6000.4` usa `UIDocument`, enquanto Unity `6000.5+` usa `PanelRenderer`. O host proprio e separado da UI estrangeira e preserva seus panel settings e children; os rebuilds removem somente o container pertencente ao PerfMeter.

## Coleta Em Segundo Plano

Use o modo em segundo plano para testes, execucoes em dispositivos ou workflows de agents quando UI visivel nao e necessaria.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Gravacao E Exportacao De Sessao

Use sessoes para janelas de profiling reproduziveis.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

As exportacoes de sessao incluem timing, FPS lows, spikes, contagens de gargalos, render counters, memory counters, estado de overdraw, disponibilidade de avisos/counters, resumos de cena, piores frames, metadados de dispositivo, metadados de camera, metadados de configuracoes e custom metrics.

## Alerts

Regras podem reportar violacoes de budget, FPS baixo, GPU timing indisponivel e limites de overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Avisos do Editor sao limitados por cooldowns e podem ser desativados por configuracoes JSON ou controles runtime. Logs de alertas estruturados e avisos do Editor sao independentes: `PerformanceMeter.SetStructuredLogsEnabled(false)` suprime apenas a saida `Debug.Log` de alertas estruturados, enquanto `PerformanceMeter.SetEditorWarningLogsEnabled(false)` controla separadamente os logs de aviso do Editor. Callbacks, alerts/history, avisos do overlay e sessoes continuam ativos.

## External GPU Capture

Use o capture coordinator para uma solicitacao limitada de RenderDoc ou PIX quando a ferramenta ja estiver conectada:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

O coordinator permite apenas uma solicitacao ativa e avanca deterministicamente por `PreRoll`, `Capturing`, `PostRoll` e `Completed`. O mesmo ID ativo e idempotente; um ID diferente e rejeitado como sobreposicao. Pre-roll e post-roll contam frames da Unity; somente `Capturing` abre o alert capture scope e invoca o `ExternalGPUProfiler` experimental da Unity. Os gates obrigatorios sao Editor ou Development Build e uma ferramenta conectada. `RenderDoc` e permitido no desktop Windows/Linux com Direct3D 11, Direct3D 12 ou Vulkan; `PIX` e permitido no desktop Windows com Direct3D 12.

`Completed` significa somente que o wrapper lifecycle protegido terminou. A Unity nao expoe a identidade da ferramenta conectada nem um path autoritativo do artefato; `Status.Tool` e somente a ferramenta solicitada. O overload com `PerfMeterCaptureBundleOptions` separa samples baseline/capture e exporta atomicamente um bundle local ao projeto; um artefato externo permanece observed, nao authoritative. Para automacao use `perfmeter.capture.request/status/cancel/export/capabilities`.

## Diagnosticos De Overdraw

Overdraw numerico e opt-in e limitado.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw numerico e heatmap usam o diagnostic path de URP Render Graph. A medicao de overdraw requer `PerfMeterRenderGraphFeature`, suporte a replacement shader, suporte a UAV/storage-buffer em fragment, suporte a compute shader, uma graphics API suportada e async GPU readback. HDRP reporta overdraw/heatmap como unsupported, enquanto core overlay, session, API e MCP diagnostics continuam disponiveis. Alvos nao suportados reportam `OverdrawState.Unsupported` em vez de executar o pass.

## Reprodutibilidade De Camera E Device

Use snapshots para preservar o ambiente que produziu uma captura de performance.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

As exportacoes de sessao incluem metadados de device e camera para que uma captura possa ser entendida ou reproduzida depois.

## Custom Metrics

Registre providers especificos do projeto sem fazer fork do PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics sao expostas por leituras de API, exportacao JSON de sessao, MCP latest metrics e ate oito linhas de overlay quando o modulo `CustomMetrics` esta ativado.

## Instrumentacao Do Unity Profiler

A instrumentacao e interna e visivel somente ao criar perfil do Editor, de um Development Build ou de outro build com Profiler habilitado. Em Release players sem Profiler, esses markers/counters sao no-op e nao geram dados de instrumentacao; os schemas de public API, status, MCP e export permanecem inalterados.

- Os markers cobrem collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) e export JSON/CSV (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` e um hook interno reservado para providers.
- Os counters cobrem tempos de frame CPU/GPU (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) como gauges de fim de frame em nanossegundos. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` e `SGG.PerfMeter.Thermal.Available` codificam availability/active como `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` e `SGG.PerfMeter.Capture.State` usam enum codes; `SGG.PerfMeter.Provider.CustomMetricCount` e um count. Todos os counters usam a categoria `Scripts` e `FlushOnEndOfFrame`.
- Nenhum thermal sample sintetico e emitido; `SGG.PerfMeter.Thermal.Available` permanece em `0`/indisponivel ate que um provider de plataforma real forneca dados.

## Self-Observability E Budgets De Overhead

Use `PerformanceMeter.GetSelfOverhead()` ou `PerformanceMeter.GetStatus().SelfOverhead` para diagnosticar custo de callbacks CPU e alocacoes de collector, custom providers, CPU-core provider, overlay e integracao URP/HDRP. A medicao usa janelas fixas de 120 frames, medias por invocacao e budgets CPU/alocacao especificos por componente.

A render integration inativa reporta `Unsupported`, um componente suportado sem chamadas reporta `NotMeasured` e o self-timing GPU reporta `Unavailable`. O accounting e apenas diagnostico: PerfMeter nao subtrai overhead nem ajusta as metricas CPU/GPU existentes.

## Automacao De Agents

Uma execucao tipica dirigida por MCP:

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

`perfmeter.profiler.capabilities {}` e uma leitura em cache; nao inicia o runtime nem executa discovery.

## Workflow de snapshot de memoria opcional

1. Use Unity `6000.4+` e instale `com.unity.memoryprofiler` `1.1.0+` pelo Package Manager. O assembly opcional `SGG.PerfMeter.MemoryProfiler` registra automaticamente o backend; sem esse pacote a integracao core permanece unavailable.
2. Em Play Mode, leia `PerformanceMeter.GetMemorySnapshotCapabilities()` ou `perfmeter.memory.snapshot.capabilities` e confirme o backend e os capture flags solicitados.
3. Solicite um snapshot manual com `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))` ou configure `ConfigureMemorySnapshotTriggers(...)` para habilitar explicitamente um limite de memoria do sistema ou uma janela limitada de crescimento de vazamento.
4. Consulte `GetMemorySnapshotStatus()` ou `perfmeter.memory.snapshot.status` ate o snapshot e seu bundle correlacionado chegarem a um estado terminal. Exporte a evidencia pronta com `PerformanceMeter.ExportCaptureBundle(captureId)` ou `perfmeter.capture.export`.

A evidencia somente de memoria passa pela API existente de capture bundle em `Temp/PerfMeter/CaptureBundles`. O bundle registra `MemoryProfiler` como ferramenta solicitada, inclui proveniencia de memoria e SHA-256 em streaming para o `.snap`, e nao inclui artefato GPU externo. A origem pertencente ao PerfMeter fica em `Temp/PerfMeter/MemorySnapshots`; uma exportacao bem-sucedida a consome uma vez.

## Diagnostico de markers graficos

1. Chame `PerformanceMeter.GetGraphicsDiagnostics()` ou `perfmeter.graphics.diagnostics` para ler os valores mais recentes dos markers e o contexto da graphics API.
2. Verifique `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, component counts resolvidos/amostrados e a revisao do catalogo de cada capability. A discovery e dinamica: ocorre no inicio do runtime e durante refresh/reconfigure explicito do catalogo do profiler.
3. Trate os valores como valores brutos do recorder nas units descobertas. Um marker pode estar unavailable, disponivel sem sample ou sampled; zero numerico nao e um sinal universal de unavailable e o valor nao e garantidamente um count de shader ou PSO.

O shader marker resolve primeiro o nome exato `Shader.CreateGPUProgram` e depois os aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` e `Shader.DynamicLoadGPUProgram`. O pipeline marker resolve exatamente `CreatePSO.Job`. Os mesmos valores e a provenance estao disponiveis em `perfmeter.metrics.latest` e no JSON/CSV de sessao.

## Correlacao De Sessao Com Profile Analyzer

Durante o profiling, cada sessao emite os samples instantaneos `SGG.PerfMeter.Session.<sessionId>.Begin` e `.End`. `SGG/Perfmeter/Open Profile Analyzer For Session` abre a janela opcional do Profile Analyzer e copia o ID da sessao atual para a area de transferencia. O comando nao instala o Profile Analyzer, nao carrega dados do Profiler nem aplica um filtro automaticamente; depois de carregar o capture relevante, procure o ID copiado.

## Trace e prewarm de GraphicsStateCollection

1. No Unity `6000.4+`, confirme que o assembly opcional `SGG.PerfMeter.GraphicsStateCollection` esta disponivel. Use o namespace `UnityEngine.Experimental.Rendering.GraphicsStateCollection` no Unity `6000.4` e `UnityEngine.Rendering.GraphicsStateCollection` no Unity `6000.5+`.
2. Inicie uma sessao PerfMeter antes do trace. Execute `StartSession(...)` e depois `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` ou o request MCP correspondente. Sem sessao ativa, o request e rejeitado; a sessao deve continuar gravando ate o trace terminar, e `PerformanceMeter.StopSession()` cancela um trace ativo.
3. Mantenha o cenario em execucao enquanto o trace limitado avanca. No Play Mode normal, cada trace frame e tickado depois de `WaitForEndOfFrame`; em batch mode, o coordinator usa um fallback no frame seguinte. Samples de sessao admitidos nesse intervalo recebem `GraphicsStateTraceId`/`graphics_state_trace_id`; as configuracoes da sessao determinam quantos samples correlacionados sao mantidos.
4. Consulte `GetGraphicsStateCollectionStatus()` ou `perfmeter.graphics.state_collection.status` ate `Completed` e, se quiser, pare a sessao. Parar durante o trace ativo o cancela e pode manter `IsBusy`/`is_busy` true enquanto o cleanup owned e tentado novamente. O artifact `.graphicsstate` owned e relativo ao projeto, fica em `Temp/PerfMeter/GraphicsStateCollections` e e limitado a 64 MiB.
5. Passe o path relativo owned informado para `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` ou para o comando MCP de prewarm. O prewarm e sincrono, preserva o artifact e informa os warmups concluidos e `IsWarmedUp`; um progressive warmup pode terminar com warning explicito de incompleto.

O coordinator de graphics-state permite um unico flight e tambem rejeita overlap com external GPU capture, memory snapshot ou alert-capture ativos. O mesmo trace ID ativo retorna `AlreadyActive`; outro ID retorna `RejectedOverlap`. `CancelGraphicsStateTrace` cancela apenas o trace ativo/em preparacao correspondente e limpa seu artifact pendente. Se um artifact owned nao puder ser excluido, `HasPendingCleanup`/`has_pending_cleanup` permanece true, um sidecar adjacente `.delete-pending` e mantido e restaurado/tentado novamente apos um domain reload; `IsBusy`/`is_busy` e o warning permanecem visiveis ate o sucesso. O backend do Unity nao suporta cache-miss tracing, portanto nao existe evidencia de cache-miss.

## Contexto de render integration

Use o snapshot neutro quando precisar de uma visao independente da pipeline sobre a ultima render integration tipada:

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

Os mesmos dados tambem podem ser lidos via MCP:

```text
perfmeter.render.snapshot {}
```

Essas leituras nao iniciam a coleta do runtime. Verifique em conjunto `State`, `ObservationAgeFrames`, `LastObservedFrame` e `ObservationMatchesCurrentPipeline`. Depois de mudar a pipeline ou a configuracao do asset, a observation anterior fica stale; preserve o warning e o non-match e nao interprete seus valores de pass, mode, GRD ou VRS como atuais. A API legacy `PerformanceMeter.GetRenderGraphSnapshot()` e o comando `perfmeter.rendergraph.snapshot` continuam disponiveis.

Para diagnosticar GRD, verifique `DegradedReason`, suporte SRP, configuracao do projeto, suporte compute, compatibilidade do modo URP e `ActivityAvailability`. `IsObservedActive` e o estado enabled global do Unity. Use `Effectiveness` apenas como contexto BRG agregado: `AvailableNoSample`/`Unavailable` nao significam workload zero, e counters BRG positivos nao provam o uso de GRD por um renderer especifico.

No capture bundle, o schema `sgg.perfmeter.capture-context` versao `1` preserva `render` e adiciona `render_integration`. Em um external GPU capture, o contexto e congelado no primeiro sample da fase `Capturing`; um bundle do Memory Profiler o registra quando a solicitacao de memoria termina. Os schemas JSON/CSV de sessao nao mudam. A API publica nao oferece viewer estavel de RenderGraph/CustomPass nem pass targets; portanto este workflow nao promete navegacao no Editor.
