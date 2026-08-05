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

O PerfMeter cria e possui um host versionado de UI Toolkit para o overlay: Unity `6000.4` usa `UIDocument`, enquanto Unity `6000.5+` usa `PanelRenderer`. O host proprio e separado da UI estrangeira e preserva seus panel settings e children; os rebuilds removem somente o container pertencente ao PerfMeter.

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

Avisos do Editor sao limitados por cooldowns e podem ser desativados por configuracoes JSON ou controles runtime. Logs de alertas estruturados e avisos do Editor sao independentes: `PerformanceMeter.SetStructuredLogsEnabled(false)` suprime apenas a saida `Debug.Log` de alertas estruturados, enquanto `PerformanceMeter.SetEditorWarningLogsEnabled(false)` controla separadamente os logs de aviso do Editor. Callbacks, alerts/history, avisos do overlay e sessoes continuam ativos.

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

## Instrumentacao Do Unity Profiler

A instrumentacao e interna e visivel somente ao criar perfil do Editor, de um Development Build ou de outro build com Profiler habilitado. Em Release players sem Profiler, esses markers/counters sao no-op e nao geram dados de instrumentacao; os schemas de public API, status, MCP e export permanecem inalterados.

- Os markers cobrem collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`) e export JSON/CSV (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` e um hook interno reservado para providers.
- Os counters cobrem tempos de frame CPU/GPU (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) como gauges de fim de frame em nanossegundos. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` e `SGG.PerfMeter.Thermal.Available` codificam availability/active como `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState` e `SGG.PerfMeter.Capture.OverdrawState` usam enum codes; `SGG.PerfMeter.Provider.CustomMetricCount` e um count. Todos os counters usam a categoria `Scripts` e `FlushOnEndOfFrame`.
- Nenhum thermal sample sintetico e emitido; `SGG.PerfMeter.Thermal.Available` permanece em `0`/indisponivel ate que um provider de plataforma real forneca dados.

## Self-Observability E Budgets De Overhead

Use `PerformanceMeter.GetSelfOverhead()` ou `PerformanceMeter.GetStatus().SelfOverhead` para diagnosticar custo de callbacks CPU e alocacoes de collector, custom providers, CPU-core provider, overlay e integracao URP/HDRP. A medicao usa janelas fixas de 120 frames, medias por invocacao e budgets CPU/alocacao especificos por componente.

A render integration inativa reporta `Unsupported`, um componente suportado sem chamadas reporta `NotMeasured` e o self-timing GPU reporta `Unavailable`. O accounting e apenas diagnostico: PerfMeter nao subtrai overhead nem ajusta as metricas CPU/GPU existentes.

## Automacao De Agents

Uma execucao tipica dirigida por MCP:

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

`perfmeter.profiler.capabilities {}` e uma leitura em cache; nao inicia o runtime nem executa discovery.
