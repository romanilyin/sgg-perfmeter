# MCP E Automazione Agent

SGG PerfMeter espone metadati dei comandi per workflow Unity MCP/editor-agent nel percorso pacchetto:

```text
Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json
```

L'obiettivo e un output JSON strutturato per agent, invece di parsing di screenshot, parsing del testo dell'overlay o scraping della Unity Console.

## Gruppi Di Comandi

| Command | Scopo |
| --- | --- |
| `perfmeter.setup.status` | Legge lo stato del setup. |
| `perfmeter.setup.run` | Esegue le azioni di setup consigliate. |
| `perfmeter.compatibility.status` | Legge separatamente la compatibilita import, core runtime e render integration attiva. |
| `perfmeter.runtime.status` | Legge lo stato runtime. |
| `perfmeter.runtime.ensure` | Avvia il runtime se necessario. |
| `perfmeter.runtime.stop` | Ferma il runtime. |
| `perfmeter.runtime.reset_stats` | Reimposta rolling stats, contatori alert e contatori della sessione attiva. |
| `perfmeter.runtime.mode.set` | Passa a `Stopped`, `Background`, `Overlay` o `OverdrawDiagnostic`. |
| `perfmeter.metrics.latest` | Legge le metriche piu recenti, incluse le custom metrics. |
| `perfmeter.profiler.capabilities` | Legge le capability e la provenienza di risoluzione delle metriche Profiler in cache senza avviare il runtime o la discovery. |
| `perfmeter.alerts.latest` | Legge alert attivi, contatori e stato degli avvisi Editor. |
| `perfmeter.alerts.clear` | Cancella alert attivi, contatori e stato cooldown. |
| `perfmeter.alerts.capture.begin` | Avvia la classificazione limitata di una cattura esterna. |
| `perfmeter.alerts.capture.end` | Termina la classificazione della cattura esterna corrispondente. |
| `perfmeter.device.info` | Legge informazioni su device, graphics, display, monitor, pipeline e ambiente Unity. |
| `perfmeter.camera.snapshot` | Legge transform/projection camera e URP/HDRP camera settings. |
| `perfmeter.rendergraph.snapshot` | Legge gli ultimi diagnostics di render integration osservati per URP Render Graph o HDRP Custom Pass. |
| `perfmeter.overlay.set` | Mostra/nasconde l'overlay e imposta preset, modules, corner, mode e target FPS. |
| `perfmeter.overdraw.start` | Avvia una misurazione overdraw limitata. |
| `perfmeter.overdraw.cancel` | Annulla la misurazione overdraw attiva. |
| `perfmeter.overdraw.heatmap.set` | Mostra o nasconde la overdraw heatmap visiva. |
| `perfmeter.session.start` | Avvia la registrazione di una sessione limitata. |
| `perfmeter.session.stop` | Ferma la registrazione e restituisce il riepilogo. |
| `perfmeter.session.summary` | Legge il riepilogo della sessione corrente. |
| `perfmeter.session.export` | Esporta la sessione corrente in JSON o CSV locale al progetto. |
| `perfmeter.capture.request` | Richiede un capture GPU esterno limitato e un bundle correlato. |
| `perfmeter.capture.status` | Legge lo stato del capture e del bundle. |
| `perfmeter.capture.cancel` | Annulla il capture attivo corrispondente. |
| `perfmeter.capture.export` | Esporta atomicamente un bundle pronto sotto la root locale del progetto. |
| `perfmeter.capture.capabilities` | Legge schema, quota, retention, screenshot e provenance capabilities. |

## Self-Overhead Nello Stato Runtime

`perfmeter.runtime.status` include l'oggetto additivo `self_overhead`; non e un comando separato. Le chiavi principali sono `state`, `cpu_timing_available`, `gpu_timing_availability` e `has_budget_violation`.

Gli oggetti componente sono `collector`, `custom_metric_providers`, `cpu_core_provider`, `overlay`, `urp_render_integration` e `hdrp_render_integration`. Ognuno contiene `component`, `state`, `window_frame_count`, `invocation_count`, `average_cpu_time_ms`, `max_cpu_time_ms`, `allocated_bytes`, `average_allocated_bytes`, `cpu_budget_ms`, `allocation_budget_bytes`, `cpu_budget_state` e `allocation_budget_state`.

I valori descrivono finestre fisse di 120 frame per callback CPU con medie per invocazione. L'attribuzione GPU e `Unavailable`; una render integration inattiva e `Unsupported`, mentre un componente supportato senza chiamate e `NotMeasured`. Gli schemi JSON/CSV di sessione non cambiano e le metriche CPU/GPU esistenti non vengono modificate.

## Esecuzione Di Profiling Tipica

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

Usa `OverdrawDiagnostic` solo per finestre diagnostiche URP limitate perche numerical overdraw e rendering heatmap aggiungono lavoro GPU extra. HDRP riporta overdraw/heatmap come unsupported, mentre il resto dei diagnostics resta disponibile.

## Comandi per gli snapshot di memoria

| Comando | Scopo e input principali |
| --- | --- |
| `perfmeter.memory.snapshot.request` | Richiede uno snapshot manuale con `capture_id`, booleani opzionali dei capture flags, `minimum_free_disk_mb` e `cooldown_seconds`. |
| `perfmeter.memory.snapshot.status` | Legge lo stato dello snapshot e del bundle correlato senza avviare il runtime né esporre il source path temporaneo. |
| `perfmeter.memory.snapshot.capabilities` | Legge la provenienza del backend, i flag supportati, il limite di 512 MiB e la root temporanea posseduta. |
| `perfmeter.memory.snapshot.triggers.configure` | Abilita o disabilita esplicitamente i trigger di soglia della memoria di sistema e crescita limitata delle perdite, la finestra di frame, i flag, la guardia dello spazio libero e il cooldown. |

I comandi di richiesta e configurazione dei trigger richiedono il Play Mode. L'automazione è disabilitata per impostazione predefinita. Sequenza tipica:

```text
perfmeter.memory.snapshot.capabilities {}
perfmeter.memory.snapshot.request {"capture_id":"memory-spike-01"}
perfmeter.memory.snapshot.status {}
perfmeter.capture.export {"capture_id":"memory-spike-01"}
```

Attendi che il bundle sia pronto per l'export, quindi usa il comando esistente `perfmeter.capture.export`. Un bundle solo memoria usa `requested_tool: MemoryProfiler`, include `memory-snapshot.json` e la provenienza nel manifest e non produce alcun artefatto GPU esterno. Un export riuscito è monouso e rimuove la sorgente di staging posseduta.

## Comandi di diagnostica grafica e GraphicsStateCollection

I sei comandi seguenti formano la superficie PM-GFX-001:

| Comando | Scopo e input principali |
| --- | --- |
| `perfmeter.graphics.diagnostics` | Leggere gli ultimi valori dei marker di creazione dei programmi GPU shader e delle graphics pipeline, la provenance dinamica delle capability, la revisione del catalogo e il contesto della graphics API. Nessun input. |
| `perfmeter.graphics.state_collection.request` | Avviare un trace limitato. Richiede Play Mode e una sessione PerfMeter attiva; `capture_id` e obbligatorio, `trace_frames` e 1–600 (default 60) e `minimum_free_disk_mb` ha default 1024. |
| `perfmeter.graphics.state_collection.status` | Leggere availability, state, avanzamento, identita del backend, counts, `is_busy`, `has_pending_cleanup`, warnings e il path relativo al progetto dell'artefatto owned. Nessun input. |
| `perfmeter.graphics.state_collection.capabilities` | Leggere provenance del backend, supporto trace/prewarm, supporto cache-miss e PSO paralleli, requisito di sessione, limiti di 600 frames/64 MiB e root degli artefatti owned. Nessun input. |
| `perfmeter.graphics.state_collection.cancel` | Annullare il trace attivo o in preparazione corrispondente e pulire l'artefatto in attesa. Richiede `capture_id`. |
| `perfmeter.graphics.state_collection.prewarm` | Caricare ed eseguire sincronicamente il prewarm di un artefatto owned relativo al progetto in Play Mode. `relative_path` e obbligatorio; `max_state_count` e 0–1.000.000, default 0. |

`perfmeter.graphics.diagnostics` restituisce `shader_gpu_program_creation_value` e `graphics_pipeline_creation_value`, oltre a `sample_state`, `resolution`, `resolved_recorder_names`, `unit`, `data_type`, `resolved_component_count` e `sampled_component_count` per ogni capability. `perfmeter.metrics.latest` e gli export di sessione espongono gli stessi metadata dei marker. I valori mantengono l'unita scoperta del recorder e non sono universalmente conteggi di shader o PSO; usa `sample_state` invece di interpretare zero come unavailable.

La risposta di state include `result`, `availability`, `state`, `capture_id`, trace frames richiesti/completati, ID/versione del backend, `artifact_relative_path`, `artifact_size_bytes`, `total_graphics_state_count`, `variant_count`, `completed_warmup_count`, `is_warmed_up`, `is_busy`, `has_pending_cleanup` e `warning`. `is_busy` resta true durante preparazione, trace, conclusione, prewarm, cleanup o cleanup persistente; `has_pending_cleanup` indica un artefatto owned in attesa di retry. Una cancellazione fallita viene persistita con un sidecar owned `.delete-pending`, ripristinato e ritentato dopo un domain reload. `StopSession` annulla un trace attivo, quindi la sessione deve restare attiva fino al completamento. Il trace raggiunge lo stato terminale dopo il tick dei frames richiesti a fine frame; in batch mode usa un fallback al frame successivo. I sample ammessi da una sessione attiva contengono `graphics_state_trace_id` uguale a `capture_id`.

Sequenza tipica di trace e prewarm:

```text
perfmeter.session.start {"warmup_seconds":0,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.graphics.state_collection.capabilities {}
perfmeter.graphics.state_collection.request {"capture_id":"shader-stutter-01","trace_frames":60}
perfmeter.graphics.state_collection.status {}
perfmeter.session.stop {}
perfmeter.graphics.state_collection.prewarm {"relative_path":"Temp/PerfMeter/GraphicsStateCollections/.sgg-perfmeter-graphics-...graphicsstate"}
```

E ammesso un solo graphics-state flight. Un ID attivo ripetuto restituisce `AlreadyActive`; un altro trace/prewarm in overlap restituisce `RejectedOverlap`. Cancel corrisponde solo all'ID attivo/in preparazione. Il backend Unity indica `supports_cache_miss_tracing: false`: l'evidence di cache-miss non e supportata e lo schema MCP del prewarm non espone questo input. Gli artefatti appartengono a PerfMeter, si trovano sotto `Temp/PerfMeter/GraphicsStateCollections` e sono limitati a 64 MiB.
