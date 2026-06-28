# Avvio Rapido

Questo percorso avvia un overlay visibile senza scrivere codice.

## 1. Apri La Finestra Di Setup

In Unity, apri:

```text
SGG/Perfmeter/Setup
```

## 2. Esegui Il Setup Consigliato

Usa la finestra di setup per:

- abilitare Frame Timing Stats;
- installare `PerfMeterRenderGraphFeature` nei renderer URP attivi e modificabili, oppure lasciare invariati i progetti HDRP perche il package Custom Pass viene registrato a runtime;
- creare le impostazioni JSON predefinite di proprieta del progetto;
- configurare visibilita overlay, angolo, target FPS, preset visivo e collection mode.

Il file di impostazioni senza codice viene salvato in:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Quando `enabled` e `autoStart` sono true, PerfMeter parte da questo JSON a runtime.

## 3. Entra In Play Mode

L'overlay predefinito dovrebbe apparire nell'angolo selezionato. Se non appare:

- conferma che il file di impostazioni JSON esista nel percorso Resources;
- conferma che l'overlay sia visibile nella finestra di setup;
- conferma che la runtime collection mode sia `Overlay`;
- conferma che il renderer URP attivo abbia `PerfMeterRenderGraphFeature` quando testi diagnostica URP Render Graph o overdraw;
- in HDRP, conferma che setup riporti HDRP Custom Pass availability. HDRP overdraw e heatmap non sono supportati by design.

## Criteri Di Completamento

Hai finito quando:

- l'overlay appare nell'angolo selezionato;
- FPS e CPU timing si aggiornano all'intervallo di refresh configurato;
- `PerformanceMeter.GetStatus().CollectionMode` restituisce `Overlay`.

## Bootstrap Manuale Opzionale

Usa il codice quando vuoi un controllo esplicito dell'avvio:

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

## Prime Letture Utili

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
