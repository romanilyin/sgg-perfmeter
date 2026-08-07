# Finestra Setup

Apri la finestra dell’Editor da `SGG/Perfmeter/Setup`.

## Comportamento attuale

- **Setup** e **Presets** mostrano le impostazioni persistenti del progetto PerfMeter e i dati dei preset dell’overlay: righe di schema/versione, compatibilità `legacy` e metadati riservati, tutte in sola lettura, oltre alla composizione dei widget e ai valori numerici normalizzati quando il campo perde il focus.
- **Runtime** mostra in sola lettura le diagnostiche di sessione, memoria, stato grafico, integrazione del rendering e GRD/BRG, incluse capacità e stato delle integrazioni opzionali. Gli stati `Unavailable`, `unknown` e senza campione restano espliciti. `Measure Overdraw (project default)` usa il valore sentinel predefinito del progetto.
- Le azioni includono `Session Analysis`, `Profile Analyzer` e `Refresh`. `Start Session` e `Stop Session` sono disponibili solo in Play Mode. Aprire o aggiornare Setup non avvia mai la raccolta runtime.
- I parametri di richiesta del memory snapshot e di trace/prewarm dello stato grafico sono input solo runtime, non impostazioni del progetto.

## Screenshot di riferimento

> Gli screenshot seguenti risalgono a prima di P3.5. Sono conservati solo come riferimento visivo e non costituiscono una prova attuale dell’UX Setup completata.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-it-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-it-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-it-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-it-debug.png)
