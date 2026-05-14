# SGG PerfMeter

SGG PerfMeter предоставляет публичный runtime API для безопасного чтения статуса и последних метрик производительности без обращения к UI или Unity Console.

## API для агентов

- Namespace: `SGG.PerfMeter`
- Статус: `PerfMeter.GetStatus()` или `PerfMeter.TryGetStatus(out PerfMeterStatusSnapshot status)`
- Метрики: `PerfMeter.GetLatestMetrics()` или `PerfMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)`
- Lifecycle: `PerfMeter.EnsureRunning()` и `PerfMeter.Stop()`
- Overlay: `PerfMeter.SetOverlayVisible(bool visible)`, `PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner corner)`, `PerfMeter.SetOverlayMode(PerfMeterOverlayMode mode)`, `PerfMeter.SetTargetFps(PerfMeterTargetFps targetFps)`, `PerfMeter.IsOverlayVisible`, `PerfMeter.OverlayCorner`, `PerfMeter.OverlayMode`, `PerfMeter.TargetFps` и status snapshot поля `OverlayVisible` / `OverlayCorner` / `OverlayMode` / `TargetFps`
- Overdraw: `PerfMeter.RequestOverdrawMeasurement(int frameCount = 60)` и `PerfMeter.CancelOverdrawMeasurement()`

Запросы безопасны до запуска runtime: обычное чтение возвращает snapshot со `State = Stopped` и не должно бросать исключения.

## Setup window

Откройте `SGG/Perfmeter/Setup`, чтобы подготовить проект без ручного редактирования URP renderer asset.

- `Project Settings` показывает состояние `Frame Timing Stats` и может включить этот Player Setting кнопкой `Enable Frame Timing`.
- `URP Renderer Features` находит URP renderer assets проекта и добавляет `PerfMeterRenderGraphFeature` кнопкой `Install Renderer Feature` без создания дублей.
- `Initialization Code` показывает bootstrap-код для запуска runtime overlay; настройки `Overlay Visible`, `Target FPS`, `Overlay Corner` и `Overlay Mode` сразу меняют код, который копирует `Copy Init Code`.
- Вкладка `Runtime` предназначена для Play Mode: в Edit Mode кнопки отключены, а в Play Mode можно переключать target FPS, режим/угол overlay, скрывать или показывать overlay и запускать короткое измерение overdraw.

Те же действия доступны агентам и Editor-скриптам без открытия окна через публичный Editor API:

```csharp
using SGG.PerfMeter.Editor.Setup;
using UnityEngine;

Debug.Log(PerfMeterSetupActions.GetStatusReport());
Debug.Log(PerfMeterSetupActions.RunRecommendedSetup());
Debug.Log(PerfMeterSetupActions.CopyInitializationSnippetToClipboard());
```

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
	{
		PerfMeter.EnsureRunning();
		PerfMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
		PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
		PerfMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
		PerfMeter.SetOverlayVisible(true);
	}
}
```

## Runtime-метрики

Runtime singleton обновляет snapshots в `Update()` реальными значениями из `FrameTimingManager` и `ProfilerRecorder` без per-frame allocation со стороны PerfMeter.

- Тайминги: `CpuFrameTimeMs`, `CpuMainThreadFrameTimeMs`, `CpuRenderThreadFrameTimeMs`, `CpuMainThreadPresentWaitTimeMs`, `GpuFrameTimeMs`.
- FPS-статистика: `AverageFps`, `OnePercentLowFps`, `PointOnePercentLowFps`, `FrameSampleCount`, `GpuValidSampleCount`, `FrameSpikeCount`, `SevereFrameSpikeCount`; источник - `FrameTiming.cpuFrameTime`, не `Time.deltaTime`.
- Render counters: `DrawCalls`, `SetPassCalls`, `Batches`, `Vertices`, `SrpBatcherInstances`.
- BRG/GRD counters, если доступны: `BrgDrawCalls`, `BrgInstances`, `IndexBufferUploadInFrameBytes` через `PerfMeterStatusSnapshot.AvailableCounters` / `UnavailableCounters`.
- Memory counters: `SystemUsedMemoryBytes`, `GcReservedMemoryBytes`, `GpuMemoryBytes`.
- Классификация: `Bottleneck` (`GpuBound`, `CpuMainThreadBound`, `CpuRenderThreadBound`, `Balanced`, `Unknown`) с текущим `FrameBudgetMs`, который определяется `PerfMeterTargetFps`.
- Численное измерение overdraw: `OverdrawState`, `OverdrawProgress` и `OverdrawRatio` доступны в status/metrics snapshots для чтения агентами без UI.

На OpenGL/OpenGLES аппаратный GPU timing может быть недоступен или ненадежен. В этом случае `GpuFrameTimeAvailable` будет `false`, а `PerfMeterStatusSnapshot.Warning` содержит предупреждение; для Android предпочтителен Vulkan.

## Overlay

Runtime overlay создается программно на UI Toolkit (`UIDocument`, `PanelSettings`, `VisualElement`, `Label`). Он создается только во время Play Mode/runtime; EditMode API-вызовы остаются безопасными и не создают видимый UI document.

- Layout абсолютный, `PickingMode.Ignore`, поэтому overlay не перехватывает input игры; в графических режимах overlay состоит из широкого graph block сверху и более узкого text block снизу.
- По умолчанию overlay находится в правом верхнем углу (`TopRight`) и режиме `Full`; доступные углы: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`.
- Режимы: `FpsOnly` показывает одну строку FPS/1%/0.1%, `TextCompact` показывает компактную текстовую сводку, `Graphs` показывает FPS и CPU/GPU графики, `Full` добавляет счетчики render/memory/overdraw.
- Label обновляется максимум 4 раза в секунду из последних runtime snapshots, а не каждый кадр.
- Графики рисуются через UI Toolkit `generateVisualContent`; CPU-граф использует stacked area для `render`, `main` и остатка до `frame`, а `frame` рисуется верхней границей без суммирования `frame + main + render`.
- Справа от графиков выводятся цветные подписи-плашки `frame`, `other`, `main`, `render` и `gpu` с текущим значением, средним, худшими 1% и 0.1% по времени кадра; числа имеют фиксированную ширину относительно текущего масштаба/максимума.
- Если GPU timing временно недоступен, GPU-плашка становится серой, текущее значение заменяется underscore placeholder, а средние/история используют только валидные samples.
- Целевой FPS выбирается из `15/30/60/90/120/144/240`; соответствующий `FrameBudgetMs` задает положение красной target line слева от графика и участвует в масштабе `max(averageTimeMs * 1.1, FrameBudgetMs * 1.2)`.
- Текстовые режимы показывают current/min/max за внутреннее окно истории overlay для timing, render counters и memory.
- Предупреждения удерживаются короткое время, чтобы transient GPU timing gaps не мигали каждый refresh.
- В `Full` отображаются state, bottleneck, FPS/lows/spikes, CPU/GPU timings, draw calls, SetPass, batches, vertices, SRP/BRG counters, index uploads, overdraw state/progress/ratio, memory и warning text при наличии.
- `PerfMeter.SetOverlayVisible(false)` скрывает retained UI без остановки сбора метрик; `PerfMeter.SetOverlayVisible(true)` гарантирует запуск runtime и показывает overlay в Play Mode.
- `PerfMeter.IsOverlayVisible` и `PerfMeterStatusSnapshot.OverlayVisible` возвращают фактическое состояние видимости overlay.

## URP Render Graph Renderer Feature

`PerfMeterRenderGraphFeature` добавляет URP 17 Render Graph marker pass с dedicated profiling sampler `SGG.PerfMeter.Overlay` и opt-in pass для численного измерения overdraw. Overlay marker не рисует тяжелую графику и нужен для будущего измерения и вычитания overhead самого инструмента через `ProfilerRecorder`.

- Предпочтительный способ установки: `SGG/Perfmeter/Setup` -> `Install Renderer Feature`.
- Ручной способ: добавьте feature в renderer asset, например `Assets/Settings/PC_Renderer.asset` для PC или `Assets/Settings/Mobile_Renderer.asset` для Mobile.
- В Inspector renderer asset выберите `Add Renderer Feature` -> `Perf Meter Render Graph Feature`.
- Оставьте `Enabled` включенным, `Render Pass Event` по умолчанию `AfterRenderingPostProcessing` или переключите на `AfterRendering`, если нужен самый поздний marker после всего кадра.
- `Marker Name` по умолчанию `SGG.PerfMeter.Overlay`; меняйте его только если downstream `ProfilerRecorder` будет искать другое имя.
- Feature использует `RecordRenderGraph(RenderGraph, ContextContainer)` и `AddRasterRenderPass`; legacy-only path не используется.

## Измерение Overdraw

Измерение overdraw является opt-in и ограничено по времени. Вызовите `PerfMeter.RequestOverdrawMeasurement()` для окна по умолчанию на 60 кадров или передайте свое положительное значение `frameCount`. Для ранней остановки используйте `PerfMeter.CancelOverdrawMeasurement()`.

- Runtime по умолчанию находится в `Off` и не записывает overdraw pass, пока нет активного запроса.
- `PerfMeterRenderGraphFeature` должен быть добавлен в активный URP renderer. Во время активного запроса он записывает Render Graph raster pass с profiling marker `SGG.PerfMeter.Overdraw`.
- Pass повторно рендерит scene renderer list с hidden replacement shader `Hidden/SGG/PerfMeter/OverdrawCounter`: `ZTest Always`, `ZWrite Off`, `ColorMask 0`.
- Fragment shader выполняет atomic increment в GPU `GraphicsBuffer`, затем readback pass вызывает `AsyncGPUReadback` без CPU stall.
- `OverdrawRatio` считается как `TotalFragments / RenderedCameraPixels` и усредняется по завершенным readback samples.
- Shader требует поддержку fragment UAV/storage buffer; на OpenGL ES или других ограниченных backend измерение может перейти в `Error` с предупреждением.
- Visual heatmap output еще не реализован.
- Progress растет после async readback, запланированного из Render Graph overdraw pass. Если progress остается `0`, проверьте, что renderer feature установлен в renderer, который использует активная камера, и что target backend поддерживает shader instrumentation.

```csharp
using SGG.PerfMeter;

PerfMeter.EnsureRunning();
PerfMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerfMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
PerfMeter.SetOverlayVisible(true);
PerfMeter.RequestOverdrawMeasurement();

if (PerfMeter.TryGetStatus(out PerfMeterStatusSnapshot status))
{
	UnityEngine.Debug.Log($"PerfMeter: {status.State}, overdraw {status.OverdrawState} {status.OverdrawProgress:P0}, {status.Warning}");
}

if (PerfMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics))
{
	UnityEngine.Debug.Log($"GPU {metrics.GpuFrameTimeMs:0.00} ms, Draws {metrics.DrawCalls}, Overdraw {metrics.OverdrawRatio:0.00}");
}
```

## Лицензия

Этот пакет лицензирован по **Stinger Royalty-Free EULA 1.0**.

- Основная версия: русский текст в [`LICENSE.ru.md`](../LICENSE.ru.md).
- Английский справочный текст: [`LICENSE.md`](../LICENSE.md), для удобства чтения.
- Уведомления проекта: [`NOTICE.md`](../NOTICE.md) и [`NOTICE.ru.md`](../NOTICE.ru.md).
- SPDX identifier: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.
- Лицензиар: ROMAN ILYIN.
- Канонический репозиторий: https://github.com/romanilyin/sgg-perfmeter.

Бесплатно для личных, внутренних, открытых и коммерческих Конечных продуктов. Без роялти. Standalone-продажа, перепродажа, платное перераспространение или отдельная коммерциализация этого Актива или Производных активов запрещены.

Русская версия EULA является основной и имеет преимущественную силу. При расхождении, противоречии или различии толкования между русской и английской версиями применяется русская версия.
