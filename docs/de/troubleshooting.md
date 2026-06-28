# Fehlerbehebung

Nutze diese Checkliste, wenn PerfMeter nicht die erwarteten Daten zeigt.

## Overlay Erscheint Nicht

- Oeffne `SGG/Perfmeter/Setup` und pruefe, dass Overlay-Sichtbarkeit aktiviert ist.
- Pruefe, dass der Collection Mode `Overlay` ist, nicht `Background` oder `Stopped`.
- Bei Zero-Code-Setup pruefe die Datei `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Bei manuellem Bootstrap pruefe, dass `PerformanceMeter.EnsureRunning()` nach dem Laden der Szene aufgerufen wird.
- Starte Play Mode; Edit-Mode-API-Aufrufe sind sicher, erstellen aber keinen Runtime-Overlay.

## Frame Timing Oder GPU Timing Fehlt

- Aktiviere Player Settings -> Rendering -> Frame Timing Stats.
- Nutze auf Android Vulkan, wenn GPU frame timing wichtig ist.
- Behandle OpenGL/OpenGLES als eingeschraenkten Modus fuer GPU timing.
- Pruefe `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` und `Warning`, bevor du einen Counter als verfuegbar annimmst.

## Overdraw-Messung Laeuft Nicht Weiter

- In URP, installiere `PerfMeterRenderGraphFeature` im aktiven URP Renderer.
- In HDRP sind overdraw und heatmap by design unsupported; nutze stattdessen core diagnostics.
- Pruefe, dass die aktive Kamera diesen Renderer nutzt.
- Pruefe fragment UAV/storage buffers, compute shaders und async GPU readback auf dem Ziel-Backend.
- Starte eine begrenzte Messung mit `PerformanceMeter.RequestOverdrawMeasurement(frameCount)`.
- Wenn das Ziel nicht unterstuetzt ist, meldet PerfMeter `OverdrawState.Unsupported` statt einen Pass zu planen.

## Session-Export Schlaegt Fehl

- Exportiere in einen projektlokalen Pfad.
- Ueberschreibe keinen bestehenden Export, wenn dein Workflow ihn nicht vorher loescht.
- Halte `MaxSamples` fuer lange Runs begrenzt.
- Nutze Warm-up-Frames/Sekunden, damit Startup-Spikes nicht in Summaries landen.

## Alerts Sind Zu Laut

- Passe thresholds und consecutive-frame windows in den JSON-Einstellungen an.
- Erhoehe Editor warning cooldowns.
- Deaktiviere Editor warning logs, wenn callbacks oder strukturierte logs ausreichen.

## Daten Unterscheiden Sich Zwischen Geraeten

Das ist erwartet. GPU timings, profiler counters, display information, async readback und overdraw support haengen von graphics API, Plattform, Unity-Version und Geraet ab. Nutze device snapshots und warnings in Session-Exporten, um Unterschiede zu erklaeren.
