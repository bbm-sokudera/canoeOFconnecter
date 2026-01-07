# MultiAddressOSCManager セットアップガイド

このガイドでは、MultiAddressOSCManagerと各コントローラーを使った完全なセットアップ方法を説明します。

## 概要

### システム構成

```
MultiAddressOSCManager (データ層)
├─ OSC送受信の管理
└─ JSON設定でパラメータ管理

↓ データを提供

コントローラー層
├─ StateController (State管理)
├─ PaddleController (パドル操作)
├─ QuizController (クイズ選択)
└─ AutoHeightController (AutoHeight送信)
```

---

## ステップ1: Hierarchyの構成

### 1-1. 空のGameObjectを作成

```
Hierarchy:
├─ OSC System (空のGameObject)
│  ├─ OSC Manager
│  ├─ State Controller
│  ├─ Paddle Controller
│  ├─ Quiz Controller
│  └─ AutoHeight Controller
└─ UI (デバッグコンソールなど)
```

### 1-2. 各GameObjectにコンポーネントをアタッチ

#### OSC Manager
```
1. 空のGameObject作成 → 名前: "OSC Manager"
2. Add Component → MultiAddressOSCManager
3. Config File Path: "multi_osc_config.json"
4. Enable Debug Log: ✓ チェック
```

#### State Controller
```
1. 空のGameObject作成 → 名前: "State Controller"
2. Add Component → StateController
3. OSC Manager: OSC Manager をドラッグ
4. Enable Debug Log: ✓ チェック
```

#### Paddle Controller
```
1. 空のGameObject作成 → 名前: "Paddle Controller"
2. Add Component → PaddleController
3. OSC Manager: OSC Manager をドラッグ
4. 設定:
   - Vector Threshold: 0.1
   - X Center Position: 0
   - Cooldown Time: 0.3
   - Enable Conditional Axis: 必要に応じて
   - Conditional Axis Min: 0
   - Conditional Axis Max: 2.0
   - Ignore Forward Backward: true
```

#### Quiz Controller
```
1. 空のGameObject作成 → 名前: "Quiz Controller"
2. Add Component → QuizController
3. OSC Manager: OSC Manager をドラッグ
4. State Controller: State Controller をドラッグ
5. 設定:
   - X Threshold: 0
   - Confirm Key: Space
   - Auto Select Mode: false (手動選択の場合)
```

#### AutoHeight Controller
```
1. 空のGameObject作成 → 名前: "AutoHeight Controller"
2. Add Component → AutoHeightController
3. OSC Manager: OSC Manager をドラッグ
4. Auto Height Value: 100 (初期値)
```

---

## ステップ2: JSON設定の確認

**Assets/StreamingAssets/multi_osc_config.json** を確認：

```json
{
  "receive": [
    {
      "address": "/state",
      "port": 7001,
      "parameters": [
        { "name": "State", "index": 0, "type": "int" }
      ]
    },
    {
      "address": "/position",
      "port": 7001,
      "parameters": [
        { "name": "PositionX", "index": 0, "type": "float" },
        ...
      ]
    }
  ],
  "transmit": [
    {
      "address": "/quiz",
      "host": "127.0.0.1",
      "port": 7002,
      "parameters": [
        { "name": "QuizChoice", "index": 0, "type": "int" }
      ]
    },
    ...
  ]
}
```

必要に応じてhost、portを変更してください。

---

## ステップ3: イベントの設定（オプション）

### StateControllerのイベント

```
Inspector → State Controller → Events:

On State Changed:
  - ログ出力や画面切り替えなど

On Calibration Start:
  - キャリブレーション画面を表示

On Quiz Start:
  - クイズUIを表示

On Game Start:
  - ゲーム画面を表示
```

### PaddleControllerのイベント

```
Inspector → Paddle Controller → Events:

On Paddle Sent:
  - パドルのビジュアルフィードバック
  - 音を鳴らす
```

### QuizControllerのイベント

```
Inspector → Quiz Controller → Events:

On Quiz Selected:
  - 選択肢をハイライト
  - 正解判定
```

---

## ステップ4: 動作確認

### 4-1. Playモードで実行

Console に以下のログが表示されることを確認：

```
[MultiAddressOSCManager] Configuration loaded: 2 receive addresses, 3 transmit addresses
[MultiAddressOSCManager] OSC Receiver created on port 7001
[MultiAddressOSCManager] Bound /state on port 7001
[MultiAddressOSCManager] Bound /position on port 7001
[MultiAddressOSCManager] OSC Transmitter initialized: 127.0.0.1:7002
```

### 4-2. OSCメッセージを送信してテスト

**テスト用のOSC送信ツールで:**

```
/state 4  → State=4(Quiz)に変更
/position 0.5 1.0 1.5 0.0 0.2 0.0 0.3 0.4 0.2
  → Position、Vector、Boxデータを送信
```

Consoleに以下が表示される：

```
[StateController] State changed: Other -> Quiz
[MultiAddressOSCManager] Received OSC message on /state with 1 values
[MultiAddressOSCManager] Received OSC message on /position with 9 values
```

### 4-3. 各コントローラーの動作確認

#### Stateの確認
```
/state 1 を送信 → [StateController] State changed: Other -> CalibrationStart
/state 5 を送信 → [StateController] State changed: CalibrationStart -> Game
```

#### Paddleの確認
```
1. /state 5 を送信（ゲームモード）
2. /position で PositionX=0.5, VectorY=0.3 を送信
3. Console: [PaddleController] Paddle sent: 1 (右前進)
```

#### Quizの確認
```
1. /state 4 を送信（クイズモード）
2. /position で PositionX=0.5 を送信
3. Spaceキーを押す
4. Console: [QuizController] Quiz selected: Right
```

---

## ステップ5: 既存システムからの移行

### OSCXPositionYVectorManager を使っている場合

#### 削除するもの
```
- OSCXPositionYVectorManager コンポーネント
- OSCZAxisController の targetManager 参照
```

#### 追加するもの
```
- MultiAddressOSCManager
- PaddleController
```

#### 設定の移行
```
OSCXPositionYVectorManager の設定:
  yVectorMagnitudeThreshold → PaddleController の vectorThreshold
  cooldownTime → PaddleController の cooldownTime
  conditionalAxisMin/Max → PaddleController の conditionalAxisMin/Max
```

---

## トラブルシューティング

### OSCメッセージを受信しない

**確認事項:**
1. JSON設定のportが正しいか
2. 送信側のアドレスとポートが一致しているか
3. Consoleに "Bound /state on port 7001" と表示されているか

### Paddleが送信されない

**確認事項:**
1. State が 5 (Game) になっているか
2. VectorY が threshold (0.1) 以上か
3. Cooldown時間が経過しているか
4. Enable Conditional Axis がONの場合、Z値が範囲内か

### Quizが反応しない

**確認事項:**
1. State が 4 (Quiz) になっているか
2. StateController が接続されているか
3. Spaceキーを押しているか（手動モードの場合）

---

## 応用例

### カスタムロジックの追加

```csharp
public class MyGameController : MonoBehaviour
{
    public MultiAddressOSCManager oscManager;
    public StateController stateController;

    void Update()
    {
        // ゲームモード時のカスタム処理
        if (stateController.IsState(StateController.GameState.Game))
        {
            Vector3 pos = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
            // カスタムロジック...
        }
    }
}
```

### UIとの連携

```csharp
public class QuizUI : MonoBehaviour
{
    public QuizController quizController;
    public Text choiceText;

    void Start()
    {
        // イベント登録
        quizController.onQuizSelected.AddListener(OnQuizSelected);
    }

    void Update()
    {
        // 現在の選択を表示
        var choice = quizController.GetCurrentChoice();
        choiceText.text = $"Current Choice: {choice}";
    }

    void OnQuizSelected(QuizController.QuizChoice choice)
    {
        Debug.Log($"Quiz selected: {choice}");
        // 選択肢をハイライト、アニメーションなど
    }
}
```

---

## まとめ

このシステムの利点:

✅ **JSON設定だけでパラメータ管理**
✅ **複数のOSCアドレスに対応**
✅ **State駆動のモード切り替え**
✅ **疎結合で拡張しやすい**
✅ **デバッグログで動作確認が簡単**

各コントローラーは独立しているので、必要なものだけ使うことができます。
