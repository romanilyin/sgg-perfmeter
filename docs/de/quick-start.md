# Schnellstart

Dieser Ablauf startet einen sichtbaren Overlay ohne Code.

## 1. Setup-Fenster Oeffnen

Oeffne in Unity:

```text
SGG/Perfmeter/Setup
```

## 2. Empfohlenes Setup Ausfuehren

Im Setup-Fenster kannst du:

- Frame Timing Stats aktivieren;
- `PerfMeterRenderGraphFeature` in editierbare aktive URP Renderer installieren oder HDRP-Projekte unveraendert lassen, weil der package Custom Pass zur Laufzeit registriert wird;
- projektverwaltete Standard-JSON-Einstellungen erstellen;
- Overlay-Sichtbarkeit, Ecke, Ziel-FPS, Visual Preset und Collection Mode konfigurieren.

Die Zero-Code-Einstellungen werden gespeichert unter:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Wenn `enabled` und `autoStart` aktiv sind, startet PerfMeter zur Laufzeit aus dieser JSON-Datei.

## 3. Play Mode Starten

Der Standard-Overlay sollte in der gewaehlten Ecke erscheinen. Falls nicht:

- pruefe, dass die JSON-Datei im Resources-Pfad existiert;
- pruefe, dass der Overlay im Setup-Fenster sichtbar ist;
- pruefe, dass der Runtime-Collection-Mode `Overlay` ist;
- pruefe, dass der aktive URP Renderer `PerfMeterRenderGraphFeature` enthaelt, wenn URP Render Graph diagnostics oder overdraw getestet wird;
- in HDRP, pruefe dass setup HDRP Custom Pass availability meldet. HDRP overdraw und heatmap sind by design unsupported.

## Fertig Wenn

- der Overlay in der gewaehlten Ecke erscheint;
- FPS und CPU timing im konfigurierten Intervall aktualisiert werden;
- `PerformanceMeter.GetStatus().CollectionMode` `Overlay` meldet.

## Optionaler Manueller Bootstrap

Nutze Code, wenn du den Start explizit kontrollieren willst:

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
    {
        PerformanceMeter.EnsureRunning();
        PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
        PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
        PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
        PerformanceMeter.SetOverlayVisible(true);
    }
}
```

## Erste Nuetzliche API-Aufrufe

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
