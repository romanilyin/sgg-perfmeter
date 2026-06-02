# Einschraenkungen

SGG PerfMeter ist eine Low-Overhead-Runtime-Diagnoseschicht. Fuer tiefe Captures nutze Unity Profiler, RenderDoc, Profile Analyzer oder Frame Debugger.

## Plattform- Und Pipeline-Scope

- Unterstuetztes Runtime-Ziel: Unity `6000.4+` mit URP `17.4+` und Render Graph.
- Built-in Render Pipeline wird nicht unterstuetzt und ist nicht geplant.
- HDRP-Unterstuetzung ist geplant, aber in `2026.6.5-1` nicht implementiert.
- Unity `2022.3` bis `6000.3` kann fuer Compile-Safety importieren, aber Runtime-Verhalten und Support zielen auf Unity `6000.4+`.

## Timing-Verfuegbarkeit

- GPU timing kann je nach Plattform und graphics API fehlen, verzoegert oder unzuverlaessig sein.
- `CollectionFrame` ist der Unity-Frame, in dem PerfMeter den Snapshot gesammelt hat, nicht zwingend der exakte Hardware-Frame aus `FrameTimingManager`.
- Android sollte Vulkan bevorzugen, wenn GPU frame timing wichtig ist.
- OpenGL/OpenGLES sollte als eingeschraenkter Modus fuer GPU timing und overdraw instrumentation behandelt werden.

## Counter-Verfuegbarkeit

Profiler counters variieren nach Plattform, Unity-Version, Render-Pipeline-Einstellungen und graphics API. Nutze `AvailableCounters`, `UnavailableCounters` und warnings statt anzunehmen, dass jeder Counter ueberall existiert.

## Overdraw-Kosten Und Support

Numerical overdraw und visual heatmap sind Diagnosemodi. Sie fuegen Render-Arbeit hinzu und sollten in begrenzten Fenstern genutzt werden, nicht dauerhaft als Gameplay-UI.

Numerical overdraw erfordert:

- `PerfMeterRenderGraphFeature` im aktiven URP Renderer;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- unterstuetzte graphics API;
- async GPU readback support.

Nicht unterstuetzte Ziele melden `OverdrawState.Unsupported` mit warnings.

## Overlay-Kosten

Der Overlay ist allokationsbewusst und gedrosselt, aber geaenderte Zahlenwerte und Graph-Labels koennen im Refresh-Intervall managed strings erzeugen. Schwere visuelle Diagnostik und Graph-Modi sollten auf Zielgeraeten validiert werden.

## Validierungsstatus

Die aktuelle Validierung umfasst automatisierte EditMode- und PlayMode-Abdeckung plus Android S23 Vulkan/GLES smoke validation. Breitere Player-Build- und Geraeteabdeckung ist weiterhin sinnvoll, bevor Daten als Release-Signoff verwendet werden.
