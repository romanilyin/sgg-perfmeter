# 制限事項

SGG PerfMeter は low-overhead runtime diagnostics layer として設計されています。Unity Profiler、RenderDoc、Profile Analyzer、Frame Debugger の deep capture を置き換えるものではありません。

## Platform And Pipeline Scope

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration。
- Built-in Render Pipeline は unsupported で、planned ではありません。
- HDRP overdraw と heatmap は unsupported です。HDRP projects でも FPS、CPU、GPU、memory、sessions、alerts、camera、device、setup、MCP diagnostics は利用できます。
- Unity `2022.3` から `6000.3` は compile-safety のため import できる場合がありますが、runtime behavior と support は Unity `6000.4+` を対象にしています。

## Timing Availability

- GPU timing は platform と graphics API によって unavailable、delayed、unreliable になる場合があります。
- `CollectionFrame` は PerfMeter が snapshot を収集した Unity frame であり、`FrameTimingManager` が表す exact hardware frame とは限りません。
- GPU frame timing が重要な Android では Vulkan を推奨します。
- OpenGL/OpenGLES は GPU timing と overdraw instrumentation の degraded mode として扱ってください。

## Counter Availability

Profiler counters は platform、Unity version、render pipeline settings、graphics API によって異なります。すべての counter がどこでも存在すると仮定せず、`AvailableCounters`、`UnavailableCounters`、warnings を使用してください。

## External GPU Capture

- coordinator は active request を 1 件だけ許可し、`PreRoll`、`Capturing`、`PostRoll`、`Completed` を deterministic に進みます。同じ active ID は idempotent、異なる active ID は overlap として reject されます。
- backend は Unity の experimental な `ExternalGPUProfiler` を Editor または Development Builds で、external tool が attach 済みの場合だけ使用します。`RenderDoc` は Windows/Linux desktop の Direct3D 11、Direct3D 12、Vulkan に限定され、`PIX` は Windows desktop の Direct3D 12 に限定されます。
- `Completed` は Unity wrapper lifecycle だけを確認します。external `.rdc`/`.wpix` artifact の存在を証明せず、artifact path も提供しません。
- automated tests は fake backend を使用します。real external tool と artifact の確認は release gate です。
- Correlated bundles と MCP capture control は利用できますが、指定された `.rdc`/`.wpix` は observed/hashed artifact にすぎません。Unity は attached tool や capture association を認証できないため、real external tool の確認は release-candidate gate のままです。

## Overdraw Cost And Support

Numerical overdraw と visual heatmap は diagnostic modes です。rendering work を追加するため、steady-state gameplay UI として常時有効にせず、bounded windows で使用してください。

URP の numerical overdraw には次が必要です。

- active URP renderer に `PerfMeterRenderGraphFeature` がインストールされていること。
- fragment-stage UAV/storage-buffer support。
- compute shader support。
- supported graphics API。
- async GPU readback support。

HDRP を含む unsupported targets は warnings とともに `OverdrawState.Unsupported` を報告します。

## Overlay Cost

overlay には 2 つの UI Toolkit backend path があります。Unity `6000.4` では owned `UIDocument` host、Unity `6000.5+` では owned `PanelRenderer` host を使用します。host は foreign UI の panel settings と children を保持し、PerfMeter が所有する container だけを rebuild します。numeric values は stable reserved numeric slots と numeric monospace role を使用します。`FpsOnly` は 1 行に収まらない場合に deterministic な bounded two-row fallback を使用し、cards と bars は狭い logical widths で wrap します。これは clipping のリスクを下げますが、任意の resolution や scale を保証するものではありません。heavy visual diagnostics、graph modes、最終 layout は target devices で検証してください。

## Validation Status

現在の validation には automated EditMode coverage、Unity `6000.4.10f1` での HDRP smoke validation、以前の Android S23 Vulkan/GLES smoke validation が含まれます。データを release-signoff evidence として扱う前に、より広い player-build と device coverage を行うと有用です。

## オプションのメモリスナップショット: 制限とプライバシー

- Unity `6000.4+` で `com.unity.memoryprofiler` `1.1.0+` がない場合、この機能は利用できません。core package はこの dependency を install も要求もしません。
- 既定で有効なのは manual capture だけです。system-memory threshold と bounded leak-growth trigger は opt-in であり、各 request に single-flight/overlap、cooldown、最低空き容量、backend、capture-flag の guard が適用されます。
- owned `.snap` staging は `Temp/PerfMeter/MemorySnapshots` にあり、512 MiB に制限されます。memory-only evidence は `Temp/PerfMeter/CaptureBundles` に export され、bundle retention の total quota は 2 GiB です。成功した export は one-shot で staging source を削除しますが、cleanup warning が明示される場合があります。
- snapshot には process の機密メモリが含まれる可能性があります。共有前に保護し、内容を確認してください。bundle は `contains_sensitive_memory`、backend/flag provenance、`memory-snapshot.json`、SHA-256 metadata を記録し、external GPU artifact は作成しません。
- OS による file lock 中の削除と、portable managed reparse-point race への保護は best-effort です。安全でない path や所有外の path は reject され、cleanup failure は warning として残ります。
- evidence には memory EditMode `9/9`、capture-bundle EditMode `14/14`、PlayMode threshold `1/1`、`com.unity.memoryprofiler@1.1.12` による optional compile、Unity `6000.4.12f1` full EditMode `182/182` と full PlayMode `14/14` が含まれます。release-player または device behavior の主張ではありません。

## Graphics diagnostics と GraphicsStateCollection の制限

- shader GPU-program creation と graphics-pipeline creation marker は動的な `ProfilerRecorder` capability です。Unity、platform、graphics API、catalog refresh の状態により availability は変わります。`Unavailable`、`AvailableNoSample`、`AvailableSampled` と provenance を使い、numeric zero から availability を推測しないでください。
- marker value は recorder の `Unit` と `DataType` を保持する raw value です。shader/PSO count とは限らず、PerfMeter は common unit への変換を行いません。capability metadata には exact/alias resolution、resolved recorder name、resolved/sampled component count、catalog revision が含まれます。
- optional `SGG.PerfMeter.GraphicsStateCollection` assembly は Unity `6000.4+` を対象とします。`6000.4` では `UnityEngine.Experimental.Rendering.GraphicsStateCollection`、`6000.5+` では `UnityEngine.Rendering.GraphicsStateCollection` を使い、それ以前の Unity ではこの integration はサポートされません。
- trace には active PerfMeter session が必要です。通常の Play Mode では end-of-frame 後、batch mode では next-frame fallback で trace frame が完了します。correlated session sample は session の warm-up、interval、max-sample 設定の影響を受けます。
- preparation、trace finalization、prewarm、cleanup を含め graphics-state flight は一つだけです。active external GPU capture、memory snapshot、alert-capture も overlap rejection の対象です。`IsBusy`/`is_busy` はこれらの flight と persisted cleanup を示し、`HasPendingCleanup`/`has_pending_cleanup` は retry 待ちの owned artifact を明示します。matching cancel は best-effort で、cleanup failure は見える状態に残り、次の request を遅らせる場合があります。
- `StopSession()` は active trace を cancel するため、trace 全体で active session が必要です。owned artifact の削除に失敗すると隣接する `.delete-pending` sidecar marker が作られ、domain reload 後に復元・再試行されます。artifact と marker が消えるまで warning と busy state は残ります。
- prewarm は owned project-relative artifact のみを受け付け、synchronous に実行し、artifact を保持します。progressive warmup が incomplete になる場合があります。Unity backend は cache-miss tracing をサポートしないため、request は `Unavailable` となり cache-miss evidence は公開されません。
- owned `.graphicsstate` artifact は `Temp/PerfMeter/GraphicsStateCollections` 以下に保存され、regular non-empty file である必要があり、64 MiB に制限されます。trace は 600 frames、progressive prewarm は 1,000,000 states に制限されます。minimum-free-disk と project-local path guard も適用されます。
- final evidence は Unity `6000.4.12f1` compile passed、GSC EditMode targeted `25/25`、`PerformanceMeter` API EditMode `47/47`、capture-bundle EditMode `14/14`、PlayMode smoke `12/12`、full post-fix EditMode `208/208`、full post-fix PlayMode `16/16` です。Unity `6000.5.6f1` の optional consumer compile も isolated に passed しました。Unity `6000.5` の full tests、release-player、target-device behavior は release gate のままで、検証済みとは主張しません。

## Render integration context の制限

- `PerfMeterRenderIntegrationSnapshot` は integration-neutral な observation contract であり、deep Render Graph/Custom Pass capture ではありません。read は runtime collection を開始しません。最初の observation 前は supported current pipeline が `Available`/`NotObserved` になる場合があり、pipeline/configuration の変更後は `ObservationMatchesCurrentPipeline: false`、明示的な frame/age、warning で stale observation を示します。
- URP は public current-frame `UniversalRenderingData.renderingMode` と実際に schedule された PerfMeter pass を報告します。HDRP は実際の PerfMeter `CustomPass` を報告しますが、effective rendering mode は unavailable です。
- private/internal な Render Graph pass/resource reflection は削除されました。stable public API がないため、legacy facade の `registered_pass_count`、`merged_pass_count`、`transient_resource_count`、`imported_resource_count`、`aliased_resource_count` は `-1` のままです。
- GRD activity は public `IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled()` の結果で、global runtime state を示します。特定の camera/renderer が GRD を使った証拠ではありません。URP Forward+ は current-frame observation であり、HDRP の rendering-mode/Forward+ availability は `Unknown` のままです。
- GRD effectiveness は exact capability provenance 付きの aggregate BRG draw-call/instance counter です。他の `BatchRendererGroup` 利用者も含み得るため、renderer ごとの GRD participation は証明しません。unavailable または未 sample 値は `null` に serialize されます。
- VRS は `SystemInfo`/`ShadingRateInfo` の authoritative hardware support を報告します。future typed adapter が証明しない限り configuration/activity は `Unknown` で、VRS activity は主張しません。
- Unity の stable public API は RenderGraph/CustomPass viewer や pass-target API を公開しません。そのため PerfMeter は Editor navigation を追加せず、約束もしません。
- capture context schema v1 は `render` を保持して `render_integration` を追加し、session JSON/CSV schema は変更しません。external capture context は最初の `Capturing` sample で freeze され、後続の read で置き換えられません。
- PM-REN-001 の最終 evidence は Unity `6000.4.12f1` main compile passed、targeted `PerformanceMeterApiTests` `53/53`、`PerfMeterCaptureBundleTests` `15/15`、`PerformanceMeterPlayModeSmokeTests` `12/12`、final full EditMode `215/215`、full PlayMode `16/16` です。Focused review P1/P2 resolved。isolated compile matrix は Unity `6000.4.12f1` URP `17.4`/HDRP `17.4` と Unity `6000.5.6f1` URP `17.5`/HDRP `17.5` で passed しました。release-player/device validation は pending のままで、release は主張しません。
- PM-GRD-001 の最終 evidence は Unity `6000.4.12f1` compile passed、targeted API `58/58`、capture-bundle `15/15`、PlayMode smoke `12/12`、full EditMode `220/220`、PlayMode `16/16` です。Focused review P1/P2 resolved。Unity `6000.4`/`6000.5` の URP `17.4`/`17.5`、HDRP `17.4`/`17.5` compile matrix も passed しました。release-player/device behavior は pending です。
