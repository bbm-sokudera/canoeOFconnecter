# OSCXPositionYVectorManager 設定ツール

## 概要

OSCXPositionYVectorManagerの全設定値を取得・設定するためのインターフェーススクリプトです。
GUI調整ツールを作成する際に使用してください。

---

## ファイル構成

### 1. **OSCXPositionYVectorConfig.cs**
設定値の読み書きインターフェース

**主な機能：**
- 全Inspector設定値へのプロパティアクセス
- 設定データの構造化（JSON化可能）
- 設定の保存・読み込み
- リアルタイム状態取得

### 2. **OSCConfigGUISample.cs**
Unity uGUIを使用したGUIツールのサンプル実装

**主な機能：**
- Slider、Toggle、InputFieldからの設定変更
- リアルタイムステータス表示
- PlayerPrefsへの保存・読み込み

---

## 使い方

### 基本セットアップ

1. **Configスクリプトをシーンに配置**
   ```
   - Hierarchy上に空のGameObjectを作成（例：「OSC Config Interface」）
   - OSCXPositionYVectorConfigコンポーネントをアタッチ
   - Target Managerに対象のOSCXPositionYVectorManagerを設定
   ```

2. **設定値にアクセス**
   ```csharp
   // スクリプトから取得
   OSCXPositionYVectorConfig config = GetComponent<OSCXPositionYVectorConfig>();

   // 読み取り
   int port = config.ReceivePort;
   float cooldown = config.CooldownTime;

   // 書き込み
   config.ReceivePort = 7001;
   config.CooldownTime = 0.5f;
   ```

### GUI作成（uGUIの場合）

1. **Canvas作成**
   ```
   - Hierarchy > 右クリック > UI > Canvas
   - UI要素（Slider、Toggle、InputField等）を配置
   ```

2. **OSCConfigGUISampleをアタッチ**
   ```
   - CanvasまたはControllerオブジェクトにOSCConfigGUISampleをアタッチ
   - Configフィールドに OSCXPositionYVectorConfig を設定
   - 各UI ElementsフィールドにUI要素をドラッグ&ドロップ
   ```

3. **実行**
   ```
   - Play モードで実行
   - UIから設定値をリアルタイム変更可能
   - Save/Loadボタンで設定の保存・読み込み
   ```

---

## 全設定項目一覧

### Receiver Settings（受信設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| ReceivePort | int | OSC受信ポート |
| ReceiveAddress | string | 位置情報受信アドレス |

### Transmitter Settings（送信設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| TransmitHost | string | OSC送信先IP |
| TransmitPort | int | OSC送信ポート |
| TransmitAddress | string | 値送信アドレス |

### Position & Vector Settings（位置・ベクトル設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| XCenterPosition | float | X軸中心位置 |
| YVectorMagnitudeThreshold | float | Y軸ベクトル閾値 |

### Direction Value Settings（方向値設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| RightValue | int | 右側の送信値 |
| LeftValue | int | 左側の送信値 |
| RightBackwardValue | int | 右＋後方の送信値 |
| LeftBackwardValue | int | 左＋後方の送信値 |

### Conditional Axis Settings（条件軸設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| EnableConditionalAxis | bool | Z軸条件を有効化 |
| ConditionalAxisMin | float | Z軸最小値 |
| ConditionalAxisMax | float | Z軸最大値 |

### Cooldown Settings（クールダウン設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| CooldownTime | float | 送信間隔（秒） |

### Consecutive Side Detection（左右連続検出）
| プロパティ | 型 | 説明 |
|---|---|---|
| EnableConsecutiveSideDetection | bool | 左右連続検出を有効化 |
| ConsecutiveSideLimit | int | ロック発動の連続回数 |
| OppositeSideRequiredCount | int | ロック解除の必要回数 |

### Consecutive Backward Detection（後進連続検出）
| プロパティ | 型 | 説明 |
|---|---|---|
| EnableConsecutiveBackwardDetection | bool | 後進連続検出を有効化 |
| ConsecutiveBackwardLimit | int | モード移行の連続回数 |
| ForwardRequiredCount | int | モード解除の必要回数 |

### Advanced Settings（詳細設定）
| プロパティ | 型 | 説明 |
|---|---|---|
| YAngleThreshold | float | Y軸角度閾値（度） |
| EnableDebugLog | bool | デバッグログ出力 |
| ShowGizmo | bool | Gizmo表示 |

---

## 便利なメソッド

### 設定の取得・適用
```csharp
// 全設定を構造化データとして取得
ConfigData data = config.GetConfigData();

// 構造化データを適用
config.ApplyConfigData(data);

// JSON化（保存用）
string json = JsonUtility.ToJson(data, true);

// JSONから復元
ConfigData loadedData = JsonUtility.FromJson<ConfigData>(json);
config.ApplyConfigData(loadedData);
```

### リアルタイム状態取得
```csharp
// 現在の移動ベクトル
Vector3 movement = config.GetMovementVector();

// 現在のX位置（Left/Right）
XPosition xPos = config.GetCurrentXPosition();

// 現在のY方向（Forward/Backward/None）
YVectorDirection yDir = config.GetCurrentYDirection();

// 設定情報の文字列
string info = config.GetAllSettings();
```

### リセット
```csharp
// 位置と全カウンターをリセット
config.ResetPosition();
```

---

## カスタマイズ例

### 独自GUIフレームワークでの使用

**例：ImGUIの場合**
```csharp
void OnGUI()
{
    if (config == null) return;

    GUILayout.Label("OSC Settings");

    // Slider例
    GUILayout.Label($"Cooldown Time: {config.CooldownTime:F2}s");
    config.CooldownTime = GUILayout.HorizontalSlider(config.CooldownTime, 0f, 2f);

    // Toggle例
    config.EnableDebugLog = GUILayout.Toggle(config.EnableDebugLog, "Enable Debug Log");

    // Button例
    if (GUILayout.Button("Reset Position"))
    {
        config.ResetPosition();
    }
}
```

### 外部ファイルからの設定読み込み

```csharp
// JSONファイルから読み込み
string json = File.ReadAllText("config.json");
var data = JsonUtility.FromJson<OSCXPositionYVectorConfig.ConfigData>(json);
config.ApplyConfigData(data);

// JSONファイルへ保存
string json = JsonUtility.ToJson(config.GetConfigData(), true);
File.WriteAllText("config.json", json);
```

---

## トラブルシューティング

**Q: プロパティで値を変更したが反映されない**
- Target Managerが正しく設定されているか確認
- Target Managerのコンポーネントが有効か確認

**Q: GetConfigData()がnullを返す**
- Target Managerが設定されていない可能性があります

**Q: GUIサンプルが動かない**
- UI要素が全て正しくアサインされているか確認
- Configフィールドが設定されているか確認

---

## ライセンス・著作権

このツールは OSCXPositionYVectorManager の補助ツールとして作成されました。
プロジェクトの一部として自由に改変・使用してください。
