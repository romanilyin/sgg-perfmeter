# トラブルシューティング

PerfMeter が期待したデータを表示しない場合は、この checklist を使用してください。

## Overlay が表示されない

- `SGG/Perfmeter/Setup` を開き、overlay visibility が enabled であることを確認します。
- collection mode が `Overlay` であり、`Background` または `Stopped` ではないことを確認します。
- zero-code setup を使用している場合、settings file が `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` に存在することを確認します。
- manual bootstrap を使用している場合、scene load 後に `PerformanceMeter.EnsureRunning()` が呼ばれていることを確認します。
- Play Mode に入ります。Edit Mode API calls は安全ですが runtime overlay は作成しません。

## Frame Timing または GPU Timing がない

- Player Settings -> Rendering -> Frame Timing Stats を有効化します。
- GPU frame timing が重要な Android では Vulkan を推奨します。
- OpenGL/OpenGLES は GPU timing の degraded mode として扱います。
- counter が存在すると仮定する前に、`PerfMeterStatusSnapshot.AvailableCounters`、`UnavailableCounters`、`Warning` を確認します。

## Overdraw Measurement が進まない

- URP では、active URP renderer に `PerfMeterRenderGraphFeature` をインストールします。
- HDRP では overdraw と heatmap は by design unsupported です。core diagnostics を使用してください。
- active camera が feature を含む renderer を使用していることを確認します。
- target backend が fragment UAV/storage buffers、compute shaders、async GPU readback をサポートしていることを確認します。
- bounded measurement window には `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` を使用します。
- target が unsupported の場合、PerfMeter は pass を scheduling せず `OverdrawState.Unsupported` を報告します。

## Session Export が失敗する

- project-local path に export します。
- workflow が明示的に既存 export を先に削除する場合を除き、既存 export を上書きしないでください。
- long runs では `MaxSamples` を bounded に保ちます。
- summaries の startup spikes を避けるため、warm-up frames/seconds を使用します。

## Alerts が多すぎる

- JSON settings で thresholds と consecutive-frame windows を調整します。
- Editor warning cooldowns を増やします。
- callbacks または structured logs で十分な場合は Editor warning logs を無効化します。

## デバイス間でデータが異なる

これは想定される動作です。GPU timings、profiler counters、display information、async readback、overdraw support は graphics API、platform、Unity version、device によって異なります。exported sessions の device snapshots と warnings を使用して差異を説明してください。
