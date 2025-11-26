# OSC Z軸ビジュアライザー & コントローラー

## 概要

Z軸の値をリアルタイムで履歴表示し、範囲設定を簡単に行えるツールです。

---

## ファイル構成

### 1. **OSCZAxisVisualizer.cs**
過去3コマ分のZ軸値を履歴表示するビジュアライザー

**主な機能：**
- 最新フレーム：赤色で表示
- 過去1-2コマ：グレーで表示
- Z値をテキストで表示
- 更新頻度を調整可能

### 2. **OSCZAxisController.cs**
Z軸範囲設定を補助するコントローラー

**主な機能：**
- 現在のZ値をリアルタイム表示
- ワンクリックでMin値設定
- ワンクリックでMax値設定
- 入力フィールドからの手動設定

---

## セットアップ手順

### 1. Z軸履歴ビジュアライザーの作成

#### UI構造
```
Canvas
└── Z Axis History Panel
    ├── Current Frame (Panel)
    │   ├── Image (赤色)
    │   └── Text (Z: 0.000)
    ├── History 1 Frame (Panel)
    │   ├── Image (グレー)
    │   └── Text (Z: 0.000)
    └── History 2 Frame (Panel)
        ├── Image (グレー)
        └── Text (Z: 0.000)
```

#### 手順
1. **Panelを3つ作成**（最新、1コマ前、2コマ前）
2. **各Panelに Image と Text を追加**
3. **空のGameObjectを作成**（例：「Z Axis Visualizer」）
4. **OSCZAxisVisualizer をアタッチ**
5. **Inspector設定：**
   - Target Manager: OSCXPositionYVectorManager
   - Current Image: 最新フレームのImage
   - History 1 Image: 1コマ前のImage
   - History 2 Image: 2コマ前のImage
   - Current Text: 最新フレームのText
   - History 1 Text: 1コマ前のText
   - History 2 Text: 2コマ前のText

---

### 2. Z軸範囲設定コントローラーの作成

#### UI構造
```
Canvas
└── Z Axis Settings Panel
    ├── Current Z Display (Text)
    │   "Current Z: 0.000"
    ├── Min Settings (Panel)
    │   ├── Label (Text: "Min")
    │   ├── Input Field
    │   └── Button (Text: "Set Current")
    └── Max Settings (Panel)
        ├── Label (Text: "Max")
        ├── Input Field
        └── Button (Text: "Set Current")
```

#### 手順
1. **現在Z値表示用のTextを作成**
2. **Min設定用のPanel作成**
   - Input Field（数値入力用）
   - Button（現在値設定用）
3. **Max設定用のPanel作成**
   - Input Field（数値入力用）
   - Button（現在値設定用）
4. **空のGameObjectを作成**（例：「Z Axis Controller」）
5. **OSCZAxisController をアタッチ**
6. **Inspector設定：**
   - Target Manager: OSCXPositionYVectorManager
   - Z Axis Visualizer: OSCZAxisVisualizer（作成した場合）
   - Min Input Field: Min用のInputField
   - Set Min Button: Min設定ボタン
   - Max Input Field: Max用のInputField
   - Set Max Button: Max設定ボタン
   - Current Z Value Text: 現在Z値表示用Text

---

## 使い方

### Z軸履歴ビジュアライザー

**自動表示：**
```
センサーから位置情報が送られると自動的に更新
→ 最新のZ値が赤色のImageに表示
→ 過去のZ値がグレーのImageにシフト
```

**表示イメージ：**
```
┌─────────────────────┐
│ 最新  [赤] Z: 0.234 │ ← 現在のZ値
│ 1前  [灰] Z: 0.221 │
│ 2前  [灰] Z: 0.198 │
└─────────────────────┘
```

---

### Z軸範囲設定コントローラー

#### ワンクリック設定の使い方

**Min値の設定：**
```
1. センサーを検出させたい最小の高さに配置
2. 「Set Current」ボタン（Min側）をクリック
3. 現在のZ値がMin入力フィールドに自動入力される
```

**Max値の設定：**
```
1. センサーを検出させたい最大の高さに配置
2. 「Set Current」ボタン（Max側）をクリック
3. 現在のZ値がMax入力フィールドに自動入力される
```

**手動設定：**
```
Input Fieldに直接数値を入力してEnterキーを押す
→ 即座にManagerの設定値に反映
```

---

## レイアウト例

### パターン1：縦並び配置

```
┌─────────────────────────┐
│   Z Axis Monitor        │
├─────────────────────────┤
│ Current Z: 0.234        │
│                         │
│ History:                │
│ ┌─────┐                │
│ │[赤] │ Z: 0.234       │
│ └─────┘                │
│ ┌─────┐                │
│ │[灰] │ Z: 0.221       │
│ └─────┘                │
│ ┌─────┐                │
│ │[灰] │ Z: 0.198       │
│ └─────┘                │
├─────────────────────────┤
│ Range Settings:         │
│ Min: [0.100][Set Cur]   │
│ Max: [0.500][Set Cur]   │
└─────────────────────────┘
```

### パターン2：横並び配置

```
┌──────────────────────────────────────┐
│ Z Axis Monitor                       │
├──────────────────────────────────────┤
│ Current Z: 0.234                     │
│                                      │
│ History:                             │
│ [赤]0.234  [灰]0.221  [灰]0.198     │
│                                      │
│ Range: Min[0.100][Set] Max[0.500][Set]│
└──────────────────────────────────────┘
```

### パターン3：オーバーレイ（画面隅）

```
┌─────────────────────────────────────┐
│        Game View                    │
│                                     │
│  ┌──────────────┐                  │
│  │Z: 0.234 [赤]│                  │
│  │   0.221 [灰]│ ← 右上固定       │
│  │   0.198 [灰]│                  │
│  │Min[0.1][Set]│                  │
│  │Max[0.5][Set]│                  │
│  └──────────────┘                  │
│                                     │
└─────────────────────────────────────┘
```

---

## 設定パラメータ

### OSCZAxisVisualizer

| パラメータ | デフォルト | 説明 |
|---|---|---|
| Update Interval | 0.1秒 | Z値を取得する間隔 |
| Current Color | 赤 | 最新フレームの色 |
| History Color | グレー | 過去フレームの色 |

### OSCZAxisController

| 項目 | 説明 |
|---|---|
| Current Z Value Text | リアルタイムのZ値を表示 |
| Set Min Button | 現在のZ値をMinに設定 |
| Set Max Button | 現在のZ値をMaxに設定 |

---

## 活用シーン

### 1. 検出範囲の調整

```
センサーを実際に動かしながら：
1. 検出したい最低の高さで「Set Min」
2. 検出したい最高の高さで「Set Max」
→ 範囲設定が一瞬で完了！
```

### 2. リアルタイム監視

```
履歴表示を見ながら：
- Z値が安定しているか確認
- ノイズが入っていないか確認
- 検出範囲内にあるか視覚的に判断
```

### 3. デバッグ

```
Z値が想定外の場合：
- 履歴を見て傾向を把握
- 範囲外になるタイミングを特定
- Min/Max設定が適切か確認
```

---

## 統合セットアップ（OSCUIControllerと連携）

OSCUIControllerに統合する場合：

```csharp
OSCUIController の Inspector:

[References]
├── ...（既存の設定）
├── Z Axis Visualizer: OSCZAxisVisualizer
└── Z Axis Controller: OSCZAxisController
```

これにより、全てのOSC UIコンポーネントが一元管理されます。

---

## トラブルシューティング

**Q: Z値が表示されない**
- Target Managerが設定されているか確認
- OSCマネージャーが正しく動作しているか確認

**Q: 履歴が更新されない**
- Update Intervalが長すぎないか確認
- センサーから位置情報が送られているか確認

**Q: Set Currentボタンが効かない**
- Target Managerが設定されているか確認
- Z Axis Visualizerが設定されているか確認（オプション）

**Q: 入力した値が反映されない**
- 入力後にEnterキーを押しているか確認
- 数値形式が正しいか確認（例：0.5）

---

## カスタマイズ例

### 履歴数を増やす

```csharp
OSCZAxisVisualizer.cs の MAX_HISTORY を変更
private const int MAX_HISTORY = 5; // 5コマ分に
```

### 色を変更

```
Inspector > OSCZAxisVisualizer
- Current Color: 任意の色に変更
- History Color: 任意の色に変更
```

### 更新頻度の変更

```
Inspector > OSCZAxisVisualizer
- Update Interval: 0.05（20fps）～ 0.5（2fps）
```

---

## パフォーマンスノート

- 履歴は最大3コマ分のみ保持（メモリ効率的）
- Update Intervalで更新頻度を調整可能
- UI更新は必要な時のみ実行

---

## ライセンス・著作権

このツールは OSCXPositionYVectorManager の補助ツールとして作成されました。
プロジェクトの一部として自由に改変・使用してください。
