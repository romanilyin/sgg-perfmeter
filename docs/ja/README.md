# SGG PerfMeter

**Unity 6 URP+HDRP (FPS meter) 向けの軽量なランタイム性能診断と、エージェントが読み取れるプロファイリング。**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](../es/README.md) | [Français](../fr/README.md) | [Italiano](../it/README.md) | [日本語](./README.md) | [한국어](../ko/README.md) | [Português (Brasil)](../pt-br/README.md) | [简体中文](../zh-cn/README.md)

[インストール](./installation.md) | [クイックスタート](./quick-start.md) | [ワークフロー](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [比較](./comparison.md) | [制限事項](./limitations.md) | [トラブルシューティング](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter は、フレームのボトルネック検出、性能変更の比較、再現可能なセッション記録、ツールや AI エージェント向けの構造化プロファイリングデータ提供を行います。

SGG PerfMeter は、フレームが CPU、GPU、レンダースレッド、present/VSync、overdraw、または利用できないプラットフォームカウンターのどれに制限されているかを説明し、その状態を後の分析用に保存できます。

## 役立つ理由

- ゲーム実行中にフレームボトルネックの文脈を確認できます。
- デバッグ状況に応じて visual preset、グラフ、metric bar、compact layout、custom metric row を切り替えられます。
- warm-up、scene scope、worst-frame summary、JSON/CSV export、device metadata、camera metadata を含む再現可能な profiling session を記録できます。
- overlay を常時監視しなくても、alerts、structured logs、callbacks、Editor warning cooldowns で回帰を検出できます。
- スクリーンショットや Console scraping に頼らず、比較、A/B test、hotspot search 用の構造化データをツールやエージェントに渡せます。

## データの公開方法

- **Runtime overlay**: live inspection 用の visual presets、compact layouts、graphs、metric bars、custom metric rows。
- **Public C# API**: status、metrics、device、camera、Render Graph、alerts、sessions、custom metrics の immutable snapshots。
- **Session recording**: warm-up、scene scope、worst frames、device/camera metadata、JSON/CSV export を持つ bounded captures。
- **Alerts**: structured logs、callbacks、Editor warning cooldowns、latest-alert snapshots。
- **Agent layer**: MCP command metadata により、agents が project inspection、run comparison、A/B test、hotspot search を構造化データで実行できます。

## 測定内容

- Unity `6000.4+` / URP `17.4+` Render Graph と HDRP `17.4+` Custom Pass の runtime state。
- FrameTimingManager CPU/GPU timing: 利用可能な場合の CPU frame、main thread、render thread、present wait、GPU frame time。
- ProfilerRecorder render counters: 利用可能な場合の draw calls、SetPass、batches、vertices、SRP Batcher、BRG/GRD、upload bytes、memory、GPU memory。
- GPU、CPU main thread、CPU render thread、present/VSync、balanced、unknown frames の bottleneck classification。
- URP Render Graph 経由の opt-in numerical overdraw measurement と visual overdraw heatmap。HDRP overdraw/heatmap は unsupported ですが、core diagnostics は利用できます。
- code と MCP automation 向けの device、URP/HDRP camera、render integration、status、metrics、alerts、session、custom metric snapshots。

## クイックスタート

1. npm registry または Git UPM から Unity package をインストールします。
2. Unity で `SGG/Perfmeter/Setup` を開きます。
3. recommended setup を実行し、Play Mode に入り、overlay が表示されることを確認します。

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.6.28-1"
  }
}
```

詳しいセットアップ手順は [インストール](./installation.md) と [クイックスタート](./quick-start.md) を参照してください。

## よく使うワークフロー

- **Zero-code overlay**: setup window から `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` を作成し、PerfMeter を auto-start させます。
- **Runtime API**: `PerformanceMeter.EnsureRunning()` を呼び出し、immutable status、metrics、device、camera、session snapshots を読み取ります。
- **Session export**: bounded profiling windows を記録し、scene、device、camera、settings、counters、warnings、worst-frame metadata 付きで JSON/CSV を export します。
- **Overdraw diagnostics**: URP renderer feature がインストールされている場合に、bounded numerical measurement を要求するか visual heatmap を有効化します。HDRP は overdraw/heatmap を unsupported として報告します。
- **MCP automation**: MCP command metadata を使って collection start、overlay mode switching、session export、alert inspection、snapshot read を行います。

[ワークフロー](./workflows.md)、[API](./api.md)、[MCP](./mcp.md) を参照してください。

## スクリーンショット

default overlay preset、setup window pages、visual presets、runtime widgets は screenshot galleries で確認できます。

[Visual Presets](./presets.md)、[Setup Window Screenshots](./setup-window-screenshots.md)、[Implemented Widgets](./widgets.md)、[Screenshots](./screenshots.md) から始めてください。

## FPS カウンターとの比較

Advanced FPS Counter と Graphy は、汎用の drop-in visual overlay として優れています。SGG PerfMeter は、modern Unity URP/HDRP diagnostics に意図的に焦点を絞っています。対象は structured timing と render counters、bottleneck classification、reproducible sessions、device/camera snapshots、URP overdraw diagnostics、URP Render Graph state、HDRP Custom Pass state、MCP/API automation です。

[比較](./comparison.md) は、測定済み runtime benchmark data ではなく product と architecture の文脈として使用してください。

## 要件

- supported runtime usage には Unity `6000.4+`。
- Render Graph path を使う URP `17.4+`、または HDRP `17.4+` Custom Pass integration。
- build で FrameTimingManager に依存する前に Frame Timing Stats を有効化。
- GPU timing が重要な Android では Vulkan を推奨。

Unity `2022.3` から `6000.3` は compile checks 用に import-safe な場合がありますが、runtime overlay、render integration、overdraw passes、support expectations は Unity `6000.4+` と URP `17.4+` または HDRP `17.4+` を対象にしています。HDRP overdraw/heatmap は unsupported ですが、core diagnostics は利用できます。

## ライセンス

この package は **Stinger Royalty-Free EULA 1.0** の下でライセンスされています。

- 正本のライセンステキスト: [LICENSE.ru.md](../../LICENSE.ru.md)
- 英語の参考翻訳: [LICENSE.md](../../LICENSE.md)
- Notices: [NOTICE.md](../../NOTICE.md) と [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy: [brand.md](./brand.md)
