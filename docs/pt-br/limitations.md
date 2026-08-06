# Limitacoes

SGG PerfMeter e projetado como uma camada runtime de diagnosticos de baixo overhead, nao como substituto de captura profunda para Unity Profiler, RenderDoc, Profile Analyzer ou Frame Debugger.

## Escopo De Plataforma E Pipeline

- Alvo runtime suportado: Unity `6000.4+` com URP `17.4+` Render Graph ou HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline nao tem suporte e nao esta planejado.
- HDRP overdraw e heatmap nao sao suportados. Projetos HDRP continuam com diagnostics de FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup e MCP.
- Unity `2022.3` ate `6000.3` pode importar para seguranca de compilacao, mas o comportamento runtime e o alvo de suporte sao Unity `6000.4+`.

## Disponibilidade De Timing

- GPU timing pode estar indisponivel, atrasado ou nao confiavel dependendo da plataforma e da graphics API.
- `CollectionFrame` e o frame Unity em que PerfMeter coletou o snapshot, nao necessariamente o frame exato de hardware representado por `FrameTimingManager`.
- Android deve preferir Vulkan quando GPU frame timing for importante.
- OpenGL/OpenGLES deve ser tratado como modo degradado para GPU timing e instrumentacao de overdraw.

## Disponibilidade De Counters

Profiler counters variam por plataforma, versao do Unity, configuracoes do render pipeline e graphics API. Use `AvailableCounters`, `UnavailableCounters` e avisos em vez de presumir que todos os counters existem em todos os lugares.

## External GPU Capture

- O coordinator permite uma solicitacao ativa e avanca deterministicamente por `PreRoll`, `Capturing`, `PostRoll` e `Completed`. O mesmo ID ativo e idempotente; um ID ativo diferente e rejeitado por sobreposicao.
- O backend usa o `ExternalGPUProfiler` experimental da Unity somente no Editor ou em Development Builds, quando uma ferramenta externa ja esta conectada. `RenderDoc` e limitado ao desktop Windows/Linux com Direct3D 11, Direct3D 12 ou Vulkan; `PIX` e limitado ao desktop Windows com Direct3D 12.
- `Completed` confirma somente o wrapper lifecycle da Unity. Nao prova que um artefato externo `.rdc`/`.wpix` exista e nao fornece um path do artefato.
- Os testes automatizados usam um fake backend. A confirmacao da ferramenta externa real e do artefato continua sendo um release gate.
- Correlated bundles e MCP capture control estao disponiveis, mas um `.rdc`/`.wpix` fornecido permanece apenas um artefato observed e hashed: a Unity nao pode autenticar a ferramenta conectada nem a associacao com o capture. A verificacao com uma ferramenta real continua sendo um release-candidate gate.

## Custo E Suporte De Overdraw

Overdraw numerico e heatmap visual sao modos diagnosticos. Eles adicionam trabalho de renderizacao e devem ser usados em janelas limitadas, sem permanecerem ativos como UI continua de gameplay.

Overdraw numerico em URP requer:

- `PerfMeterRenderGraphFeature` instalado no URP renderer ativo;
- suporte a UAV/storage-buffer no estagio de fragment;
- suporte a compute shader;
- graphics API suportada;
- suporte a async GPU readback.

Alvos nao suportados, incluindo HDRP, reportam `OverdrawState.Unsupported` com avisos.

## Custo Do Overlay

O overlay considera as alocacoes e usa throttling, mas valores numericos alterados e labels de graficos ainda podem materializar strings gerenciadas no intervalo de refresh. Ele tem dois backend paths de UI Toolkit: um host proprio `UIDocument` no Unity `6000.4` e um host proprio `PanelRenderer` no Unity `6000.5+`. O host preserva panel settings e children da UI estrangeira e reconstrói somente o container pertencente ao PerfMeter. Valores numericos usam numeric slots reservados estaveis e um numeric monospace role; `FpsOnly` usa um fallback deterministico e limitado de duas linhas quando uma linha nao cabe, enquanto cards e barras fazem wrap em logical widths estreitas. Isso reduz o risco de clipping, mas nao promete toda resolution ou scale arbitraria; diagnosticos visuais pesados, modos de grafico e o layout resultante devem ser validados nos dispositivos alvo.

## Status De Validacao

A validacao atual inclui cobertura automatizada EditMode, HDRP smoke validation no Unity `6000.4.10f1` e validacao smoke anterior no Android S23 Vulkan/GLES. Cobertura mais ampla de player-build e dispositivos ainda e util antes de tratar os dados como evidencia de sign-off de release.

## Limites e privacidade dos snapshots de memoria opcionais

- O recurso fica indisponivel sem `com.unity.memoryprofiler` `1.1.0+` no Unity `6000.4+`; o pacote core nao instala nem exige essa dependencia.
- A captura manual e a unica opcao padrao. Triggers de limite de memoria do sistema e crescimento limitado de vazamento sao opt-in; cada solicitacao passa por guards de single-flight/overlap, cooldown, espaco livre minimo, backend e capture flags.
- O staging `.snap` pertencente ao PerfMeter fica em `Temp/PerfMeter/MemorySnapshots` e e limitado a 512 MiB. A evidencia somente de memoria e exportada em `Temp/PerfMeter/CaptureBundles`, com quota total de retencao de 2 GiB. Uma exportacao bem-sucedida e de uso unico e remove a origem de staging, com avisos explicitos se a limpeza falhar.
- Snapshots podem conter memoria sensivel do processo. Proteja-os e revise-os antes de compartilhar. O bundle registra `contains_sensitive_memory`, proveniencia de backend/flags, `memory-snapshot.json` e metadados SHA-256; nao cria artefato GPU externo.
- Exclusao bloqueada pelo sistema operacional e protecao portable managed contra races com reparse points sao best-effort. Paths inseguros ou nao pertencentes sao rejeitados, e falhas de limpeza permanecem visiveis como warnings.
- A evidencia inclui memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, compilacao opcional com `com.unity.memoryprofiler@1.1.12` e Unity `6000.4.12f1` full EditMode `182/182` mais full PlayMode `14/14`. Isso nao afirma comportamento de release-player ou de dispositivos.

## Limites dos diagnosticos graficos e GraphicsStateCollection

- Os markers de criacao de programas GPU de shaders e de graphics pipelines sao capabilities dinamicas de `ProfilerRecorder`. Unity, plataforma, graphics API e estado do refresh do catalogo podem mudar sua availability. Use `Unavailable`, `AvailableNoSample`, `AvailableSampled` e a provenance; nao deduza availability de um valor zero.
- Os valores dos markers mantem `Unit` e `DataType` do recorder e permanecem brutos. Nao sao universalmente counts de shaders ou PSO, e o PerfMeter nao os converte para uma unidade comum. A capability metadata inclui resolucao exact/alias, nomes de recorder resolvidos, component counts resolvidos/amostrados e revisao do catalogo.
- O assembly opcional `SGG.PerfMeter.GraphicsStateCollection` tem como alvo Unity `6000.4+`. Usa `UnityEngine.Experimental.Rendering.GraphicsStateCollection` em `6000.4` e `UnityEngine.Rendering.GraphicsStateCollection` em `6000.5+`; versoes anteriores nao sao suportadas para esta integracao.
- Um trace exige sessao PerfMeter ativa. No Play Mode normal os trace frames terminam apos o end-of-frame; em batch mode usa-se fallback no frame seguinte. Samples correlacionados estao sujeitos ao warm-up, intervalo e limite maximo de samples da sessao.
- Apenas um graphics-state flight e admitido, incluindo preparacao, finalizacao do trace, prewarm e cleanup. External GPU capture, memory snapshot ou alert-capture ativos tambem causam rejeicao por overlap. `IsBusy`/`is_busy` cobre esses flights e o cleanup persistente; `HasPendingCleanup`/`has_pending_cleanup` informa especificamente um artifact owned aguardando retry. O cancel correspondente e best-effort; falhas de cleanup ficam visiveis e podem atrasar o proximo request.
- `StopSession()` cancela um trace ativo, portanto uma sessao ativa e necessaria durante todo o trace. Uma exclusao falha do artifact owned cria um sidecar adjacente `.delete-pending`; ele e restaurado e tentado novamente apos um domain reload. Warning e estado busy permanecem visiveis ate que artifact e marker sejam removidos.
- O prewarm aceita apenas artifact owned relativo ao projeto, executa de forma sincrona, preserva o artifact e pode informar progressive warmup incompleto. O backend Unity nao suporta cache-miss tracing: o request retorna `Unavailable` e nenhuma evidencia de cache-miss e exposta.
- Artifacts `.graphicsstate` owned ficam em `Temp/PerfMeter/GraphicsStateCollections`, devem ser arquivos regulares nao vazios e sao limitados a 64 MiB. O trace e limitado a 600 frames e o prewarm progressivo a 1.000.000 states. Guards de espaco livre minimo e paths locais ao projeto se aplicam.
- A evidencia final inclui compile aprovado no Unity `6000.4.12f1`; GSC EditMode targeted `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208` e full post-fix PlayMode `16/16`. Um optional consumer compile isolado no Unity `6000.5.6f1` tambem foi aprovado. Full tests do Unity `6000.5`, comportamento de release-player e de dispositivos continuam sendo release gates e nao sao afirmados aqui.
