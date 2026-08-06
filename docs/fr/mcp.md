# MCP Et Automatisation Par Agents

SGG PerfMeter expose des metadonnees de commandes pour les workflows Unity MCP/editor-agent dans le chemin de package:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

L'objectif est une sortie JSON structuree pour les agents, sans analyse de captures d'ecran, analyse du texte d'overlay ou extraction depuis Unity Console.

## Groupes De Commandes

| Commande | Objectif |
| --- | --- |
| `perfmeter.setup.status` | Lire l'etat de configuration. |
| `perfmeter.setup.run` | Executer les actions de configuration recommandees. |
| `perfmeter.compatibility.status` | Lire separement les compatibilites import, core runtime et render integration active. |
| `perfmeter.runtime.status` | Lire l'etat runtime. |
| `perfmeter.runtime.ensure` | Demarrer le runtime si necessaire. |
| `perfmeter.runtime.stop` | Arreter le runtime. |
| `perfmeter.runtime.reset_stats` | Reinitialiser les stats roulantes, les compteurs d'alertes et les compteurs de session active. |
| `perfmeter.runtime.mode.set` | Basculer vers `Stopped`, `Background`, `Overlay` ou `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Lire les dernieres metriques, y compris les metriques personnalisees. |
| `perfmeter.profiler.capabilities` | Lire les capabilities et la provenance de resolution des metriques Profiler en cache sans demarrer le runtime ni la discovery. |
| `perfmeter.alerts.latest` | Lire les alertes actives, les compteurs et l'etat des avertissements Editor. |
| `perfmeter.alerts.clear` | Effacer les alertes actives, les compteurs et l'etat de cooldown. |
| `perfmeter.alerts.capture.begin` | Demarrer la classification bornee d'une capture externe. |
| `perfmeter.alerts.capture.end` | Terminer la classification de capture externe correspondante. |
| `perfmeter.device.info` | Lire les informations de device, graphics, display, monitor, pipeline et environnement Unity. |
| `perfmeter.camera.snapshot` | Lire transform/projection de camera et URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Lire les derniers diagnostics render integration observes pour URP Render Graph ou HDRP Custom Pass. |
| `perfmeter.overlay.set` | Afficher/masquer l'overlay et definir preset, modules, coin, mode et FPS cible. |
| `perfmeter.overdraw.start` | Demarrer une mesure d'overdraw bornee. |
| `perfmeter.overdraw.cancel` | Annuler la mesure d'overdraw active. |
| `perfmeter.overdraw.heatmap.set` | Afficher ou masquer la heatmap visuelle d'overdraw. |
| `perfmeter.session.start` | Demarrer un enregistrement de session borne. |
| `perfmeter.session.stop` | Arreter l'enregistrement et renvoyer le resume. |
| `perfmeter.session.summary` | Lire le resume de session courant. |
| `perfmeter.session.export` | Exporter la session courante en JSON ou CSV local au projet. |
| `perfmeter.capture.request` | Demander une capture GPU externe bornee et un bundle correle. |
| `perfmeter.capture.status` | Lire l'etat de la capture et du bundle. |
| `perfmeter.capture.cancel` | Annuler la capture active correspondante. |
| `perfmeter.capture.export` | Exporter atomiquement un bundle pret sous la racine locale du projet. |
| `perfmeter.capture.capabilities` | Lire les capacites de schema, quota, retention, screenshot et provenance. |

## Self-Overhead Dans Le Status Runtime

`perfmeter.runtime.status` inclut l'objet additif `self_overhead`; ce n'est pas une commande separee. Les cles principales sont `state`, `cpu_timing_available`, `gpu_timing_availability` et `has_budget_violation`.

Les objets de composants sont `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration` et `hdrp_render_integration`. Chacun contient `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state` et `allocation_budget_state`.

Les valeurs decrivent des fenetres fixes de 120 frames pour les callbacks CPU, avec moyennes par invocation. L'attribution GPU est `Unavailable`; une render integration inactive est `Unsupported` et un composant pris en charge sans appel est `NotMeasured`. Les schemas JSON/CSV de session restent inchanges et les metriques CPU/GPU existantes ne sont pas ajustees.

## Execution De Profilage Typique

```text
perfmeter.profiler.capabilities {}
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Utilisez `OverdrawDiagnostic` uniquement pour des fenetres de diagnostic URP bornees, car l'overdraw numerique et le rendu de heatmap ajoutent du travail GPU. HDRP signale overdraw/heatmap comme unsupported, tandis que les autres diagnostics restent disponibles.

## Commandes de snapshots mémoire

| Commande | But et principales entrées |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Demander un snapshot manuel avec `capture_id`, des booléens de capture-flags facultatifs, `minimum_free_disk_mb` et `cooldown_seconds`. |
| `perfmeter.memory.snapshot.status` | Lire l'état du snapshot et du bundle corrélé sans démarrer le runtime ni exposer le source path temporaire. |
| `perfmeter.memory.snapshot.capabilities` | Lire la provenance du backend, les flags pris en charge, la limite de 512 Mio et la racine temporaire possédée. |
| `perfmeter.memory.snapshot.triggers.configure` | Activer ou désactiver explicitement les triggers de seuil de mémoire système et de croissance de fuite bornée, leur fenêtre de frames, les flags, la garde d'espace libre et le cooldown. |

Les commandes de requête et de configuration des triggers nécessitent le Play Mode. L'automatisation est désactivée par défaut. Séquence typique :

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Attendez que le bundle soit prêt à exporter, puis utilisez la commande existante `perfmeter.capture.export`. Un bundle uniquement mémoire utilise `requested_tool: MemoryProfiler`, contient `memory-snapshot.json` et la provenance du manifest, et ne produit aucun artefact GPU externe. L'export réussi est à usage unique et supprime la source de staging possédée.

## Commandes de diagnostic graphique et GraphicsStateCollection

Les six commandes suivantes constituent la surface PM-GFX-001:

| Commande | But et principales entrées |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Lire les dernières valeurs des marqueurs de création de programmes GPU de shader et de graphics pipeline, la provenance dynamique des capabilities, la révision du catalogue et le contexte de l'API graphique. Aucune entrée. |
| `perfmeter.graphics.state_collection.request` | Démarrer un trace borné. Requiert le Play Mode et une session PerfMeter active; `capture_id` est obligatoire, `trace_frames` vaut 1–600 (60 par défaut) et `minimum_free_disk_mb` vaut 1024 par défaut. |
| `perfmeter.graphics.state_collection.status` | Lire availability, state, progression, identité du backend, counts, `is_busy`, `has_pending_cleanup`, warnings et le chemin relatif au projet de l'artefact owned. Aucune entrée. |
| `perfmeter.graphics.state_collection.capabilities` | Lire la provenance du backend, le support du trace/prewarm, du cache-miss et du PSO parallèle, l'exigence de session, les limites de 600 frames/64 Mio et la racine d'artefacts owned. Aucune entrée. |
| `perfmeter.graphics.state_collection.cancel` | Annuler le trace actif ou en préparation correspondant et nettoyer son artefact en attente. Requiert `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Charger et préchauffer de façon synchrone un artefact owned relatif au projet en Play Mode. `relative_path` est obligatoire; `max_state_count` vaut 0–1 000 000 et 0 par défaut. |

`perfmeter.graphics.diagnostics` renvoie `shader_gpu_program_creation_value` et `graphics_pipeline_creation_value`, ainsi que, pour chaque capability, `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count` et `sampled_component_count`. `perfmeter.metrics.latest` et les exports de session exposent les mêmes métadonnées de marqueur. Les valeurs conservent l'unité découverte du recorder et ne sont pas universellement des counts de shaders ou de PSO; utilisez `sample_state` au lieu d'interpréter zéro comme unavailable.

La réponse de state contient `result`, `availability`, `state`, `capture_id`, les trace frames demandés/terminés, l'ID/version du backend, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup` et `warning`. `is_busy` reste true pendant la préparation, le trace, la fin, le prewarm, le cleanup ou un cleanup persistant; `has_pending_cleanup` identifie un artefact owned en attente de retry. Une suppression échouée est persistée avec un sidecar owned `.delete-pending`, restauré et retenté après un domain reload. `StopSession` annule un trace actif; la session doit donc rester active jusqu'à la fin. Le trace atteint son état terminal après le tick des frames demandés en fin de frame; le batch mode utilise un fallback au frame suivant. Les samples admis par une session active portent `graphics_state_trace_id` égal à `capture_id`.

Séquence typique de trace et de prewarm:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

Un seul graphics-state flight est admis. Un ID actif répété renvoie `AlreadyActive`; un autre trace/prewarm en overlap renvoie `RejectedOverlap`. Cancel ne correspond qu'à l'ID actif/en préparation. Le backend Unity indique `supports_cache_miss_tracing: false`: l'evidence de cache-miss n'est pas prise en charge et le schema MCP de prewarm n'expose pas cette entrée. Les artefacts sont possédés par PerfMeter, se trouvent sous `Temp/PerfMeter/GraphicsStateCollections` et sont limités à 64 Mio.
