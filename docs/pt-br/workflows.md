# Workflows

## Configuracao Do FTUE E Continuacoes

Abra `SGG/Perfmeter/Setup` e selecione a aba **FTUE**. As verificacoes obrigatorias cobrem compatibilidade, integracao de render, Frame Timing Stats, o caminho do package e um JSON de settings carregado. As linhas opcionais podem ser instaladas ou ignoradas; uma linha instalada mostra a proxima acao em vez de afirmar silenciosamente que o workflow esta concluido.

### Memory Profiler

Depois de instalar `com.unity.memoryprofiler`, a linha **Memory Profiler** oferece **Open Window/Analysis/Memory Profiler**, **Copy RequestMemorySnapshot Snippet**, **Copy Memory Trigger Snippet**, **Open Runtime** e **Reveal Snapshots** quando a pasta gerenciada existe. Os snippets copiados sao codigo runtime que o projeto deve invocar; o FTUE nao solicita um snapshot nem configura triggers por conta propria. Arquivos `.snap` one-shot sao preparados em `Temp/PerfMeter/MemorySnapshots`; abra ou copie o resultado antes que uma solicitacao posterior ou a limpeza do runtime remova a fonte gerenciada.

O snippet one-shot e:

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

O snippet de trigger opt-in e:

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

Use **Open Runtime** para inspecionar o snapshot de capability/status. A captura manual e o padrao; os limites de trigger permanecem desabilitados ate serem configurados explicitamente.

### Profile Analyzer

A linha instalada **Profile Analyzer** oferece **Open Profile Analyzer** e **Open Runtime**. Comece a gravar primeiro no Unity Profiler e depois inicie e pare uma sessao do PerfMeter dentro dessa gravacao. O opener usa `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()` para abrir o Profile Analyzer e copiar o ID da sessao; carregue os dados gravados do Profiler e procure esse ID. Ele nao instala o Profile Analyzer, nao carrega dados do Profiler e nao aplica um filtro automaticamente.

### Adaptive Performance

A linha instalada **Adaptive Performance** oferece **Open Runtime** para inspecionar o status atual do provider de telemetria opcional. A acao do FTUE nao inicia uma sessao nem faz captura.

### RenderDoc

RenderDoc e uma ferramenta externa e nao vem incluido com o PerfMeter. Siga o fluxo oficial de integracao do Unity:

1. Instale o RenderDoc pela pagina oficial de download: <https://renderdoc.org/builds>.
2. Salve as alteracoes do projeto e use **Load RenderDoc** no menu da aba Game View ou Scene View. Como alternativa, inicie o Unity Editor ou um Development Build pelo RenderDoc; reinicie o Unity se ele nao expuser a conexao depois da instalacao. O guia oficial do Unity e <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html>.
3. Clique em **Check Attachment** no FTUE. Isso atualiza apenas o sinal compartilhado de external-profiler do Unity; o FTUE nao consegue detectar a instalacao do RenderDoc e o Unity nao consegue identificar RenderDoc em vez de PIX por esse sinal.
4. Clique em **Copy Capture Snippet**, entre no Play Mode e invoque o codigo copiado a partir do codigo runtime do projeto:

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions("ftue-renderdoc-capture", PerfMeterCaptureTool.RenderDoc, 1));
   ```

5. No Editor Windows x64, voce pode primeiro usar **Download Verified Bridge** ou **Install Local Bridge**; somente o bridge separado exatamente fixado e instalado como plugin Editor-only, nunca o RenderDoc. Reinicie o Editor. A solicitacao nativa copiada usa `NativeRequired` + `Copy`; MetadataOnly e `DoNotShare` e Copy/Embed sao `ReviewBeforeShare`.

### GraphicsStateCollection

A linha opcional incluida **GraphicsStateCollection** nao precisa de instalacao de package. Ela oferece **Open Runtime**, **Copy Trace Snippet**, **Copy Prewarm Snippet** e **Reveal Artifacts**. O FTUE nao solicita automaticamente um trace nem um prewarm. Use esta sequencia:

1. No Play Mode, inicie e mantenha gravando uma sessao do PerfMeter com `PerformanceMeter.StartSession(...)`.
2. Invoque o codigo de trace copiado a partir do codigo runtime do projeto:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. Consulte `PerformanceMeter.GetGraphicsStateCollectionStatus()` ate que `State == PerfMeterGraphicsStateCollectionState.Completed`. Use seu `ArtifactRelativePath`, que aponta para baixo de `Temp/PerfMeter/GraphicsStateCollections`, como entrada do prewarm. Parar a sessao durante o tracing cancela o trace.
4. Substitua `<trace-artifact-file>` no snippet de prewarm copiado pelo caminho retornado:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. Clique em **Reveal Artifacts** depois de um trace para revelar a pasta de artefatos local do projeto. O prewarm e sincrono, preserva o artefato e pode reportar um progressive warmup incompleto. O trace e limitado a 600 frames e os artefatos gerenciados a 64 MiB; o backend do Unity nao fornece evidencia de cache miss.

## Bootstrap Completo De Inicializacao

Em **Setup > Initialization Code**, clique em **Refresh from Project Settings** e depois em **Copy Init Code**. O `PerfMeterBootstrap` gerado incorpora o snapshot completo e normalizado das configuracoes do projeto e chama `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)` depois do carregamento da cena. Ele transporta configuracoes de overlay, logging, alert, session-default e overdraw, respeita `enabled` e `collectionMode: Stopped` e nao executa `StartSession` nem uma solicitacao de captura.

Use este bootstrap explicito em vez do caminho de settings zero-code do Resources quando for preferida uma inicializacao controlada por codigo. Se ambos estiverem presentes, uma chamada explicita analisada com sucesso suprime o callback de auto-start do Resources para o dominio atual; se o Resources ja tiver iniciado primeiro, o snapshot explicito sera aplicado depois e se tornara authoritative. Um JSON explicito invalido deixa o runtime atual inalterado e nao suprime um auto-start posterior do Resources. As operacoes de sessao e overdraw padrao usam o snapshot runtime explicito ativo.

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

`GenericUnity` preserva a matriz anterior de `ExternalGPUProfiler` e nao pode autenticar tool/artifact. `NativePreferred` so pode fazer fallback antes de begin; `NativeRequired` nunca. Native RenderDoc e suportado somente no Editor Unity Windows x64 com D3D11, D3D12 ou Vulkan.

Generic `Completed` continua sendo apenas wrapper lifecycle. O status nativo informa backend kind e generation-bound phase e pode autenticar um `.rdc` finalizado. Artefatos generic/caller continuam observed. MCP aceita `backend_mode`, mas o storage mode e selecionado na API C#.

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

## Janela De Analise Da Sessao

Abra `SGG/Perfmeter/Session Analysis` para uma visualizacao somente leitura, no Editor, da sessao atual em memoria. As abas virtualizadas mostram a timeline dos samples retidos, o worst frame autoritativo com detalhes do sample disponiveis, violacoes derivadas dos budgets CPU-main/CPU-render/GPU e os scopes autoritativos whole-run/current-scene. CPU-main exclui present wait; valores e violacoes GPU exigem disponibilidade explicita do timing GPU.

A janela le somente `GetSessionSummary()` e `GetSessionSamples()` e nunca inicia o runtime. Timing indisponivel aparece como `Unavailable`, nao como zero numerico. Uma sessao parada permanece visivel enquanto sua instancia runtime existir; `PerformanceMeter.Stop()`, um domain reload ou sair do Play Mode podem descartar a sessao em memoria.

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
