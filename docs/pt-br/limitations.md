# Limitacoes

SGG PerfMeter e projetado como uma camada runtime de diagnosticos de baixo overhead, nao como substituto de captura profunda para Unity Profiler, RenderDoc, Profile Analyzer ou Frame Debugger.

## Escopo De Plataforma E Pipeline

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.
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

## Custo E Suporte De Overdraw

Overdraw numerico e heatmap visual sao modos diagnosticos. Eles adicionam trabalho de renderizacao e devem ser usados em janelas limitadas, sem permanecerem ativos como UI continua de gameplay.

Overdraw numerico requer:

- `PerfMeterRenderGraphFeature` instalado no URP renderer ativo;
- suporte a UAV/storage-buffer no estagio de fragment;
- suporte a compute shader;
- graphics API suportada;
- suporte a async GPU readback.

Alvos nao suportados reportam `OverdrawState.Unsupported` com avisos.

## Custo Do Overlay

O overlay e consciente de alocacoes e usa throttling, mas valores numericos alterados e labels de graficos ainda podem materializar strings gerenciadas no intervalo de refresh. Diagnosticos visuais pesados e modos de grafico devem ser validados nos dispositivos alvo.

## Status De Validacao

A validacao atual inclui cobertura automatizada EditMode e PlayMode, alem de validacao smoke Android S23 Vulkan/GLES. Cobertura mais ampla de player-build e dispositivos ainda e util antes de tratar os dados como evidencia de sign-off de release.
