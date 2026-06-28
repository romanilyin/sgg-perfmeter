# Demarrage Rapide

Ce parcours affiche un overlay visible sans ecrire de code.

## 1. Ouvrir La Fenetre De Configuration

Dans Unity, ouvrez:

```text
SGG/Perfmeter/Setup
```

## 2. Executer La Configuration Recommandee

Utilisez la fenetre de configuration pour:

- activer Frame Timing Stats;
- installer `PerfMeterRenderGraphFeature` dans les renderers URP actifs et modifiables, ou laisser les projets HDRP inchanges car le package Custom Pass est enregistre au runtime;
- creer les reglages JSON par defaut appartenant au projet;
- configurer la visibilite de l'overlay, le coin, le FPS cible, le preset visuel et le mode de collecte.

Le fichier de reglages sans code est enregistre ici:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Quand `enabled` et `autoStart` valent true, PerfMeter demarre a l'execution depuis ce JSON.

## 3. Entrer En Play Mode

L'overlay par defaut doit apparaitre dans le coin selectionne. Si ce n'est pas le cas:

- confirmez que le fichier de reglages JSON existe dans le chemin Resources;
- confirmez que l'overlay est visible dans la fenetre de configuration;
- confirmez que le mode de collecte runtime est `Overlay`;
- confirmez que le renderer URP actif contient `PerfMeterRenderGraphFeature` lors des tests de diagnostics URP Render Graph ou d'overdraw;
- en HDRP, confirmez que setup signale HDRP Custom Pass availability. HDRP overdraw et heatmap ne sont pas pris en charge by design.

## Criteres De Fin

Vous avez termine lorsque:

- l'overlay apparait dans le coin selectionne;
- les FPS et le timing CPU se mettent a jour a l'intervalle de rafraichissement configure;
- `PerformanceMeter.GetStatus().CollectionMode` renvoie `Overlay`.

## Bootstrap Manuel Optionnel

Utilisez du code lorsque vous voulez controler explicitement le demarrage:

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

## Premiers Appels Utiles

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
