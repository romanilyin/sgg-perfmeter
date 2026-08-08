# ワークフロー

## FTUE のセットアップと継続

`SGG/Perfmeter/Setup` を開き、**FTUE** タブを選択します。必須チェックでは、互換性、render integration、Frame Timing Stats、package path、読み込まれた settings JSON を確認します。オプションの行はインストールまたはスキップできます。インストール済みの行は workflow が完了したと黙って表示するのではなく、次の action を表示します。

### Memory Profiler

`com.unity.memoryprofiler` のインストール後、**Memory Profiler** 行には、管理対象フォルダーが存在すると **Open Window/Analysis/Memory Profiler**、**Copy RequestMemorySnapshot Snippet**、**Copy Memory Trigger Snippet**、**Open Runtime**、**Reveal Snapshots** が表示されます。コピーされた snippet は project が呼び出す runtime code です。FTUE 自身は snapshot を要求せず、trigger も設定しません。one-shot の `.snap` ファイルは `Temp/PerfMeter/MemorySnapshots` の下に staging されます。後続の request または runtime cleanup によって管理対象の source が削除される前に、結果を開くかコピーしてください。

one-shot snippet は次のとおりです。

```csharp
PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(
    new PerfMeterMemorySnapshotOptions("ftue-memory-snapshot"));
```

opt-in trigger snippet は次のとおりです。

```csharp
bool configured = PerformanceMeter.ConfigureMemorySnapshotTriggers(
    new PerfMeterMemorySnapshotTriggerOptions(
        enabled: true,
        systemMemoryThresholdBytes: 2L * 1024L * 1024L * 1024L,
        leakGrowthThresholdBytes: 256L * 1024L * 1024L));
```

**Open Runtime** で capability/status snapshot を確認します。手動 capture がデフォルトです。trigger threshold は明示的に設定するまで無効のままです。

### Profile Analyzer

インストール済みの **Profile Analyzer** 行には **Open Profile Analyzer** と **Open Runtime** が表示されます。最初に Unity Profiler で recording を開始し、その recording の中で PerfMeter session を start して stop します。opener は `PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession()` を使用して Profile Analyzer を開き、session ID をコピーします。recording した Profiler data をロードして、その ID を検索してください。Profile Analyzer のインストール、Profiler data のロード、filter の自動適用は行いません。

### Adaptive Performance

インストール済みの **Adaptive Performance** 行には **Open Runtime** があり、optional telemetry provider の現在の status を確認できます。FTUE action は session を開始せず、capture も行いません。

### RenderDoc

RenderDoc は external tool であり、PerfMeter には同梱されません。Unity の公式 integration flow に従ってください。

1. 公式 download page から RenderDoc をインストールします: <https://renderdoc.org/builds>。
2. project の変更を保存し、Game View または Scene View の tab menu から **Load RenderDoc** を使用します。別の方法として、RenderDoc から Unity Editor または Development Build を起動できます。インストール後に Unity が attachment を表示しない場合は Unity を再起動してください。公式 Unity guide は <https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html> です。
3. FTUE で **Check Attachment** をクリックします。これは Unity の shared external-profiler signal だけを refresh します。FTUE は RenderDoc のインストールを検出できず、Unity もその signal から RenderDoc と PIX を識別できません。
4. **Copy Capture Snippet** をクリックし、Play Mode に入り、コピーした code を project runtime code から invoke します。

   ```csharp
   PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
       new PerfMeterCaptureOptions("ftue-renderdoc-capture", PerfMeterCaptureTool.RenderDoc, 1));
   ```

5. capture status には **Open Runtime** を使用します。コピーした request は persist されず、自動的にも invoke されません。Editor/Development Build、attached-tool、desktop platform、graphics API の要件が適用されます。`Completed` は Unity wrapper lifecycle の完了だけを確認します。attached tool を識別せず、`.rdc` artifact を authenticate せず、artifact path も返しません。

### GraphicsStateCollection

同梱される optional の **GraphicsStateCollection** 行には package install は必要ありません。**Open Runtime**、**Copy Trace Snippet**、**Copy Prewarm Snippet**、**Reveal Artifacts** が提供されます。FTUE は trace や prewarm を自動的に request しません。次の sequence を使用してください。

1. Play Mode で `PerformanceMeter.StartSession(...)` により recording 中の PerfMeter session を開始し、維持します。
2. コピーした trace code を project runtime code から invoke します。

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.RequestGraphicsStateTrace(
       new PerfMeterGraphicsStateTraceOptions("ftue-graphics-state-trace", 60));
   ```

3. `State == PerfMeterGraphicsStateCollectionState.Completed` になるまで `PerformanceMeter.GetGraphicsStateCollectionStatus()` を poll します。`ArtifactRelativePath` を prewarm の input に使用します。この path は `Temp/PerfMeter/GraphicsStateCollections` の下を指します。tracing 中に session を stop すると trace は cancel されます。
4. コピーした prewarm snippet の `<trace-artifact-file>` を、返された path に置き換えます。

   ```csharp
   PerfMeterGraphicsStateCollectionRequestResult result = PerformanceMeter.PrewarmGraphicsStateCollection(
       new PerfMeterGraphicsStatePrewarmOptions("Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>"));
   ```

5. trace 後に **Reveal Artifacts** をクリックすると、project-local artifact folder が表示されます。prewarm は synchronous で artifact を保持し、不完全な progressive warmup を報告する場合があります。trace length は 600 frames、管理対象 artifact は 64 MiB に制限されています。Unity backend は cache-miss evidence を提供しません。

## Full Initialization Bootstrap

**Setup > Initialization Code** で **Refresh from Project Settings** をクリックし、続けて **Copy Init Code** をクリックします。生成された `PerfMeterBootstrap` は、project settings の完全な normalized snapshot を埋め込み、scene load 後に `PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)` を呼び出します。overlay、logging、alert、session-default、overdraw settings を保持し、`enabled` と `collectionMode: Stopped` を尊重します。`StartSession` や capture request は実行しません。

code-owned startup を選ぶ場合は、Resources の zero-code settings path の代わりにこの explicit bootstrap を使用します。両方が存在する場合、正常に parse された explicit call は current domain の Resources auto-start callback を抑制します。Resources が先に start していた場合は、explicit snapshot が後から適用され、authoritative になります。invalid な explicit JSON は current runtime を変更せず、後続の Resources auto-start も抑制しません。session と default overdraw の operation は active explicit runtime snapshot を使用します。

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

## オプションのメモリスナップショット workflow

1. Unity `6000.4+` を使い、Package Manager から `com.unity.memoryprofiler` `1.1.0+` を install します。オプションの `SGG.PerfMeter.MemoryProfiler` assembly が backend を自動登録します。この package がない場合、core integration は unavailable のままです。
2. Play Mode で `PerformanceMeter.GetMemorySnapshotCapabilities()` または `perfmeter.memory.snapshot.capabilities` を読み、backend と必要な capture flags を確認します。
3. `RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("memory-spike-01"))` で manual snapshot を request するか、`ConfigureMemorySnapshotTriggers(...)` で system-memory threshold または bounded leak-growth window を明示的に有効化します。
4. `GetMemorySnapshotStatus()` または `perfmeter.memory.snapshot.status` を読み、snapshot と correlated bundle が terminal state になるまで待ちます。完成した evidence は `PerformanceMeter.ExportCaptureBundle(captureId)` または `perfmeter.capture.export` で export します。

memory-only evidence は既存の capture-bundle API により `Temp/PerfMeter/CaptureBundles` の下へ出力されます。bundle は requested tool として `MemoryProfiler` を記録し、メモリの provenance と `.snap` の streaming SHA-256 を含みますが、external GPU artifact は含みません。owned source は `Temp/PerfMeter/MemorySnapshots` の下にあり、成功した export で一度だけ消費されます。

## Graphics marker diagnostics

1. `PerformanceMeter.GetGraphicsDiagnostics()` または `perfmeter.graphics.diagnostics` を呼び、最新の marker value と graphics API context を読みます。
2. 各 capability の `SampleState`、`Resolution`、`ResolvedRecorderNames`、`Unit`、`DataType`、resolved/sampled component count、catalog revision を確認します。discovery は動的で、runtime start と明示的な profiler catalog refresh/reconfigure で行われます。
3. value は検出された unit の raw recorder value として扱います。marker は unavailable、sample なしで available、sampled のいずれにもなり得ます。numeric zero は universal な unavailable signal ではなく、shader/PSO count も保証されません。

shader marker は exact `Shader.CreateGPUProgram` を優先し、その後 aliases `Shader.CreateGPUPrograms`、`Shader.CompileGPUProgram`、`Shader.DynamicLoadGPUProgram` を解決します。pipeline marker は exact `CreatePSO.Job` を解決します。同じ value と provenance は `perfmeter.metrics.latest` と session JSON/CSV にもあります。

## Profile Analyzer とのセッション相関

Profiler 記録中、各 session は瞬間的な `SGG.PerfMeter.Session.<sessionId>.Begin` と `.End` sample を生成します。`SGG/Perfmeter/Open Profile Analyzer For Session` は optional Profile Analyzer window を開き、current session ID を clipboard にコピーします。この command は Profile Analyzer の install、Profiler data の load、filter の自動適用を行いません。対象 capture を読み込んだ後、コピーした ID を検索してください。

## セッション分析ウィンドウ

`SGG/Perfmeter/Session Analysis` を開くと、Editor のメモリにある current session を read-only で確認できます。virtualized tab には retained sample timeline、利用可能な sample detail を含む authoritative worst frame、derived CPU-main/CPU-render/GPU budget violation、authoritative whole-run/current-scene scope が表示されます。CPU-main は present wait を除外し、GPU value と violation には明示的な GPU timing availability が必要です。

この window は `GetSessionSummary()` と `GetSessionSamples()` だけを読み、runtime を開始しません。利用できない timing は数値 0 ではなく `Unavailable` と表示されます。stopped session は runtime instance が存在する間だけ表示され、`PerformanceMeter.Stop()`、domain reload、Play Mode の終了でメモリ上の session が破棄される場合があります。

## GraphicsStateCollection trace と prewarm

1. Unity `6000.4+` で optional `SGG.PerfMeter.GraphicsStateCollection` assembly が利用可能であることを確認します。Unity `6000.4` では `UnityEngine.Experimental.Rendering.GraphicsStateCollection`、Unity `6000.5+` では `UnityEngine.Rendering.GraphicsStateCollection` namespace を使います。
2. trace の前に PerfMeter session を開始します。`StartSession(...)` の後、`RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("shader-stutter-01", 60))` または対応する MCP request を実行します。active session がない場合、request は reject されます。trace 完了まで session は recording を続ける必要があり、`PerformanceMeter.StopSession()` は active trace を cancel します。
3. bounded trace が進む間、scenario を実行し続けます。通常の Play Mode では各 trace frame が `WaitForEndOfFrame` の後に tick され、batch mode では coordinator が next-frame fallback を使います。この間に admitted された session sample には `GraphicsStateTraceId`/`graphics_state_trace_id` が入り、session settings が保持する correlated sample 数を決めます。
4. `GetGraphicsStateCollectionStatus()` または `perfmeter.graphics.state_collection.status` が `Completed` になるまで poll し、必要なら session を stop します。active trace 中に stop すると cancel され、owned cleanup の retry 中は `IsBusy`/`is_busy` が true のままになる場合があります。owned `.graphicsstate` artifact は project-relative の `Temp/PerfMeter/GraphicsStateCollections` 以下にあり、64 MiB に制限されます。
5. status の owned relative path を `PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(path, maxStateCount))` または MCP prewarm command に渡します。prewarm は synchronous で artifact を保持し、completed warmup と `IsWarmedUp` を報告します。progressive warmup は explicit incomplete warning 付きで終了する場合があります。

graphics-state coordinator は一つの flight だけを許可し、active external GPU capture、memory snapshot、alert-capture との overlap も reject します。同じ active trace ID は `AlreadyActive`、別の ID は `RejectedOverlap` です。`CancelGraphicsStateTrace` は一致する active/preparing trace だけを cancel して pending artifact を cleanup します。owned artifact の削除に失敗すると `HasPendingCleanup`/`has_pending_cleanup` が true のままとなり、隣接する `.delete-pending` sidecar が domain reload 後に復元・再試行されます。`IsBusy`/`is_busy` と warning は成功まで visible です。Unity backend は cache-miss tracing をサポートしないため、cache-miss evidence はありません。

## Render integration context

pipeline に依存しない最新の typed render integration を読むには neutral snapshot を使います。

```csharp
PerfMeterRenderIntegrationSnapshot context = PerformanceMeter.GetRenderIntegrationSnapshot();
```

同じ情報は MCP からも読めます。

```text
perfmeter.render.snapshot {}
```

これらの read は runtime collection を開始しません。`State`、`ObservationAgeFrames`、`LastObservedFrame`、`ObservationMatchesCurrentPipeline` を一緒に確認してください。pipeline または asset configuration が変わると以前の observation は stale になります。warning と non-match を尊重し、その pass、mode、GRD、VRS の値を current frame の値として扱わないでください。legacy API `PerformanceMeter.GetRenderGraphSnapshot()` と `perfmeter.rendergraph.snapshot` は引き続き利用できます。

GRD diagnosis では `DegradedReason`、SRP support、project configuration、compute support、URP mode compatibility、`ActivityAvailability` を確認します。`IsObservedActive` は Unity の global enabled state です。`Effectiveness` は aggregate BRG workload context としてのみ使用してください。`AvailableNoSample`/`Unavailable` は workload 0 を意味せず、positive BRG counter も特定 renderer の GRD use を証明しません。

capture bundle の schema `sgg.perfmeter.capture-context` version `1` は既存の `render` を保持し、`render_integration` を追加します。external GPU capture では `Capturing` phase の最初の sample で context を freeze し、Memory Profiler bundle では memory request の完了時に記録します。session JSON/CSV schema は変更されません。public API に安定した RenderGraph/CustomPass viewer や pass target はないため、この workflow は Editor navigation を約束しません。
