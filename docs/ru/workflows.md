# Сценарии работы

## Настройка FTUE и продолжение сценариев

Откройте `SGG/Perfmeter/Setup` и выберите вкладку **FTUE**. Обязательные проверки охватывают совместимость, render integration, Frame Timing Stats, путь package и загруженный settings JSON. Необязательные строки можно установить или пропустить; установленная строка показывает следующее действие, а не молча заявляет о завершении workflow.

### Memory Profiler

После установки `com.unity.memoryprofiler` строка **Memory Profiler** предоставляет **Open Window/Analysis/Memory Profiler**, **Copy RequestMemorySnapshot Snippet**, **Copy Memory Trigger Snippet**, **Open Runtime** и **Reveal Snapshots**, когда существует принадлежащая PerfMeter папка. Скопированные snippets являются runtime-кодом, который должен вызвать проект; FTUE сам не запрашивает snapshot и не настраивает triggers. Одноразовые `.snap`-файлы размещаются в `Temp/PerfMeter/MemorySnapshots`; откройте или скопируйте результат до того, как последующий request или runtime cleanup удалит принадлежащий PerfMeter source.

Одноразовый snippet:

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

Opt-in snippet для trigger:

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

Используйте **Open Runtime**, чтобы проверить capability/status snapshot. Ручной capture является режимом по умолчанию; trigger thresholds остаются выключенными, пока их явно не настроить.

### Profile Analyzer

Установленная строка **Profile Analyzer** предоставляет **Open Profile Analyzer** и **Open Runtime**. Сначала начните запись в Unity Profiler, затем запустите и остановите PerfMeter session внутри этой записи. Opener использует `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()`, чтобы открыть Profile Analyzer и скопировать ID сессии; загрузите записанные данные Profiler и найдите этот ID. Он не устанавливает Profile Analyzer, не загружает Profiler data и не применяет filter автоматически.

### Adaptive Performance

Установленная строка **Adaptive Performance** предоставляет **Open Runtime** для проверки текущего status необязательного telemetry provider. Действие FTUE не запускает session и не выполняет capture.

### RenderDoc

RenderDoc — внешняя tool, она не входит в PerfMeter. Следуйте официальному integration flow Unity:

1. Установите RenderDoc с официальной страницы загрузки: <https://renderdoc.org/builds>.
2. Сохраните изменения проекта и используйте **Load RenderDoc** в меню вкладки Game View или Scene View. Также можно запустить Unity Editor или Development Build через RenderDoc; перезапустите Unity, если после установки Unity не показывает attachment. Официальное руководство Unity: <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html>.
3. Нажмите **Check Attachment** в FTUE. Это обновляет только общий signal Unity для external profiler; FTUE не умеет определять установку RenderDoc, а Unity не может отличить RenderDoc от PIX по этому signal.
4. Нажмите **Copy Capture Snippet**, войдите в Play Mode и вызовите скопированный код из project runtime code:

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions("ftue-renderdoc-capture", PerfMeterCaptureTool.RenderDoc, 1));
   ```

5. В Windows x64 Editor сначала можно использовать **Download Verified Bridge** или **Install Local Bridge**; как Editor-only plugin устанавливается только точно pinned отдельный bridge, но не RenderDoc. После этого перезапустите Editor. Скопированный native request использует `NativeRequired` + `Copy`; MetadataOnly имеет `DoNotShare`, а Copy/Embed требуют `ReviewBeforeShare`.

### GraphicsStateCollection

Встроенная необязательная строка **GraphicsStateCollection** не требует установки package. Она предоставляет **Open Runtime**, **Copy Trace Snippet**, **Copy Prewarm Snippet** и **Reveal Artifacts**. FTUE не запрашивает trace или prewarm автоматически. Используйте такую последовательность:

1. В Play Mode запустите и оставьте записывающей PerfMeter session через `PerformanceMeter.StartSession(...)`.
2. Вызовите скопированный trace code из project runtime code:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. Опросите `PerformanceMeter.GetGraphicsStateCollectionStatus()` до состояния `State == PerfMeterGraphicsStateCollectionState.Completed`. Используйте `ArtifactRelativePath`, указывающий под `Temp/PerfMeter/GraphicsStateCollections`, как input для prewarm. Остановка session во время tracing отменяет trace.
4. Замените `<trace-artifact-file>` в скопированном prewarm snippet возвращенным path:

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. После trace нажмите **Reveal Artifacts**, чтобы открыть project-local artifact folder. Prewarm выполняется синхронно, сохраняет artifact и может сообщить о неполном progressive warmup. Длина trace ограничена 600 frames, а принадлежащие артефакты — 64 MiB; Unity backend не предоставляет свидетельства cache miss.

## Полный bootstrap инициализации

В **Setup > Initialization Code** нажмите **Refresh from Project Settings**, затем **Copy Init Code**. Сгенерированный `PerfMeterBootstrap` встраивает полный нормализованный snapshot настроек проекта и после загрузки сцены вызывает `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)`. Он переносит настройки overlay, logging, alert, session-default и overdraw, учитывает `enabled` и `collectionMode: Stopped`, не выполняя `StartSession` или capture request.

Используйте этот явный bootstrap вместо Resources zero-code settings path, если нужен запуск, управляемый кодом. Если присутствуют оба варианта, успешно разобранный explicit call подавляет Resources auto-start callback для текущего domain; если Resources уже запустил первым, explicit snapshot применяется после этого и становится authoritative. Некорректный explicit JSON оставляет текущую runtime без изменений и не подавляет последующий Resources auto-start. Операции session и default overdraw используют активный explicit runtime snapshot.

## Runtime-оверлей

Используйте оверлей, когда нужна быстрая видимость прямо в игре.

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

Оверлей использует UI Toolkit и не перехватывает игровой ввод. Он поддерживает режим только FPS, компактный текст, графики, полную диагностику, полосы метрик, визуальные темы, фильтры модулей, графики CPU/GPU, виджеты ядер CPU и ограниченные строки пользовательских метрик.

PerfMeter создает и владеет версионированным host UI Toolkit для оверлея: в Unity `6000.4` используется `UIDocument`, а в Unity `6000.5+` — `PanelRenderer`. Собственный host отделен от чужого UI и сохраняет его panel settings и children; при rebuild удаляется только container, принадлежащий PerfMeter.

## Фоновый сбор

Фоновый режим подходит для тестов, прогонов на устройствах и агентских сценариев без видимого UI.

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Запись и экспорт сессий

Сессии нужны для повторяемых окон профилирования.

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Запустите измеряемый сценарий.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Экспорт сессии включает тайминги, FPS lows, spikes, счетчики узких мест, счетчики рендера, счетчики памяти, состояние overdraw, доступность предупреждений и счетчиков, сводки сцен, худшие кадры, метаданные устройства, камеры и настроек, а также пользовательские метрики.

## Alerts/оповещения

Правила могут сообщать о нарушениях бюджета кадра, низком FPS, недоступном GPU timing и превышении порогов overdraw.

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warnings ограничены паузой между срабатываниями и могут быть отключены через JSON-настройки или контролы во время выполнения. Логи структурированных alert и Editor warnings независимы: `PerformanceMeter.SetStructuredLogsEnabled(false)` подавляет только structured alert `Debug.Log`, а `PerformanceMeter.SetEditorWarningLogsEnabled(false)` отдельно управляет логами предупреждений Editor. Callback-и, alerts/history, предупреждения оверлея и сессии продолжают работать.

## External GPU Capture

Используйте capture coordinator для ограниченного запроса RenderDoc или PIX, когда tool уже подключена:

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

`GenericUnity` сохраняет прежнюю matrix `ExternalGPUProfiler` и не может аутентифицировать tool/artifact. `NativePreferred` может fallback только до begin; `NativeRequired` никогда. Native RenderDoc поддерживается только в Windows x64 Unity Editor с D3D11, D3D12 или Vulkan.

Generic `Completed` остается только wrapper lifecycle. Native status сообщает backend kind и generation-bound phase и может аутентифицировать finalized `.rdc`. Generic/caller artifacts остаются observed. MCP принимает `backend_mode`, но storage mode выбирается через C# API.

## Диагностика overdraw

Числовой overdraw включается явно и работает в ограниченном окне.

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Числовой overdraw и heatmap используют диагностический путь URP Render Graph. Измерение overdraw требует `PerfMeterRenderGraphFeature`, поддержки replacement shader, fragment UAV/storage buffer, compute shaders и async GPU readback, а также поддерживаемого graphics API. HDRP возвращает unsupported для overdraw/heatmap, при этом core overlay, session, API и MCP diagnostics остаются доступны. Неподдерживаемые цели возвращают `OverdrawState.Unsupported` вместо запуска pass.

## Воспроизводимость камеры и устройства

Снимки сохраняют окружение, в котором получен захват производительности.

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Экспорт сессии включает метаданные устройства и камеры, чтобы захват можно было понять или воспроизвести позже.

## Пользовательские метрики

Регистрируйте провайдеры проекта без форка PerfMeter.

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Пользовательские метрики доступны через API-чтение, экспорт сессии в JSON, latest metrics в MCP и до восьми строк оверлея при включенном модуле `CustomMetrics`.

## Инструментация Unity Profiler

Инструментация является внутренней и видна только при профилировании Editor, Development Build или другого profiler-enabled build. В Release player без Profiler эти markers/counters являются no-op и не создают instrumentation data; public API, status, MCP и export schemas не меняются.

- Маркеры охватывают collect/frame timing (`SGG.PerfMeter.Collect`, `SGG.PerfMeter.Collect.FrameTiming`), providers (`SGG.PerfMeter.Provider.CustomMetrics`, `SGG.PerfMeter.Provider.CpuCore`, `SGG.PerfMeter.Provider.DeviceSnapshot`, `SGG.PerfMeter.Provider.CameraSnapshot`), bottleneck/capture (`SGG.PerfMeter.Bottleneck.Classify`, `SGG.PerfMeter.Capture.Session`, `SGG.PerfMeter.Capture.AlertScope`, `SGG.PerfMeter.Capture.Coordinator`) и JSON/CSV export (`SGG.PerfMeter.Export.Json`, `SGG.PerfMeter.Export.Csv`). `SGG.PerfMeter.Thermal.Sample` — зарезервированный internal provider hook.
- Counters охватывают CPU/GPU frame times (`SGG.PerfMeter.CPU.FrameTime`, `SGG.PerfMeter.CPU.MainThreadTime`, `SGG.PerfMeter.CPU.RenderThreadTime`, `SGG.PerfMeter.CPU.PresentWaitTime`, `SGG.PerfMeter.GPU.FrameTime`) как end-of-frame gauges в nanoseconds. `SGG.PerfMeter.CPU.FrameTimingAvailable`, `SGG.PerfMeter.GPU.FrameTimingAvailable`, `SGG.PerfMeter.Capture.AlertScopeActive` и `SGG.PerfMeter.Thermal.Available` кодируют availability/active как `0`/`1`; `SGG.PerfMeter.Bottleneck.Kind`, `SGG.PerfMeter.Capture.SessionState`, `SGG.PerfMeter.Capture.OverdrawState` и `SGG.PerfMeter.Capture.State` используют enum codes; `SGG.PerfMeter.Provider.CustomMetricCount` — count. Все counters используют category `Scripts` и `FlushOnEndOfFrame`.
- Synthetic thermal sample не создается; `SGG.PerfMeter.Thermal.Available` остается `0`/unavailable, пока реальный platform provider не начнет поставлять данные.

## Self-observability и бюджеты overhead

Используйте `PerformanceMeter.GetSelfOverhead()` или `PerformanceMeter.GetStatus().SelfOverhead` для диагностики стоимости CPU callbacks и allocations у collector, custom providers, CPU-core provider, overlay и URP/HDRP integration. Измерения используют фиксированные окна по 120 кадров, средние на один вызов и отдельные CPU/allocation budgets для компонентов.

Неактивная render integration возвращает `Unsupported`, поддерживаемый компонент без вызовов — `NotMeasured`, а GPU self-timing — `Unavailable`. Accounting носит только диагностический характер: PerfMeter не вычитает overhead и не корректирует существующие CPU/GPU-метрики.

## MCP-автоматизация

Типичный прогон через MCP:

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

`perfmeter.profiler.capabilities {}` — это чтение кэшированного состояния; команда не запускает runtime и discovery.

## Workflow опционального снимка памяти

1. Используйте Unity `6000.4+` и установите через Package Manager `com.unity.memoryprofiler` `1.1.0+`. После этого отдельная сборка `SGG.PerfMeter.MemoryProfiler` автоматически регистрирует backend; без этой зависимости core integration остаётся unavailable.
2. В Play Mode прочитайте `PerformanceMeter.GetMemorySnapshotCapabilities()` или `perfmeter.memory.snapshot.capabilities` и проверьте backend и требуемые capture flags.
3. Запросите ручной снимок через `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))` либо вызовите `ConfigureMemorySnapshotTriggers(...)` для явно включённого system-memory threshold или bounded leak-growth window.
4. Ожидайте через `GetMemorySnapshotStatus()` или `perfmeter.memory.snapshot.status` terminal state снимка и связанного bundle. Готовое evidence экспортируйте через `PerformanceMeter.ExportCaptureBundle(captureId)` или `perfmeter.capture.export`.

Memory-only evidence проходит через существующий capture-bundle API и сохраняется под `Temp/PerfMeter/CaptureBundles`. Bundle записывает `MemoryProfiler` как requested tool, memory-snapshot provenance и streaming SHA-256 для `.snap`; external GPU artifact не создаётся. Source принадлежит PerfMeter и находится под `Temp/PerfMeter/MemorySnapshots`; успешный export использует его один раз.

## Диагностика graphics markers

1. Вызовите `PerformanceMeter.GetGraphicsDiagnostics()` или `perfmeter.graphics.diagnostics`, чтобы получить последние marker values и graphics API context.
2. Проверьте `SampleState`, `Resolution`, `ResolvedRecorderNames`, `Unit`, `DataType`, resolved/sampled component counts и catalog revision каждой capability. Discovery выполняется динамически: при запуске runtime и при явном profiler-catalog refresh/reconfigure.
3. Рассматривайте значения как raw recorder values в обнаруженных units. Marker может быть unavailable, available без sample или sampled; numeric zero не является универсальным признаком unavailable, а значение не гарантированно является shader или PSO count.

Shader marker сначала разрешает exact `Shader.CreateGPUProgram`, затем aliases `Shader.CreateGPUPrograms`, `Shader.CompileGPUProgram` и `Shader.DynamicLoadGPUProgram`. Pipeline marker использует exact `CreatePSO.Job`. Те же values и provenance доступны через `perfmeter.metrics.latest` и session JSON/CSV.

## Корреляция сессии с Profile Analyzer

Во время профилирования каждая сессия создаёт мгновенные samples `SGG.PerfMeter.Session.<sessionId>.Begin` и `.End`. Команда `SGG/Perfmeter/Open Profile Analyzer For Session` открывает опциональное окно Profile Analyzer и копирует ID текущей сессии в буфер обмена. Команда не устанавливает Profile Analyzer, не загружает данные Profiler и не применяет фильтр автоматически; после загрузки нужного capture найдите скопированный ID.

## Окно анализа сессии

Откройте `SGG/Perfmeter/Session Analysis` для read-only анализа текущей сессии в памяти Editor. Виртуализированные вкладки показывают timeline сохранённых samples, authoritative worst frame с деталями сохранённого sample, derived нарушения CPU-main/CPU-render/GPU budget и authoritative scopes whole-run/current-scene. Для CPU-main исключается present wait; GPU values и нарушения показываются только при явной доступности GPU timing.

Окно читает только `GetSessionSummary()` и `GetSessionSamples()` и никогда не запускает runtime. Недоступный timing отображается как `Unavailable`, а не числовой ноль. Остановленная сессия видна, пока существует её runtime instance; `PerformanceMeter.Stop()`, domain reload или выход из Play Mode могут удалить эту сессию из памяти.

## Trace и prewarm GraphicsStateCollection

1. В Unity `6000.4+` убедитесь, что доступна опциональная сборка `SGG.PerfMeter.GraphicsStateCollection`. В Unity `6000.4` она использует namespace `UnityEngine.Experimental.Rendering.GraphicsStateCollection`, а в Unity `6000.5+` — `UnityEngine.Rendering.GraphicsStateCollection`.
2. До trace запустите PerfMeter session. Вызовите `StartSession(...)`, затем `RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` или соответствующую MCP-команду. Без active session запрос отклоняется; session должна оставаться recording до завершения trace, а `PerformanceMeter.StopSession()` отменяет активный trace.
3. Оставьте сценарий работающим, пока bounded trace продвигается. В обычном Play Mode каждый trace frame tick-ается после `WaitForEndOfFrame`; в batch mode coordinator использует fallback следующего кадра. Session samples, принятые в этот период, получают `GraphicsStateTraceId`/`graphics_state_trace_id`; session settings определяют, сколько связанных samples будет сохранено.
4. Опросите `GetGraphicsStateCollectionStatus()` или `perfmeter.graphics.state_collection.status` до `Completed`, затем при необходимости остановите session. Остановка во время active trace отменяет его и может оставить `IsBusy`/`is_busy` true, пока выполняется retry owned cleanup. Owned `.graphicsstate` artifact находится в project-relative root `Temp/PerfMeter/GraphicsStateCollections` и ограничен 64 MiB.
5. Передайте сообщённый owned relative path в `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` или MCP prewarm. Prewarm синхронный, сохраняет artifact и сообщает completed warmups и `IsWarmedUp`; progressive warmup может завершиться с явным incomplete warning.

Graphics-state coordinator разрешает один flight и также отклоняет overlap с active external GPU capture, memory snapshot или alert-capture work. Повторный active trace ID — `AlreadyActive`, другой ID — `RejectedOverlap`. `CancelGraphicsStateTrace` отменяет только matching active/preparing trace и очищает pending artifact. Если owned artifact не удалён, `HasPendingCleanup`/`has_pending_cleanup` остаётся true, рядом сохраняется sidecar `.delete-pending`, а после domain reload cleanup восстанавливается и повторяется; `IsBusy`/`is_busy` и warning остаются видимыми до успеха. Unity backend не поддерживает cache-miss tracing, поэтому cache-miss evidence недоступно.

## Контекст интеграции рендеринга

Используйте neutral snapshot, когда нужен единый pipeline-independent вид последней typed render integration:

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

Или прочитайте те же данные через MCP:

```text
perfmeter.render.snapshot {}
```

Эти read-операции не запускают runtime collection. Проверяйте вместе `State`, `ObservationAgeFrames`, `LastObservedFrame` и `ObservationMatchesCurrentPipeline`. После смены pipeline или asset configuration предыдущая observation становится stale; сохраняйте явные warning и non-match и не считайте её pass/mode/GRD/VRS данными текущего кадра. Legacy API `PerformanceMeter.GetRenderGraphSnapshot()` и команда `perfmeter.rendergraph.snapshot` остаются доступны.

Для диагностики GRD проверяйте `DegradedReason`, SRP support, project configuration, compute support, compatibility режима URP и `ActivityAvailability`. `IsObservedActive` — global enabled state Unity. Используйте `Effectiveness` только как aggregate BRG workload context: `AvailableNoSample`/`Unavailable` не означают нулевую нагрузку, а положительные BRG counters не доказывают использование GRD конкретным renderer.

В capture bundle schema `sgg.perfmeter.capture-context` версии `1` сохраняет существующий `render` и добавляет `render_integration`. Для external GPU capture этот context фиксируется на первом sample фазы `Capturing`; Memory Profiler bundle записывает его при завершении memory request. Session JSON/CSV schemas не изменяются. Public API не предоставляет стабильного RenderGraph/CustomPass viewer или pass targets, поэтому workflow не обещает Editor navigation.
