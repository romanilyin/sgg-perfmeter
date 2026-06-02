# 制限事項

SGG PerfMeter は low-overhead runtime diagnostics layer として設計されています。Unity Profiler、RenderDoc、Profile Analyzer、Frame Debugger の deep capture を置き換えるものではありません。

## Platform And Pipeline Scope

- Supported runtime target: Unity `6000.4+`、URP `17.4+`、Render Graph path。
- Built-in Render Pipeline は unsupported で、planned ではありません。
- HDRP support は planned future work ですが、`2026.6.5-1` では実装されていません。
- Unity `2022.3` から `6000.3` は compile-safety のため import できる場合がありますが、runtime behavior と support は Unity `6000.4+` を対象にしています。

## Timing Availability

- GPU timing は platform と graphics API によって unavailable、delayed、unreliable になる場合があります。
- `CollectionFrame` は PerfMeter が snapshot を収集した Unity frame であり、`FrameTimingManager` が表す exact hardware frame とは限りません。
- GPU frame timing が重要な Android では Vulkan を推奨します。
- OpenGL/OpenGLES は GPU timing と overdraw instrumentation の degraded mode として扱ってください。

## Counter Availability

Profiler counters は platform、Unity version、render pipeline settings、graphics API によって異なります。すべての counter がどこでも存在すると仮定せず、`AvailableCounters`、`UnavailableCounters`、warnings を使用してください。

## Overdraw Cost And Support

Numerical overdraw と visual heatmap は diagnostic modes です。rendering work を追加するため、steady-state gameplay UI として常時有効にせず、bounded windows で使用してください。

Numerical overdraw には次が必要です。

- active URP renderer に `PerfMeterRenderGraphFeature` がインストールされていること。
- fragment-stage UAV/storage-buffer support。
- compute shader support。
- supported graphics API。
- async GPU readback support。

unsupported targets は warnings とともに `OverdrawState.Unsupported` を報告します。

## Overlay Cost

overlay は allocation-conscious かつ throttled ですが、変化する numeric values と graph labels は refresh interval で managed strings を materialize する場合があります。heavy visual diagnostics と graph modes は target devices で検証してください。

## Validation Status

現在の validation には automated EditMode と PlayMode coverage、Android S23 Vulkan/GLES smoke validation が含まれます。データを release-signoff evidence として扱う前に、より広い player-build と device coverage を行うと有用です。
