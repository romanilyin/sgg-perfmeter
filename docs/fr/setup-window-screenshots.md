# Fenêtre de configuration

Ouvrez la fenêtre de l’éditeur depuis `SGG/Perfmeter/Setup`.

## Comportement actuel

- **Setup** et **Presets** exposent les paramètres persistants du projet PerfMeter et les données des presets de l’overlay : lignes de schéma/version, de compatibilité `legacy` et de métadonnées réservées, toutes en lecture seule, ainsi que la composition des widgets et les valeurs numériques normalisées à la perte du focus.
- **Runtime** affiche en lecture seule les diagnostics de session, de mémoire, d’état graphique, d’intégration du rendu et de GRD/BRG, ainsi que la capacité et l’état des intégrations optionnelles. Les états `Unavailable`, `unknown` et sans échantillon restent explicites. `Measure Overdraw (project default)` utilise la sentinelle par défaut du projet.
- Les actions comprennent `Session Analysis`, `Profile Analyzer` et `Refresh`. `Start Session` et `Stop Session` sont disponibles uniquement en Play Mode. Ouvrir ou actualiser Setup ne démarre jamais la collecte runtime.
- Les paramètres de demande de memory snapshot et de trace/prewarm de l’état graphique sont des entrées runtime uniquement, pas des paramètres du projet.

## Captures de référence

> Les captures ci-dessous sont antérieures à P3.5. Elles sont conservées uniquement comme référence visuelle et ne constituent pas une preuve actuelle de l’UX Setup terminée.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-fr-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-fr-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-fr-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-fr-debug.png)
