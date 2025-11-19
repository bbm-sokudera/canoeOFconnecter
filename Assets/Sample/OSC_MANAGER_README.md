# OSCManager - 汎用OSC受信マネージャー

## 概要

OSCManagerは、x, y, z座標を受信して、選択した軸の値の変化方向に応じて異なる整数値を送信する汎用的なOSC受信マネージャーです。

## 主な機能

### 1. 軸の選択
- X, Y, Zのいずれかの軸を監視対象として選択可能
- Inspectorで簡単に切り替え可能

### 2. 値の変化検出
選択した軸の値が負か正かを判断し、その変化方向に応じて異なる値を送信：

#### 負の値の場合
- **より負になった場合（増加）**: `negativeIncreaseValue`を送信（デフォルト: 0）
- **0に近づいた場合（減少）**: `negativeDecreaseValue`を送信（デフォルト: 1）

#### 正の値の場合
- **より正になった場合（増加）**: `positiveIncreaseValue`を送信（デフォルト: 2）
- **0に近づいた場合（減少）**: `positiveDecreaseValue`を送信（デフォルト: 3）

### 3. 完全にカスタマイズ可能
すべての送信値はInspectorで変更可能なため、様々な用途に対応できます。

## セットアップ方法

### 1. GameObjectの作成

1. Unity Editorで空のGameObjectを作成
2. `OSCManager.cs`スクリプトをアタッチ

### 2. Inspector設定

#### Receiver Settings（受信設定）
- **Receive Port**: OSC受信ポート（デフォルト: 7001）
- **Receive Address**: 位置情報を受信するOSCアドレス（デフォルト: /position）

#### Transmitter Settings（送信設定）
- **Transmit Host**: OSC送信先IPアドレス（デフォルト: 127.0.0.1）
- **Transmit Port**: OSC送信ポート（デフォルト: 7002）
- **Transmit Address**: 値を送信するOSCアドレス（デフォルト: /output）

#### Axis Settings（軸設定）
- **Selected Axis**: 監視する軸を選択（X, Y, Z）

#### Negative Value Settings（負の値設定）
- **Negative Increase Value**: 負の値が増加した時に送信する値（デフォルト: 0）
- **Negative Decrease Value**: 負の値が減少した時に送信する値（デフォルト: 1）

#### Positive Value Settings（正の値設定）
- **Positive Increase Value**: 正の値が増加した時に送信する値（デフォルト: 2）
- **Positive Decrease Value**: 正の値が減少した時に送信する値（デフォルト: 3）

#### Conditional Axis Settings（条件軸設定）NEW!
- **Enable Conditional Axis**: 条件軸を有効にする（デフォルト: OFF）
- **Conditional Axis**: 条件軸の選択（X, Y, Z）（デフォルト: Z）
- **Conditional Axis Min**: 条件軸の最小値（デフォルト: -1.0）
- **Conditional Axis Max**: 条件軸の最大値（デフォルト: 1.0）

**条件軸とは？**
メイン監視軸とは別の軸が指定範囲内にある場合のみ、メイン軸の変化を検出する機能です。

**例**：
- メイン軸: X（変化を監視）
- 条件軸: Z（範囲チェック）
- 条件範囲: -1.0 ～ 1.0

→ Zが-1.0～1.0の範囲内にある時だけ、Xの変化を検出して値を送信します。
→ Zが範囲外の時は、Xの変化を無視します。

#### Advanced Settings（詳細設定）
- **Change Threshold**: 値の変化がこの閾値以下の場合は無視（ノイズ除去、デフォルト: 0.001）
- **Enable Debug Log**: デバッグログを出力するか（デフォルト: true）

### 3. 実行

Playボタンを押してシーンを実行します。

## テスト方法

### Pythonテストスクリプトを使用

#### 基本機能のテスト
```bash
pip install python-osc
python Assets/Sample/osc_manager_test.py
```

テストスクリプトには以下のテストが含まれています：

1. **X軸 - 負の値テスト**: 負の値の増減を確認
2. **X軸 - 正の値テスト**: 正の値の増減を確認
3. **Y軸テスト**: Y軸の動作確認
4. **Z軸テスト**: Z軸の動作確認
5. **サインカーブテスト**: 連続的な値の変化を確認
6. **カスタム値テスト**: カスタム送信値の動作確認
7. **対話モード**: 手動でテスト

#### 条件軸機能のテスト NEW!
```bash
pip install python-osc
python Assets/Sample/osc_conditional_axis_test.py
```

条件軸テストスクリプトには以下のテストが含まれています：

1. **条件軸が範囲内の場合**: 条件軸が範囲内にある時、メイン軸の変化を検出
2. **条件軸が範囲外の場合**: 条件軸が範囲外の時、メイン軸の変化を無視
3. **条件軸の境界値テスト**: 境界値（min, max）が正しく処理される
4. **条件軸が範囲をまたぐ場合**: 条件軸が範囲内外を行き来する
5. **異なる条件軸の組み合わせ**: メイン軸と条件軸の様々な組み合わせ
6. **対話モード**: 手動でテスト

## 使用例

### 例1: ジョイスティック入力の方向検出

```
Settings:
- Selected Axis: X
- Negative Increase Value: 0  // 左に倒す
- Negative Decrease Value: 1  // 左から戻る
- Positive Increase Value: 2  // 右に倒す
- Positive Decrease Value: 3  // 右から戻る

動作:
X軸 -1.0 → -2.0  →  送信: 0 (左に倒した)
X軸 -2.0 → -1.0  →  送信: 1 (左から戻した)
X軸  1.0 →  2.0  →  送信: 2 (右に倒した)
X軸  2.0 →  1.0  →  送信: 3 (右から戻した)
```

### 例2: Y軸のジャンプ/しゃがみ検出

```
Settings:
- Selected Axis: Y
- Negative Increase Value: 10  // しゃがみ中
- Negative Decrease Value: 11  // しゃがみから立ち上がり
- Positive Increase Value: 20  // ジャンプ中
- Positive Decrease Value: 21  // ジャンプから降下

動作:
Y軸  0.0 →  1.5  →  送信: 20 (ジャンプ上昇)
Y軸  1.5 →  0.5  →  送信: 21 (ジャンプ降下)
Y軸  0.0 → -0.5  →  送信: 10 (しゃがみ)
Y軸 -0.5 →  0.0  →  送信: 11 (立ち上がり)
```

### 例3: MIDIノート番号として使用

```
Settings:
- Selected Axis: Z
- Negative Increase Value: 60  // Middle C
- Negative Decrease Value: 62  // D
- Positive Increase Value: 64  // E
- Positive Decrease Value: 65  // F

音楽アプリケーションに異なるノート番号を送信
```

### 例4: 条件軸を使用した高度な制御 NEW!

**ケース1: 特定の高さでのみ水平移動を検出**
```
Settings:
- Selected Axis: X（水平方向の移動を監視）
- Enable Conditional Axis: ON
- Conditional Axis: Y（高さ）
- Conditional Axis Min: 0.5
- Conditional Axis Max: 1.5

動作:
→ Y（高さ）が0.5～1.5の範囲内にある時のみ、X（水平移動）を検出
→ 手が適切な高さにある時だけジェスチャーを認識

例:
(x, y, z) = (-1.0, 1.0, 0.0) → Y=1.0は範囲内
(x, y, z) = (-2.0, 1.0, 0.0) → Xの変化を検出 → 送信

(x, y, z) = (-1.0, 2.0, 0.0) → Y=2.0は範囲外
(x, y, z) = (-2.0, 2.0, 0.0) → Xの変化を無視 → 送信しない
```

**ケース2: 特定の深度範囲でのみ動作を検出**
```
Settings:
- Selected Axis: X（左右の動き）
- Enable Conditional Axis: ON
- Conditional Axis: Z（奥行き/深度）
- Conditional Axis Min: -2.0
- Conditional Axis Max: -0.5

動作:
→ Z（深度）が-2.0～-0.5の範囲内（カメラから適切な距離）にある時のみ、
  X（左右の動き）を検出

用途: 深度カメラで、特定の距離範囲内の動きだけを認識
```

**ケース3: Y軸の動きをZ軸の条件で制御**
```
Settings:
- Selected Axis: Y（上下の動き）
- Enable Conditional Axis: ON
- Conditional Axis: Z（前後の位置）
- Conditional Axis Min: -0.5
- Conditional Axis Max: 0.5

動作:
→ 中央付近（Z軸が-0.5～0.5）にいる時のみ、Y軸の上下動作を検出

用途: 特定のゾーン内でのみジェスチャーを有効化
```

## 動作の詳細

### 値の変化判定

1. **初回の値**: 初回は比較対象がないため、送信はスキップされます
2. **2回目以降**: 前回の値と比較して変化を判定

### 変化の判定ロジック

```
現在の値が負の場合:
  if (現在値 < 前回値):
    より負になった → negativeIncreaseValue を送信
  else:
    0に近づいた → negativeDecreaseValue を送信

現在の値が正の場合:
  if (現在値 > 前回値):
    より正になった → positiveIncreaseValue を送信
  else:
    0に近づいた → positiveDecreaseValue を送信

現在の値が0の場合:
  前回の値の符号から判断
```

### ノイズ除去

`Change Threshold`を設定することで、微小な変化を無視できます。
センサーのノイズなど、意図しない小さな変化を除外するのに便利です。

```
例: Change Threshold = 0.01
X軸 1.000 → 1.005  →  変化量0.005 < 閾値0.01 → 送信しない
X軸 1.000 → 1.050  →  変化量0.050 > 閾値0.01 → 送信する
```

## API リファレンス

### パブリックメソッド

#### SetAxis(AxisSelection axis)
監視する軸を動的に変更します。

```csharp
oscManager.SetAxis(OSCManager.AxisSelection.Y);
```

#### SetReceiveAddress(string address)
受信アドレスを動的に変更します。

```csharp
oscManager.SetReceiveAddress("/custom/position");
```

#### SetTransmitTarget(string host, int port)
送信先を動的に変更します。

```csharp
oscManager.SetTransmitTarget("192.168.1.100", 8000);
```

#### ResetPreviousValue()
前回の値をリセットします（テスト用）。

```csharp
oscManager.ResetPreviousValue();
```

#### GetConfigInfo()
現在の設定情報を文字列で取得します。

```csharp
string info = oscManager.GetConfigInfo();
Debug.Log(info);
```

### イベント

#### onValueSent (UnityEvent<int>)
値を送信した時に発火するイベントです。

Inspectorまたはコードから登録できます：

```csharp
oscManager.onValueSent.AddListener((value) => {
    Debug.Log($"Value sent: {value}");
    // カスタム処理
});
```

## トラブルシューティング

### メッセージが受信できない

1. **ポート番号の確認**: 送信側と受信側で同じポート番号を使用しているか確認
2. **ファイアウォールの確認**: ポートがブロックされていないか確認
3. **デバッグログの確認**: `Enable Debug Log`をオンにして詳細ログを確認

### 送信が行われない

1. **初回値の確認**: 初回は比較できないため送信されません（2回目以降から送信）
2. **閾値の確認**: `Change Threshold`が大きすぎないか確認
3. **値の変化の確認**: 実際に値が変化しているか確認

### 期待と異なる値が送信される

1. **軸の確認**: `Selected Axis`が正しく設定されているか確認
2. **送信値の確認**: Inspector の各値設定が意図通りか確認
3. **デバッグログの確認**: ログで詳細な動作を確認

## 応用例

### 1. ゲームコントローラー入力

VRコントローラーやモーションセンサーからの位置情報を受け取り、
移動方向に応じたコマンドを送信

### 2. モーションキャプチャ連携

モーションキャプチャシステムから関節の位置を受け取り、
特定の動作パターンを検出

### 3. 音楽制作

MIDI機器やDAWと連携して、空間的な動きを音楽パラメータに変換

### 4. インタラクティブアート

来場者の位置情報から動きの方向を検出し、
インスタレーションを制御

## ライセンス

このスクリプトはextOSCライブラリを使用しています。
