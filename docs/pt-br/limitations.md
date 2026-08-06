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
