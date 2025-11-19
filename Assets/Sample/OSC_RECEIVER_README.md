# OSC受信機能サンプル

このサンプルは、extOSCライブラリを使用してOSC（Open Sound Control）メッセージを受信する機能を実装しています。

## 概要

OSC（Open Sound Control）は、音楽制作ソフトウェアや映像制作ソフトウェア、センサーデバイスなどの間でリアルタイムにデータを送受信するためのプロトコルです。

## セットアップ方法

### 1. GameObjectの作成

1. Unity Editorで新しいSceneを作成または開きます
2. 空のGameObjectを作成します（GameObject > Create Empty）
3. GameObjectに名前を付けます（例: "OSCReceiver"）

### 2. スクリプトのアタッチ

1. 作成したGameObjectに`OSCReceiverSample.cs`スクリプトをアタッチします
2. Inspectorで以下の設定を行います：
   - **Receive Port**: OSCメッセージを受信するポート番号（デフォルト: 7001）
   - **Float Address**: Float値を受信するOSCアドレス（デフォルト: /float）
   - **Int Address**: Int値を受信するOSCアドレス（デフォルト: /int）
   - **String Address**: String値を受信するOSCアドレス（デフォルト: /string）
   - **Multiple Address**: 複数の値を受信するOSCアドレス（デフォルト: /multiple）

### 3. 実行

1. Playボタンを押してシーンを実行します
2. Consoleウィンドウに"OSC Receiver started on port XXXX"というメッセージが表示されます

## テスト方法

### OSCメッセージの送信ツール

OSCメッセージを送信してテストするには、以下のツールが利用できます：

1. **TouchOSC** (iOS/Android)
   - モバイルデバイスからOSCメッセージを送信

2. **OSC/PILOT** (Web)
   - ブラウザベースのOSC送信ツール
   - https://oscpilot.com/

3. **Pure Data** (Windows/Mac/Linux)
   - オープンソースのビジュアルプログラミング環境

4. **Max/MSP** (Windows/Mac)
   - 商用のビジュアルプログラミング環境

### サンプル送信例（Pure Dataの場合）

```
# Float値の送信
send /float 3.14

# Int値の送信
send /int 42

# String値の送信
send /string "Hello OSC"

# 複数の値の送信
send /multiple 1.5 2 "test"
```

## カスタマイズ

### 新しいOSCアドレスの追加

`OSCReceiverSample.cs`の`Start()`メソッド内で新しいバインドを追加できます：

```csharp
_receiver.Bind("/custom/address", OnCustomMessageReceived);
```

対応するコールバック関数を追加：

```csharp
private void OnCustomMessageReceived(OSCMessage message)
{
    // メッセージの処理
    Debug.Log($"Custom message received: {message.Address}");
}
```

### ワイルドカードの使用

OSCアドレスにワイルドカードを使用できます：

- `*`: 任意の文字列にマッチ
- `?`: 任意の1文字にマッチ
- `[]`: 文字のリストにマッチ

例：
```csharp
_receiver.Bind("/sensor/*", OnAnySensorMessage);  // /sensor/1, /sensor/2 など全てにマッチ
_receiver.Bind("/data/?", OnSingleCharData);      // /data/a, /data/b などにマッチ
```

## 応用例

### 1. センサーデータの受信

```csharp
_receiver.Bind("/sensor/gyro", (message) => {
    float x = message.Values[0].FloatValue;
    float y = message.Values[1].FloatValue;
    float z = message.Values[2].FloatValue;
    // ジャイロデータの処理
});
```

### 2. UIコントロール

```csharp
_receiver.Bind("/ui/slider", (message) => {
    float value = message.Values[0].FloatValue;
    // スライダーの値を使用してパラメータを制御
});
```

### 3. トリガーイベント

```csharp
_receiver.Bind("/trigger/start", (message) => {
    // アニメーションやエフェクトをトリガー
    StartCoroutine(SomeAnimation());
});
```

## トラブルシューティング

### メッセージが受信できない場合

1. **ファイアウォールの確認**:
   - ポート7001（または設定したポート）がファイアウォールでブロックされていないか確認

2. **ポート番号の確認**:
   - 送信側と受信側で同じポート番号を使用しているか確認

3. **IPアドレスの確認**:
   - 送信側で正しいIPアドレス（Unity実行中のPCのIPアドレス）を指定しているか確認
   - ローカルテストの場合は`127.0.0.1`または`localhost`を使用

4. **Consoleログの確認**:
   - Unity ConsoleでOSC Receiverが正常に起動しているか確認

## 参考リンク

- extOSC Documentation: https://github.com/Iam1337/extOSC
- OSC Specification: http://opensoundcontrol.org/spec-1_0

## ライセンス

このサンプルコードは、extOSCライブラリを使用しています。
extOSCライブラリのライセンスについては、Assets/extOSC/のLICENSEファイルを参照してください。
