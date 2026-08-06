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
| `perfmeter.alerts.latest` | Aktive alerts, counters und Editor warning state lesen. |
| `perfmeter.alerts.clear` | Aktive alerts, counters und cooldown state loeschen. |
| `perfmeter.alerts.capture.begin` | Begrenzte Klassifizierung fuer eine externe Aufnahme starten. |
| `perfmeter.alerts.capture.end` | Passende Klassifizierung fuer die externe Aufnahme beenden. |
| `perfmeter.device.info` | Device, graphics, display, monitor, pipeline und Unity environment info lesen. |
| `perfmeter.camera.snapshot` | Kamera transform/projection und URP/HDRP camera settings lesen. |
| `perfmeter.rendergraph.snapshot` | Zuletzt beobachtete PerfMeter render integration diagnostics fuer URP Render Graph oder HDRP Custom Pass lesen. |
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
| `perfmeter.capture.capabilities` | Schema-, Quota-, Retention-, Screenshot- und Provenance-Capabilities lesen. |

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
