# OSC UI ビジュアライザー & デバッグコンソール

## 概要

OSC送信に反応してゲームビュー内で視覚的なフィードバックとログを表示するUIシステムです。

---

## ファイル構成

### 1. **OSCValueVisualizer.cs**
OSC送信値に応じてUI-Imageの色を変更するビジュアライザー

**主な機能：**
- 4つの方向（右、左、右後進、左後進）をそれぞれ別のImageで表示
- 送信値に応じて色が光る（フラッシュ効果）
- カスタマイズ可能な色設定
- フェードアウトアニメーション

### 2. **OSCDebugConsole.cs**
ゲームビュー内でConsole風のログ表示を行うスクリプト

**主な機能：**
- Unity標準のDebug.Logをキャプチャ
- OSC送受信イベントの記録
- タイムスタンプ付きログ
- ログレベル別の色分け（Info/Warning/Error/OSC）
- スクロール対応
- 最大行数制限

### 3. **OSCUIController.cs**
ビジュアライザーとコンソールを統合管理するコントローラー

**主な機能：**
- パネルの表示/非表示切替
- リセットボタン
- テスト送信機能
- リアルタイムステータス表示

---

## セットアップ手順

### 基本セットアップ

#### 1. Canvas作成
```
Hierarchy > 右クリック > UI > Canvas
```

#### 2. ビジュアライザーパネルの作成

**パネル構造：**
```
Canvas
└── VisualizerPanel (Panel)
    ├── RightImage (Image) - 右方向（通常）
    ├── LeftImage (Image) - 左方向（通常）
    ├── RightBackwardImage (Image) - 右方向（後進）
    └── LeftBackwardImage (Image) - 左方向（後進）
```

**手順：**
1. Panel作成：`GameObject > UI > Panel`
2. 4つのImage作成：`GameObject > UI > Image`
3. 各Imageを適切な位置に配置
4. 空のGameObjectに`OSCValueVisualizer`をアタッチ
5. Inspector設定：
   - Target Manager: OSCXPositionYVectorManagerを設定
   - 各UI Imageフィールドに対応するImageをドラッグ
   - 色を調整（Active Colors / Inactive Color）

#### 3. デバッグコンソールパネルの作成

**パネル構造：**
```
Canvas
└── ConsolePanel (Panel)
    ├── ScrollView (Scroll View)
    │   └── Viewport
    │       └── Content
    │           └── LogText (Text)
    └── ClearButton (Button)
```

**手順：**
1. Panel作成：`GameObject > UI > Panel`
2. ScrollView作成：`GameObject > UI > Scroll View`
3. ScrollView内のContentに`Text`コンポーネントを追加
   - Textの設定：
     - Alignment: Left & Top
     - Vertical Overflow: Truncate
     - Rich Text: チェックON（色付けに必要）
4. 空のGameObjectに`OSCDebugConsole`をアタッチ
5. Inspector設定：
   - Log Text: 作成したTextを設定
   - Scroll Rect: ScrollViewのScrollRectを設定
   - OSC Manager: OSCXPositionYVectorManagerを設定（オプション）

#### 4. コントローラーのセットアップ（オプション）

1. 空のGameObjectを作成（例：「OSC UI Controller」）
2. `OSCUIController`をアタッチ
3. 各フィールドに対応するコンポーネント/GameObjectを設定
4. ボタンを作成して、各フィールドに設定

---

## 使い方

### ビジュアライザーの使用

**自動反応：**
```
OSCXPositionYVectorManagerがOSC値を送信すると、
自動的に対応するImageの色が変化します。

値 0 → RightImage が光る
値 1 → RightBackwardImage が光る
値 2 → LeftImage が光る
値 3 → LeftBackwardImage が光る
```

**手動テスト：**
```csharp
OSCValueVisualizer visualizer = GetComponent<OSCValueVisualizer>();
visualizer.TestVisualize(0); // 右を光らせる
```

### デバッグコンソールの使用

**自動ログ記録：**
```
- Unity標準のDebug.Logを自動キャプチャ（有効な場合）
- OSC送信時に自動でログ表示
- 方向検出時に自動でログ表示
```

**手動ログ追加：**
```csharp
OSCDebugConsole console = GetComponent<OSCDebugConsole>();

console.LogInfo("情報メッセージ");
console.LogWarning("警告メッセージ");
console.LogError("エラーメッセージ");
console.LogOSCSend("OSC送信メッセージ");
console.LogOSCReceive("OSC受信メッセージ");

// ログクリア
console.ClearLogs();
```

### コントローラーの使用

**パネル制御：**
```csharp
OSCUIController controller = GetComponent<OSCUIController>();

// パネルの表示/非表示
controller.SetPanelVisible("visualizer", true);
controller.SetPanelVisible("console", false);

// ログ追加
controller.AddLog("カスタムメッセージ");
```

---

## カスタマイズ

### 色の変更

**Visualizer:**
```
Inspector > OSCValueVisualizer
- Right Active Color: 右（通常）の色
- Left Active Color: 左（通常）の色
- Right Backward Active Color: 右（後進）の色
- Left Backward Active Color: 左（後進）の色
- Inactive Color: 非アクティブ時の色
```

**Console:**
```
Inspector > OSCDebugConsole
- Info Color: 通常ログの色
- Warning Color: 警告ログの色
- Error Color: エラーログの色
- OSC Send Color: OSC送信ログの色
- OSC Receive Color: OSC受信ログの色
```

### アニメーション設定

**フラッシュ時間の変更：**
```
Inspector > OSCValueVisualizer
- Flash Duration: 光ってから消えるまでの時間（秒）
- Enable Fade Out: フェードアウト効果のON/OFF
```

### ログ設定

**ログフィルタリング：**
```
Inspector > OSCDebugConsole
- Show Info Logs: 通常ログを表示
- Show Warning Logs: 警告ログを表示
- Show Error Logs: エラーログを表示
- Capture Unity Log: Unity標準ログをキャプチャ
```

**ログ行数制限：**
```
Inspector > OSCDebugConsole
- Max Log Lines: 表示する最大ログ行数（デフォルト: 50）
```

---

## UI配置例

### レイアウト例1：横並び

```
┌─────────────────────────────────────┐
│  Visualizer      │    Console       │
│  ┌──┬──┐        │  [Log Text]      │
│  │ L│ R│        │  [Log Text]      │
│  └──┴──┘        │  [Log Text]      │
│  ┌──┬──┐        │  [Clear]         │
│  │LB│RB│        │                  │
│  └──┴──┘        │                  │
└─────────────────────────────────────┘
```

### レイアウト例2：縦並び

```
┌─────────────────────┐
│    Visualizer       │
│    ┌──┬──┐         │
│    │ L│ R│         │
│    └──┴──┘         │
│    ┌──┬──┐         │
│    │LB│RB│         │
│    └──┴──┘         │
├─────────────────────┤
│      Console        │
│  [Log Text]         │
│  [Log Text]         │
│  [Clear]            │
└─────────────────────┘
```

### レイアウト例3：オーバーレイ

```
┌─────────────────────────────────────┐
│                                     │
│          Game View                  │
│                                     │
│  ┌──┬──┐                           │
│  │ L│ R│    ┌─────────────────┐   │
│  └──┴──┘    │ Console (Float) │   │
│  ┌──┬──┐    │ [Log Text]      │   │
│  │LB│RB│    │ [Log Text]      │   │
│  └──┴──┘    └─────────────────┘   │
└─────────────────────────────────────┘
```

---

## トラブルシューティング

**Q: Imageの色が変わらない**
- Target Managerが正しく設定されているか確認
- OSCXPositionYVectorManagerのonValueSentイベントが発火しているか確認
- Imageコンポーネントが正しくアサインされているか確認

**Q: ログが表示されない**
- Log TextのRich Textが有効になっているか確認
- Capture Unity Logが有効か確認
- OSC Managerが設定されているか確認（OSCログの場合）

**Q: ログが途中で切れる**
- Max Log Linesの値を増やす
- TextのVertical Overflowを確認

**Q: スクロールが効かない**
- ScrollRectが正しく設定されているか確認
- Auto Scroll To Bottomが有効か確認

---

## 応用例

### 1. カスタムビジュアライザー

```csharp
public class CustomVisualizer : MonoBehaviour
{
    public OSCValueVisualizer visualizer;

    void Update()
    {
        // 現在の値を取得
        int currentValue = visualizer.GetCurrentActiveValue();

        // カスタム処理
        if (currentValue == 0)
        {
            // 右方向の時の追加処理
        }
    }
}
```

### 2. ログのファイル出力

```csharp
public class LogExporter : MonoBehaviour
{
    public OSCDebugConsole console;

    public void ExportLogs()
    {
        // 独自のログ保存処理を実装
        // console経由でログ情報を取得して保存
    }
}
```

### 3. 統計情報の表示

```csharp
public class OSCStatistics : MonoBehaviour
{
    private int[] valueCounts = new int[4]; // 各値の送信回数

    void Start()
    {
        var manager = GetComponent<OSCXPositionYVectorManager>();
        manager.onValueSent.AddListener(OnValueSent);
    }

    void OnValueSent(int value)
    {
        if (value >= 0 && value < 4)
            valueCounts[value]++;

        // 統計をUIに表示
    }
}
```

---

## パフォーマンスノート

- ログ行数が多いと描画負荷が増加します（Max Log Linesを調整）
- Rich Textの使用は若干パフォーマンスに影響します
- フレームごとにUIを更新する処理は最小限にしています

---

## ライセンス・著作権

このUIツールは OSCXPositionYVectorManager の補助ツールとして作成されました。
プロジェクトの一部として自由に改変・使用してください。
