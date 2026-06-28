# Workflows

## Overlay Runtime

Use o overlay quando precisar de visibilidade imediata dentro do jogo.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

O overlay usa UI Toolkit e nao intercepta input de gameplay. Ele suporta FPS-only, texto compacto, grafico, diagnosticos completos, barras de metrica, visual themes, filtros de modulo, graficos de CPU/GPU, widgets de nucleos de CPU e um conjunto limitado de linhas de custom metrics.

## Coleta Em Segundo Plano

Use o modo em segundo plano para testes, execucoes em dispositivos ou workflows de agents quando UI visivel nao e necessaria.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Gravacao E Exportacao De Sessao

Use sessoes para janelas de profiling reproduziveis.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

As exportacoes de sessao incluem timing, FPS lows, spikes, contagens de gargalos, render counters, memory counters, estado de overdraw, disponibilidade de avisos/counters, resumos de cena, piores frames, metadados de dispositivo, metadados de camera, metadados de configuracoes e custom metrics.

## Alerts

Regras podem reportar violacoes de budget, FPS baixo, GPU timing indisponivel e limites de overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Avisos do Editor sao limitados por cooldowns e podem ser desativados por configuracoes JSON ou controles runtime.

## Diagnosticos De Overdraw

Overdraw numerico e opt-in e limitado.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Overdraw numerico e heatmap usam o diagnostic path de URP Render Graph. A medicao de overdraw requer `PerfMeterRenderGraphFeature`, suporte a replacement shader, suporte a UAV/storage-buffer em fragment, suporte a compute shader, uma graphics API suportada e async GPU readback. HDRP reporta overdraw/heatmap como unsupported, enquanto core overlay, session, API e MCP diagnostics continuam disponiveis. Alvos nao suportados reportam `OverdrawState.Unsupported` em vez de executar o pass.

## Reprodutibilidade De Camera E Device

Use snapshots para preservar o ambiente que produziu uma captura de performance.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

As exportacoes de sessao incluem metadados de device e camera para que uma captura possa ser entendida ou reproduzida depois.

## Custom Metrics

Registre providers especificos do projeto sem fazer fork do PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics sao expostas por leituras de API, exportacao JSON de sessao, MCP latest metrics e ate oito linhas de overlay quando o modulo `CustomMetrics` esta ativado.

## Automacao De Agents

Uma execucao tipica dirigida por MCP:

```text
perfmeter.runtime.mode.set {"mode":"Background"}
perfmeter.session.start {"warmup_seconds":1,"sample_interval_seconds":0.25,"max_samples":240}
perfmeter.runtime.mode.set {"mode":"Overlay"}
perfmeter.overlay.set {"preset":"Timing","mode":"Graphs","visible":true}
perfmeter.session.summary {}
perfmeter.session.export {"format":"json","path":"Temp/PerfMeter/session.json"}
perfmeter.alerts.latest {}
```
