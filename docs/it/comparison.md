# Confronto Con Advanced FPS Counter E Graphy

Questo e un confronto di prodotto e architettura, non un benchmark runtime misurato.

## Versione Breve

Advanced FPS Counter e Graphy sono overlay visivi general-purpose solidi. Sono utili quando il bisogno principale e un HUD FPS/memoria/device rapido da integrare, con ampio supporto per versioni Unity meno recenti e personalizzazione visiva.

SGG PerfMeter e intenzionalmente piu mirato e diagnostico: Unity `6000.4+`, URP `17.4+`, Render Graph, snapshot strutturati, esportazione sessioni, diagnostica overdraw, metadati camera/device riproducibili e automazione MCP/API.

## Tabella Di Confronto

| Area | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Posizionamento primario | 🔵 Diagnostica URP Render Graph / HDRP Custom Pass + API di profiling pronta per automazione | ⚠️ Counter FPS/memoria/device in-game flessibile | ⚠️ Monitor e debugger visivo per statistiche FPS/memoria/audio |
| Target Unity | ⚠️ Unity `6000.4+`, URP `17.4+` | 🔵 Ampio supporto per Unity meno recenti | 🔵 Ampio supporto per Unity meno recenti |
| Backend UI | 🔵 Overlay UI Toolkit | ⚠️ Label uGUI Canvas/Text | ⚠️ Moduli uGUI Text/Image |
| Sorgente timing | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Campionamento runtime frame/update | ⚠️ Campionamento storico `Time.unscaledDeltaTime` |
| Split CPU/GPU | 🔵 CPU frame, main thread, render thread, present wait, GPU quando disponibile | 🛑 Nessuno split equivalente | 🛑 Nessuno split equivalente |
| Classificazione bottleneck | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 Nessun equivalente | 🛑 Nessun equivalente |
| Counter render | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 Nessun set di counter URP/SRP | 🛑 Nessun set di counter URP/SRP |
| Riproducibilita device/camera | 🔵 Snapshot strutturati di device e camera | ⚠️ Solo pannello device | ⚠️ Solo pannello device |
| Sessioni | 🔵 Recorder limitato, warm-up, ambito scena, frame peggiori, esportazione JSON/CSV | 🛑 Non e una feature primaria | ⚠️ Idea simile a roadmap |
| Overdraw | 🔵 Misurazione numerica + heatmap visiva tramite URP Render Graph | 🛑 No | 🛑 No |
| Automazione | 🔵 Superficie comandi MCP e snapshot pubblici | 🛑 No | 🛑 No |

## Cosa SGG PerfMeter Fa Meglio

- Spiega i probabili colli di bottiglia dei frame con CPU frame, main thread, render thread, present wait, GPU timing e dati di frame budget.
- Espone counter render orientati a URP e diagnostica Render Graph.
- Produce report prestazionali riproducibili con scena, device, camera, impostazioni, sample sessione, riepiloghi e metadati dei frame peggiori.
- Fornisce dati strutturati a strumenti e automazione tramite API pubblica e comandi MCP.
- Integra misurazione overdraw limitata e heatmap visiva come diagnostica esplicita.

## Cosa I Concorrenti Fanno Ancora Meglio

- Entrambi i concorrenti supportano un intervallo piu ampio di versioni Unity meno recenti, un vantaggio per progetti legacy.
- Advanced FPS Counter ha una UX di counter visivo molto diretta, personalizzazione inspector matura, toggle con hotkey/circle gesture, pattern UI min/max/average ed esempi VR/world-space.
- Graphy ha materiale marketing pubblico forte, stati modulo chiari, personalizzazione visiva, hotkey/background mode, UX matura dei debugger packet e ampia notorieta pubblica.

## Cosa Non Dichiarare

- SGG PerfMeter non sostituisce Unity Profiler, RenderDoc, Profile Analyzer o Frame Debugger.
- SGG PerfMeter non e zero-overhead; usa low-overhead e documenta i costi diagnostici espliciti.
- SGG PerfMeter non e un pacchetto di compatibilita legacy per tutte le piattaforme e tutte le pipeline.
