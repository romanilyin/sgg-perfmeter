# SGG PerfMeter

**Diagnostic d'execution leger et profilage lisible par les agents pour Unity 6 URP+HDRP (FPS meter).**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](../es/README.md) | [Français](./README.md) | [Italiano](../it/README.md) | [日本語](../ja/README.md) | [한국어](../ko/README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](../zh-cn/README.md)

[Installation](./installation.md) | [Demarrage rapide](./quick-start.md) | [Workflows](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [Comparaison](./comparison.md) | [Limitations](./limitations.md) | [Depannage](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter detecte les goulets d'etranglement des frames, compare les changements de performance, enregistre des sessions reproductibles et fournit des donnees de profilage structurees aux outils et aux agents IA.

## Pourquoi L'utiliser

- Voir le contexte des goulets d'etranglement directement pendant le jeu.
- Basculer entre presets, graphes, barres de metriques, dispositions compactes et lignes de metriques personnalisees.
- Enregistrer des sessions de profilage reproductibles avec warm-up, contexte de scene, resume des pires frames et export JSON/CSV.
- Coordonner une requete RenderDoc/PIX explicite et bornee avec des etats deterministes de pre-roll, capture et post-roll lorsque le profiler GPU externe est deja attache; cette coordination est limitee a l'Editor/Development Build et ne revendique aucun path d'artefact faisant autorite.
- Utiliser des alertes, logs structures, callbacks et cooldowns d'avertissements Editor sans surveiller l'overlay en continu.
- Fournir aux outils et agents des donnees structurees pour les comparaisons, tests A/B et recherches de points chauds.

## Ce Qui Est Mesure

- Etat Unity `6000.4+` / URP `17.4+` Render Graph et HDRP `17.4+` Custom Pass a l'execution.
- Timing CPU/GPU via FrameTimingManager: CPU frame, main thread, render thread, present wait et GPU frame time quand disponible.
- Compteurs de rendu ProfilerRecorder: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory et GPU memory quand disponible.
- Classification de goulet d'etranglement pour GPU, CPU main, CPU render, present/VSync, balanced ou unknown.
- Mesure d'overdraw opt-in et overdraw heatmap visuelle via URP Render Graph; en HDRP overdraw/heatmap ne sont pas pris en charge, mais les core diagnostics restent disponibles.
- Snapshots device, URP/HDRP camera, render integration, status, metrics, alerts, sessions et custom metrics pour le code et l'automatisation MCP.

## Telemetrie de plateforme optionnelle

PerfMeter peut collecter des signaux thermiques et Adaptive Performance optionnels via un provider. L'assembly core n'a aucune dependance forte a `com.unity.adaptiveperformance`; l'assembly optionnelle `SGG.PerfMeter.AdaptivePerformance` s'active pour Unity `6000.4+` lorsque `com.unity.adaptiveperformance` est en version `5.1.0+`.

- **API du provider**: `PerformanceMeter.RegisterPlatformTelemetryProvider(...)`, `UnregisterPlatformTelemetryProvider(...)` et `GetPlatformTelemetry()` gerent au plus un provider actif et renvoient le `PerfMeterPlatformTelemetrySnapshot` immuable avec l'ID/version du provider, les temps de sample/changement, l'alerte thermique, le niveau/tendance de temperature, les niveaux de performance CPU/GPU, le bottleneck adaptatif et la disponibilite de chaque champ. Un second provider different est refuse.
- **Collecte et exports**: le runtime echantillonne le provider une fois par frame collectee. Le JSON/CSV de session et les capture samples preservent le snapshot et la provenance du provider. La commande MCP `perfmeter.platform.telemetry` expose le snapshot structure actuel.
- **Profiler et alertes**: `SGG.PerfMeter.Thermal.Sample` et `SGG.PerfMeter.Thermal.Available` exposent le marker de collecte thermique et le counter de disponibilite. L'alerte par defaut `thermal.throttling` utilise la metrique `ThermalWarningLevel` lorsqu'un niveau de throttling imminent ou actif est disponible.
- **L'indisponibilite est explicite**: les providers et champs non pris en charge restent unavailable; le JSON serialise les valeurs numeriques indisponibles en `null` et le CSV utilise des champs vides, avec des flags de disponibilite plutot que des zeros artificiels. La validation du package Adaptive Performance reel et des appareils cibles reste un gate de la release matrix/release candidate; ce README ne revendique aucun resultat sur appareil.

## Demarrage Rapide

1. Installez le package Unity depuis npm registry ou Git UPM.
2. Ouvrez `SGG/Perfmeter/Setup` dans Unity.
3. Executez la configuration recommandee, lancez Play Mode et verifiez que l'overlay apparait.

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
    "com.sungeargames.perfmeter": "2026.8.7-1"
  }
}
```

## Documentation

- [Installation](./installation.md)
- [Demarrage rapide](./quick-start.md)
- [Workflows](./workflows.md)
- [API](./api.md)
- [MCP et automatisation par agents](./mcp.md)
- [Presets visuels](./presets.md)
- [Widgets implementes](./widgets.md)
- [Captures d'ecran](./screenshots.md)
- [Captures d'ecran de la fenetre de configuration](./setup-window-screenshots.md)
- [Limitations](./limitations.md)
- [Depannage](./troubleshooting.md)
- [Comparaison](./comparison.md)
- [Politique d'utilisation de la marque](./brand.md)

## Licence

Le package est sous licence **Stinger Royalty-Free EULA 1.0**.

- Texte de licence russe faisant autorite: [LICENSE.ru.md](../../LICENSE.ru.md)
- Traduction anglaise auxiliaire: [LICENSE.md](../../LICENSE.md)
- Avis: [NOTICE.md](../../NOTICE.md) et [NOTICE.ru.md](../../NOTICE.ru.md)
- Politique d'utilisation de la marque: [brand.md](./brand.md)
