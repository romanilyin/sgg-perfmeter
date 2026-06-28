# Widgets Implementes

SGG PerfMeter fournit actuellement 16 widgets d'overlay runtime de haut niveau. Ce sont les blocs de composition de presets affiches dans la fenetre de configuration et utilises par les presets visuels d'overlay.

`FPS Only` est un mode de preset/disposition, pas un widget separe. Il reutilise les donnees FPS et timing dans une seule ligne compacte.

La plupart des groupes de metriques ont des formes textuelles et graphiques. Les formes textuelles sont des cartes ou des lignes `MetricBars` avec des valeurs numeriques, tandis que les formes graphiques sont des barres de budget ou des graphes d'historique. Le preset selectionne decide quelle forme est affichee; la meme source de metriques peut apparaitre dans differentes dispositions.

Le texte de l'overlay runtime n'est pas localise, donc la documentation francaise utilise les memes captures de widgets partagees.

| Widget ID | Capture d'ecran | Type | Module | Affiche |
| --- | --- | --- | --- | --- |
| `fps.summary-card` | <img src="../assets/screenshots/widgets/fps-summary-card.png" alt="FPS summary card" width="360"> | Carte | FPS | FPS moyen, FPS courant, 1% low, 0.1% low et etat de budget. |
| `timing.cpu-card` | <img src="../assets/screenshots/widgets/timing-cpu-card.png" alt="CPU timing card" width="360"> | Carte | Timing | CPU frame, main thread, render thread et etat de budget de frame. |
| `timing.gpu-card` | <img src="../assets/screenshots/widgets/timing-gpu-card.png" alt="GPU timing card" width="360"> | Carte | GPU timing | Temps de frame GPU et nombre d'echantillons GPU valides quand Unity expose le timing GPU. |
| `timing.frame-spikes-card` | <img src="../assets/screenshots/widgets/timing-frame-spikes-card.png" alt="Frame spikes card" width="360"> | Carte | FPS / Warnings | Compteurs de frame spikes et etat d'avertissement courant. |
| `overdraw.card` | <img src="../assets/screenshots/widgets/overdraw-card.png" alt="Overdraw card" width="360"> | Carte | Overdraw / Heatmap | Etat de mesure d'overdraw, progression, ratio et etat de heatmap; en HDRP, il peut afficher unsupported state pour overdraw/heatmap. |
| `timing.cpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-cpu-budget-bar.png" alt="CPU budget bar" width="360"> | Barre de budget | Timing | Temps de frame CPU par rapport au budget de FPS cible selectionne. |
| `timing.gpu-budget-bar` | <img src="../assets/screenshots/widgets/timing-gpu-budget-bar.png" alt="GPU budget bar" width="360"> | Barre de budget | GPU timing | Temps de frame GPU par rapport au budget de FPS cible selectionne. |
| `graphs.cpu-timing` | <img src="../assets/screenshots/widgets/graphs-cpu-timing.png" alt="CPU timing graph" width="360"> | Graphe | Graphs / Timing | CPU frame, main thread, render thread et autre historique de timing. |
| `graphs.gpu-timing` | <img src="../assets/screenshots/widgets/graphs-gpu-timing.png" alt="GPU timing graph" width="360"> | Graphe | Graphs / GPU timing | Historique du timing de frame GPU avec ligne de budget cible. |
| `cpu.cores-bars` | <img src="../assets/screenshots/widgets/cpu-cores-bars.png" alt="CPU core bars" width="360"> | Panneau | CPU core sampling | Barres de charge CPU par coeur logique quand l'echantillonnage de plateforme est disponible. |
| `cpu.cores-graphs` | <img src="../assets/screenshots/widgets/cpu-cores-graphs.png" alt="CPU core graphs" width="360"> | Panneau | CPU core sampling / Graphs | Graphes d'historique de charge CPU par coeur logique. |
| `custom-metrics.panel` | <img src="../assets/screenshots/widgets/custom-metrics-panel.png" alt="Custom metrics panel" width="360"> | Panneau | Custom metrics | Valeurs fournies par les implementations `IPerfMeterCustomMetricProvider` du projet. |
| `rendering.summary-card` | <img src="../assets/screenshots/widgets/rendering-summary-card.png" alt="Rendering summary card" width="360"> | Carte | Rendering | Draw calls, SetPass calls, batches et vertices. |
| `memory.summary-card` | <img src="../assets/screenshots/widgets/memory-summary-card.png" alt="Memory summary card" width="360"> | Carte | Memory / GC / GPU memory | Compteurs de memoire systeme, memoire GC et memoire GPU. |
| `batching.summary-card` | <img src="../assets/screenshots/widgets/batching-summary-card.png" alt="Batching summary card" width="360"> | Carte | SRP Batcher / BRG | Compteurs SRP Batcher et BatchRendererGroup / GPU Resident Drawer. |
| `uploads.summary-card` | <img src="../assets/screenshots/widgets/uploads-summary-card.png" alt="Uploads summary card" width="360"> | Carte | Uploads | Compteurs d'index/upload, y compris les octets d'upload d'index buffer dans la frame. |

## Captures Metric Bar

La disposition `MetricBars` par defaut rend des lignes compactes pour les categories souvent surveillees:

| Capture | Affiche |
| --- | --- |
| <img src="../assets/screenshots/widgets/metric-bars-fps.png" alt="FPS metric bars" width="480"> | FPS, budget de frame et indicateurs de FPS faibles. |
| <img src="../assets/screenshots/widgets/metric-bars-timing.png" alt="Timing metric bars" width="480"> | Lignes de timing CPU/GPU par rapport au FPS cible selectionne. |
| <img src="../assets/screenshots/widgets/metric-bars-rendering.png" alt="Rendering metric bars" width="480"> | Draw calls, SetPass calls, batches et vertices. |
| <img src="../assets/screenshots/widgets/metric-bars-batching-brg.png" alt="Batching and BRG metric bars" width="480"> | Compteurs SRP Batcher et BatchRendererGroup / GPU Resident Drawer. |
| <img src="../assets/screenshots/widgets/metric-bars-memory.png" alt="Memory metric bars" width="480"> | Compteurs de memoire systeme, memoire GC et memoire GPU. |
| <img src="../assets/screenshots/widgets/metric-bars-uploads.png" alt="Uploads metric bars" width="480"> | Compteurs d'upload et octets d'upload d'index-buffer. |
| <img src="../assets/screenshots/widgets/metric-bars-custom-metrics.png" alt="Custom metrics metric bars" width="480"> | Lignes de metriques personnalisees fournies par le projet. |

## Notes

- Les presets peuvent activer un sous-ensemble de ces widgets et choisir une disposition comme `MetricBars`, `CompactCards`, `Graphs` ou `DiagnosticsWide`.
- Les lignes textuelles et les lignes de barres de metriques sont des renderers de plus bas niveau derriere le systeme de disposition et exposent des versions textuelles des memes groupes de metriques qui peuvent apparaitre comme cartes, barres de budget ou graphes dans d'autres dispositions.
