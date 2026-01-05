# Multi-Address OSC Configuration Guide

このJSON設定ファイルで、**複数のOSCアドレス**を簡単に管理できます。

## 概要

MultiAddressOSCManagerは、複数の異なるOSCアドレスを1つの設定ファイルで管理できます。

### 対応する仕様

#### 受信 (Receive)
- `/state` (int): 0-5（状態管理）
  - 0: その他
  - 1: キャリブレーション開始
  - 2: キャリブレーション終了
  - 3: チュートリアル
  - 4: クイズ
  - 5: ゲーム

- `/position` (9 floats): 位置・ベクトル・ボックスデータ

#### 送信 (Transmit)
- `/quiz` (int): 0-2
  - 0: 選択していない
  - 1: 右選択
  - 2: 左選択

- `/paddle` (int): 1-4
  - 1: 右前進
  - 2: 左前進
  - 3: 右後進
  - 4: 左後進

- `/cropbox/autoheight` (int): 自動高さ調整

## JSON構造

```json
{
  "receive": [
    {
      "address": "/state",       // OSCアドレス
      "port": 7001,              // 受信ポート
      "parameters": [...]         // パラメータリスト
    }
  ],
  "transmit": [
    {
      "address": "/quiz",        // OSCアドレス
      "host": "127.0.0.1",       // 送信先ホスト
      "port": 7002,              // 送信先ポート
      "parameters": [...]         // パラメータリスト
    }
  ]
}
```

## パラメータの定義

```json
{
  "name": "State",      // コードで使用する名前
  "index": 0,           // OSCメッセージ内のインデックス
  "type": "int"         // データ型（float, int, string）
}
```

## コードでの使用方法

### 受信データの取得

```csharp
// Int値を取得
int state = oscManager.GetInt("State");

// Float値を取得
float vectorY = oscManager.GetFloat("VectorY");

// Vector3として取得
Vector3 position = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
```

### データの送信

```csharp
// 値を設定
oscManager.SetInt("QuizChoice", 1);

// 指定したアドレスに送信
oscManager.SendMessage("/quiz");
```

### 複数アドレスへの送信例

```csharp
// クイズの選択を送信
oscManager.SetInt("QuizChoice", 2);
oscManager.SendMessage("/quiz");

// パドルの方向を送信
oscManager.SetInt("PaddleDirection", 1);
oscManager.SendMessage("/paddle");

// AutoHeightを送信
oscManager.SetInt("AutoHeight", 100);
oscManager.SendMessage("/cropbox/autoheight");
```

## パラメータの追加方法

### 新しい受信アドレスを追加

```json
{
  "address": "/newaddress",
  "port": 7001,
  "parameters": [
    {
      "name": "NewParameter",
      "index": 0,
      "type": "float"
    }
  ]
}
```

### 新しい送信アドレスを追加

```json
{
  "address": "/output",
  "host": "127.0.0.1",
  "port": 7002,
  "parameters": [
    {
      "name": "OutputValue",
      "index": 0,
      "type": "int"
    }
  ]
}
```

## 重要な注意点

### ポートの共有
- **同じポートで複数のアドレスを受信可能**
- 例: `/state`と`/position`は両方ともポート7001で受信

### パラメータ名のユニークさ
- **パラメータ名は全体でユニーク**にする必要があります
- 例: `State`, `QuizChoice`, `PaddleDirection`など

### 送信先の動的変更
- `SendMessage()`を呼ぶたびに、そのアドレスの設定に応じて送信先が変更されます

## トラブルシューティング

### JSONパースエラー
- JSONの構文を確認（カンマ、括弧など）
- [JSONLint](https://jsonlint.com/)で検証

### パラメータが取得できない
- `name`がコードと一致しているか確認
- `type`が正しいか確認（int/float/string）

### 送信できない
- `address`が正しいか確認
- `host`と`port`が正しいか確認

## 設定例

### State値に応じた処理

```csharp
int state = oscManager.GetInt("State");

switch (state)
{
    case 1:
        Debug.Log("キャリブレーション開始");
        break;
    case 4:
        Debug.Log("クイズモード");
        break;
    case 5:
        Debug.Log("ゲームモード");
        break;
}
```

### 位置データの活用

```csharp
Vector3 pos = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
float boxZ = oscManager.GetFloat("BoxZ");
float actualZ = pos.z + boxZ * 0.5f; // ボックスの上端
```
