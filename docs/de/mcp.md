# MCP Und Agent-Automation

SGG PerfMeter stellt Command-Metadaten fuer Unity MCP/editor-agent workflows bereit unter:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

Ziel ist strukturierte JSON-Ausgabe fuer Agents statt Screenshot-, Overlay-Text- oder Unity-Console-Parsing.

## Command Groups

| Command | Zweck |
| --- | --- |
| `perfmeter.setup.status` | Setup-Status lesen. |
| `perfmeter.setup.run` | Empfohlene Setup-Aktionen ausfuehren. |
| `perfmeter.compatibility.status` | Separate Import-, Core-Runtime- und aktive Render-Integration-Kompatibilitaet lesen. |
| `perfmeter.runtime.status` | Runtime-Status lesen. |
| `perfmeter.runtime.ensure` | Runtime starten, falls noetig. |
| `perfmeter.runtime.stop` | Runtime stoppen. |
| `perfmeter.runtime.reset_stats` | Rolling stats, alert counters und aktive Session-Counter zuruecksetzen. |
| `perfmeter.runtime.mode.set` | `Stopped`, `Background`, `Overlay` oder `OverdrawDiagnostic` setzen. |
| `perfmeter.metrics.latest` | Latest metrics inklusive custom metrics lesen. |
| `perfmeter.profiler.capabilities` | Gecachte Profiler-Metrik-Capabilities und Aufloesungs-Provenienz lesen, ohne Runtime oder Discovery zu starten. |
| `perfmeter.profiler.lease.capabilities` | Prozesslokale Profiler-Lease-Ressourcen und Reload-Semantik lesen. |
| `perfmeter.profiler.lease.status` | Aktuellen oder passenden prozesslokalen Profiler-Lease-Status lesen. |
| `perfmeter.alerts.latest` | Aktive alerts, counters und Editor warning state lesen. |
| `perfmeter.alerts.clear` | Aktive alerts, counters und cooldown state loeschen. |
| `perfmeter.alerts.capture.begin` | Begrenzte Klassifizierung fuer eine externe Aufnahme starten. |
| `perfmeter.alerts.capture.end` | Passende Klassifizierung fuer die externe Aufnahme beenden. |
| `perfmeter.device.info` | Device, graphics, display, monitor, pipeline und Unity environment info lesen. |
| `perfmeter.camera.snapshot` | Kamera transform/projection und URP/HDRP camera settings lesen. |
| `perfmeter.rendergraph.snapshot` | Zuletzt beobachtete PerfMeter render integration diagnostics fuer URP Render Graph oder HDRP Custom Pass lesen. |
| `perfmeter.render.snapshot` | Den integrationsneutralen Render-Integrationssnapshot mit Freshness, Kamera-/Pass-Kontext, GRD/VRS und Legacy-Render-Graph-Fassade lesen. |
| `perfmeter.overlay.set` | Overlay anzeigen/verbergen und preset, modules, corner, mode und target FPS setzen. |
| `perfmeter.overdraw.start` | Begrenzte overdraw measurement starten. |
| `perfmeter.overdraw.cancel` | Aktive overdraw measurement abbrechen. |
| `perfmeter.overdraw.heatmap.set` | Visual overdraw heatmap anzeigen oder verbergen. |
| `perfmeter.session.start` | Begrenzte Session-Aufzeichnung starten. |
| `perfmeter.session.stop` | Aufzeichnung stoppen und summary zurueckgeben. |
| `perfmeter.session.summary` | Aktuelle Session-summary lesen. |
| `perfmeter.session.export` | Aktuelle Session als projektlokales JSON oder CSV exportieren. |
| `perfmeter.capture.request` | Begrenzten externen GPU-Capture mit korreliertem Bundle anfordern. |
| `perfmeter.capture.status` | Capture- und Bundle-Status lesen. |
| `perfmeter.capture.cancel` | Passenden aktiven Capture abbrechen. |
| `perfmeter.capture.export` | Bereites Bundle atomar unter dem projektlokalen Bundle-Root exportieren. |
| `perfmeter.capture.export.request` | Single-Flight-Bundle-Export einreihen und Export-ID sowie Fortschritt zurueckgeben. |
| `perfmeter.capture.export.status` | Exportphase, Fortschritt, Abbruch, Retry und Artefakt-Autoritaet lesen. |
| `perfmeter.capture.export.cancel` | Abbruch des passenden aktiven Exports anfordern. |
| `perfmeter.capture.capabilities` | Schema-, Quota-, Retention-, Screenshot- und Provenance-Capabilities lesen. |

Nutze vorzugsweise `perfmeter.capture.export.request`, polle anschliessend `perfmeter.capture.export.status` und rufe optional `perfmeter.capture.export.cancel` auf. Der Legacy-Befehl `perfmeter.capture.export` blockiert aus Kompatibilitaetsgruenden. Export-Antworten enthalten die generische `external_artifact`-Envelope mit Zuordnung, Autoritaet, Finalisierung, Inhalt, Datenschutz-/Freigaberichtlinie, Groesse sowie Source- und Post-Copy-Hashes. Die read-only Lease-Befehle zeigen den prozesslokalen Konfliktstatus an, ohne eine Lease zu erwerben.

## Self-Overhead Im Runtime-Status

`perfmeter.runtime.status` enthaelt das additive Objekt `self_overhead`; dies ist kein separater Command. Top-level Keys sind `state`, `cpu_timing_available`, `gpu_timing_availability` und `has_budget_violation`.

Komponentenobjekte sind `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration` und `hdrp_render_integration`. Jedes enthaelt `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state` und `allocation_budget_state`.

Die Werte beschreiben feste 120-Frame-Fenster fuer CPU-Callbacks mit Durchschnitt pro Aufruf. GPU-Attribution ist `Unavailable`; eine inaktive Render-Integration ist `Unsupported`, eine unterstuetzte Komponente ohne Aufruf `NotMeasured`. Session-JSON/CSV-Schemas bleiben unveraendert, bestehende CPU/GPU-Metriken werden nicht angepasst.

## Typischer Profiling-Run

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

Nutze `OverdrawDiagnostic` nur fuer begrenzte URP-Diagnosefenster, weil numerical overdraw und heatmap rendering zusaetzliche GPU-Arbeit erzeugen. HDRP meldet overdraw/heatmap als unsupported, waehrend die restlichen diagnostics verfuegbar bleiben.

## Befehle fuer Speicher-Snapshots

| Befehl | Zweck und wichtigste Eingaben |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Einen manuellen Snapshot mit `capture_id`, optionalen Boolean-Capture-Flags, `minimum_free_disk_mb` und `cooldown_seconds` anfordern. |
| `perfmeter.memory.snapshot.status` | Snapshot- und korrelierten Bundle-Status lesen, ohne die Runtime zu starten oder den temporaeren Source-Pfad offenzulegen. |
| `perfmeter.memory.snapshot.capabilities` | Backend-Provenance, unterstuetzte Flags, das 512-MiB-Limit und den eigenen temporaeren Root lesen. |
| `perfmeter.memory.snapshot.triggers.configure` | System-Speicherschwellen- und begrenzte Leak-Wachstums-Trigger, Frame-Fenster, Flags, Free-Space-Guard und cooldown explizit aktivieren/deaktivieren. |

Die Request- und Trigger-Konfigurationsbefehle benoetigen Play Mode. Automation ist standardmaessig deaktiviert. Ein typischer Ablauf:

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Warte, bis das Bundle exportbereit ist, und verwende dann den bestehenden Befehl `perfmeter.capture.export`. Ein Memory-only-Bundle verwendet `requested_tool: MemoryProfiler`, enthaelt `memory-snapshot.json` und Manifest-Provenance, erzeugt aber kein externes GPU-Artefakt. Ein erfolgreicher Export ist einmalig und entfernt die eigene Staging-Quelle.

## Grafikdiagnose- und State-Collection-Befehle

Die folgenden sechs Befehle bilden die PM-GFX-001-Oberflaeche:

| Befehl | Zweck und wichtigste Eingaben |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Neueste Shader-GPU-Programm- und Graphics-Pipeline-Marker, dynamische Capability-Provenance, Katalogrevision und Graphics-API-Kontext lesen. Keine Eingaben. |
| `perfmeter.graphics.state_collection.request` | Einen begrenzten Trace starten. Erfordert Play Mode und eine aktive PerfMeter-Session; `capture_id` ist erforderlich, `trace_frames` ist 1–600 (Standard 60), `minimum_free_disk_mb` hat Standard 1024. |
| `perfmeter.graphics.state_collection.status` | Availability, State, Fortschritt, Backend-Identitaet, Counts, `is_busy`, `has_pending_cleanup`, Warnungen und den project-relativen Pfad des eigenen Artefakts lesen. Keine Eingaben. |
| `perfmeter.graphics.state_collection.capabilities` | Backend-Provenance, Trace-/Prewarm-Support, Cache-Miss- und Parallel-PSO-Support, Session-Anforderung, 600-Frame-/64-MiB-Limits und eigenen Artefakt-Root lesen. Keine Eingaben. |
| `perfmeter.graphics.state_collection.cancel` | Den passenden aktiven oder vorbereitenden Trace abbrechen und sein ausstehendes Artefakt bereinigen. Erfordert `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Ein eigenes project-relatives Artefakt in Play Mode laden und synchron prewarm-en. `relative_path` ist erforderlich; `max_state_count` ist 0–1.000.000 und standardmaessig 0. |

`perfmeter.graphics.diagnostics` liefert `shader_gpu_program_creation_value` und `graphics_pipeline_creation_value` sowie fuer jede Capability `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count` und `sampled_component_count`. `perfmeter.metrics.latest` und Session-Exporte liefern dieselben Marker-Metadaten. Werte behalten die entdeckte Recorder-Einheit und sind nicht grundsaetzlich Shader- oder PSO-Counts; verwende `sample_state`, statt Null als unavailable zu interpretieren.

Die State-Antwort enthaelt `result`, `availability`, `state`, `capture_id`, angeforderte/abgeschlossene Trace-Frames, Backend-ID/-Version, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup` und `warning`. `is_busy` bleibt waehrend Vorbereitung, Trace, Abschluss, Prewarm, Cleanup oder persistiertem Cleanup true; `has_pending_cleanup` bezeichnet ein eigenes Artefakt, das auf einen Retry wartet. Eine fehlgeschlagene Loeschung wird mit einem eigenen `.delete-pending`-Sidecar gespeichert, nach Domain Reload wiederhergestellt und erneut versucht. `StopSession` bricht einen aktiven Trace ab, daher muss die Session bis zum Abschluss aktiv bleiben. Ein Trace erreicht seinen terminal state, nachdem die angeforderten Frames am Frame-Ende getickt wurden; im Batch Mode gilt der Next-Frame-Fallback. Samples einer aktiven Session tragen `graphics_state_trace_id` gleich `capture_id`.

Typische Trace- und Prewarm-Sequenz:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

Es wird nur ein Graphics-State-Flight zugelassen. Eine wiederholte aktive ID liefert `AlreadyActive`, eine andere ueberlappende Trace-/Prewarm-Anfrage `RejectedOverlap`. Cancel trifft nur die passende aktive/vorbereitende ID. Das Unity-Backend meldet `supports_cache_miss_tracing: false`; Cache-Miss-Evidence wird nicht unterstuetzt, und das MCP-Prewarm-Schema bietet dafuer keine Eingabe. Artefakte gehoeren zu PerfMeter, liegen unter `Temp/PerfMeter/GraphicsStateCollections` und sind auf 64 MiB begrenzt.

## Render-Integrationssnapshot

`perfmeter.render.snapshot {}` ist ein read-only Befehl ohne Eingaben. Er startet die Runtime nicht. Die Antwort verwendet `schema_version: 1` und liefert `render_integration` mit aktueller Pipeline/Quelle, Observation-Frame und -Alter, `observation_matches_current_pipeline`, beobachteter Kameraidentitaet, Integrations-/Pass-/Injection-Metadaten, tatsaechlich geplanten PerfMeter-Passes, effektivem Rendering-Modus sofern verfuegbar, verschachteltem `gpu_resident_drawer`- und `variable_rate_shading`-Kontext sowie `legacy_render_graph`.

`gpu_resident_drawer` enthaelt Projekt-/Compute-Support, globale public Activity mit `activity_source`, URP-Forward+/Cluster-Kompatibilitaet, `degraded_reason` und verschachtelte BRG-`effectiveness`. Werte sind `null`, solange die Capability nicht `AvailableSampled` ist; Recorder-Namen, Exact-/Alias-Aufloesung und Component Counts erhalten die Provenienz. `scope: "brg_aggregate"` beweist keine GRD-Nutzung pro Renderer.

Der Befehl entspricht `PerformanceMeter.GetRenderIntegrationSnapshot()` und `TryGetRenderIntegrationSnapshot(...)`. Eine veraltete Observation wird mit explizitem Non-Match und Warning gemeldet, nicht als aktuell ausgegeben. `perfmeter.rendergraph.snapshot` bleibt als Legacy-Fassade erhalten. Der Befehl fuegt keine Editor-Navigation hinzu: Stabile Unity-APIs legen keinen RenderGraph-/CustomPass-Viewer und keine Pass-Ziele offen.
