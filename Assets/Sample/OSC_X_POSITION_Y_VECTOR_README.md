# OSCXPositionYVectorManager - X位置+Yベクトル方向OSCマネージャー

## 概要

OSCXPositionYVectorManagerは、**X軸の位置（左右）**と**Y軸のベクトル方向（前後）**を組み合わせて判定し、4つの異なる値を送信するOSCマネージャーです。

## 判定ロジック

### 2つの要素を組み合わせる

1. **X軸の位置**：現在のX座標が中心（デフォルト: 0）より左か右か
2. **Y軸のベクトル方向**：移動ベクトルのY成分が前（+）か後（-）か

### 4つの組み合わせ

| X位置 | Y方向 | 送信値 | 説明 |
|-------|-------|--------|------|
| 左 (X < 0) | 前 (Y+) | 0 | Left Forward |
| 右 (X ≥ 0) | 前 (Y+) | 2 | Right Forward |
| 左 (X < 0) | 後 (Y-) | 1 | Left Backward |
| 右 (X ≥ 0) | 後 (Y-) | 3 | Right Backward |

## 動作イメージ

```
        前方向 (Y+)
            ↑
            |
  左(0)    中心    右(1)
  ←--------●--------→ X軸
            |
            ↓
        後方向 (Y-)

例:
位置 (-1, 0, 0) で Y+ 方向に移動 → 0 (Left Forward)
位置 (1, 0, 0) で Y+ 方向に移動 → 2 (Right Forward)
位置 (-1, 0, 0) で Y- 方向に移動 → 1 (Left Backward)
位置 (1, 0, 0) で Y- 方向に移動 → 3 (Right Backward)
```

## セットアップ方法

### 1. GameObjectの作成

1. Unity Editorで空のGameObjectを作成
2. `OSCXPositionYVectorManager.cs`スクリプトをアタッチ

### 2. Inspector設定

#### Receiver Settings（受信設定）
- **Receive Port**: OSC受信ポート（デフォルト: 7001）
- **Receive Address**: 位置情報を受信するOSCアドレス（デフォルト: /position）

#### Transmitter Settings（送信設定）
- **Transmit Host**: OSC送信先IPアドレス（デフォルト: 127.0.0.1）
- **Transmit Port**: OSC送信ポート（デフォルト: 7002）
- **Transmit Address**: 値を送信するOSCアドレス（デフォルト: /direction）

#### Position & Vector Settings（位置とベクトル設定）
- **X Center Position**: X軸の中心位置（デフォルト: 0）
  - この値より小さい → 左
  - この値以上 → 右
- **Y Vector Magnitude Threshold**: Y軸ベクトルの最小閾値（デフォルト: 0.1）
  - Y方向の移動がこの値以上の時のみ判定

#### Direction Value Settings（方向値設定）
- **Left Forward Value**: 左＋前方向の時に送信する値（デフォルト: 0）
- **Right Forward Value**: 右＋前方向の時に送信する値（デフォルト: 2）
- **Left Backward Value**: 左＋後方向の時に送信する値（デフォルト: 1）
- **Right Backward Value**: 右＋後方向の時に送信する値（デフォルト: 3）

#### Advanced Settings（詳細設定）
- **Y Angle Threshold**: Y軸方向の角度閾値（度）（デフォルト: 45°）
- **Enable Debug Log**: デバッグログを出力するか（デフォルト: true）
- **Show Gizmo**: Scene Viewで位置とベクトルをGizmo表示（デフォルト: true）

### 3. 実行

Playボタンを押してシーンを実行します。

## 動作の仕組み

### ステップバイステップ

```
1. OSCで位置を受信: (x, y, z)
   ↓
2. X軸の位置を判定
   if (x < xCenterPosition):
     X位置 = Left
   else:
     X位置 = Right
   ↓
3. 前回位置と比較して移動ベクトルを計算
   移動ベクトル = 現在位置 - 前回位置
   ↓
4. Y軸のベクトル方向を判定
   if (|movementVector.y| < threshold):
     判定しない（閾値未満）
   else if (movementVector.y > 0):
     Y方向 = Forward
   else:
     Y方向 = Backward
   ↓
5. X位置とY方向を組み合わせる
   - Left + Forward → 0
   - Right + Forward → 2
   - Left + Backward → 1
   - Right + Backward → 3
   ↓
6. 対応する値を送信
```

## 使用例

### 例1: 左右の領域で前後移動を検出

```
設定:
- X Center Position: 0
- Y Vector Magnitude Threshold: 0.15

シナリオ: 画面の左右で異なる操作を検出

使用例:
(x, y, z) = (-2, 0, 0)  # 左側に位置
→ Y+方向に移動 → 0 (Left Forward)
→ Y-方向に移動 → 1 (Left Backward)

(x, y, z) = (2, 0, 0)  # 右側に位置
→ Y+方向に移動 → 2 (Right Forward)
→ Y-方向に移動 → 3 (Right Backward)
```

### 例2: パドル操作（左右パドルで前進/後退）

```
設定:
- X Center Position: 0
- Left Forward Value: 0 (左パドル前進)
- Left Backward Value: 1 (左パドル後退)
- Right Forward Value: 2 (右パドル前進)
- Right Backward Value: 3 (右パドル後退)

用途: カヌーやボートのパドル操作シミュレーション
- 左側で前に動かす → 左パドル前進
- 右側で前に動かす → 右パドル前進
- 左側で後ろに動かす → 左パドル後退
- 右側で後ろに動かす → 右パドル後退
```

### 例3: 2つのゾーンでのジェスチャー認識

```
設定:
- X Center Position: 0 (画面中央)
- Y Vector Magnitude Threshold: 0.2

動作:
左ゾーン: 上スワイプ→0、下スワイプ→1
右ゾーン: 上スワイプ→2、下スワイプ→3

用途: 左右で異なる機能を持つジェスチャーUI
```

### 例4: RegionDetectorとの統合

```
RegionDetectorのOSC出力を使用:
- Region 0, 1: 左側 (X < 0)
- Region 2, 3: 右側 (X ≥ 0)

Y軸のベクトルで前後を検出:
- 前に動く → Forward
- 後ろに動く → Backward

組み合わせて4方向の操作を検出
```

## テスト方法

### Pythonテストスクリプトを使用

```bash
pip install python-osc
python Assets/Sample/osc_x_position_y_vector_test.py
```

テストメニュー:
1. **左＋前方向テスト (0)**
2. **右＋前方向テスト (2)**
3. **左＋後方向テスト (1)**
4. **右＋後方向テスト (3)**
5. **Y軸ベクトル閾値テスト**
6. **X軸中心を跨ぐ移動テスト**
7. **ジグザグ移動テスト**
8. **対話モード**
9. **すべてのテスト実行**

### 対話モードの使い方

```
コマンド> left       # X=-1に移動
>>> [SEND] /position (-1.000, 0.000, 0.000)

コマンド> forward    # Y+0.3移動
>>> [SEND] /position (-1.000, 0.300, 0.000)
<<< [RECEIVED] /direction = 0 (Left Forward)

コマンド> right      # X=1に移動
>>> [SEND] /position (1.000, 0.300, 0.000)

コマンド> forward    # Y+0.3移動
>>> [SEND] /position (1.000, 0.600, 0.000)
<<< [RECEIVED] /direction = 2 (Right Forward)
```

## Gizmo表示

Scene Viewで視覚的に確認できます：

- **グレーの縦線**: X軸の中心位置
- **青い球**: X < 0（左側）の位置
- **赤い球**: X ≥ 0（右側）の位置
- **黄色の線**: Y軸のベクトル方向
- **緑/赤のワイヤー球**: Y軸ベクトルの閾値（緑=検出、赤=無視）

## 他のOSCマネージャーとの比較

| マネージャー | 監視内容 | 判定基準 | 出力 |
|-------------|---------|---------|------|
| **OSCManager** | 単一軸の値 | 値の増減 | 4値（正負×増減） |
| **OSCVectorManager** | 移動ベクトル | 方向と大きさ | 6値（6方向） |
| **OSCXPositionYVectorManager** | X位置+Yベクトル | 位置×ベクトル方向 | **4値（左右×前後）** |

### 使い分け

**OSCXPositionYVectorManagerを使う場合（今回実装）**:
- X軸の**現在位置**（左右）で判定を分けたい
- Y軸の**移動方向**（前後）を検出したい
- 左右のゾーンで前後の動きを検出したい
- 4つの明確な組み合わせパターンがある

**OSCVectorManagerを使う場合**:
- すべての方向（左右上下前後）を平等に検出したい
- 移動の方向だけが重要で、現在位置は関係ない

**OSCManagerを使う場合**:
- 単一軸の値の変化だけを見たい
- ベクトルや移動方向は不要

## API リファレンス

### パブリックメソッド

#### SetXCenterPosition(float center)
X軸の中心位置を変更します。

```csharp
oscManager.SetXCenterPosition(0.5f);
```

#### SetYVectorThreshold(float threshold)
Y軸ベクトルの閾値を変更します。

```csharp
oscManager.SetYVectorThreshold(0.2f);
```

#### ResetPosition()
前回位置をリセットします。

```csharp
oscManager.ResetPosition();
```

#### GetMovementVector()
現在の移動ベクトルを取得します。

```csharp
Vector3 vector = oscManager.GetMovementVector();
```

#### GetCurrentXPosition()
現在のX位置（Left/Right）を取得します。

```csharp
var xPos = oscManager.GetCurrentXPosition();
```

#### GetCurrentYDirection()
現在のY方向（Forward/Backward/None）を取得します。

```csharp
var yDir = oscManager.GetCurrentYDirection();
```

### イベント

#### onValueSent (UnityEvent<int>)
値を送信した時に発火します。

```csharp
oscManager.onValueSent.AddListener((value) => {
    Debug.Log($"Sent: {value}");
});
```

#### onDirectionDetected (UnityEvent<CombinedDirection>)
方向を検出した時に発火します。

```csharp
oscManager.onDirectionDetected.AddListener((direction) => {
    Debug.Log($"Direction: {direction}");
});
```

## トラブルシューティング

### 値が送信されない

1. **Y軸閾値の確認**: `Y Vector Magnitude Threshold`が適切か確認
2. **デバッグログの確認**: `Enable Debug Log`をONにして詳細を確認
3. **初回位置**: 初回の位置送信後、2回目から検出が始まります

### 期待と異なる方向が検出される

1. **X中心位置の確認**: `X Center Position`が正しく設定されているか
2. **Y方向の確認**: 移動ベクトルのY成分が十分大きいか
3. **Gizmoの確認**: Scene Viewで位置とベクトルを視覚的に確認

### 左右の判定が逆

1. **X Center Position**: 中心位置が意図通りか確認
2. **座標系**: Unityの座標系（Y-up、右手座標系）に注意

## まとめ

OSCXPositionYVectorManagerは、X軸の位置とY軸の移動方向を組み合わせた4方向の検出を実現します。

**主な特徴**:
- ✅ X軸の位置（左右）を判定
- ✅ Y軸のベクトル方向（前後）を検出
- ✅ 2つを組み合わせて4つの明確なパターン
- ✅ 閾値によるノイズ除去
- ✅ 視覚的なデバッグ（Gizmo）

**用途**:
- 左右パドルの前進/後退操作
- 左右ゾーンでの異なるジェスチャー
- 2つの領域での上下スワイプ
- RegionDetectorとの統合
