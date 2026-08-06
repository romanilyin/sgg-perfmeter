# Limitations

SGG PerfMeter est concu comme une couche de diagnostics runtime a faible overhead, sans remplacer les captures profondes de Unity Profiler, RenderDoc, Profile Analyzer ou Frame Debugger.

## Portee Plateforme Et Pipeline

- Cible runtime prise en charge: Unity `6000.4+` avec URP `17.4+` Render Graph ou HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline n'est pas pris en charge et n'est pas planifie.
- HDRP overdraw et heatmap ne sont pas pris en charge. Les projets HDRP gardent les diagnostics FPS, CPU, GPU, memory, sessions, alerts, camera, device, setup et MCP.
- Unity `2022.3` a `6000.3` peut importer le package pour la securite de compilation, mais le comportement runtime et la cible de support sont Unity `6000.4+`.

## Disponibilite Du Timing

- Le timing GPU peut etre indisponible, retarde ou peu fiable selon la plateforme et l'API graphique.
- `CollectionFrame` est la frame Unity ou PerfMeter a collecte le snapshot, pas forcement la frame materielle exacte representee par `FrameTimingManager`.
- Android devrait privilegier Vulkan lorsque le timing de frame GPU est important.
- OpenGL/OpenGLES doit etre traite comme mode degrade pour le timing GPU et l'instrumentation d'overdraw.

## Disponibilite Des Compteurs

Les compteurs Profiler varient selon la plateforme, la version Unity, les reglages du render pipeline et l'API graphique. Utilisez `AvailableCounters`, `UnavailableCounters` et les avertissements au lieu de supposer que chaque compteur existe partout.

## External GPU Capture

- Le coordinator autorise une requete active et avance de maniere deterministe dans `PreRoll`, `Capturing`, `PostRoll` et `Completed`. La meme ID active est idempotente; une autre ID active est rejetee pour chevauchement.
- Le backend utilise l'`ExternalGPUProfiler` experimental de Unity uniquement dans l'Editor ou les Development Builds, lorsqu'un outil externe est deja attache. `RenderDoc` est limite au desktop Windows/Linux avec Direct3D 11, Direct3D 12 ou Vulkan; `PIX` est limite au desktop Windows avec Direct3D 12.
- `Completed` confirme uniquement le wrapper lifecycle de Unity. Il ne prouve pas qu'un artefact externe `.rdc`/`.wpix` existe et ne fournit aucun path d'artefact.
- Les tests automatises utilisent un fake backend. La confirmation de l'outil externe reel et de l'artefact reste un release gate.
- Les correlated bundles et MCP capture control sont disponibles, mais un `.rdc`/`.wpix` fourni reste seulement un artefact observe et hashe: Unity ne peut pas authentifier l'outil attache ni l'association avec la capture. La verification par un outil reel reste un release-candidate gate.

## Cout Et Support De L'overdraw

L'overdraw numerique et la heatmap visuelle sont des modes de diagnostic. Ils ajoutent du travail de rendu et doivent etre utilises dans des fenetres bornees, sans rester actifs comme UI de gameplay en continu.

L'overdraw numerique en URP necessite:

- `PerfMeterRenderGraphFeature` installe dans le renderer URP actif;
- prise en charge fragment-stage UAV/storage-buffer;
- prise en charge des compute shaders;
- API graphique prise en charge;
- prise en charge async GPU readback.

Les cibles non prises en charge, y compris HDRP, signalent `OverdrawState.Unsupported` avec des avertissements.

## Cout De L'overlay

L'overlay limite les allocations et est cadence, mais les valeurs numeriques et labels de graphes modifies peuvent quand meme materialiser des chaines managees a l'intervalle de rafraichissement. Il possede deux backend paths UI Toolkit : un host `UIDocument` propre a Unity `6000.4` et un host `PanelRenderer` propre a Unity `6000.5+`. Le host preserve les panel settings et children de l'UI etrangere et ne reconstruit que le container appartenant a PerfMeter. Les valeurs numeriques utilisent des numeric slots reserves et stables ainsi qu'un numeric monospace role ; `FpsOnly` utilise un fallback deterministe et borne a deux lignes quand une ligne ne tient pas, tandis que les cartes et barres passent a la ligne avec des logical widths etroites. Cela reduit le risque de clipping, mais ne promet pas toutes les resolutions ou echelles arbitraires ; les diagnostics visuels lourds, les modes graphes et le layout obtenu doivent etre valides sur les appareils cibles.

## Etat De Validation

La validation actuelle inclut une couverture automatisee EditMode, HDRP smoke validation dans Unity `6000.4.10f1` et une precedente validation smoke Android S23 Vulkan/GLES. Une couverture plus large de player builds et d'appareils reste utile avant de traiter les donnees comme preuve de validation de release.
