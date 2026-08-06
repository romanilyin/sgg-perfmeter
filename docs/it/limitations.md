# Limitazioni

SGG PerfMeter e progettato come livello di diagnostica runtime a basso overhead. Non sostituisce catture approfondite con Unity Profiler, RenderDoc, Profile Analyzer o Frame Debugger.

## Ambito Di Piattaforma E Pipeline

- Target runtime supportato: Unity `6000.4+` con URP `17.4+` Render Graph o HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline non e supportata e non e pianificata.
- HDRP overdraw e heatmap non sono supportati. I progetti HDRP mantengono diagnostics di FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup e MCP.
- Unity da `2022.3` a `6000.3` puo importare per sicurezza di compilazione, ma comportamento runtime e supporto puntano a Unity `6000.4+`.

## Disponibilita Del Timing

- Il GPU timing puo essere non disponibile, ritardato o non affidabile a seconda di piattaforma e graphics API.
- `CollectionFrame` e il frame Unity in cui PerfMeter ha raccolto lo snapshot, non necessariamente il frame hardware esatto rappresentato da `FrameTimingManager`.
- Android dovrebbe preferire Vulkan quando il GPU frame timing e importante.
- OpenGL/OpenGLES dovrebbe essere trattato come modalita degradata per GPU timing e strumentazione overdraw.

## Disponibilita Dei Counter

I profiler counter variano per piattaforma, versione Unity, impostazioni render pipeline e graphics API. Usa `AvailableCounters`, `UnavailableCounters` e warning invece di assumere che ogni counter esista ovunque.

## External GPU Capture

- Il coordinator consente una richiesta attiva e avanza in modo deterministico attraverso `PreRoll`, `Capturing`, `PostRoll` e `Completed`. Lo stesso ID attivo e idempotente; un ID attivo diverso viene rifiutato per sovrapposizione.
- Il backend usa l'`ExternalGPUProfiler` sperimentale di Unity solo nell'Editor o nei Development Builds, quando uno strumento esterno e gia collegato. `RenderDoc` e limitato al desktop Windows/Linux con Direct3D 11, Direct3D 12 o Vulkan; `PIX` e limitato al desktop Windows con Direct3D 12.
- `Completed` conferma solo il wrapper lifecycle di Unity. Non dimostra che esista un artefatto esterno `.rdc`/`.wpix` e non fornisce un path dell'artefatto.
- I test automatizzati usano un fake backend. La conferma dello strumento esterno reale e dell'artefatto resta un release gate.
- Correlated bundles e MCP capture control sono disponibili, ma un `.rdc`/`.wpix` fornito resta solo un artefatto osservato e con hash: Unity non puo autenticare lo strumento collegato o l'associazione con il capture. La verifica con uno strumento reale resta un release-candidate gate.

## Costo E Supporto Overdraw

Numerical overdraw e heatmap visiva sono modalita diagnostiche. Aggiungono lavoro di rendering e dovrebbero essere usate in finestre limitate, non lasciate attive come UI di gameplay stabile.

Numerical overdraw in URP richiede:

- `PerfMeterRenderGraphFeature` installato nel renderer URP attivo;
- supporto fragment-stage UAV/storage-buffer;
- supporto compute shader;
- graphics API supportata;
- supporto async GPU readback.

I target non supportati, incluso HDRP, riportano `OverdrawState.Unsupported` con warning.

## Costo Overlay

L'overlay e attento alle allocazioni e throttled, ma valori numerici e label dei grafici che cambiano possono comunque materializzare stringhe managed all'intervallo di refresh. Ha due backend path UI Toolkit: un host `UIDocument` di proprieta su Unity `6000.4` e un host `PanelRenderer` di proprieta su Unity `6000.5+`. L'host conserva panel settings e children della UI estranea e ricostruisce solo il container di proprieta di PerfMeter. I valori numerici usano numeric slots riservati e stabili e un numeric monospace role; `FpsOnly` usa un fallback deterministico e bounded a due righe quando una riga non entra, mentre card e barre vanno a capo con logical widths ridotte. Questo riduce il rischio di clipping, ma non promette ogni resolution o scale arbitraria; diagnostica visiva pesante, modalita grafiche e layout risultante devono essere validate sui dispositivi target.

## Stato Della Validazione

La validazione attuale include copertura automatizzata EditMode, HDRP smoke validation in Unity `6000.4.10f1` e precedente smoke validation su Android S23 Vulkan/GLES. Una copertura piu ampia di player-build e dispositivi resta utile prima di trattare i dati come evidenza per il sign-off di release.

## Limiti e privacy degli snapshot di memoria opzionali

- La funzione non è disponibile senza `com.unity.memoryprofiler` `1.1.0+` su Unity `6000.4+`; il pacchetto core non installa né richiede questa dipendenza.
- La cattura manuale è l'unica modalità predefinita. I trigger di soglia della memoria di sistema e crescita limitata delle perdite sono opt-in; ogni richiesta è soggetta a guardie single-flight/overlap, cooldown, spazio libero minimo, backend e capture flags.
- Lo staging `.snap` posseduto è sotto `Temp/PerfMeter/MemorySnapshots` ed è limitato a 512 MiB. L'evidence solo memoria viene esportata sotto `Temp/PerfMeter/CaptureBundles`, con una quota totale di retention di 2 GiB. Un export riuscito è monouso e rimuove la sorgente di staging, con avvisi espliciti se la pulizia non è possibile.
- Gli snapshot possono contenere memoria sensibile del processo. Proteggili e controllali prima di condividerli. Il bundle registra `contains_sensitive_memory`, la provenienza di backend/flag, `memory-snapshot.json` e i metadati SHA-256; non crea un artefatto GPU esterno.
- La cancellazione bloccata dal sistema operativo e la protezione portable managed contro le race con reparse point sono best-effort. I path non sicuri o non posseduti vengono rifiutati e gli errori di pulizia restano visibili come warning.
- Le evidenze includono memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, compilazione opzionale con `com.unity.memoryprofiler@1.1.12` e Unity `6000.4.12f1` full EditMode `182/182` più full PlayMode `14/14`. Non è una dichiarazione sul release-player o sul comportamento su dispositivi.

## Limiti della diagnostica grafica e di GraphicsStateCollection

- I marker di creazione dei programmi GPU shader e delle graphics pipeline sono capability dinamiche di `ProfilerRecorder`. Unity, piattaforma, graphics API e stato del refresh del catalogo possono cambiarne la disponibilita. Usa `Unavailable`, `AvailableNoSample`, `AvailableSampled` e la provenance; non dedurre l'availability da un valore zero.
- I valori dei marker mantengono `Unit` e `DataType` del recorder e restano valori raw. Non sono universalmente conteggi di shader o PSO e PerfMeter non li converte in un'unita comune. I metadata della capability includono risoluzione exact/alias, nomi dei recorder risolti, component counts risolti/campionati e revisione del catalogo.
- L'assembly opzionale `SGG.PerfMeter.GraphicsStateCollection` e destinato a Unity `6000.4+`. Usa `UnityEngine.Experimental.Rendering.GraphicsStateCollection` in `6000.4` e `UnityEngine.Rendering.GraphicsStateCollection` in `6000.5+`; le versioni precedenti non sono supportate per questa integrazione.
- Un trace richiede una sessione PerfMeter attiva. In Play Mode normale i trace frames terminano dopo l'end-of-frame e in batch mode viene usato un fallback al frame successivo. I sample correlati sono soggetti a warm-up, intervallo e limite massimo di sample della sessione.
- E ammesso un solo graphics-state flight, inclusi preparazione, finalizzazione del trace, prewarm e cleanup. Anche un external GPU capture, memory snapshot o alert-capture attivo causa un rifiuto per overlap. `IsBusy`/`is_busy` copre questi flight e il cleanup persistente; `HasPendingCleanup`/`has_pending_cleanup` segnala in modo specifico un artefatto owned in attesa di retry. La cancellazione corrispondente e best-effort; gli errori di cleanup restano visibili e possono ritardare la richiesta successiva.
- `StopSession()` annulla un trace attivo, quindi serve una sessione attiva per tutta la durata del trace. Una cancellazione fallita dell'artefatto owned crea un sidecar adiacente `.delete-pending`; viene ripristinato e ritentato dopo un domain reload. Warning e stato busy restano visibili finche artefatto e marker non sono stati rimossi.
- Il prewarm accetta solo un artefatto owned relativo al progetto, e sincrono, conserva l'artefatto e puo segnalare un progressive warmup incompleto. Il backend Unity non supporta il cache-miss tracing: la richiesta restituisce `Unavailable` e non viene esposta alcuna evidence di cache-miss.
- Gli artefatti `.graphicsstate` owned sono archiviati sotto `Temp/PerfMeter/GraphicsStateCollections`, devono essere file regolari non vuoti e sono limitati a 64 MiB. Il trace e limitato a 600 frames e il prewarm progressivo a 1.000.000 di states. Si applicano i guard di spazio libero minimo e di path locali al progetto.
- L'evidence finale comprende compile Unity `6000.4.12f1` superato; GSC EditMode targeted `25/25`, API EditMode `PerformanceMeter` `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208` e full post-fix PlayMode `16/16`. Anche un optional consumer compile isolato su Unity `6000.5.6f1` e superato. I test full di Unity `6000.5`, il comportamento release-player e quello dei device restano release gate e non vengono dichiarati qui.
