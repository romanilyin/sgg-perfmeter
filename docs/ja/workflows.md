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
