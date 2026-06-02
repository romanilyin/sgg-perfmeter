# Limitations

SGG PerfMeter est concu comme une couche de diagnostics runtime a faible overhead, sans remplacer les captures profondes de Unity Profiler, RenderDoc, Profile Analyzer ou Frame Debugger.

## Portee Plateforme Et Pipeline

- Cible runtime prise en charge: Unity `6000.4+` avec URP `17.4+` et chemin Render Graph.
- Built-in Render Pipeline et HDRP ne sont pas des cibles de premier niveau.
- Unity `2022.3` a `6000.3` peut importer le package pour la securite de compilation, mais le comportement runtime et la cible de support sont Unity `6000.4+`.

## Disponibilite Du Timing

- Le timing GPU peut etre indisponible, retarde ou peu fiable selon la plateforme et l'API graphique.
- `CollectionFrame` est la frame Unity ou PerfMeter a collecte le snapshot, pas forcement la frame materielle exacte representee par `FrameTimingManager`.
- Android devrait privilegier Vulkan lorsque le timing de frame GPU est important.
- OpenGL/OpenGLES doit etre traite comme mode degrade pour le timing GPU et l'instrumentation d'overdraw.

## Disponibilite Des Compteurs

Les compteurs Profiler varient selon la plateforme, la version Unity, les reglages du render pipeline et l'API graphique. Utilisez `AvailableCounters`, `UnavailableCounters` et les avertissements au lieu de supposer que chaque compteur existe partout.

## Cout Et Support De L'overdraw

L'overdraw numerique et la heatmap visuelle sont des modes de diagnostic. Ils ajoutent du travail de rendu et doivent etre utilises dans des fenetres bornees, sans rester actifs comme UI de gameplay en continu.

L'overdraw numerique necessite:

- `PerfMeterRenderGraphFeature` installe dans le renderer URP actif;
- prise en charge fragment-stage UAV/storage-buffer;
- prise en charge des compute shaders;
- API graphique prise en charge;
- prise en charge async GPU readback.

Les cibles non prises en charge signalent `OverdrawState.Unsupported` avec des avertissements.

## Cout De L'overlay

L'overlay limite les allocations et est cadence, mais les valeurs numeriques et labels de graphes modifies peuvent quand meme materialiser des chaines managees a l'intervalle de rafraichissement. Les diagnostics visuels lourds et modes graphes doivent etre valides sur les appareils cibles.

## Etat De Validation

La validation actuelle inclut une couverture automatisee EditMode et PlayMode, plus une validation smoke Android S23 Vulkan/GLES. Une couverture plus large de player builds et d'appareils reste utile avant de traiter les donnees comme preuve de validation de release.
