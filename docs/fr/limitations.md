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
- `GenericUnity` utilise l'`ExternalGPUProfiler` experimental de Unity dans l'Editor/Development Build. Sa matrice reste RenderDoc sur desktop Windows/Linux avec D3D11/D3D12/Vulkan et PIX sur desktop Windows avec D3D12; la completion n'authentifie ni l'outil ni l'artefact.
- Le chemin natif optionnel prend uniquement en charge RenderDoc dans l'Editor Unity Windows x64 avec D3D11, D3D12 ou Vulkan. Development Player, Linux natif, IL2CPP, mobile et macOS natif ne sont pas pris en charge.
- Le package UPM reste sans binaire. Le bridge separe et epingle utilise seulement une `renderdoc.dll` deja chargee et n'installe, ne charge, ne lance ni n'injecte jamais RenderDoc.
- Native MetadataOnly utilise `DoNotShare` par defaut; Copy/Embed sont sensibles, soumis a des quotas separes et `ReviewBeforeShare`. Les artefacts generiques/caller restent observes, non autoritatifs.
- La capture de timing circulaire native de PIX n'est pas disponible. L'API de timing Windows documentee par Microsoft prend en charge la capture vers l'avant, mais ignore les controles de stockage circulaire, de limite memoire et d'abandon; PerfMeter ne remplace pas l'anneau pre-alerte demande par une capture vers l'avant sans limite de stockage documentee ni par une integration PIX privee.
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

## Limites et confidentialité des snapshots mémoire optionnels

- La fonction est indisponible sans `com.unity.memoryprofiler` `1.1.0+` sous Unity `6000.4+`; le package core n'installe ni n'exige cette dépendance.
- La capture manuelle est la seule option par défaut. Les triggers de seuil de mémoire système et de croissance de fuite bornée sont opt-in; chaque requête est soumise aux gardes single-flight/overlap, cooldown, espace libre minimum, backend et capture-flags.
- Le staging `.snap` possédé se trouve sous `Temp/PerfMeter/MemorySnapshots` et est limité à 512 Mio. L'evidence uniquement mémoire est exportée sous `Temp/PerfMeter/CaptureBundles`, avec un quota total de rétention de 2 Gio. Un export réussi est à usage unique et supprime la source de staging, avec des avertissements explicites en cas de nettoyage impossible.
- Les snapshots peuvent contenir de la mémoire sensible du processus. Protégez-les et examinez-les avant tout partage. Le bundle indique `contains_sensitive_memory`, la provenance du backend et des flags, `memory-snapshot.json` et les métadonnées SHA-256; il ne crée aucun artefact GPU externe.
- La suppression bloquée par l'OS et la protection portable managed contre les courses avec des reparse points sont best-effort. Les chemins dangereux ou non possédés sont rejetés et les échecs de nettoyage restent visibles comme avertissements.
- Les preuves comprennent memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, la compilation optionnelle avec `com.unity.memoryprofiler@1.1.12`, ainsi que Unity `6000.4.12f1` full EditMode `182/182` et full PlayMode `14/14`. Cela ne constitue pas une affirmation sur le release-player ou le comportement appareil.

## Limites du diagnostic graphique et de GraphicsStateCollection

- Les marqueurs de création de programmes GPU de shader et de graphics pipeline sont des capabilities `ProfilerRecorder` dynamiques. Unity, la plateforme, l'API graphique et l'état du refresh du catalogue peuvent changer leur availability. Utilisez `Unavailable`, `AvailableNoSample`, `AvailableSampled` et la provenance; ne déduisez pas l'availability d'une valeur nulle.
- Les valeurs des marqueurs conservent `Unit` et `DataType` du recorder et restent brutes. Elles ne sont pas universellement des counts de shaders ou de PSO, et PerfMeter ne les convertit pas vers une unité commune. Les métadonnées de capability contiennent la résolution exact/alias, les noms de recorders résolus, les component counts résolus/échantillonnés et la révision du catalogue.
- L'assembly optionnelle `SGG.PerfMeter.GraphicsStateCollection` cible Unity `6000.4+`. Elle utilise `UnityEngine.Experimental.Rendering.GraphicsStateCollection` sous `6000.4` et `UnityEngine.Rendering.GraphicsStateCollection` sous `6000.5+`; les versions antérieures ne sont pas prises en charge pour cette intégration.
- Un trace exige une session PerfMeter active. En Play Mode normal, les trace frames se terminent après l'end-of-frame; en batch mode, un fallback au frame suivant est utilisé. Les samples corrélés sont soumis aux réglages de warm-up, d'intervalle et de nombre maximal de samples de la session.
- Un seul graphics-state flight est admis, y compris préparation, finalisation du trace, prewarm et cleanup. Un external GPU capture, memory snapshot ou alert-capture actif provoque aussi un rejet d'overlap. `IsBusy`/`is_busy` couvre ces flights et le cleanup persistant; `HasPendingCleanup`/`has_pending_cleanup` signale précisément un artefact owned en attente de retry. L'annulation correspondante est best-effort; les échecs de cleanup restent visibles et peuvent retarder la demande suivante.
- `StopSession()` annule un trace actif; une session active est donc nécessaire pendant tout le trace. Une suppression échouée de l'artefact owned crée un sidecar voisin `.delete-pending`; il est restauré et retenté après un domain reload. Le warning et l'état busy restent visibles jusqu'à la suppression de l'artefact et du marker.
- Le prewarm accepte uniquement un artefact owned relatif au projet, s'exécute de manière synchrone, conserve l'artefact et peut signaler un progressive warmup incomplet. Le backend Unity ne prend pas en charge le cache-miss tracing: la demande renvoie `Unavailable` et aucune evidence de cache-miss n'est exposée.
- Les artefacts `.graphicsstate` owned sont stockés sous `Temp/PerfMeter/GraphicsStateCollections`, doivent être des fichiers réguliers non vides et sont limités à 64 Mio. Le trace est limité à 600 frames et le prewarm progressif à 1 000 000 states. Les gardes d'espace libre minimum et de chemins project-locaux s'appliquent.
- Les preuves finales sont: compile Unity `6000.4.12f1` réussi; GSC EditMode targeted `25/25`, API EditMode `PerformanceMeter` `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208` et full post-fix PlayMode `16/16`. Un compile isolé de l'optional consumer sous Unity `6000.5.6f1` a également réussi. Les tests full Unity `6000.5`, le comportement release-player et celui des appareils restent des release gates et ne sont pas affirmés ici.

## Limites du contexte d'intégration du rendu

- `PerfMeterRenderIntegrationSnapshot` est un contrat d'observation neutre vis-à-vis de l'intégration, pas un capture profond de Render Graph ou Custom Pass. Les lectures ne démarrent pas le runtime; avant la première observation, le pipeline courant supporté peut être `Available` avec `NotObserved`, et un changement de pipeline/configuration marque l'observation précédente comme obsolète via `ObservationMatchesCurrentPipeline: false`, frame/age explicites et warning.
- URP utilise le `UniversalRenderingData.renderingMode` public de la frame courante et indique les passes PerfMeter effectivement planifiés. HDRP indique le `CustomPass` PerfMeter réel, mais le effective rendering mode reste indisponible.
- La reflection privée/interne des passes et ressources Render Graph a été supprimée. La façade legacy conserve `registered_pass_count`, `merged_pass_count`, `transient_resource_count`, `imported_resource_count` et `aliased_resource_count` à `-1`, car aucune API publique stable ne les expose.
- L'activité GRD utilise le résultat public de `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` et décrit l'état runtime global, pas l'utilisation de GRD par une caméra ou un renderer précis. Forward+ sous URP est une observation de la frame courante; sous HDRP, l'availability du rendering mode/Forward+ reste `Unknown`.
- L'effectiveness GRD utilise les compteurs agrégés BRG de draw calls/instances avec leur provenance exacte. Ils peuvent inclure d'autres utilisateurs de `BatchRendererGroup` et ne prouvent donc pas la participation GRD par renderer. Les valeurs indisponibles ou sans sample sont sérialisées en `null`.
- VRS expose le support matériel faisant autorité de `SystemInfo`/`ShadingRateInfo`. Configuration et activité restent `Unknown` jusqu'à ce qu'un futur typed adapter les prouve; aucune activité VRS n'est revendiquée.
- Unity n'expose pas de viewer public stable RenderGraph/CustomPass ni d'API de pass targets. PerfMeter n'ajoute donc pas de navigation dans l'Editor et ne la promet pas.
- Le schema de contexte de capture v1 conserve `render` et ajoute `render_integration`; les schemas JSON/CSV de session ne changent pas. Le contexte d'un capture externe est figé au premier sample `Capturing`, et non remplacé par des lectures ultérieures.
- Evidence finale PM-REN-001: main compile Unity `6000.4.12f1` réussi; `PerformanceMeterApiTests` targeted `53/53`, `PerfMeterCaptureBundleTests` `15/15` et `PerformanceMeterPlayModeSmokeTests` `12/12`; full EditMode final `215/215` et full PlayMode `16/16` réussis. Focused review P1/P2 resolved. La compile matrix isolée a réussi sur Unity `6000.4.12f1` URP `17.4` et HDRP `17.4`, ainsi que Unity `6000.5.6f1` URP `17.5` et HDRP `17.5`. La validation release-player/appareil reste pending; aucune release n'est revendiquée.
- Evidence finale PM-GRD-001: compile Unity `6000.4.12f1` réussi; API targeted `58/58`, capture-bundle `15/15` et PlayMode smoke `12/12`; full EditMode `220/220` et PlayMode `16/16` réussis. Focused review P1/P2 resolved; la compile matrix Unity `6000.4`/`6000.5` avec URP `17.4`/`17.5` et HDRP `17.4`/`17.5` a réussi. Le comportement release-player/appareil reste pending.
