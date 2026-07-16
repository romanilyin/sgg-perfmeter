# SGG PerfMeter

**Diagnostica runtime leggera e profiling leggibile dagli agenti per Unity 6 URP+HDRP (FPS meter).**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](../es/README.md) | [Français](../fr/README.md) | [Italiano](./README.md) | [日本語](../ja/README.md) | [한국어](../ko/README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](../zh-cn/README.md)

[Installazione](./installation.md) | [Avvio Rapido](./quick-start.md) | [Workflow](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [Confronto](./comparison.md) | [Limitazioni](./limitations.md) | [Risoluzione Problemi](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter identifica i colli di bottiglia dei frame, confronta le variazioni di prestazioni, registra sessioni riproducibili e fornisce dati di profiling strutturati a strumenti e AI agent.

## Perche Aiuta

- Mostra il contesto dei colli di bottiglia direttamente durante il gioco.
- Permette di passare tra preset, grafici, barre metriche, layout compatti e righe di metriche personalizzate.
- Registra sessioni di profiling riproducibili con warm-up, ambito scena, riepilogo dei frame peggiori ed esportazione JSON/CSV.
- Usa alert, log strutturati, callback e cooldown degli avvisi Editor senza dover osservare sempre l'overlay.
- Fornisce a strumenti e agent dati strutturati per confronti, test A/B e ricerca degli hotspot.

## Cosa Viene Misurato

- Stato runtime di Unity `6000.4+` / URP `17.4+` Render Graph e HDRP `17.4+` Custom Pass.
- Timing CPU/GPU di FrameTimingManager: CPU frame, main thread, render thread, present wait e GPU frame time quando disponibile.
- Contatori di rendering ProfilerRecorder: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory e GPU memory quando disponibile.
- Classificazione del collo di bottiglia per GPU, CPU main, CPU render, present/VSync, balanced o unknown.
- Opt-in overdraw measurement e overdraw heatmap visiva tramite URP Render Graph; in HDRP overdraw/heatmap non sono supportati, mentre i core diagnostics restano disponibili.
- Snapshot di device, URP/HDRP camera, render integration, status, metrics, alerts, sessions e custom metrics per codice e automazione MCP.

## Avvio Rapido

1. Installa il pacchetto Unity da npm registry o Git UPM.
2. Apri `SGG/Perfmeter/Setup` in Unity.
3. Esegui il setup consigliato, avvia Play Mode e verifica che l'overlay appaia.

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
    "com.sungeargames.perfmeter": "2026.7.16-1"
  }
}
```

## Documentazione

- [Installazione](./installation.md)
- [Avvio Rapido](./quick-start.md)
- [Workflow](./workflows.md)
- [API](./api.md)
- [MCP e Automazione Agent](./mcp.md)
- [Preset Visivi](./presets.md)
- [Widget Implementati](./widgets.md)
- [Screenshot](./screenshots.md)
- [Screenshot della Finestra di Setup](./setup-window-screenshots.md)
- [Limitazioni](./limitations.md)
- [Risoluzione Problemi](./troubleshooting.md)
- [Confronto](./comparison.md)
- [Brand Usage Policy](./brand.md)

## Licenza

Il pacchetto e concesso in licenza con **Stinger Royalty-Free EULA 1.0**.

- Testo russo autorevole della licenza: [LICENSE.ru.md](../../LICENSE.ru.md)
- Traduzione inglese ausiliaria: [LICENSE.md](../../LICENSE.md)
- Note: [NOTICE.md](../../NOTICE.md) e [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy: [brand.md](./brand.md)
