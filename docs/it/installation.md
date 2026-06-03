# Installazione

SGG PerfMeter e distribuito come pacchetto Unity denominato `com.sungeargames.perfmeter`. Il pacchetto del npm registry pubblico parte da `2026.6.5-2`; Git UPM e la copia locale restano disponibili.

## Requisiti

- Unity `6000.4+` per l'uso runtime supportato.
- URP `17.4+` con percorso Render Graph.
- Supporto runtime UI Toolkit.
- Frame Timing Stats abilitato prima di fare affidamento su FrameTimingManager nelle build.

I metadati del pacchetto mantengono Unity `2022.3` come soglia di sicurezza per importazione e controlli di compilazione. Il target runtime attualmente supportato e Unity `6000.4+` con URP `17.4+` Render Graph.

## Installazione Con npm Scoped Registry

Aggiungi il npm registry come Unity Package Manager scoped registry nel `Packages/manifest.json` del tuo progetto Unity:

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.6.5-2"
  }
}
```

Se il manifest contiene gia `scopedRegistries`, aggiungi la voce `npmjs` all'array esistente.

## Installazione Git UPM

Il pacchetto si trova dentro questo repository:

```text
Assets/Scripts/SGG.PerfMeter
```

Aggiungilo al file `Packages/manifest.json` del tuo progetto Unity:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Se il tuo ambiente usa SSH per le dipendenze Git:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Fissa un tag o un commit per installazioni ripetibili:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.5-2"
  }
}
```

## Installazione Con Copia Locale

Copia questa cartella nel tuo progetto Unity:

```text
Assets/Scripts/SGG.PerfMeter
```

Questo e utile per lo sviluppo locale del pacchetto o quando non si vogliono usare dipendenze Git.

## Setup Iniziale Del Progetto

Apri:

```text
SGG/Perfmeter/Setup
```

Poi esegui il setup consigliato:

1. Abilita Frame Timing Stats.
2. Installa `PerfMeterRenderGraphFeature` negli asset renderer URP attivi e modificabili.
3. Salva le impostazioni JSON in `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` per il setup senza codice, oppure copia lo snippet di inizializzazione.
4. Entra in Play Mode e verifica l'overlay.

## Samples

Importa i sample del pacchetto dal pannello dei dettagli del Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
