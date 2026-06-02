# インストール

SGG PerfMeter は現在、`com.sungeargames.perfmeter` という Unity package として配布されています。`2026.6.5-1` release では npm install path は利用できません。

## 要件

- supported runtime usage には Unity `6000.4+`。
- Render Graph path を使う URP `17.4+`。
- UI Toolkit runtime support。
- build で FrameTimingManager に依存する前に Frame Timing Stats を有効化。

Package metadata は、import と compile checks の import-safety floor として Unity `2022.3` を保持しています。現在の supported runtime target は Unity `6000.4+` と URP `17.4+` Render Graph です。

## Git UPM インストール

package はこのリポジトリ内にあります。

```text
Assets/Scripts/SGG.PerfMeter
```

Unity project の `Packages/manifest.json` に追加します。

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Git dependencies に SSH を使う環境では次を使用します。

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

再現可能な install のため、tag または commit に固定します。

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.5-1"
  }
}
```

## ローカルコピーでのインストール

このフォルダーを Unity project にコピーします。

```text
Assets/Scripts/SGG.PerfMeter
```

local package development や Git dependencies を使いたくない場合に有用です。

## 初期プロジェクトセットアップ

次を開きます。

```text
SGG/Perfmeter/Setup
```

その後、recommended setup を実行します。

1. Frame Timing Stats を有効化します。
2. editable active URP renderer assets に `PerfMeterRenderGraphFeature` をインストールします。
3. zero-code setup 用に JSON settings を `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` に保存するか、initialization snippet をコピーします。
4. Play Mode に入り、overlay を確認します。

## サンプル

Package Manager details panel から package samples をインポートします。

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
