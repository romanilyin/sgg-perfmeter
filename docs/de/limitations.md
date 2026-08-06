# Einschraenkungen

SGG PerfMeter ist eine Low-Overhead-Runtime-Diagnoseschicht. Fuer tiefe Captures nutze Unity Profiler, RenderDoc, Profile Analyzer oder Frame Debugger.

## Plattform- Und Pipeline-Scope

- Unterstuetztes Runtime-Ziel: Unity `6000.4+` mit URP `17.4+` Render Graph oder HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline wird nicht unterstuetzt und ist nicht geplant.
- HDRP overdraw und heatmap werden nicht unterstuetzt. HDRP-Projekte behalten FPS-, CPU-, GPU-, memory-, sessions-, alerts-, camera-, device-, setup- und MCP diagnostics.
- Unity `2022.3` bis `6000.3` kann fuer Compile-Safety importieren, aber Runtime-Verhalten und Support zielen auf Unity `6000.4+`.

## Timing-Verfuegbarkeit

- GPU timing kann je nach Plattform und graphics API fehlen, verzoegert oder unzuverlaessig sein.
- `CollectionFrame` ist der Unity-Frame, in dem PerfMeter den Snapshot gesammelt hat, nicht zwingend der exakte Hardware-Frame aus `FrameTimingManager`.
- Android sollte Vulkan bevorzugen, wenn GPU frame timing wichtig ist.
- OpenGL/OpenGLES sollte als eingeschraenkter Modus fuer GPU timing und overdraw instrumentation behandelt werden.

## Counter-Verfuegbarkeit

Profiler counters variieren nach Plattform, Unity-Version, Render-Pipeline-Einstellungen und graphics API. Nutze `AvailableCounters`, `UnavailableCounters` und warnings statt anzunehmen, dass jeder Counter ueberall existiert.

## External GPU Capture

- Der Coordinator erlaubt eine aktive Anfrage und durchlaeuft deterministisch `PreRoll`, `Capturing`, `PostRoll` und `Completed`. Dieselbe aktive ID ist idempotent; eine andere aktive ID wird wegen Ueberlappung abgewiesen.
- Das Backend verwendet Unitys experimentellen `ExternalGPUProfiler` nur im Editor oder in Development Builds, wenn ein externes Tool bereits angehaengt ist. `RenderDoc` ist auf Windows/Linux desktop mit Direct3D 11, Direct3D 12 oder Vulkan unterstuetzt; `PIX` ist auf Windows desktop mit Direct3D 12 unterstuetzt.
- `Completed` bestaetigt nur den Unity wrapper lifecycle. Es beweist nicht, dass ein externes `.rdc`/`.wpix`-Artefakt existiert, und liefert keinen Artefaktpfad.
- Automatisierte Tests verwenden ein fake backend. Die Bestaetigung durch echtes externes Tool und Artefakt bleibt ein release gate.
- Correlated bundles und MCP capture control sind verfuegbar, aber eine uebergebene `.rdc`/`.wpix`-Datei bleibt nur ein beobachtetes und gehashtes Artefakt: Unity kann attached tool und artifact association nicht authentifizieren. Die Pruefung mit einem echten externen Tool bleibt release-candidate gate.

## Overdraw-Kosten Und Support

Numerical overdraw und visual heatmap sind Diagnosemodi. Sie fuegen Render-Arbeit hinzu und sollten in begrenzten Fenstern genutzt werden, nicht dauerhaft als Gameplay-UI.

Numerical overdraw in URP erfordert:

- `PerfMeterRenderGraphFeature` im aktiven URP Renderer;
- fragment-stage UAV/storage-buffer support;
- compute shader support;
- unterstuetzte graphics API;
- async GPU readback support.

Nicht unterstuetzte Ziele, einschliesslich HDRP, melden `OverdrawState.Unsupported` mit warnings.

## Overlay-Kosten

Der Overlay ist allokationsbewusst und gedrosselt, aber geaenderte Zahlenwerte und Graph-Labels koennen im Refresh-Intervall managed strings erzeugen. Es gibt zwei UI Toolkit backend paths: einen eigenen `UIDocument`-host auf Unity `6000.4` und einen eigenen `PanelRenderer`-host auf Unity `6000.5+`. Der host bewahrt panel settings und children fremder UI und baut nur den PerfMeter-eigenen container neu auf. Zahlenwerte verwenden stabile reservierte numeric slots und eine numeric monospace role; `FpsOnly` nutzt einen deterministischen begrenzten Zwei-Zeilen-fallback, wenn eine Zeile nicht passt, waehrend Karten und Balken bei schmaler logical width umbrechen. Das reduziert Clipping-Risiken, verspricht aber nicht jede beliebige resolution oder scale; schwere visuelle Diagnostik, Graph-Modi und das resultierende Layout muessen auf Zielgeraeten validiert werden.

## Validierungsstatus

Die aktuelle Validierung umfasst automatisierte EditMode-Abdeckung, HDRP smoke validation in Unity `6000.4.10f1` und fruehere Android S23 Vulkan/GLES smoke validation. Breitere Player-Build- und Geraeteabdeckung ist weiterhin sinnvoll, bevor Daten als Release-Signoff verwendet werden.
