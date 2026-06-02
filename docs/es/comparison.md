# Comparación Con Advanced FPS Counter Y Graphy

Esta es una comparación de producto y arquitectura, no un benchmark runtime medido.

## Versión Corta

Advanced FPS Counter y Graphy son overlays visuales generalistas sólidos. Son útiles cuando la necesidad principal es un HUD rápido de FPS/memory/device con amplio soporte para versiones antiguas de Unity y personalización visual.

SGG PerfMeter tiene un alcance intencionadamente más estrecho y más diagnóstico: Unity `6000.4+`, URP `17.4+`, Render Graph, snapshots estructurados, exportación de sesiones, diagnósticos de overdraw, metadatos reproducibles de camera/device y automatización MCP/API.

## Tabla Comparativa

| Área | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Posicionamiento principal | 🔵 Diagnósticos URP Render Graph + API de profiling lista para automatización | ⚠️ Contador FPS/memory/device flexible dentro del juego | ⚠️ Monitor visual de estadísticas FPS/memory/audio + debugger |
| Target Unity | ⚠️ Unity `6000.4+`, URP `17.4+` | 🔵 Amplio soporte de Unity antiguo | 🔵 Amplio soporte de Unity antiguo |
| Backend UI | 🔵 Overlay UI Toolkit | ⚠️ uGUI Canvas/Text labels | ⚠️ Módulos uGUI Text/Image |
| Fuente de timing | 🔵 `FrameTimingManager` + rolling stats | ⚠️ Sampling runtime de frame/update | ⚠️ Sampling de historial de `Time.unscaledDeltaTime` |
| Separación CPU/GPU | 🔵 CPU frame, main thread, render thread, present wait, GPU cuando está disponible | 🛑 Sin separación equivalente | 🛑 Sin separación equivalente |
| Clasificación de cuello de botella | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 Sin equivalente | 🛑 Sin equivalente |
| Contadores de render | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 Sin conjunto de contadores URP/SRP | 🛑 Sin conjunto de contadores URP/SRP |
| Reproducibilidad device/camera | 🔵 Snapshots estructurados de device y camera | ⚠️ Solo panel de device | ⚠️ Solo panel de device |
| Sesiones | 🔵 Recorder acotado, warm-up, alcance por escena, peores frames, exportación JSON/CSV | 🛑 No es una función principal | ⚠️ Idea similar a roadmap |
| Overdraw | 🔵 Medición numérica + heatmap visual mediante URP Render Graph | 🛑 No | 🛑 No |
| Automatización | 🔵 Superficie de comandos MCP y snapshots públicos | 🛑 No | 🛑 No |

## Qué Hace Mejor SGG PerfMeter

- Explica cuellos de botella probables de frames con CPU frame, main thread, render thread, present wait, GPU timing y datos de frame budget.
- Expone contadores de render orientados a URP y diagnósticos Render Graph.
- Produce informes de rendimiento reproducibles con scene, device, camera, settings, session samples, summaries y metadatos de peores frames.
- Proporciona datos estructurados a herramientas y automatización mediante API pública y comandos MCP.
- Integra medición de overdraw acotada y un heatmap visual como diagnósticos explícitos.

## Qué Siguen Haciendo Mejor Los Competidores

- Ambos competidores soportan un rango más amplio de versiones antiguas de Unity, lo que es una ventaja para proyectos legacy.
- Advanced FPS Counter tiene una UX muy directa de contador visual drop-in, personalización madura en inspector, hotkeys/toggles por gesto circular, patrones UI de min/max/average y ejemplos de VR/world-space.
- Graphy tiene material público de marketing sólido, estados de módulos claros, personalización visual, hotkeys/background mode, UX madura de paquetes de debugger y amplio reconocimiento público.

## Qué No Afirmar

- SGG PerfMeter no reemplaza a Unity Profiler, RenderDoc, Profile Analyzer ni Frame Debugger.
- SGG PerfMeter no tiene overhead cero; usa low-overhead y documenta los costes explícitos de diagnóstico.
- SGG PerfMeter no es un paquete de compatibilidad legacy para todas las plataformas y todos los pipelines.
