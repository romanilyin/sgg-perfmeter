# Installazione

SGG PerfMeter e distribuito come pacchetto Unity denominato `com.sungeargames.perfmeter`. La versione npm pubblica attuale e `2026.8.11-2`; Git UPM e la copia locale restano disponibili.

## Requisiti

- Unity `6000.4+` per l'uso runtime supportato.
- URP `17.4+` con Render Graph path o HDRP `17.4+` con Custom Pass integration.
- Supporto runtime UI Toolkit.
- Frame Timing Stats abilitato prima di fare affidamento su FrameTimingManager nelle build.
- La cattura nativa RenderDoc opzionale supporta solo l'Editor Unity Windows x64 con Direct3D 11, Direct3D 12 o Vulkan; Development Player, Linux nativo, IL2CPP, mobile e macOS nativo non sono supportati.
- Il pacchetto UPM resta senza binari e non installa mai RenderDoc. FTUE puo solo scaricare o installare localmente il bridge pubblicato separatamente e fissato per dimensione, SHA-256 e contratto PE AMD64; poi e necessario riavviare l'Editor.

I metadati del pacchetto mantengono Unity `2022.3` come soglia di sicurezza per importazione e controlli di compilazione. Il target runtime attualmente supportato e Unity `6000.4+` con URP `17.4+` Render Graph o HDRP `17.4+` Custom Pass integration.

Sono livelli di compatibilita separati: `ImportCompatible` non promette runtime behavior supportato; `CoreRuntimeCompatible` richiede Unity `6000.4+` ma non un pipeline specifico; `RenderIntegrationCompatible` richiede inoltre URP/HDRP attivo `17.4+` e l'adapter PerfMeter. Interrogali tramite `PerfMeterSetupActions.GetCompatibilityStatus()` o MCP `perfmeter.compatibility.status`; la configuration readiness e separata.

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
    "com.sungeargames.perfmeter": "2026.8.11-2"
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
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.8.11-2"
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

La scheda di configurazione iniziale controlla in tempo reale i requisiti obbligatori. Installa o ignora ogni integrazione chiaramente indicata come opzionale; la scheda si nasconde quando tutti i passaggi sono risolti e riappare se un controllo obbligatorio smette di essere valido.

Poi esegui il setup consigliato:

1. Abilita Frame Timing Stats.
2. Installa `PerfMeterRenderGraphFeature` negli asset renderer URP attivi e modificabili. I progetti HDRP saltano le modifiche al renderer URP; il package HDRP Custom Pass viene registrato a runtime quando HDRP `17.4+` e installato.
3. Salva le impostazioni JSON in `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` per il setup senza codice, oppure copia lo snippet di inizializzazione.
4. Entra in Play Mode e verifica l'overlay.

## Samples

Importa i sample del pacchetto dal pannello dei dettagli del Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
