# ワークフロー

## Runtime Overlay

ゲーム内で即時に確認したい場合は overlay を使用します。

```csharp
PerformanceMeter.EnsureRunning();
PerformanceMeter.SetOverlayVisible(true);
PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
```

overlay は UI Toolkit を使用し、gameplay input を横取りしません。FPS-only、compact text、graph、full diagnostics、metric bars、visual themes、module filters、CPU/GPU graphs、CPU core widgets、限定的な custom metric rows をサポートします。

PerfMeter は overlay 用に versioned UI Toolkit host を作成して所有します。Unity `6000.4` では `UIDocument`、Unity `6000.5+` では `PanelRenderer` を使用します。この owned host は foreign UI と分離され、foreign UI の panel settings と children を保持します。rebuild では PerfMeter が所有する container だけを削除します。

## Background Collection

visible UI が不要な tests、device runs、agent workflows では background mode を使用します。

```csharp
PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
```

## Session Recording And Export

repeatable profiling windows には sessions を使用します。

```csharp
PerformanceMeter.StartSession(new PerfMeterSessionOptions(30, 0.25f, 600));

// Run the measured scenario.

PerformanceMeter.StopSession();
PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
PerformanceMeter.ExportSessionJson("Logs/perfmeter-session.json");
PerformanceMeter.ExportSessionCsv("Logs/perfmeter-session.csv");
```

Session exports には timing、FPS lows、spikes、bottleneck counts、render counters、memory counters、overdraw state、warning/counter availability、scene summaries、worst frames、device metadata、camera metadata、settings metadata、custom metrics が含まれます。

## Alerts

rules は budget violations、low FPS、unavailable GPU timing、overdraw thresholds を報告できます。

```csharp
PerformanceMeter.AlertFired += alert => UnityEngine.Debug.Log(alert.Message);
PerfMeterAlertSnapshot[] latestAlerts = PerformanceMeter.GetLatestAlerts();
```

Editor warnings は cooldown で throttled され、JSON settings または runtime controls で無効化できます。structured alert logs と Editor warnings は独立しています。`PerformanceMeter.SetStructuredLogsEnabled(false)` は structured alert の `Debug.Log` 出力だけを抑制し、`PerformanceMeter.SetEditorWarningLogsEnabled(false)` は Editor warning logs を別に制御します。callbacks、alert/history、overlay warnings、sessions は有効なままです。

## External GPU Capture

tool がすでに attach されている場合、限定的な RenderDoc または PIX request には capture coordinator を使用します。

```csharp
PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
    new PerfMeterCaptureOptions("gpu-spike", PerfMeterCaptureTool.RenderDoc, 1, 30, 30));

PerfMeterCaptureStatusSnapshot status = PerformanceMeter.GetCaptureStatus();
```

coordinator は 1 つの active request だけを所有し、`PreRoll`、`Capturing`、`PostRoll`、`Completed` を deterministic に進みます。同じ active ID は idempotent で、異なる ID は overlap として reject されます。pre-roll と post-roll は Unity frames を数え、`Capturing` だけが alert capture scope を開いて Unity の experimental な `ExternalGPUProfiler` を invoke します。Editor または Development Build であることと attached tool があることは必須 gate です。`RenderDoc` は Windows/Linux desktop の Direct3D 11、Direct3D 12、Vulkan で、`PIX` は Windows desktop の Direct3D 12 で使用できます。

`Completed` は guarded Unity wrapper lifecycle が終了したことだけを示します。Unity API は attached tool の identity や authoritative artifact path を公開しないため、`Status.Tool` は requested tool だけを示します。`PerfMeterCaptureBundleOptions` overload は baseline/capture samples を分離して project-local bundle を atomic export します。external artifact は observed であり authoritative ではありません。automation には `perfmeter.capture.request/status/cancel/export/capabilities` を使用します。

## Overdraw Diagnostics

numerical overdraw は opt-in で bounded です。

```csharp
PerformanceMeter.RequestOverdrawMeasurement(frameCount: 60);
PerformanceMeter.SetOverdrawHeatmapVisible(true);
```

Numerical overdraw と heatmap は URP Render Graph diagnostic path を使用します。Overdraw measurement には、`PerfMeterRenderGraphFeature`、replacement shader support、fragment UAV/storage-buffer support、compute shader support、supported graphics API、async GPU readback が必要です。HDRP は overdraw/heatmap を unsupported として報告しますが、core overlay、session、API、MCP diagnostics は利用できます。unsupported targets では pass を実行せず `OverdrawState.Unsupported` を報告します。

## Camera And Device Reproducibility

performance capture を生成した環境を保持するには snapshots を使用します。

```csharp
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```

Session exports には device と camera metadata が含まれるため、capture を後で理解または再現できます。

## Custom Metrics

PerfMeter を fork せずに project-specific providers を登録できます。

```csharp
PerformanceMeter.RegisterCustomMetricProvider(provider);
PerfMeterCustomMetricSnapshot[] customMetrics = PerformanceMeter.GetCustomMetrics();
```

Custom metrics は API reads、session JSON export、MCP latest metrics、`CustomMetrics` module が有効な場合の最大 8 行の overlay rows で公開されます。

## Unity Profiler Instrumentation

この instrumentation は internal であり、Editor、Development Build、または別の profiler-enabled build を profiling している場合だけ Unity Profiler に表示されます。Profiler を有効にしていない Release player では、これらの marker/counter は no-op で instrumentation data を生成しません。public API、status、MCP、export schema は変更しません。

- Marker は collection/frame timing（`SGG.PerfMeter.Collect`、`SGG.PerfMeter.Collect.FrameTiming`）、providers（`SGG.PerfMeter.Provider.CustomMetrics`、`SGG.PerfMeter.Provider.CpuCore`、`SGG.PerfMeter.Provider.DeviceSnapshot`、`SGG.PerfMeter.Provider.CameraSnapshot`）、bottleneck/capture（`SGG.PerfMeter.Bottleneck.Classify`、`SGG.PerfMeter.Capture.Session`、`SGG.PerfMeter.Capture.AlertScope`、`SGG.PerfMeter.Capture.Coordinator`）、JSON/CSV export（`SGG.PerfMeter.Export.Json`、`SGG.PerfMeter.Export.Csv`）を計測します。`SGG.PerfMeter.Thermal.Sample` は reserved internal provider hook です。
- Counter は CPU/GPU frame time（`SGG.PerfMeter.CPU.FrameTime`、`SGG.PerfMeter.CPU.MainThreadTime`、`SGG.PerfMeter.CPU.RenderThreadTime`、`SGG.PerfMeter.CPU.PresentWaitTime`、`SGG.PerfMeter.GPU.FrameTime`）を nanoseconds の end-of-frame gauge として記録します。`SGG.PerfMeter.CPU.FrameTimingAvailable`、`SGG.PerfMeter.GPU.FrameTimingAvailable`、`SGG.PerfMeter.Capture.AlertScopeActive`、`SGG.PerfMeter.Thermal.Available` は availability/active を `0`/`1` で表し、`SGG.PerfMeter.Bottleneck.Kind`、`SGG.PerfMeter.Capture.SessionState`、`SGG.PerfMeter.Capture.OverdrawState`、`SGG.PerfMeter.Capture.State` は enum code、`SGG.PerfMeter.Provider.CustomMetricCount` は count です。Counter は `Scripts` category と `FlushOnEndOfFrame` を使用します。
- synthetic thermal sample は生成されません。`SGG.PerfMeter.Thermal.Available` は `0`/unavailable のままで、real platform provider が data を供給するまで利用できません。

## Self-Observability And Overhead Budgets

`PerformanceMeter.GetSelfOverhead()` または `PerformanceMeter.GetStatus().SelfOverhead` で、collector、custom providers、CPU-core provider、overlay、URP/HDRP integration の CPU callback cost と allocation を診断できます。固定 120-frame window、invocation 単位の average、component ごとの CPU/allocation budget を使用します。

Inactive render integration は `Unsupported`、呼び出されていない supported component は `NotMeasured`、GPU self-timing は `Unavailable` です。Accounting は diagnostics 専用であり、PerfMeter は既存の CPU/GPU metrics から overhead を差し引かず、値を補正しません。

## Agent Automation

典型的な MCP-driven run は次の通りです。

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

`perfmeter.profiler.capabilities {}` は cache 済み state の read であり、runtime の起動や discovery は行いません。
