# Runtime API

Пространство имен:

```csharp
using SGG.PerfMeter;
```

Все API чтения безопасны до запуска PerfMeter. Чтение возвращает снимки остановленного состояния или значения по умолчанию, а не исключение из-за неактивного состояния во время выполнения.

## Жизненный цикл

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.Stop();
	PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
```

Для операций, где нельзя молча скрывать rejection, normalization или unsupported state, доступны additive mutation methods с результатом:

```csharp
PerfMeterMutationResultSnapshot modeResult = PerformanceMeter.TrySetCollectionMode(PerfMeterCollectionMode.Background);
PerfMeterMutationResultSnapshot sessionResult = PerformanceMeter.TryStartSession(PerfMeterSessionOptions.Default);
PerfMeterMutationResultSnapshot overdrawResult = PerformanceMeter.TryRequestOverdrawMeasurement(60);
```

`Status` принимает значения `Applied`, `NoChange`, `Normalized`, `Rejected`, `Unavailable` или `Unsupported`; `Reason`, `RequestedValue` и `EffectiveValue` сохраняют machine-readable outcome. Существующие `void` lifecycle/session/overdraw methods остаются compatibility wrappers. Для полной конфигурации overlay доступен `TryApplyOverlayConfiguration(...)` с тем же контрактом.

Режимы сбора:

- `Stopped`
- `Background`
- `Overlay`
- `OverdrawDiagnostic`

## Статус и метрики

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDiagnosticsSnapshot diagnostics = PerformanceMeter.GetDiagnostics();

if (PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot safeStatus))
{
    UnityEngine.Debug.Log($"PerfMeter state: {safeStatus.State}");
}
```

Основные группы метрик:

- FPS: средний FPS, 1% low, 0.1% low и счетчики spikes.
- Тайминги: CPU frame, CPU main thread, CPU render thread, present wait и GPU frame, когда доступно.
- Рендеринг: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD и uploads.
- Память: system/app memory, GC reserved memory и GPU memory, когда доступно.
- Узкое место: GPU, CPU main, CPU render, present-limited, balanced или unknown.
- Overdraw: state, progress, ratio и heatmap visibility.

Доступность счетчиков видна через `AvailableCounters`, `UnavailableCounters` и warnings.

`metrics.Bottleneck` остается instantaneous-классификацией, а raw timings не меняются. `diagnostics.StableBottleneck` — отдельный hysteresis-based результат с `Availability`, `Freshness`, `Provenance`, `Confidence`, `Coverage`, typed `Flags`, verification steps, количеством/возрастом evidence и неизмененным последним warning коллектора. При недостаточном, осциллирующем или stale evidence публикуется `Unknown`.

## Self-observability и бюджеты overhead

```csharp
PerfMeterSelfOverheadSnapshot overhead = PerformanceMeter.GetSelfOverhead();
PerfMeterSelfOverheadSnapshot statusOverhead = PerformanceMeter.GetStatus().SelfOverhead;
```

Self-observability публикует low-overhead измерения стоимости CPU callbacks в фиксированных окнах по 120 кадров. Средние значения считаются на один вызов. Общее состояние: `NotInitialized`, `Collecting` или `Ready`; состояние компонента: `NotMeasured`, `Collecting`, `Ready` или `Unsupported`.

Компоненты: `Collector`, `CustomMetricProviders`, `CpuCoreProvider`, `Overlay`, `UrpRenderIntegration` и `HdrpRenderIntegration`. Для каждого доступны число кадров и вызовов, среднее/максимальное CPU-время, общий/средний объем allocations, заданные бюджеты и состояния `NotEvaluated`/`WithinBudget`/`Exceeded`.

| Компонент | CPU budget | Allocation budget |
| --- | ---: | ---: |
| Collector | 0.5 ms | 0 B |
| Custom metric providers | 0.5 ms | 4096 B |
| CPU core provider | 1.0 ms | 0 B |
| Overlay | 2.0 ms | 131072 B |
| URP/HDRP render integration | 0.5 ms | 0 B |

GPU self-timing явно имеет состояние `Unavailable`. Диагностика не вычитает overhead и не корректирует существующие CPU/GPU-метрики.

## Динамический каталог Profiler-метрик

```csharp
PerfMeterProfilerMetricCatalogSnapshot catalog = PerformanceMeter.GetProfilerMetricCatalog();
PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = PerformanceMeter.GetProfilerMetricCapabilities();
bool refreshed = PerformanceMeter.TryRefreshProfilerMetricCatalog();
```

`GetProfilerMetricCatalog()` и `GetProfilerMetricCapabilities()` читают кэшированный каталог. Состояние каталога — `NotInitialized`, `Ready` или `Error`; каждая capability сообщает `Unavailable`, `AvailableNoSample` или `AvailableSampled`, а `Resolution` указывает provenance `None`, `Exact` или `Alias`. Discovery выполняется только при старте runtime и в явных путях refresh/reconfigure, но не при steady-state collection. Существующие numeric metrics сохраняют compatibility values; authoritative availability определяется по `SampleState`/`IsAvailable` capability.

## Структурированные снимки

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
PerfMeterRenderGraphSnapshot renderGraph = PerformanceMeter.GetRenderGraphSnapshot();
PerfMeterSettingsSnapshot settings = PerformanceMeter.GetSettings();
PerfMeterPlatformTelemetrySnapshot platformTelemetry = PerformanceMeter.GetPlatformTelemetry();
```

Снимки устройства содержат информацию о Unity, платформе, OS, CPU, GPU, API, дисплее, окне и поддержке возможностей. Снимки камеры содержат scene, transform, projection, clipping, pixel rect, target display и URP/HDRP camera settings, когда доступно.

Platform telemetry использует core-owned ограниченный интервал 0.25 секунды вместо вызова optional provider на каждом кадре. Snapshot сообщает `LastAttemptTimeSeconds`, `LastSuccessTimeSeconds`, `SampleAgeSeconds`, `Freshness`, `LastAttemptResult` и факт принудительного вызова на capture boundary. Неуспешный forced attempt остается явно `Unavailable`, а не заменяется старым available sample.

## Загрузка CPU-ядер

```csharp
PerfMeterCpuCoreLoadSnapshot[] cores = PerformanceMeter.GetCpuCoreLoads();
```

Каждый снимок содержит `CoreIndex`, `LoadPercent` и `Available`. Массив может быть пустым до запуска PerfMeter, во время прогрева sampler или на неподдерживаемых платформах; воспринимайте это как информацию о возможностях платформы, а не как ошибку API.

## Оверлей

```csharp
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.ClassicDark);
PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.Manrope);
PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.FullDiagnostics);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Устаревшие режимы оверлея и семантические флаги модулей остаются доступными для совместимости и фильтрации.

## Сессии

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

Опции сессии включают кадры/секунды warm-up, интервал сэмплов, максимальное количество сэмплов, reset-on-scene-load и окна игнорирования после загрузки сцены.

## Alerts/оповещения

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] alerts = PerformanceMeter.GetLatestAlerts();
PerformanceMeter.ClearAlerts();
bool structuredLogs = PerformanceMeter.StructuredLogsEnabled;
PerformanceMeter.SetStructuredLogsEnabled(false);
PerformanceMeter.SetEditorWarningLogsEnabled(false);
```

Свойство `StructuredLogsEnabled` по умолчанию равно `true` и управляет только структурированным alert `Debug.Log`. Значение `false` не отключает callback `AlertFired`, последние alerts и историю оповещений, предупреждения оверлея, логи предупреждений Editor или сессии. `PerformanceMeter.SetEditorWarningLogsEnabled(bool)` независимо управляет логами предупреждений Editor.

## Editor Compatibility Status

Editor API `PerfMeterSetupActions.GetCompatibilityStatus()` возвращает `PerfMeterCompatibilityStatus` и отдельно сообщает `ImportCompatible` для package floor Unity `2022.3`, `CoreRuntimeCompatible` для supported runtime Unity `6000.4+` и `RenderIntegrationCompatible` для active URP/HDRP `17.4+` с доступным adapter. Каждый результат содержит reason. Render compatibility не означает, что renderer assets уже настроены; для configuration readiness используйте setup status.

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

Coordinator допускает только один активный запрос и детерминированно проходит состояния `PreRoll`, `Capturing`, `PostRoll` и `Completed`. Повтор того же активного ID идемпотентен; другой активный ID отклоняется как пересечение. `Canceled`, `Unavailable` и `Error` — явные конечные состояния.

`PerfMeterCaptureBackendMode.GenericUnity` остается compatibility default для `ExternalGPUProfiler`; identity tool и artifact не аутентифицируется. `NativePreferred` запрашивает опциональный Windows x64 Editor bridge и может fallback только до native begin; `NativeRequired` никогда не fallback. Native-путь поддерживает D3D11, D3D12 и Vulkan. Status сообщает `RequestedBackendMode`, `EffectiveBackendKind`, `NativePhase`, result code и fallback reason.

Значения по умолчанию `PerfMeterCaptureOptions`: `captureFrames: 1`, `preRollFrames: 0` и `postRollFrames: 0`. Валидный `RequestCapture` автоматически запускает runtime. `CancelCapture()` без ID отменяет текущий отображаемый активный запрос; передача ID защищает от отмены более нового запроса.

Generic и caller-supplied `.rdc`/`.wpix` остаются observed. Только generation-bound native descriptor может аутентифицировать finalized `.rdc`. Native MetadataOnly использует `DoNotShare`; Copy/Embed имеют отдельные квоты и `ReviewBeforeShare`. Traversal, reparse points и файлы вне owned roots отклоняются.

Экспорт capture bundle также предоставляет неблокирующий single-flight API: `RequestCaptureBundleExport(..., out exportId)`, `GetCaptureBundleExportStatus(exportId)` и `CancelCaptureBundleExport(exportId)`. Статус содержит фазу, прогресс, размер в байтах, сведения об отмене и повторной попытке, путь commit и универсальный envelope внешнего артефакта. Существующий API `ExportCaptureBundle(...)` остается блокирующим compatibility wrapper, а сериализация, файловый I/O, хеширование, retention и атомарный commit выполняются в worker thread.

Session и capture JSON дополняются типизированными timeline events для отсутствующих samples и границ capture. Существующие версии схем, массивы samples и столбцы CSV остаются совместимыми; legacy или неизвестные timeline payloads считываются без создания несуществующих gaps. Custom metric providers используют кэшированный snapshot providers и переиспользуемый buffer, принадлежащий core, на прогретом collection path; копии создаются только для сохраняемых samples, экспортов и публичных snapshots. Координация Profiler является process-local через `GetProfilerLeaseCapabilities()`, `GetProfilerLeaseStatus()`, `TryAcquireProfilerLease(...)` и `ReleaseProfilerLease(...)`; удерживаемые leases не переживают domain reload.

## Пользовательские метрики

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
PerformanceMeter.UnregisterCustomMetricProvider(provider);
PerformanceMeter.ClearCustomMetricProviders();
```

Исключения провайдеров превращаются в недоступные снимки пользовательских метрик и не прерывают основной сбор метрик.

## Overdraw

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.CancelOverdrawMeasurement();
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Диагностика overdraw использует явные диагностические режимы, которые могут добавлять работу GPU. В HDRP эти API безопасно возвращают unsupported state для overdraw и heatmap, не обещая HDRP heatmap output.

## Опциональные снимки памяти

Снимки памяти — это опциональная интеграция. В Unity `6000.4+` установка `com.unity.memoryprofiler` `1.1.0+` включает отдельную сборку `SGG.PerfMeter.MemoryProfiler`, которая автоматически регистрирует backend `MemoryProfiler`. У core assembly нет hard dependency.

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

Публичная поверхность: `RegisterMemorySnapshotBackend(...)`, `UnregisterMemorySnapshotBackend(...)`, `GetMemorySnapshotCapabilities()`, `GetMemorySnapshotStatus()`, `RequestMemorySnapshot(PerfMeterMemorySnapshotOptions)`, `ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions)` и `GetMemorySnapshotTriggers()`. Пользовательский backend реализует `IPerfMeterMemorySnapshotBackend`; опциональная сборка поставляет Unity Memory Profiler backend.

`PerfMeterMemorySnapshotOptions` по умолчанию выбирает managed/native object flags, требует 1 GiB свободного места и задаёт cooldown 300 секунд. `RequestMemorySnapshot` по умолчанию выполняет ручной захват и возвращает явный результат: `Started`, `AlreadyActive`, `RejectedOverlap`, `Cooldown`, `Unavailable`, `InsufficientDiskSpace`, `InvalidRequest` или `Failed`. Read-методы не запускают runtime, а корректный запрос запускает его.

`ConfigureMemorySnapshotTriggers` включает opt-in эвристику порога system memory и ограниченного роста утечки. `GetMemorySnapshotTriggers()` по умолчанию возвращает disabled. Для trigger-запросов действуют те же single-flight, cooldown, free-space и capture-flag guards, что и для ручных запросов.

## Диагностика графики и GraphicsStateCollection

Графическая диагностика добавляет данные к существующим snapshot-ам. `PerformanceMeter.GetGraphicsDiagnostics()` возвращает последние значения маркеров создания shader GPU program и graphics pipeline, контекст graphics API, возможность parallel PSO и revision каталога profiler-метрик.

```csharp
PerfMeterGraphicsDiagnosticsSnapshot graphics = PerformanceMeter.GetGraphicsDiagnostics();
PerfMeterProfilerMetricCapabilitySnapshot shader = graphics.ShaderGpuProgramCreationCapability;
PerfMeterProfilerMetricCapabilitySnapshot pipeline = graphics.GraphicsPipelineCreationCapability;

UnityEngine.Debug.Log($"Shader marker: {graphics.ShaderGpuProgramCreationValue} {shader.Unit} ({shader.SampleState})");
UnityEngine.Debug.Log($"Pipeline marker: {graphics.GraphicsPipelineCreationValue} {pipeline.Unit} ({pipeline.SampleState})");
```

Каталог обнаруживает дескрипторы Unity `ProfilerRecorder` при запуске runtime и при явном refresh/reconfigure. Для shader используется exact name `Shader.CreateGPUProgram` и aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram`, `Shader.DynamicLoadGPUProgram`. Для graphics pipeline используется exact name `CreatePSO.Job`. Каждый capability сохраняет `Resolution` (`None`, `Exact` или `Alias`), `ResolvedRecorderNames`, `Category`, обнаруженные `Unit` и `DataType`, `ResolvedComponentCount` и `SampledComponentCount`. `PerfMeterMetricsSnapshot` и session JSON/CSV содержат те же значения маркеров, metadata capability и catalog revision.

Доступность маркеров динамическая. Используйте `SampleState` (`Unavailable`, `AvailableNoSample` или `AvailableSampled`) и metadata capability; нулевое значение не доказывает отсутствие маркера. Значения являются raw-значениями recorder и сохраняют обнаруженную единицу: это не универсальные counts shader или PSO, и PerfMeter не приводит их к общей единице измерения.

Опциональная сборка `SGG.PerfMeter.GraphicsStateCollection` ограничена Unity `6000.4+` и регистрирует Unity backend, если он доступен. В Unity `6000.4` используется `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, а в Unity `6000.5+` — `UnityEngine.Rendering.GraphicsStateCollection`. Core assembly не зависит от этого backend.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.25f, 240));

PerfMeterGraphicsStateCollectionRequestResult request =
    PerformanceMeter.RequestGraphicsStateTrace(
        new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", traceFrames: 60));

PerfMeterGraphicsStateCollectionStatusSnapshot status =
    PerformanceMeter.GetGraphicsStateCollectionStatus();
if (status.State == PerfMeterGraphicsStateCollectionState.Completed)
{
    PerformanceMeter.PrewarmGraphicsStateCollection(
        new PerfMeterGraphicsStatePrewarmOptions(status.ArtifactRelativePath));
}
```

Публичная поверхность state collection: `RegisterGraphicsStateCollectionBackend(...)`, `UnregisterGraphicsStateCollectionBackend(...)`, `GetGraphicsStateCollectionCapabilities()`, `GetGraphicsStateCollectionStatus()`, `RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions)`, `PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions)` и `CancelGraphicsStateTrace(string captureId)`. Пользовательский backend реализует `IPerfMeterGraphicsStateCollectionBackend` и сообщает о trace/prewarm, cache-miss и parallel-PSO capabilities.

`PerfMeterGraphicsStateTraceOptions` требует непустой `CaptureId`, принимает 1–600 trace frames и по умолчанию использует 60 frames и 1 GiB minimum free disk. Trace допустим только при активной записи PerfMeter session. Связанные session samples получают active capture ID в `GraphicsStateTraceId` (`graphics_state_trace_id` в exports). Настройки sampling session управляют плотностью связанных samples, но не меняют число trace frames.

`PerfMeterGraphicsStateCollectionStatusSnapshot` предоставляет `IsBusy` и `HasPendingCleanup`. `IsBusy` остаётся true во время подготовки, trace, завершения trace, prewarm, cleanup или persisted pending cleanup; `HasPendingCleanup` отдельно указывает на owned artifact, ожидающий retry cleanup. Если вызвать `PerformanceMeter.StopSession()` во время активного trace, он отменяет trace, поэтому session должна оставаться recording до завершения trace. После неудачного удаления owned artifact создаётся соседний owned sidecar `.delete-pending`; после domain reload marker восстанавливается и cleanup повторяется. Status остаётся видимым и busy, пока artifact и marker не очищены.

Coordinator разрешает только один graphics-state flight. Повторный active ID возвращает `AlreadyActive`; другой trace или prewarm во время подготовки, trace, завершения, cleanup или другой capture-domain возвращает `RejectedOverlap`. `CancelGraphicsStateTrace` действует только для совпадающего active/preparing ID, отменяет backend и удаляет pending owned artifact. Ошибки cleanup остаются видимыми и могут блокировать замену до повторной очистки.

`PerfMeterGraphicsStatePrewarmOptions` принимает только owned project-relative `.graphicsstate` path и необязательный `MaxStateCount` от 0 до 1 000 000. Prewarm выполняется синхронно, сохраняет artifact и сообщает `CompletedWarmupCount` и `IsWarmedUp`; успешный, но неполный progressive warmup сопровождается warning. `TraceCacheMisses` оставлен для расширяемых backend-ов, но Unity backend не поддерживает cache-miss evidence, поэтому запрос с ним возвращает `Unavailable`.

## Контекст интеграции рендеринга

Integration-neutral snapshot доступен через оба метода:

```csharp
PerfMeterRenderIntegrationSnapshot renderIntegration =
    PerformanceMeter.GetRenderIntegrationSnapshot();

if (PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot safeRenderIntegration))
{
    UnityEngine.Debug.Log($"{safeRenderIntegration.RenderPipeline.Kind}: {safeRenderIntegration.State}");
}
```

`PerfMeterRenderIntegrationSnapshot` содержит `RenderPipeline`, `RenderPipelineAssetSource`, `LastObservedFrame`, `ObservationAgeFrames`, `ObservationMatchesCurrentPipeline`, `ObservedCameraEntityId`, `ObservedCameraName`, `ObservedCameraType`, `IntegrationId`, `IntegrationName`, `IntegrationVersion`, `PassKind`, `PassName`, `InjectionPoint`, `PerfMeterPassCount`, `EffectiveRenderingMode`, `GpuResidentDrawer`, `VariableRateShading`, `LegacyRenderGraph` и `Warning`. Вложенные snapshots GRD и VRS сообщают availability, поля configuration/support, activity availability и warnings.

Read безопасен до запуска runtime и не запускает сбор. Поддерживаемый current pipeline может иметь `Available` при `State = NotObserved`; если последняя observation относится к другой конфигурации pipeline, `ObservationMatchesCurrentPipeline` равно `false`, frame/age остаются явными, а warning указывает на stale data. Не принимайте stale fields за текущую observation.

URP сообщает public current-frame `UniversalRenderingData.renderingMode` и фактически scheduled PerfMeter passes. HDRP сообщает наблюдаемый PerfMeter `CustomPass`, но effective rendering mode недоступен. `GpuResidentDrawer` сообщает configured mode, SRP/project/compute support, Forward+ и clustered-mode compatibility текущего URP frame, а также global runtime activity из `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()`. Для HDRP поля Forward+/rendering mode остаются `Unknown`. `VariableRateShading` сообщает authoritative hardware support из `SystemInfo`/`ShadingRateInfo`.

`LegacyRenderGraph` — встроенный compatibility facade для `GetRenderGraphSnapshot()`. Private/internal reflection pass/resource удалён, поэтому legacy counters остаются `-1`. Стабильный public Unity API также не предоставляет RenderGraph/CustomPass viewer или pass targets; Editor navigation этот API не обещает.

`GpuResidentDrawer` дополнительно содержит `ProjectConfigurationAvailability`, `IsProjectConfigurationSupported`, `ComputeShaderAvailability`, `SupportsComputeShaders`, `ForwardPlusActivityAvailability`, `IsObservedForwardPlusActive`, `RenderingModeCompatibilityAvailability`, `IsRenderingModeCompatible`, `ActivitySource`, `DegradedReason` и `Effectiveness`. `PerfMeterGpuResidentDrawerReason` задаёт structured fallback states. `PerfMeterGpuResidentDrawerEffectivenessSnapshot` хранит BRG draw calls/instances и provenance profiler capabilities; значения без sample равны `-1` в C# и `null` в JSON. Это aggregate BatchRendererGroup counters, а не authoritative evidence участия GRD для каждого renderer.

## Корреляция сессии

`PerformanceMeter.GetSessionSummary().SessionId` возвращает 32-символьный шестнадцатеричный идентификатор в нижнем регистре. Он создаётся при `StartSession`, остаётся стабильным после `StopSession`, меняется при запуске новой сессии и пуст, когда сессии нет. Session JSON публикует то же значение в корневом поле `session_id`; CSV добавляет его последним столбцом `session_id`, сохраняя позиции существующих столбцов; `perfmeter.session.summary` возвращает поле `session_id`.
