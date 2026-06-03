# SGG PerfMeter

**Leichtgewichtige Runtime-Diagnose und agentenlesbares Profiling fuer Unity 6 URP.**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](./README.md) | [Español](../es/README.md) | [Français](../fr/README.md) | [Italiano](../it/README.md) | [日本語](../ja/README.md) | [한국어](../ko/README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](../zh-cn/README.md)

[Installation](./installation.md) | [Schnellstart](./quick-start.md) | [Workflows](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [Vergleich](./comparison.md) | [Einschraenkungen](./limitations.md) | [Fehlerbehebung](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter erkennt Frame-Bottlenecks, vergleicht Performance-Aenderungen, zeichnet reproduzierbare Sessions auf und stellt strukturierte Profiling-Daten fuer Tools und AI-Agents bereit.

## Warum Es Hilft

- Bottleneck-Kontext direkt waehrend des Spiels sehen.
- Zwischen Presets, Graphen, Metrikleisten, kompakten Layouts und Custom-Metric-Zeilen wechseln.
- Reproduzierbare Profiling-Sessions mit Warm-up, Szenenbezug, Worst-Frame-Zusammenfassung und JSON/CSV-Export aufzeichnen.
- Alerts, strukturierte Logs, Callbacks und Editor-Warnungs-Cooldowns nutzen, ohne den Overlay dauerhaft beobachten zu muessen.
- Tools und Agents strukturierte Daten fuer Vergleiche, A/B-Tests und Hotspot-Suche geben.

## Was Gemessen Wird

- Unity `6000.4+` / URP `17.4+` Render-Graph-Status zur Laufzeit.
- FrameTimingManager CPU/GPU-Timing: CPU frame, main thread, render thread, present wait und GPU frame time, wenn verfuegbar.
- ProfilerRecorder-Render-Counter: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory und GPU memory, wenn verfuegbar.
- Bottleneck-Klassifizierung fuer GPU, CPU main, CPU render, present/VSync, balanced oder unknown.
- Opt-in overdraw measurement und visuelle overdraw heatmap ueber URP Render Graph.
- Snapshots fuer device, camera, Render Graph, status, metrics, alerts, sessions und custom metrics fuer Code und MCP-Automation.

## Schnellstart

1. Installiere das Unity-Paket ueber npm registry oder Git UPM.
2. Oeffne `SGG/Perfmeter/Setup` in Unity.
3. Fuehre das empfohlene Setup aus, starte Play Mode und pruefe, dass der Overlay erscheint.

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

## Dokumentation

- [Installation](./installation.md)
- [Schnellstart](./quick-start.md)
- [Workflows](./workflows.md)
- [API](./api.md)
- [MCP und Agent-Automation](./mcp.md)
- [Visual Presets](./presets.md)
- [Implementierte Widgets](./widgets.md)
- [Screenshots](./screenshots.md)
- [Setup-Fenster-Screenshots](./setup-window-screenshots.md)
- [Einschraenkungen](./limitations.md)
- [Fehlerbehebung](./troubleshooting.md)
- [Vergleich](./comparison.md)
- [Brand Usage Policy](./brand.md)

## Lizenz

Das Paket ist unter **Stinger Royalty-Free EULA 1.0** lizenziert.

- Massgeblicher russischer Lizenztext: [LICENSE.ru.md](../../LICENSE.ru.md)
- Englische Hilfsuebersetzung: [LICENSE.md](../../LICENSE.md)
- Hinweise: [NOTICE.md](../../NOTICE.md) und [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy: [brand.md](./brand.md)
