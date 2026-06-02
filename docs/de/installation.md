# Installation

SGG PerfMeter wird derzeit als Unity-Paket `com.sungeargames.perfmeter` verteilt. Ein npm-Installationspfad ist fuer Release `2026.6.5-1` nicht verfuegbar.

## Anforderungen

- Unity `6000.4+` fuer unterstuetzte Runtime-Nutzung.
- URP `17.4+` mit Render Graph.
- UI Toolkit-Unterstuetzung zur Laufzeit.
- Frame Timing Stats muessen aktiviert sein, bevor FrameTimingManager-Daten in Builds genutzt werden.

Die Paketmetadaten verwenden Unity `2022.3` als import-sicheren Mindestwert fuer Import- und Compile-Checks. Die aktuell unterstuetzte Runtime-Zielversion beginnt bei Unity `6000.4+` mit URP `17.4+` Render Graph.

## Installation Ueber Git UPM

Das Paket liegt in diesem Repository unter:

```text
Assets/Scripts/SGG.PerfMeter
```

Fuege es der `Packages/manifest.json` deines Unity-Projekts hinzu:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Wenn deine Umgebung SSH fuer Git-Abhaengigkeiten nutzt:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Pinne ein Tag oder einen Commit fuer wiederholbare Installationen:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.5-1"
  }
}
```

## Lokale Kopie

Kopiere diesen Ordner in dein Unity-Projekt:

```text
Assets/Scripts/SGG.PerfMeter
```

Das ist nuetzlich fuer lokale Paketentwicklung oder wenn Git-Abhaengigkeiten nicht gewuenscht sind.

## Erstes Projekt-Setup

Oeffne:

```text
SGG/Perfmeter/Setup
```

Fuehre dann das empfohlene Setup aus:

1. Aktiviere Frame Timing Stats.
2. Installiere `PerfMeterRenderGraphFeature` in editierbare aktive URP Renderer Assets.
3. Speichere JSON-Einstellungen unter `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` fuer Zero-Code-Setup oder kopiere das Initialisierungs-Snippet.
4. Starte Play Mode und pruefe den Overlay.

## Samples

Importiere Paket-Samples aus dem Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
