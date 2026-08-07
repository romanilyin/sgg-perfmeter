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

## Grenzen und Datenschutz optionaler Speicher-Snapshots

- Die Funktion ist ohne `com.unity.memoryprofiler` `1.1.0+` unter Unity `6000.4+` nicht verfuegbar; das Core-Paket installiert oder benoetigt diese Abhaengigkeit nicht.
- Standardmaessig ist nur manuelle Aufnahme aktiv. System-Speicherschwellen und begrenztes Leak-Wachstum sind opt-in; jede Anfrage unterliegt Single-Flight/Overlap-, cooldown-, Mindestfreispeicher-, Backend- und Capture-Flag-Guards.
- Eigenes `.snap`-Staging liegt unter `Temp/PerfMeter/MemorySnapshots` und ist auf 512 MiB begrenzt. Memory-only-Evidence wird unter `Temp/PerfMeter/CaptureBundles` exportiert; die gesamte Bundle-Quota betraegt 2 GiB. Ein erfolgreicher Export ist einmalig und entfernt die Staging-Quelle, mit expliziten Cleanup-Warnungen bei Problemen.
- Speicher-Snapshots koennen sensible Prozessspeicher enthalten. Schuetze und pruefe sie vor dem Teilen. Das Bundle markiert `contains_sensitive_memory`, speichert Backend-/Flag-Provenance, `memory-snapshot.json` und SHA-256-Metadaten und erzeugt kein externes GPU-Artefakt.
- Loeschung bei OS-Sperren und portable managed Reparse-Point-Race-Schutz sind best effort. Unsichere oder fremde Pfade werden abgewiesen; Cleanup-Fehler bleiben als Warnungen sichtbar.
- Die Evidenz umfasst Memory EditMode `9/9`, Capture-Bundle EditMode `14/14`, PlayMode-Schwelle `1/1`, optionale Kompilierung mit `com.unity.memoryprofiler@1.1.12` sowie Unity `6000.4.12f1` Full EditMode `182/182` und Full PlayMode `14/14`. Dies ist keine Aussage zu Release-Player oder Geraeteverhalten.

## Grenzen der Grafikdiagnose und GraphicsStateCollection

- Shader-GPU-Programm- und Graphics-Pipeline-Marker sind dynamische `ProfilerRecorder`-Faehigkeiten. Unity, Plattform, Graphics-API und der Zustand eines Katalog-Refreshs koennen die Availability veraendern. Verwende `Unavailable`, `AvailableNoSample`, `AvailableSampled` und die Provenance; leite Availability nicht aus einem Nullwert ab.
- Markerwerte behalten `Unit` und `DataType` des Recorders und bleiben rohe Werte. Sie sind nicht grundsaetzlich Shader- oder PSO-Counts, und PerfMeter rechnet sie nicht in eine gemeinsame Einheit um. Exact/Alias-Aufloesung, aufgeloeste Recorder-Namen, aufgeloeste/gesampelte Component-Counts und Katalogrevision gehoeren zu den Capability-Metadaten.
- Die optionale Assembly `SGG.PerfMeter.GraphicsStateCollection` zielt auf Unity `6000.4+`. Sie verwendet `UnityEngine.Experimental.Rendering.GraphicsStateCollection` unter Unity `6000.4` und `UnityEngine.Rendering.GraphicsStateCollection` unter Unity `6000.5+`; aeltere Unity-Versionen werden fuer diese Integration nicht unterstuetzt.
- Ein Trace erfordert eine aktive PerfMeter-Session. Trace-Frames werden im normalen Play Mode nach dem Frame-Ende und im Batch Mode mit einem Next-Frame-Fallback abgeschlossen. Korrelierte Session-Samples unterliegen Warm-up-, Intervall- und Max-Sample-Einstellungen der Session.
- Es wird nur ein Graphics-State-Flight zugelassen, einschliesslich Vorbereitung, Trace-Finalisierung, Prewarm und Cleanup. Aktives externes GPU-Capture, Memory-Snapshot oder Alert-Capture fuehrt ebenfalls zu Overlap-Rejection. `IsBusy`/`is_busy` deckt diese Flights und persistiertes Cleanup ab; `HasPendingCleanup`/`has_pending_cleanup` meldet gezielt ein eigenes Artefakt, das auf einen Retry wartet. Passendes Cancel ist best effort; Cleanup-Fehler bleiben sichtbar und koennen die naechste Anfrage verzoegern.
- `StopSession()` bricht einen aktiven Trace ab, daher ist eine aktive Session waehrend des gesamten Traces erforderlich. Eine fehlgeschlagene Loeschung des eigenen Artefakts erzeugt einen benachbarten `.delete-pending`-Sidecar; nach Domain Reload wird er wiederhergestellt und das Cleanup erneut versucht. Warnung und Busy-State bleiben sichtbar, bis Artefakt und Marker entfernt sind.
- Prewarm akzeptiert nur ein eigenes project-relatives Artefakt, laeuft synchron, bewahrt das Artefakt und kann ein unvollstaendiges progressives Warmup melden. Das Unity-Backend unterstuetzt kein Cache-Miss-Tracing; die Anfrage liefert `Unavailable`, und es wird keine Cache-Miss-Evidence ausgegeben.
- Eigene `.graphicsstate`-Artefakte liegen unter `Temp/PerfMeter/GraphicsStateCollections`, muessen regulaere nichtleere Dateien sein und sind auf 64 MiB begrenzt. Trace-Laenge ist auf 600 Frames, progressives Prewarm auf 1.000.000 States begrenzt. Mindestfreispeicher- und project-lokale Pfad-Guards gelten.
- Die finale Evidenz umfasst einen bestandenen Compile mit Unity `6000.4.12f1`, targeted GSC EditMode `25/25`, `PerformanceMeter` API EditMode `47/47`, Capture-Bundle EditMode `14/14`, PlayMode smoke `12/12`, Full Post-Fix EditMode `208/208` und Full Post-Fix PlayMode `16/16`. Ein isolierter optionaler Consumer-Compile mit Unity `6000.5.6f1` ist ebenfalls bestanden. Full Unity `6000.5`-Tests, Release-Player- und Geraeteverhalten bleiben Release-Gates und werden hier nicht behauptet.

## Grenzen des Render-Integrationskontexts

- `PerfMeterRenderIntegrationSnapshot` ist ein integrationsneutraler Beobachtungsvertrag, kein tiefer Render-Graph- oder Custom-Pass-Capture. Lesevorgaenge starten keine Runtime; vor der ersten Observation kann die unterstuetzte aktuelle Pipeline `Available` mit `NotObserved` sein. Eine Pipeline-/Konfigurationsaenderung markiert die alte Observation mit `ObservationMatchesCurrentPipeline: false`, explizitem Frame/Age und Warning als veraltet.
- URP verwendet das oeffentliche aktuelle `UniversalRenderingData.renderingMode` und meldet die tatsaechlich geplanten PerfMeter-Passes. HDRP meldet den tatsaechlichen PerfMeter-`CustomPass`, der effektive Rendering-Modus bleibt jedoch unavailable.
- Private/interne Pass-/Ressourcen-Reflection wurde entfernt. Die Legacy-Fassade laesst `registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count` und `aliased_resource_count` auf `-1`, weil keine stabile oeffentliche API sie liefert.
- GRD meldet konfigurierten Modus und oeffentlichen SRP-Support, aber die Aktivitaet ist `Unknown`, weil sich die Enabled-Semantik in Unity 17.4 und 17.5 unterscheidet. Tiefere GRD-Telemetrie gehoert zu `PM-GRD-001`; dieser Snapshot behauptet keine GRD-Aktivitaet.
- VRS meldet autoritativen Hardware-Support aus `SystemInfo`/`ShadingRateInfo`. Konfiguration und Aktivitaet bleiben `Unknown`, bis ein kuenftiger typisierter Adapter sie beweist; keine VRS-Aktivitaet wird behauptet.
- Unity bietet keinen stabilen oeffentlichen RenderGraph-/CustomPass-Viewer und keine Pass-Target-API. Deshalb fuegt PerfMeter keine Editor-Navigation hinzu und verspricht sie nicht.
- Capture-Context-Schema v1 behaelt `render` bei und fuegt `render_integration` hinzu; Session-JSON/CSV-Schemas bleiben unveraendert. Bei externem Capture wird der Kontext beim ersten `Capturing`-Sample eingefroren und nicht durch spaetere Reads ersetzt.
- PM-REN-001-Finalevidenz: Unity `6000.4.12f1` Main-Compile bestanden; targeted `PerformanceMeterApiTests` `53/53`, `PerfMeterCaptureBundleTests` `15/15` und `PerformanceMeterPlayModeSmokeTests` `12/12`; final Full EditMode `215/215` und Full PlayMode `16/16` bestanden. Focused Review P1/P2 resolved. Die isolierte Compile-Matrix fuer Unity `6000.4.12f1` URP `17.4` und HDRP `17.4` sowie Unity `6000.5.6f1` URP `17.5` und HDRP `17.5` ist bestanden. Release-Player-/Geraetevalidierung bleibt pending; kein Release wird behauptet.
