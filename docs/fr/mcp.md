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
| `perfmeter.runtime.status` | Lire l'etat runtime. |
| `perfmeter.runtime.ensure` | Demarrer le runtime si necessaire. |
| `perfmeter.runtime.stop` | Arreter le runtime. |
| `perfmeter.runtime.reset_stats` | Reinitialiser les stats roulantes, les compteurs d'alertes et les compteurs de session active. |
| `perfmeter.runtime.mode.set` | Basculer vers `Stopped`, `Background`, `Overlay` ou `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Lire les dernieres metriques, y compris les metriques personnalisees. |
| `perfmeter.alerts.latest` | Lire les alertes actives, les compteurs et l'etat des avertissements Editor. |
| `perfmeter.alerts.clear` | Effacer les alertes actives, les compteurs et l'etat de cooldown. |
| `perfmeter.device.info` | Lire les informations de device, graphics, display, monitor, pipeline et environnement Unity. |
| `perfmeter.camera.snapshot` | Read camera transform/projection and URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Read latest observed PerfMeter render integration diagnostics for URP Render Graph or HDRP Custom Pass. |
| `perfmeter.overlay.set` | Afficher/masquer l'overlay et definir preset, modules, coin, mode et FPS cible. |
| `perfmeter.overdraw.start` | Demarrer une mesure d'overdraw bornee. |
| `perfmeter.overdraw.cancel` | Annuler la mesure d'overdraw active. |
| `perfmeter.overdraw.heatmap.set` | Afficher ou masquer la heatmap visuelle d'overdraw. |
| `perfmeter.session.start` | Demarrer un enregistrement de session borne. |
| `perfmeter.session.stop` | Arreter l'enregistrement et renvoyer le resume. |
| `perfmeter.session.summary` | Lire le resume de session courant. |
| `perfmeter.session.export` | Exporter la session courante en JSON ou CSV local au projet. |

## Execution De Profilage Typique

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```

Utilisez `OverdrawDiagnostic` uniquement pour des fenetres de diagnostic bornees, car l'overdraw numerique et le rendu de heatmap ajoutent du travail GPU.
