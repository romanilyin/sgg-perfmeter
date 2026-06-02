# Preset Visivi

I preset visivi sono file JSON di progetto che definiscono layout dell'overlay, stile, widget abilitati e ordine dei widget. Sono creati nella tab `Presets` di `SGG/Perfmeter/Setup` e incorporati in JSON Resources per le build, quindi il runtime non dipende da `AssetDatabase`.

Gli screenshot sotto sono catture fullscreen dalla scena capture-lab dopo 1000 frame di warmup. Il testo dell'overlay runtime non e localizzato, quindi la documentazione italiana usa le stesse immagini dei preset.

## Default

Preset diagnostico predefinito senza codice. Usa il layout `MetricBars`, quindi il blocco di testo inferiore viene renderizzato come barre metriche compatte invece che come semplice blocco di testo.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Preset FPS su una sola riga con current FPS, average FPS, 1% low, 0.1% low e render-thread time. I valori della famiglia FPS sono colorati rispetto al target FPS selezionato.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Preset timing compatto con FPS, card di timing CPU/GPU e barre budget CPU/GPU.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Preset centrato sulle card per FPS, CPU, GPU, frame spikes, rendering e memoria senza grafici.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Preset centrato sul timing con grafici storici CPU e GPU piu card core FPS/timing.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Preset diagnostico ampio con tutti i principali widget PerfMeter di alto livello abilitati.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Scala Colori FPS

Il preset `FPS Only` colora i valori della famiglia FPS rispetto al target FPS selezionato:

| Rapporto Sul Target | Colore |
| --- | --- |
| `> 2.0x` | Blu |
| `>= 1.0x` | Verde |
| `0.75x` to `< 1.0x` | Giallo |
| `0.25x` to `< 0.75x` | Arancione |
| `< 0.25x` | Rosso |
