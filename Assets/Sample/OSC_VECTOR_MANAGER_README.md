# OSCVectorManager - ベクトルベースOSC監視マネージャー

## 概要

OSCVectorManagerは、x, y, z座標から移動ベクトルを計算し、ベクトルの方向と大きさに基づいて判定を行うOSC監視システムです。

従来のOSCManager（単一軸の値監視）とは異なり、3次元空間での**移動の方向と大きさ**を検出します。

## 主な機能

### 1. ベクトル計算
- 前回の位置と現在の位置から**移動ベクトル**を自動計算
- ベクトルの**大きさ（magnitude）**と**方向**を判定

### 2. 大きさの閾値設定
- `Magnitude Threshold`：ベクトルの大きさがこの値以上の時のみ検出
- 小さい移動やノイズを無視できる

### 3. 3つのベクトルモード
- **Horizontal 2D**：水平面（X-Z平面）での移動を検出
- **Vertical 2D**：垂直面（X-Y平面）での移動を検出
- **Full 3D**：3次元空間での移動を検出

### 4. 方向判定
- **左右**：Left (左)、Right (右)
- **上下**：Up (上)、Down (下)
- **前後**：Forward (前)、Backward (後)

### 5. カスタマイズ可能な送信値
各方向に対して送信する値をInspectorで設定可能

## OSCManagerとの比較

| 機能 | OSCManager | OSCVectorManager |
|------|------------|------------------|
| 監視対象 | 単一軸の値の変化 | 移動ベクトル |
| 判定基準 | 値の増減 | ベクトルの方向と大きさ |
| 用途 | 1次元的な動き | 2D/3D空間での移動 |
| 閾値 | 値の変化量 | ベクトルの大きさ |
| 方向検出 | 正/負のみ | 6方向（左右上下前後） |

## セットアップ方法

### 1. GameObjectの作成

1. Unity Editorで空のGameObjectを作成
2. `OSCVectorManager.cs`スクリプトをアタッチ

### 2. Inspector設定

#### Receiver Settings（受信設定）
- **Receive Port**: OSC受信ポート（デフォルト: 7001）
- **Receive Address**: 位置情報を受信するOSCアドレス（デフォルト: /position）

#### Transmitter Settings（送信設定）
- **Transmit Host**: OSC送信先IPアドレス（デフォルト: 127.0.0.1）
- **Transmit Port**: OSC送信ポート（デフォルト: 7002）
- **Transmit Address**: 値を送信するOSCアドレス（デフォルト: /vector）

#### Vector Settings（ベクトル設定）
- **Vector Mode**: ベクトル判定モード
  - `Horizontal2D`: X-Z平面（水平）
  - `Vertical2D`: X-Y平面（垂直）
  - `Full3D`: 3次元空間
- **Magnitude Threshold**: ベクトルの大きさの最小閾値（デフォルト: 0.1）

#### Direction Value Settings（方向値設定）

**Horizontal（水平）:**
- **Left Value**: 左方向に移動した時に送信する値（デフォルト: 0）
- **Right Value**: 右方向に移動した時に送信する値（デフォルト: 1）

**Vertical（垂直）:**
- **Up Value**: 上方向に移動した時に送信する値（デフォルト: 2）
- **Down Value**: 下方向に移動した時に送信する値（デフォルト: 3）

**Depth（奥行き）:**
- **Forward Value**: 前方向に移動した時に送信する値（デフォルト: 4）
- **Backward Value**: 後方向に移動した時に送信する値（デフォルト: 5）

#### Advanced Settings（詳細設定）
- **Angle Threshold**: 角度の閾値（度）、この角度以内なら主方向として判定（デフォルト: 45°）
- **Enable Debug Log**: デバッグログを出力するか（デフォルト: true）
- **Show Vector Gizmo**: Scene ViewでベクトルをGizmoで表示（デフォルト: true）

### 3. 実行

Playボタンを押してシーンを実行します。

## 動作の仕組み

### ベクトル計算

```
位置1: (x1, y1, z1)
位置2: (x2, y2, z2)

移動ベクトル = (x2-x1, y2-y1, z2-z1)
大きさ = √((x2-x1)² + (y2-y1)² + (z2-z1)²)
```

### 判定フロー

```
1. OSCで位置を受信: (x, y, z)
   ↓
2. 前回位置と比較して移動ベクトルを計算
   ↓
3. モードに応じてベクトルを処理
   - Horizontal 2D: Y軸を無視 → (x, 0, z)
   - Vertical 2D: Z軸を無視 → (x, y, 0)
   - Full 3D: そのまま → (x, y, z)
   ↓
4. ベクトルの大きさを計算
   ↓
5. 大きさが閾値以上？
   ├─ NO → 処理をスキップ
   └─ YES → 次へ
   ↓
6. ベクトルの方向を判定
   - 6つの主要方向（左右上下前後）との角度を計算
   - 最も近い方向を選択
   ↓
7. 角度が閾値以内？
   ├─ NO → 方向不明、送信しない
   └─ YES → 次へ
   ↓
8. 方向に応じた値を送信
```

## 使用例

### 例1: 水平面での手の動き検出

```
設定:
- Vector Mode: Horizontal2D
- Magnitude Threshold: 0.15
- Left Value: 0
- Right Value: 1

動作:
(0, 0, 0) → (1, 0.1, 0)
  移動ベクトル: (1, 0, 0) ← Y軸無視
  大きさ: 1.0 > 閾値0.15 ✓
  方向: 右 → 送信: 1

(1, 0.1, 0) → (0.95, 0.12, 0)
  移動ベクトル: (-0.05, 0, 0)
  大きさ: 0.05 < 閾値0.15 ✗
  → 送信しない（微小な動き）
```

### 例2: ジェスチャー認識（上下左右）

```
設定:
- Vector Mode: Vertical2D
- Magnitude Threshold: 0.2
- Left Value: 10
- Right Value: 11
- Up Value: 20
- Down Value: 21

用途: スワイプジェスチャーの検出
- 左スワイプ → 10
- 右スワイプ → 11
- 上スワイプ → 20
- 下スワイプ → 21
```

### 例3: 3D空間でのナビゲーション

```
設定:
- Vector Mode: Full3D
- Magnitude Threshold: 0.3
- Left Value: 0 (左回転)
- Right Value: 1 (右回転)
- Forward Value: 4 (前進)
- Backward Value: 5 (後退)
- Up Value: 2 (上昇)
- Down Value: 3 (降下)

用途: VR/ARでのナビゲーション制御
```

### 例4: 深度カメラでの手の方向検出

```
設定:
- Vector Mode: Horizontal2D
- Magnitude Threshold: 0.1
- Angle Threshold: 30° (より厳密な方向判定)

動作:
テーブル上での手の移動を検出
小さな手ぶれは無視（閾値0.1）
明確な方向のみ検出（角度閾値30°）
```

## テスト方法

### Pythonテストスクリプトを使用

```bash
pip install python-osc
python Assets/Sample/osc_vector_test.py
```

テストメニュー:
1. **左右移動テスト**: 水平移動の検出
2. **大きさ閾値テスト**: 閾値による検出/無視の確認
3. **上下移動テスト**: 垂直移動の検出
4. **3D移動テスト**: 3次元移動の検出
5. **円運動テスト**: 連続的な方向変化
6. **ジグザグ移動テスト**: 方向の切り替わり
7. **対話モード**: 手動テスト
8. **基本テスト実行**: 自動テスト

### 対話モードの使い方

```
コマンド> pos 1 0 0      # 位置(1,0,0)に移動
>>> [SEND] /position (1.000, 0.000, 0.000)
<<< [RECEIVED] /vector = 1  # 右方向検出

コマンド> move -2 0 0    # 相対移動(-2,0,0)
>>> [SEND] /position (-1.000, 0.000, 0.000)
<<< [RECEIVED] /vector = 0  # 左方向検出

コマンド> show           # 現在の状態表示
受信した値: [1, 0]
現在位置: (-1.000, 0.000, 0.000)
```

## ベクトルモードの詳細

### Horizontal 2D (水平2D)

**対象平面**: X-Z平面（Y軸を無視）

**検出方向**:
- Left: X軸負方向
- Right: X軸正方向
- Forward: Z軸正方向
- Backward: Z軸負方向

**用途**:
- テーブル上での手の動き
- 床面での移動
- 水平ジェスチャー認識

### Vertical 2D (垂直2D)

**対象平面**: X-Y平面（Z軸を無視）

**検出方向**:
- Left: X軸負方向
- Right: X軸正方向
- Up: Y軸正方向
- Down: Y軸負方向

**用途**:
- 壁面でのジェスチャー
- スクリーン上のスワイプ
- 上下左右のコマンド入力

### Full 3D (完全3D)

**対象空間**: X-Y-Z空間（すべての軸を使用）

**検出方向**: 6方向すべて
- Left/Right (X軸)
- Up/Down (Y軸)
- Forward/Backward (Z軸)

**用途**:
- VR/AR空間でのナビゲーション
- 3Dモデリングツール
- 飛行シミュレーション

## 角度閾値について

`Angle Threshold`は、ベクトルが主要方向からどれだけ離れていても検出するかを制御します。

```
例: Angle Threshold = 45°

ベクトルが右方向(1,0,0)から45°以内なら「右」と判定

  45°
   ↗
  → (右)
   ↘
  45°

範囲外のベクトル → Direction.None → 送信しない
```

**設定の目安**:
- **30°**: 厳密な方向判定（明確な動きのみ検出）
- **45°**: 標準（デフォルト）
- **60°**: 緩い判定（斜め方向も含む）

## Gizmo表示

Scene Viewでベクトルを視覚的に確認できます：

- **水色の線**: 移動ベクトル
- **緑のワイヤー球**: 閾値（ベクトルの大きさが球より大きい→検出）
- **赤のワイヤー球**: ベクトルが閾値未満の状態

## API リファレンス

### パブリックメソッド

#### SetVectorMode(VectorMode mode)
ベクトルモードを変更します。

```csharp
oscVectorManager.SetVectorMode(OSCVectorManager.VectorMode.Full3D);
```

#### SetMagnitudeThreshold(float threshold)
大きさの閾値を変更します。

```csharp
oscVectorManager.SetMagnitudeThreshold(0.2f);
```

#### ResetPosition()
前回位置をリセットします。

```csharp
oscVectorManager.ResetPosition();
```

#### GetMovementVector()
現在の移動ベクトルを取得します。

```csharp
Vector3 vector = oscVectorManager.GetMovementVector();
```

#### GetMagnitude()
現在のベクトルの大きさを取得します。

```csharp
float magnitude = oscVectorManager.GetMagnitude();
```

### イベント

#### onValueSent (UnityEvent<int>)
値を送信した時に発火します。

```csharp
oscVectorManager.onValueSent.AddListener((value) => {
    Debug.Log($"Sent: {value}");
});
```

#### onVectorDetected (UnityEvent<Direction, float>)
ベクトルを検出した時に発火します。

```csharp
oscVectorManager.onVectorDetected.AddListener((direction, magnitude) => {
    Debug.Log($"Direction: {direction}, Magnitude: {magnitude}");
});
```

## トラブルシューティング

### ベクトルが検出されない

1. **大きさ閾値の確認**: `Magnitude Threshold`が大きすぎないか確認
2. **デバッグログの確認**: `Enable Debug Log`をONにして詳細を確認
3. **Gizmoの確認**: Scene Viewでベクトルが表示されているか確認
4. **初回位置**: 初回の位置送信後、2回目から検出が始まります

### 期待と異なる方向が検出される

1. **モードの確認**: `Vector Mode`が適切か確認
2. **角度閾値の調整**: `Angle Threshold`を調整
3. **座標系の確認**: UnityのY-up座標系に注意

### 値が送信されすぎる

1. **閾値を上げる**: `Magnitude Threshold`を大きくする
2. **角度閾値を厳しくする**: `Angle Threshold`を小さくする

## まとめ

OSCVectorManagerは、位置情報から移動ベクトルを自動計算し、方向と大きさに基づいた柔軟な判定を実現します。

**主な利点**:
- ✅ 2D/3D空間での移動検出
- ✅ ノイズ除去（大きさ閾値）
- ✅ 明確な方向判定（角度閾値）
- ✅ 視覚的なデバッグ（Gizmo）
- ✅ 柔軟なカスタマイズ

**用途**:
- ジェスチャー認識
- VR/ARナビゲーション
- モーショントラッキング
- インタラクティブアート
- ゲームコントロール
