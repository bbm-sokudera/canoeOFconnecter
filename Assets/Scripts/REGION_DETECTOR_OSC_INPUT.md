# RegionDetector OSC入力モード

## 概要

RegionDetectorに、FrameSourceからの深度データの代わりに、OSCメッセージで領域のアクティブ状態を受信する機能を追加しました。

## 新機能

### 1. 入力ソースの切り替え

Inspectorで入力ソースを切り替えることができます：

- **Use Frame Source**: チェックON → FrameSourceから深度データを使用（従来の動作）
- **Use Frame Source**: チェックOFF → OSCメッセージから領域情報を受信

### 2. OSC入力設定

OSC入力モード（Use Frame Source = OFF）の時、以下の設定が有効になります：

#### Inspector設定項目

- **OSC Receive Port**: OSC受信ポート番号（デフォルト: 7003）
- **Region Active Address**: 領域アクティブ化を受信するOSCアドレス（デフォルト: /region/active）

#### OSCメッセージ形式

```
/region/active [領域番号]
```

**例**:
```
/region/active 0  → 領域0をアクティブ化
/region/active 1  → 領域1をアクティブ化
/region/active 2  → 領域2をアクティブ化
/region/active 3  → 領域3をアクティブ化
```

## セットアップ方法

### FrameSourceモード（従来の動作）

1. Inspector設定:
   - ☑ **Use Frame Source**: チェックON
   - **Frame Source**: OrbbecFrameSourceを割り当て

2. 深度カメラから自動的に領域を検出

### OSC入力モード（新機能）

1. Inspector設定:
   - ☐ **Use Frame Source**: チェックOFF
   - **OSC Receive Port**: 7003
   - **Region Active Address**: /region/active
   - **OSC Manager**: OSCManagerコンポーネントを割り当て

2. 外部アプリケーションからOSCメッセージを送信

## 動作の詳細

### OSC入力モードの動作

1. **領域アクティブ化の受信**:
   - `/region/active [領域番号]` を受信すると、その領域がアクティブ化されます
   - 複数の領域を連続して送信することでシーケンスを作成できます

2. **シーケンス検出**:
   - FrameSourceモードと同じシーケンス検出ロジックが適用されます
   - 定義されたシーケンスが検出されると `/paddle` メッセージを送信します

3. **タイムアウト**:
   - OSC入力が0.2秒以上ない場合、アクティブ領域リストがクリアされます
   - これにより、古いアクティブ状態が残らないようにします

### シーケンス定義（変更なし）

| 開始領域 | 終了領域 | Paddle番号 | 説明 |
|---------|---------|-----------|------|
| 2 | 3 | 1 | 右前進 |
| 0 | 1 | 2 | 左前進 |
| 3 | 2 | 3 | 右後進 |
| 1 | 0 | 4 | 左後進 |

## 使用例

### 例1: 外部センサーからの入力

外部のセンサーシステム（TouchDesigner、Max/MSPなど）から領域の検出情報をOSCで送信:

```python
# Pythonの例
from pythonosc import udp_client

client = udp_client.SimpleUDPClient("127.0.0.1", 7003)

# 領域2がアクティブ
client.send_message("/region/active", 2)
time.sleep(0.2)

# 領域3がアクティブ → シーケンス2->3が検出され、/paddle 1が送信される
client.send_message("/region/active", 3)
```

### 例2: 機械学習モデルからの入力

機械学習モデル（姿勢推定など）の出力をOSCでRegionDetectorに送信:

```python
# 姿勢推定の結果から領域を判定
def detect_region_from_pose(pose_data):
    # 姿勢データから領域番号を計算
    region = calculate_region(pose_data)

    # RegionDetectorに送信
    client.send_message("/region/active", region)
```

### 例3: 手動テスト

対話的にシーケンスをテストする:

```bash
python Assets/Sample/region_detector_test.py
```

## テスト方法

### Pythonテストスクリプトを使用

```bash
pip install python-osc
python Assets/Sample/region_detector_test.py
```

テストメニューから選択:
1. 各シーケンスの個別テスト
2. 無効なシーケンスのテスト
3. 連続シーケンスのテスト
4. 対話モード（手動でテスト）

### 対話モードの使い方

```
コマンド> 2        # 領域2をアクティブ化
>>> [SEND] /region/active 2

コマンド> 3        # 領域3をアクティブ化
>>> [SEND] /region/active 3
<<< [PADDLE RECEIVED] /paddle = 1  # シーケンス検出！

コマンド> s 0 1    # シーケンステスト: 領域0->1
```

## OSC通信フロー

```
外部アプリ                RegionDetector           OSCManager
   |                           |                       |
   | /region/active 2          |                       |
   |-------------------------->|                       |
   |                           | (領域2アクティブ化)    |
   |                           |                       |
   | /region/active 3          |                       |
   |-------------------------->|                       |
   |                           | (シーケンス2->3検出)   |
   |                           |                       |
   |                           | SendPaddleOSC(1)      |
   |                           |---------------------->|
   |                           |                       |
   |                           |                       | /paddle 1
   |                           |                       |-----------> 外部へ送信
```

## トラブルシューティング

### OSCメッセージが受信されない

1. **ポート番号の確認**:
   - 送信側と受信側で同じポート番号（デフォルト: 7003）を使用しているか確認

2. **Use Frame Sourceの設定**:
   - OSC入力を使用する場合は、必ずチェックを外す

3. **ファイアウォールの確認**:
   - ポート7003がブロックされていないか確認

4. **コンソールログの確認**:
   - Unityコンソールに `[RegionDetector] OSC Receiver initialized on port 7003` が表示されているか確認

### シーケンスが検出されない

1. **領域番号の確認**:
   - 有効な領域番号（0-3）を送信しているか確認

2. **シーケンス定義の確認**:
   - 送信している領域の組み合わせが定義済みシーケンスに一致しているか確認

3. **タイミングの確認**:
   - 領域の送信間隔が `sequenceMaxTimeGap`（デフォルト: 0.5秒）以内か確認

4. **OSCManagerの確認**:
   - OSCManagerが正しく設定され、割り当てられているか確認

### Paddleメッセージが送信されない

1. **OSCManagerの設定**:
   - OSCManagerの `Transmit Host` と `Transmit Port` が正しいか確認

2. **受信側の確認**:
   - 受信側（外部アプリ）が正しいポートでリッスンしているか確認

## 利点

### OSC入力モードを使用する利点

1. **柔軟性**: 任意の入力ソースから領域情報を送信できる
2. **統合性**: TouchDesigner、Max/MSP、機械学習モデルなど、様々なシステムと統合可能
3. **テスト**: 深度カメラなしでシーケンス検出ロジックをテストできる
4. **プロトタイピング**: OSCで手動送信してシーケンスを素早く試せる

### FrameSourceモードを使用する利点

1. **自動検出**: 深度カメラから自動的に領域を検出
2. **リアルタイム**: カメラからの連続的な深度データを処理
3. **統合システム**: OrbbecSDKとの統合が既に完了している

## まとめ

RegionDetectorは、2つのモードで動作可能になりました：

- **FrameSourceモード**: 深度カメラから自動検出（従来の動作）
- **OSC入力モード**: 外部システムから領域情報を受信（新機能）

Inspectorのチェックボックス1つで簡単に切り替えられます。
