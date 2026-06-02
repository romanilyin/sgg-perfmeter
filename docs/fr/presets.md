# Presets Visuels

Les presets visuels sont des fichiers JSON de projet qui definissent la disposition de l'overlay, le style, les widgets actifs et leur ordre. Ils sont crees dans l'onglet `Presets` de `SGG/Perfmeter/Setup` et integres dans le JSON Resources pour les builds, afin que le runtime ne depende pas de `AssetDatabase`.

Les captures ci-dessous sont des captures plein ecran de la scene capture-lab apres 1000 frames de warm-up. Le texte de l'overlay runtime n'est pas localise, donc les docs francaises utilisent les memes images de presets partagees.

## Default

Preset de diagnostics sans code par defaut. Il utilise la disposition `MetricBars`, donc le bloc de texte inferieur est rendu comme barres de metriques compactes au lieu d'un bloc de texte simple.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Preset FPS sur une seule ligne avec FPS courant, FPS moyen, 1% low, 0.1% low et temps du render thread. Les valeurs de la famille FPS sont codees par couleur par rapport au FPS cible selectionne.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Preset de timing compact avec FPS, cartes de timing CPU/GPU et barres de budget CPU/GPU.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Preset axe sur les cartes pour FPS, CPU, GPU, frame spikes, rendering et memory sans graphes.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Preset axe sur le timing avec graphes d'historique CPU et GPU, plus cartes FPS/timing principales.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Preset de diagnostic large avec tous les principaux widgets PerfMeter de haut niveau actives.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Echelle De Couleur FPS

Le preset `FPS Only` colore les valeurs de la famille FPS par rapport au FPS cible selectionne:

| Ratio Par Rapport A La Cible | Couleur |
| --- | --- |
| `> 2.0x` | Bleu |
| `>= 1.0x` | Vert |
| `0.75x` to `< 1.0x` | Jaune |
| `0.25x` to `< 0.75x` | Orange |
| `< 0.25x` | Rouge |
