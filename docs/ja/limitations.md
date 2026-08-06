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
- Capture bundles、artifact provenance、MCP capture control はこの coordinator の対象外で、別の future work です。

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
