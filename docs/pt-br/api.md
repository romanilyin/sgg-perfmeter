# API Runtime

Namespace:

```csharp
using SGG.PerfMeter;
```

Todas as APIs de leitura sao seguras antes do runtime iniciar. Leituras retornam snapshots parados/padrao em vez de lancar excecoes porque o runtime nao esta ativo.

## Ciclo De Vida

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Modos de coleta:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Status E Metrics

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Principais grupos de metricas:

- FPS: media, 1% low, 0.1% low, contagens de spikes.
- Timing: CPU frame, CPU main thread, CPU render thread, present wait, GPU frame quando disponivel.
- Rendering: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads.
- Memory: system/app memory, GC reserved memory, GPU memory quando disponivel.
- Bottleneck: GPU, CPU main, CPU render, present-limited, balanced ou unknown.
- Overdraw: estado, progresso, ratio e visibilidade de heatmap.

A disponibilidade de counters e exposta por `AvailableCounters`, `UnavailableCounters` e avisos.

## Self-Observability E Budgets De Overhead

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

A self-observability publica medicoes low-overhead do custo dos callbacks CPU em janelas fixas de 120 frames. As medias sao por invocacao. O estado geral e `NotInitialized`, `Collecting` ou `Ready`; o estado de componente e `NotMeasured`, `Collecting`, `Ready` ou `Unsupported`.

Os componentes sao `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` e `HdrpRenderIntegration`. Cada um expoe contagens de frames/invocacoes, milissegundos CPU medios/maximos, bytes alocados totais/medios, budgets e estados `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Componente | Budget CPU | Budget de alocacao |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

O self-timing de GPU e explicitamente `Unavailable`. Esses diagnostics nao subtraem nem ajustam as metricas CPU/GPU existentes.

## Catalogo Dinamico De Metricas Do Profiler

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` e `GetProfilerMetricCapabilities()` leem o catalogo em cache. O estado do catalogo e `NotInitialized`, `Ready` ou `Error`; cada capability informa `Unavailable`, `AvailableNoSample` ou `AvailableSampled`, e `Resolution` indica a proveniencia `None`, `Exact` ou `Alias`. A discovery ocorre somente no inicio do runtime e em refresh/reconfigure explicitos, nao durante a coleta steady-state. Os valores numericos existentes continuam sendo valores de compatibilidade; use `SampleState`/`IsAvailable` da capability como sinal autoritativo de disponibilidade.

## Snapshots Estruturados

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
```

Snapshots de device incluem informacoes de Unity/plataforma/SO/CPU/GPU/API/display/janela/suporte. Snapshots de camera incluem cena, transform, projection, clipping, pixel rect, target display e configuracoes URP/HDRP da camera quando disponiveis.

## Cargas Dos Nucleos De CPU

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Cada snapshot expoe `CoreIndex`, `LoadPercent` e `Available`. O array pode estar vazio antes da inicializacao runtime, durante o warm-up do sampler ou em plataformas nao suportadas; trate isso como informacao de capacidade da plataforma, nao como falha de chamada da API.

## Overlay

```csharp
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.ClassicDark);
PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.Manrope);
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Modos legados de overlay e flags semanticas de modulo continuam disponiveis para compatibilidade e filtragem.

## Sessions

```csharp
PerformanceMeter.StartSession();
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));
PerformanceMeter.StopSession();
PerformanceMeter.ResetStats();

PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerfMeterSessionSampleSnapshot[] samples = PerformanceMeter.GetSessionSamples();

PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Opcoes de sessao incluem warm-up em frames/segundos, intervalo de amostra, maximo de amostras, reset-on-scene-load e janelas para ignorar carregamento de cena.

## Alerts

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

`StructuredLogsEnabled` e `true` por padrao e controla apenas a saida `Debug.Log` de alertas estruturados. O valor `false` nao desativa callbacks `AlertFired`, alertas recentes ou historico de alertas, avisos do overlay, logs de aviso do Editor nem sessoes. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` controla os logs de aviso do Editor de forma independente.

## Editor Compatibility Status

A API Editor `PerfMeterSetupActions.GetCompatibilityStatus()` retorna `PerfMeterCompatibilityStatus` e separa `ImportCompatible` para o floor Unity `2022.3`, `CoreRuntimeCompatible` para o runtime suportado Unity `6000.4+` e `RenderIntegrationCompatible` para URP/HDRP ativo `17.4+` com adapter disponivel. Cada resultado inclui uma reason. Compatibilidade de render nao significa que renderer assets estejam configurados; use setup status para configuration readiness.

## External GPU Capture Coordinator

```csharp
PerfMeterCaptureOptions options = new PerfMeterCaptureOptions(
    "renderdoc-spike-01",
    PerfMeterCaptureTool.RenderDoc,
    captureFrames: 1,
    preRollFrames: 30,
    postRollFrames: 30);

PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(options);
PerfMeterCaptureStatusSnapshot capture = PerformanceMeter.GetCaptureStatus();
if (capture.IsActive && userRequestedCancellation)
{
    PerformanceMeter.CancelCapture(capture.CaptureId);
}
```

O coordinator permite uma unica solicitacao ativa e avanca deterministicamente por `PreRoll`, `Capturing`, `PostRoll` e `Completed`. Repetir o mesmo ID ativo e idempotente; um ID ativo diferente e rejeitado por sobreposicao. `Canceled`, `Unavailable` e `Error` sao estados terminais explicitos.

O backend integrado envolve o `ExternalGPUProfiler` experimental da Unity somente no Editor ou em um Development Build, somente quando uma ferramenta externa esta conectada e somente para combinacoes de plataforma/API desktop suportadas. As combinacoes suportadas sao `RenderDoc` no desktop Windows/Linux com Direct3D 11, Direct3D 12 ou Vulkan e `PIX` no desktop Windows com Direct3D 12. Selecione `RenderDoc` ou `Pix` explicitamente, pois a Unity nao expoe a identidade da ferramenta conectada. `Status.Tool` e somente a ferramenta solicitada, nao a identidade verificada da ferramenta conectada. `Completed` confirma somente o wrapper lifecycle da Unity; nao verifica nem retorna um artefato externo `.rdc`/`.wpix` ou seu path. Os testes automatizados usam um fake backend; a confirmacao da ferramenta externa real e do artefato continua sendo um release gate.

Os valores padrao de `PerfMeterCaptureOptions` sao `captureFrames: 1`, `preRollFrames: 0` e `postRollFrames: 0`. Um `RequestCapture` valido inicia o runtime automaticamente. `CancelCapture()` sem ID cancela a solicitacao ativa atualmente reportada; passar um ID protege contra cancelar uma solicitacao mais nova.

O overload com `PerfMeterCaptureBundleOptions` separa capture samples da baseline session e pode incluir screenshot opt-in. Quando `PerformanceMeter.GetCaptureBundleStatus(captureId).IsExportReady`, `PerformanceMeter.ExportCaptureBundle(captureId)` cria atomicamente um bundle versionado sob `Temp/PerfMeter/CaptureBundles` com manifest SHA-256, samples, alerts, contexto, screenshot opcional e metadata do artefato externo. Um `.rdc`/`.wpix` local ao projeto e apenas observed, nunca authoritative; traversal, reparse points e arquivos fora do projeto sao rejeitados.

## Custom Metrics

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Excecoes de providers sao reportadas como snapshots de custom metric indisponiveis e nao interrompem a coleta das metricas principais.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Diagnosticos de overdraw sao modos diagnosticos explicitos e podem adicionar trabalho de GPU. Em HDRP estas APIs reportam com seguranca unsupported state para overdraw e heatmap, sem prometer HDRP heatmap output.

## Snapshots de memoria opcionais

Snapshots de memoria sao uma integracao opcional. No Unity `6000.4+`, `com.unity.memoryprofiler` `1.1.0+` habilita o assembly separado `SGG.PerfMeter.MemoryProfiler`, que registra automaticamente o backend `MemoryProfiler`. O assembly core nao possui dependencia obrigatoria.

```csharp
PerfMeterMemorySnapshotCapabilitiesSnapshot capabilities =
    PerformanceMeter.GetMemorySnapshotCapabilities();

if (capabilities.Availability == PerfMeterAvailability.Available)
{
    PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
        new PerfMeterMemorySnapshotOptions("memory-spike-01"));
}

PerfMeterMemorySnapshotStatusSnapshot status = PerformanceMeter.GetMemorySnapshotStatus();
if (status.State == PerfMeterMemorySnapshotState.Completed &&
    PerformanceMeter.GetCaptureBundleStatus(status.CaptureId).IsExportReady)
{
    PerformanceMeter.ExportCaptureBundle(status.CaptureId);
}
```

A superficie publica inclui `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` e `GetMemorySnapshotTriggers()`. Um backend personalizado implementa `IPerfMeterMemorySnapshotBackend`; o assembly opcional fornece o backend do Unity Memory Profiler.

`PerfMeterMemorySnapshotOptions` usa por padrao flags de objetos managed/native, 1 GiB de espaco livre minimo e cooldown de 300 segundos. `RequestMemorySnapshot` e manual por padrao e retorna resultados explicitos como `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest` ou `Failed`. Leituras nao iniciam o runtime; uma solicitacao valida inicia.

`ConfigureMemorySnapshotTriggers` habilita, por opt-in, a heuristica de limite de memoria do sistema e crescimento limitado de vazamento. `GetMemorySnapshotTriggers()` fica desabilitado por padrao. Solicitacoes disparadas usam os mesmos guards de single-flight, cooldown, espaco livre e capture flags das solicitacoes manuais.
