# Workflow

## Overlay Runtime

Usa l'overlay quando ti serve visibilita immediata dentro il gioco.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

L'overlay usa UI Toolkit e non intercetta l'input di gameplay. Supporta FPS-only, testo compatto, grafici, diagnostica completa, barre metriche, temi visivi, filtri modulo, grafici CPU/GPU, widget dei core CPU e righe limitate di metriche personalizzate.

## Raccolta In Background

Usa la modalita background per test, esecuzioni su dispositivo o workflow agent in cui non serve UI visibile.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Registrazione Ed Esportazione Sessioni

Usa le sessioni per finestre di profiling ripetibili.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Le esportazioni sessione includono timing, FPS lows, spikes, conteggi dei colli di bottiglia, contatori render, contatori memoria, stato overdraw, disponibilita di warning/counter, riepiloghi scena, frame peggiori, metadati dispositivo, metadati camera, metadati impostazioni e custom metrics.

## Alert

Le regole possono segnalare violazioni del budget, FPS bassi, GPU timing non disponibile e soglie overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Gli avvisi Editor sono limitati da cooldown e possono essere disabilitati tramite impostazioni JSON o controlli runtime.

## Diagnostica Overdraw

Il numerical overdraw e opt-in e limitato nel tempo.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

La misurazione overdraw richiede `PerfMeterRenderGraphFeature`, supporto replacement shader, supporto fragment UAV/storage-buffer, supporto compute shader, una graphics API supportata e async GPU readback. I target non supportati restituiscono `OverdrawState.Unsupported` invece di eseguire il pass.

## Riproducibilita Di Camera E Device

Usa gli snapshot per conservare l'ambiente che ha prodotto una cattura prestazionale.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Le esportazioni sessione includono metadati di device e camera, cosi una cattura puo essere compresa o riprodotta in seguito.

## Custom Metrics

Registra provider specifici del progetto senza fare fork di PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Le custom metrics sono esposte tramite letture API, esportazione sessione JSON, MCP latest metrics e fino a otto righe overlay quando il modulo `CustomMetrics` e abilitato.

## Automazione Agent

Una tipica esecuzione guidata da MCP:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
