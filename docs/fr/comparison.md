# Comparaison Avec Advanced FPS Counter Et Graphy

Ceci est une comparaison de produit et d'architecture, pas un benchmark runtime mesure.

## Version Courte

Advanced FPS Counter et Graphy sont de solides overlays visuels generalistes. Ils sont utiles lorsque le besoin principal est un HUD FPS/memoire/device rapide a integrer, avec une large prise en charge des anciennes versions d'Unity et une personnalisation visuelle.

SGG PerfMeter est volontairement plus cible et plus diagnostique: Unity `6000.4+`, URP `17.4+` Render Graph, HDRP `17.4+` Custom Pass diagnostics, snapshots structures, export de session, URP overdraw diagnostics, metadonnees reproductibles de camera/device et automatisation MCP/API.

## Tableau De Comparaison

| Zone | SGG PerfMeter | Advanced FPS Counter | Graphy |
| --- | --- | --- | --- |
| Positionnement principal | 🔵 Diagnostics URP Render Graph / HDRP Custom Pass + API de profilage prete pour l'automatisation | ⚠️ Compteur FPS/memoire/device flexible en jeu | ⚠️ Moniteur et debugger visuel de stats FPS/memoire/audio |
| Cible Unity | ⚠️ Unity `6000.4+`, URP `17.4+` / HDRP `17.4+` | 🔵 Large prise en charge des anciennes versions Unity | 🔵 Large prise en charge des anciennes versions Unity |
| Backend UI | 🔵 Overlay UI Toolkit | ⚠️ Labels uGUI Canvas/Text | ⚠️ Modules uGUI Text/Image |
| Source de timing | 🔵 `FrameTimingManager` + stats roulantes | ⚠️ Echantillonnage runtime frame/update | ⚠️ Echantillonnage d'historique `Time.unscaledDeltaTime` |
| Separation CPU/GPU | 🔵 CPU frame, main thread, render thread, present wait, GPU quand disponible | 🛑 Pas de separation equivalente | 🛑 Pas de separation equivalente |
| Classification de goulet | 🔵 GPU, CPU main, CPU render, present-limited, balanced, unknown | 🛑 Pas d'equivalent | 🛑 Pas d'equivalent |
| Compteurs de rendu | 🔵 Draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory | 🛑 Pas de set de compteurs URP/SRP | 🛑 Pas de set de compteurs URP/SRP |
| Reproductibilite device/camera | 🔵 Snapshots structures de device et camera | ⚠️ Panneau device seulement | ⚠️ Panneau device seulement |
| Sessions | 🔵 Recorder borne, warm-up, portee de scene, pires frames, export JSON/CSV | 🛑 Fonctionnalite non principale | ⚠️ Idee de type roadmap |
| Overdraw | 🔵 Mesure numerique + heatmap visuelle via URP Render Graph; explicit unsupported state en HDRP | 🛑 Non | 🛑 Non |
| Automatisation | 🔵 Surface de commandes MCP et snapshots publics | 🛑 Non | 🛑 Non |

## Ce Que SGG PerfMeter Fait Mieux

- Explique les goulets d'etranglement probables des frames avec CPU frame, main thread, render thread, present wait, timing GPU et donnees de budget de frame.
- Expose des compteurs de rendu orientes URP, des diagnostics URP Render Graph et l'observation HDRP Custom Pass.
- Produit des rapports de performance reproductibles avec scene, device, camera, reglages, echantillons de session, resumes et metadonnees des pires frames.
- Donne aux outils et a l'automatisation des donnees structurees via API publique et commandes MCP.
- Integre la mesure d'overdraw bornee et une heatmap visuelle comme diagnostics explicites.

## Ce Que Les Concurrents Font Encore Mieux

- Les deux concurrents prennent en charge une plage plus large d'anciennes versions d'Unity, ce qui est un avantage pour les projets legacy.
- Advanced FPS Counter a une UX de compteur visuel tres directe a integrer, une personnalisation d'inspecteur mature, des bascules hotkeys/gesture circulaire, des motifs UI min/max/average et des exemples VR/world-space.
- Graphy a des supports marketing publics solides, des etats de modules clairs, une personnalisation visuelle, hotkeys/background mode, une UX mature de paquets debugger et une large notoriete publique.

## Ce Qu'il Ne Faut Pas Revendiquer

- SGG PerfMeter ne remplace pas Unity Profiler, RenderDoc, Profile Analyzer ou Frame Debugger.
- SGG PerfMeter n'est pas zero-overhead; utilisez low-overhead et documentez les couts explicites des diagnostics.
- SGG PerfMeter n'est pas un package de compatibilite legacy toutes plateformes/tous pipelines.
