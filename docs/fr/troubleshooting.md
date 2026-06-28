# Depannage

Utilisez cette checklist lorsque PerfMeter n'affiche pas les donnees attendues.

## L'overlay N'apparait Pas

- Ouvrez `SGG/Perfmeter/Setup` et confirmez que la visibilite de l'overlay est activee.
- Confirmez que le mode de collecte est `Overlay`, pas `Background` ni `Stopped`.
- Si vous utilisez la configuration sans code, confirmez que le fichier de reglages existe dans `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Si vous utilisez un bootstrap manuel, confirmez que `PerformanceMeter.EnsureRunning()` est appele apres le chargement de scene.
- Entrez en Play Mode; les appels API en Edit Mode sont surs mais ne creent pas d'overlay runtime.

## Frame Timing Ou GPU Timing Manquant

- Activez Player Settings -> Rendering -> Frame Timing Stats.
- Privilegiez Vulkan sur Android lorsque le timing de frame GPU est important.
- Traitez OpenGL/OpenGLES comme mode degrade pour le timing GPU.
- Verifiez `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` et `Warning` avant de supposer qu'un compteur existe.

## La Mesure D'overdraw Ne Progresse Pas

- En URP, installez `PerfMeterRenderGraphFeature` dans le renderer URP actif.
- En HDRP, overdraw et heatmap ne sont pas pris en charge by design; utilisez les core diagnostics.
- Confirmez que la camera active utilise le renderer qui contient la feature.
- Confirmez que le backend cible prend en charge fragment UAV/storage buffers, compute shaders et async GPU readback.
- Utilisez `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` pour une fenetre de mesure bornee.
- Si la cible n'est pas prise en charge, PerfMeter signale `OverdrawState.Unsupported` au lieu de planifier la passe.

## L'export De Session Echoue

- Exportez vers un chemin local au projet.
- N'ecrasez pas un export existant sauf si votre workflow le supprime explicitement d'abord.
- Gardez `MaxSamples` borne pour les longues executions.
- Utilisez des frames/secondes de warm-up pour eviter les spikes de demarrage dans les resumes.

## Les Alertes Sont Trop Bruyantes

- Ajustez les seuils et fenetres de frames consecutives dans les reglages JSON.
- Augmentez les cooldowns d'avertissements Editor.
- Desactivez les logs d'avertissements Editor lorsque les callbacks ou logs structures suffisent.

## Les Donnees Different Entre Appareils

C'est attendu. Les timings GPU, compteurs Profiler, informations d'affichage, async readback et support d'overdraw varient selon l'API graphique, la plateforme, la version Unity et l'appareil. Utilisez les snapshots de device et les avertissements dans les sessions exportees pour expliquer les differences.
