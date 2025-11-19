# デバイス接続制御機能

## 概要

`useFrameSource`チェックボックスの状態に応じて、Orbbecセンサーとの接続・接続解除を自動的に制御する機能を実装しました。

## 変更内容

### 1. OrbbecDevice.cs の変更

#### 追加したフィールド
- `autoConnectOnStart` (bool): Start()メソッドで自動的にデバイス接続を開始するかどうかを制御
  - デフォルト値: `true`（既存の動作を維持）

#### 追加したメソッド
- `StartDeviceConnection()`: デバイスへの接続を手動で開始
- `StopDeviceConnection()`: デバイスへの接続を停止し、デバイスリソースを解放

### 2. RegionDetector.cs (NormLowSequenceDetector) の変更

#### 追加したフィールド
- `orbbecDevice` (OrbbecDevice): Orbbecデバイスへの参照（デバイス接続制御用）

#### Start()メソッドの変更
- `useFrameSource = true` の場合:
  - `orbbecDevice.StartDeviceConnection()` を呼び出してデバイス接続を開始
  - FrameSourceから深度データを取得

- `useFrameSource = false` の場合:
  - `orbbecDevice.StopDeviceConnection()` を呼び出してデバイス接続を停止
  - OSC入力モードに切り替え

#### OnDestroy()メソッドの変更
- クリーンアップ時に `orbbecDevice.StopDeviceConnection()` を呼び出してデバイス接続を停止

## Unity Editorでの設定方法

### OrbbecDeviceコンポーネント
1. `Auto Connect On Start` チェックボックスを **オフ** にする
   - これにより、Start()での自動接続が無効化されます
   - RegionDetectorが接続を制御するようになります

### RegionDetectorコンポーネント
1. `Orbbec Device` フィールドに、シーン内の OrbbecDevice コンポーネントを割り当てる
2. `Use Frame Source` チェックボックスで動作モードを切り替える:
   - **ON (チェック有り)**: Orbbecセンサーに接続し、深度データを使用
   - **OFF (チェック無し)**: Orbbecセンサーへの接続を行わず、OSC入力を使用

## 動作確認ポイント

### useFrameSource = true の場合
- コンソールに `[RegionDetector] useFrameSource=true: Starting Orbbec device connection...` と表示される
- その後 `[OrbbecDevice] Starting device connection...` と表示される
- デバイスが見つかると `Device found: ...` と表示される
- 深度データに基づいて領域検出が動作する

### useFrameSource = false の場合
- コンソールに `[RegionDetector] useFrameSource=false: Stopping Orbbec device connection...` と表示される
- デバイスへの接続は行われない
- OSC入力（ポート7003、アドレス `/region/active`）で領域アクティブ化を受信

## 注意事項

1. **OrbbecDeviceコンポーネントの設定**
   - `Auto Connect On Start` を **オフ** にしてください
   - これを忘れると、RegionDetectorの制御とは別に自動接続が開始されます

2. **RegionDetectorコンポーネントの設定**
   - `Orbbec Device` フィールドに必ず OrbbecDevice を割り当ててください
   - 割り当てないと、デバイス接続の制御ができません

3. **実行時のチェックボックス変更**
   - 現在の実装では、Start()でのみ接続制御が行われます
   - 実行中に `useFrameSource` を変更しても、デバイス接続状態は変更されません
   - 実行中の動的な切り替えが必要な場合は、追加の実装が必要です
