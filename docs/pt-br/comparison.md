# Comparacao Com Advanced FPS Counter E Graphy

Esta e uma comparacao de produto e arquitetura, nao um benchmark runtime medido.

## Versao Curta

Advanced FPS Counter e Graphy sao overlays visuais generalistas fortes. Eles sao uteis quando a principal necessidade e um HUD rapido de FPS/memoria/device, com suporte amplo a versoes antigas do Unity e customizacao visual.

SGG PerfMeter e intencionalmente mais estreito e mais diagnostico: Unity `6000.4+`, URP `17.4+` Render Graph, HDRP `17.4+` Custom Pass diagnostics, snapshots estruturados, exportacao de sessao, URP overdraw diagnostics, metadados reproduziveis de camera/device e automacao MCP/API.

## Tabela De Comparacao

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Posicionamento principal | 🔵 Diagnosticos URP Render Graph / HDRP Custom Pass + API de profiling pronta para automacao | ⚠️ Contador flexivel in-game de FPS/memoria/device | ⚠️ Monitor visual de stats de FPS/memoria/audio + debugger |
| Alvo Unity | ⚠️ Unity `6000.4+`, URP `17.4+` / HDRP `17.4+` | 🔵 Suporte amplo a versoes antigas do Unity | 🔵 Suporte amplo a versoes antigas do Unity |
| Backend de UI | 🔵 Overlay UI Toolkit | ⚠️ Labels uGUI Canvas/Text | ⚠️ Modulos uGUI Text/Image |
| Fonte de timing | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Sampling runtime de frame/update | ⚠️ Sampling de historico por `Time.unscaledDeltaTime` |
| Separacao CPU/GPU | 🔵 CPU frame, main thread, render thread, present wait, GPU quando disponivel | 🛑 Sem separacao equivalente | 🛑 Sem separacao equivalente |
| Classificacao de gargalo | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 Sem equivalente | 🛑 Sem equivalente |
| Render counters | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 Sem conjunto de counters URP/SRP | 🛑 Sem conjunto de counters URP/SRP |
| Reprodutibilidade de device/camera | 🔵 Snapshots estruturados de device e camera | ⚠️ Apenas painel de device | ⚠️ Apenas painel de device |
| Sessions | 🔵 Gravador limitado, warm-up, escopo de cena, piores frames, exportacao JSON/CSV | 🛑 Nao e feature primaria | ⚠️ Ideia semelhante a roadmap |
| Overdraw | 🔵 Medicao numerica + heatmap visual por URP Render Graph; explicit unsupported state em HDRP | 🛑 Nao | 🛑 Nao |
| Automacao | 🔵 Superficie de comandos MCP e snapshots publicos | 🛑 Nao | 🛑 Nao |

## Onde SGG PerfMeter E Melhor

- Explica provaveis gargalos de frame com CPU frame, main thread, render thread, present wait, GPU timing e dados de frame budget.
- Expoe render counters orientados a URP, diagnosticos URP Render Graph e observacao HDRP Custom Pass.
- Produz relatorios de performance reproduziveis com metadados de cena, device, camera, configuracoes, amostras de sessao, resumos e piores frames.
- Fornece dados estruturados a ferramentas e automacao por API publica e comandos MCP.
- Integra medicao limitada de overdraw e heatmap visual como diagnosticos explicitos.

## Onde Concorrentes Ainda Sao Melhores

- Ambos os concorrentes suportam uma faixa mais ampla de versoes antigas do Unity, o que e uma vantagem para projetos legados.
- Advanced FPS Counter tem UX visual de contador drop-in muito direta, customizacao madura no Inspector, hotkeys/circle gesture toggles, padroes de UI min/max/average e exemplos em VR/world-space.
- Graphy tem material publico de marketing forte, estados claros de modulos, customizacao visual, hotkeys/background mode, UX madura de pacotes de debugger e ampla consciencia publica.

## O Que Nao Declarar

- SGG PerfMeter nao substitui Unity Profiler, RenderDoc, Profile Analyzer ou Frame Debugger.
- SGG PerfMeter nao tem overhead zero; use baixo overhead e documente custos diagnosticos explicitos.
- SGG PerfMeter nao e um pacote de compatibilidade legado para todas as plataformas e todos os pipelines.
