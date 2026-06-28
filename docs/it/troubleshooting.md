# Risoluzione Problemi

Usa questa checklist quando PerfMeter non mostra i dati attesi.

## L'Overlay Non Appare

- Apri `SGG/Perfmeter/Setup` e conferma che la visibilita overlay sia abilitata.
- Conferma che collection mode sia `Overlay`, non `Background` o `Stopped`.
- Se usi il setup senza codice, conferma che il file di impostazioni esista in `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Se usi bootstrap manuale, conferma che `PerformanceMeter.EnsureRunning()` venga chiamato dopo il caricamento scena.
- Entra in Play Mode; le chiamate API in Edit Mode sono sicure ma non creano un overlay runtime.

## Manca Frame Timing O GPU Timing

- Abilita Player Settings -> Rendering -> Frame Timing Stats.
- Preferisci Vulkan su Android quando il GPU frame timing e importante.
- Tratta OpenGL/OpenGLES come modalita degradata per GPU timing.
- Controlla `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` e `Warning` prima di assumere che un counter esista.

## La Misurazione Overdraw Non Avanza

- In URP, installa `PerfMeterRenderGraphFeature` nel renderer URP attivo.
- In HDRP, overdraw e heatmap non sono supportati by design; usa core diagnostics.
- Conferma che la camera attiva usi il renderer che contiene la feature.
- Conferma che il backend target supporti fragment UAV/storage buffers, compute shaders e async GPU readback.
- Usa `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` per una finestra di misurazione limitata.
- Se il target non e supportato, PerfMeter restituisce `OverdrawState.Unsupported` invece di schedulare il pass.

## L'Export Sessione Fallisce

- Esporta in un percorso locale al progetto.
- Non sovrascrivere un export esistente a meno che il tuo workflow lo rimuova esplicitamente prima.
- Mantieni `MaxSamples` limitato per esecuzioni lunghe.
- Usa warm-up frames/seconds per evitare spike di avvio nei riepiloghi.

## Gli Alert Sono Troppo Rumorosi

- Regola soglie e finestre consecutive-frame nelle impostazioni JSON.
- Aumenta i cooldown degli avvisi Editor.
- Disabilita i log di avviso Editor quando callback o log strutturati sono sufficienti.

## I Dati Sembrano Diversi Tra Dispositivi

Questo e previsto. GPU timings, profiler counters, informazioni display, async readback e supporto overdraw variano in base a graphics API, piattaforma, versione Unity e dispositivo. Usa snapshot device e warning nelle sessioni esportate per spiegare le differenze.
