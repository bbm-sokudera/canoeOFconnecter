# OSC Z軸オシロスコープ

## 概要
OSCで受信したZ軸の値をリアルタイムでオシロスコープ形式のグラフとして可視化するコンポーネントです。
UI.Imageとテクスチャを使って波形を描画し、表示/非表示切り替え時のパフォーマンス負荷を最小化しています。

## 特徴
- ✅ **リアルタイム波形表示**: Z軸の値を左から右へスクロールするグラフで表示
- ✅ **Min-Max範囲オーバーレイ**: 設定した範囲を半透明の矩形で視覚的に表示
- ✅ **パフォーマンス最適化**: 非表示時はデータ収集・描画処理を完全停止
- ✅ **高度なカスタマイズ**: インスペクターから見た目や動作を細かく調整可能
- ✅ **グリッド表示**: オシロスコープらしいグリッド線表示
- ✅ **可変データポイント**: 表示するデータ数を動的に変更可能
- ✅ **可変サンプリングレート**: データ取得間隔を調整可能

## セットアップ手順

### 1. UIの準備
1. Canvas内に新しいGameObjectを作成し、`OSCZAxisOscilloscope`という名前にする
2. `OSCZAxisOscilloscope`コンポーネントをアタッチ

### 2. UI Imageの作成
1. `OSCZAxisOscilloscope`の子オブジェクトとして`Image`を作成
2. この`Image`のRectTransformを調整してグラフ表示エリアを設定
3. `Image`の`Source Image`は空のままでOK（スクリプトが自動設定）

### 3. Toggleの作成
1. `Toggle`コンポーネントを持つUIオブジェクトを作成
2. このToggleで表示/非表示を切り替えます

### 4. コンポーネントの設定

#### Data Source
- **Target Manager**: `OSCXPositionYVectorManager`をアサイン

#### UI References
- **Graph Image**: 上記で作成したImageをアサイン
- **Visibility Toggle**: 上記で作成したToggleをアサイン
- **Min Input Field**: `OSCZAxisController`の最小値InputFieldをアサイン（範囲オーバーレイ用）
- **Max Input Field**: `OSCZAxisController`の最大値InputFieldをアサイン（範囲オーバーレイ用）

#### Graph Settings
- **Max Data Points**: グラフに表示する最大データポイント数（デフォルト: 100）
  - 範囲: 10〜500
  - 例: 100点なら最新100個のデータを表示
- **Sampling Interval**: データ取得間隔（秒）（デフォルト: 1.0）
  - 範囲: 0.01〜5.0秒
  - 例: 1.0なら1秒ごとにZ値を取得

#### Graph Dimensions
- **Graph Width**: グラフの幅（ピクセル）（デフォルト: 512）
- **Graph Height**: グラフの高さ（ピクセル）（デフォルト: 256）

#### Y-Axis Scale
- **Y Min**: Y軸の最小値（デフォルト: -1.0）
- **Y Max**: Y軸の最大値（デフォルト: 1.0）
  - Z値がこの範囲内で正規化されて表示されます

#### Appearance（見た目のカスタマイズ）
- **Background Color**: 背景色（デフォルト: 濃いグレー）
- **Waveform Color**: 波形の線の色（デフォルト: 緑）
- **Line Thickness**: 波形の線の太さ（ピクセル）（デフォルト: 2）
  - 範囲: 1〜5
- **Show Grid**: グリッド線を表示するか（デフォルト: true）
- **Grid Color**: グリッド線の色（デフォルト: グレー）
- **Horizontal Grid Lines**: 横グリッド線の数（デフォルト: 4）
- **Vertical Grid Lines**: 縦グリッド線の数（デフォルト: 10）

#### Range Overlay（範囲オーバーレイ）
- **Range Overlay Color**: Min-Max範囲の塗りつぶし色（デフォルト: 水色、アルファ0.3）
- **Range Border Color**: Min-Max範囲の枠線の色（デフォルト: 水色、アルファ0.8）
- **Range Border Thickness**: 範囲枠線の太さ（ピクセル）（デフォルト: 2）
  - 範囲: 1〜5
  - InputFieldで設定されたMin-Max値が、グラフ上に半透明の矩形として表示されます
  - InputFieldの値を変更すると、リアルタイムで矩形の位置が更新されます

#### Debug
- **Enable Debug Log**: デバッグログを出力するか（デフォルト: false）

## 使い方

### 基本的な使い方
1. セットアップが完了したら、Playモードに入る
2. Toggleをオンにすると、グラフが表示され、リアルタイムでZ値が描画される
3. Toggleをオフにすると、グラフが非表示になり、データ収集・描画処理が停止する

### プログラムから制御する場合

```csharp
public OSCZAxisOscilloscope oscilloscope;

// 表示する
oscilloscope.SetVisible(true);

// 非表示にする
oscilloscope.SetVisible(false);

// データをクリア
oscilloscope.ClearData();

// 現在のデータポイント数を取得
int count = oscilloscope.GetDataPointCount();
```

## パフォーマンス最適化

### 非表示時の動作
Toggleがオフの時：
- ✅ `Update()`内の全処理をスキップ
- ✅ データ収集を停止
- ✅ データをクリア
- ✅ グラフを背景色でクリア
- ✅ CPU負荷をほぼゼロに削減

### メモリ管理
- `OnDestroy()`でテクスチャを適切に破棄
- データポイント数が最大値を超えると古いデータを自動削除
- メモリリークを防止

## カスタマイズ例

### オシロスコープ風（緑色の波形 + 黒背景）
```
Background Color: (0, 0, 0, 1) - 黒
Waveform Color: (0, 1, 0, 1) - 緑
Grid Color: (0.2, 0.2, 0.2, 1) - 暗いグレー
Show Grid: true
```

### モダンUI風（青色の波形 + 白背景）
```
Background Color: (1, 1, 1, 1) - 白
Waveform Color: (0, 0.5, 1, 1) - 青
Grid Color: (0.8, 0.8, 0.8, 1) - 明るいグレー
Show Grid: true
```

### シンプル（波形のみ）
```
Background Color: お好みの色
Waveform Color: お好みの色
Show Grid: false
Line Thickness: 3
```

## トラブルシューティング

### グラフが表示されない
- `Graph Image`が正しくアサインされているか確認
- `Target Manager`が正しくアサインされているか確認
- `Visibility Toggle`がオンになっているか確認
- コンソールにエラーが出ていないか確認

### グラフがカクカクする
- `Sampling Interval`を大きくする（例: 0.1秒 → 0.5秒）
- `Max Data Points`を減らす（例: 200 → 100）
- `Graph Width`と`Graph Height`を小さくする

### Z値が範囲外で見えない
- `Y Min`と`Y Max`をZ値の範囲に合わせて調整
- デバッグログを有効にして実際のZ値を確認

## 技術詳細

### 描画方式
- UI.Image + Texture2D（動的生成）
- Bresenhamアルゴリズムによる直線描画
- ピクセルパーフェクトなフィルタリング

### データ構造
- List<float>でデータポイントを管理
- FIFO（先入れ先出し）方式で古いデータを削除

### 更新頻度
- データ収集: `samplingInterval`ごと
- グラフ描画: 毎フレーム（表示時のみ）

## バージョン履歴
- v1.0 (2024-11-27): 初回リリース
  - オシロスコープ形式グラフ表示
  - 表示/非表示切り替え
  - パフォーマンス最適化
  - カスタマイズ可能な見た目

## ライセンス
このスクリプトはプロジェクトの一部として自由に使用・改変できます。
