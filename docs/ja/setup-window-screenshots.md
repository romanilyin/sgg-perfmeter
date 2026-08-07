# Setup ウィンドウ

Editor ウィンドウは `SGG/Perfmeter/Setup` から開けます。

## 現在の動作

- **Setup** と **Presets** では、永続化された PerfMeter のプロジェクト設定とオーバーレイプリセットのデータを確認できます。スキーマ/バージョン、`legacy` 互換、予約メタデータの各行は読み取り専用で、ウィジェット構成とフォーカスを外したときに正規化される数値も表示します。
- **Runtime** には、セッション、メモリ、graphics-state、render integration、GRD/BRG の診断情報と、オプション統合の機能/状態が読み取り専用で表示されます。`Unavailable`、`unknown`、サンプルなしの状態は明示されたままです。`Measure Overdraw (project default)` はプロジェクト既定の sentinel 値を使用します。
- `Session Analysis`、`Profile Analyzer`、`Refresh` を利用できます。`Start Session` と `Stop Session` は Play Mode でのみ利用できます。Setup を開いたり `Refresh` したりしても、ランタイムの収集は開始されません。
- memory snapshot と graphics-state trace/prewarm のリクエストパラメーターはランタイム専用の入力であり、プロジェクト設定ではありません。

## 参照用スクリーンショット

> 以下のスクリーンショットは P3.5 より前のものです。視覚的な参照としてのみ残しており、完成した Setup UX の現在の根拠ではありません。

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-ja-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-ja-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-ja-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-ja-debug.png)
