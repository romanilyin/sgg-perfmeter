# Limitazioni

SGG PerfMeter e progettato come livello di diagnostica runtime a basso overhead. Non sostituisce catture approfondite con Unity Profiler, RenderDoc, Profile Analyzer o Frame Debugger.

## Ambito Di Piattaforma E Pipeline

- Target runtime supportato: Unity `6000.4+` con URP `17.4+` e percorso Render Graph.
- Built-in Render Pipeline non e supportata e non e pianificata.
- Il supporto HDRP e pianificato, ma non e implementato in `2026.6.5-2`.
- Unity da `2022.3` a `6000.3` puo importare per sicurezza di compilazione, ma comportamento runtime e supporto puntano a Unity `6000.4+`.

## Disponibilita Del Timing

- Il GPU timing puo essere non disponibile, ritardato o non affidabile a seconda di piattaforma e graphics API.
- `CollectionFrame` e il frame Unity in cui PerfMeter ha raccolto lo snapshot, non necessariamente il frame hardware esatto rappresentato da `FrameTimingManager`.
- Android dovrebbe preferire Vulkan quando il GPU frame timing e importante.
- OpenGL/OpenGLES dovrebbe essere trattato come modalita degradata per GPU timing e strumentazione overdraw.

## Disponibilita Dei Counter

I profiler counter variano per piattaforma, versione Unity, impostazioni render pipeline e graphics API. Usa `AvailableCounters`, `UnavailableCounters` e warning invece di assumere che ogni counter esista ovunque.

## Costo E Supporto Overdraw

Numerical overdraw e heatmap visiva sono modalita diagnostiche. Aggiungono lavoro di rendering e dovrebbero essere usate in finestre limitate, non lasciate attive come UI di gameplay stabile.

Numerical overdraw richiede:

- `PerfMeterRenderGraphFeature` installato nel renderer URP attivo;
- supporto fragment-stage UAV/storage-buffer;
- supporto compute shader;
- graphics API supportata;
- supporto async GPU readback.

I target non supportati riportano `OverdrawState.Unsupported` con warning.

## Costo Overlay

L'overlay e attento alle allocazioni e throttled, ma valori numerici e label dei grafici che cambiano possono comunque materializzare stringhe managed all'intervallo di refresh. Diagnostica visiva pesante e modalita grafico dovrebbero essere validate sui dispositivi target.

## Stato Della Validazione

La validazione attuale include copertura automatizzata EditMode e PlayMode piu smoke validation su Android S23 Vulkan/GLES. Una copertura piu ampia di player-build e dispositivi resta utile prima di trattare i dati come evidenza per il sign-off di release.
