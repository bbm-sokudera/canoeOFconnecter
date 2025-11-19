using UnityEngine;
using extOSC;
using System.Collections.Generic;
using System.Linq;

public class OSCTestManager : MonoBehaviour
{
    // 💡 Inspectorで設定可能にする新しいフィールド
    [Header("OSC Transmitter Settings")]
    [SerializeField]
    private string remoteHost = "127.0.0.1"; // デフォルト値
    [SerializeField]
    private int remotePort = 8000; // デフォルト値

    // 🌟 送信レート制限のための新しいフィールド
    [Header("Rate Limiter Settings")]
    [Tooltip("レート制限機能を有効にするか")]
    [SerializeField]
    private bool enableRateLimiter = true; // チェックボックスでオンオフ
    [Tooltip("制限時間 (秒)")]
    [SerializeField]
    private float limitTime = 0.2f; // 制限時間
    [Tooltip("制限時間内に許可される最大送信回数")]
    [SerializeField]
    private int limitCount = 3; // 最大送信回数

    private OSCTransmitter _transmitter;

    // 🌟 送信時刻を記録するためのリスト
    // <float>はTime.timeの値を格納
    private List<float> sendTimes = new List<float>();

    // StartとUpdateは空のまま
    void Start()
    {
        
    }

    void Update()
    {
        // 🌟 制限時間外の古い記録を定期的に削除（クリーンアップ）
        if (sendTimes.Count > 0)
        {
            float oldestAllowedTime = Time.time - limitTime;
            // limitTimeより古い時刻をすべて削除
            sendTimes.RemoveAll(time => time < oldestAllowedTime);
        }
    }

    /// <summary>
    /// OSCトランスミッターを初期化します。
    /// </summary>
    public void init()
    {
        // Creating a transmitter.
        _transmitter = gameObject.AddComponent<OSCTransmitter>();

        // 💡 Inspectorで設定した値を使用
        _transmitter.RemoteHost = remoteHost;
        _transmitter.RemotePort = remotePort;

        Debug.Log($"OSC Transmitter Initialized: Host={remoteHost}, Port={remotePort}");
    }

    // 🌟 送信制限のチェック
    private bool CanSend()
    {
        if (!enableRateLimiter)
        {
            return true; // 制限が無効なら常に送信可能
        }

        // 制限時間外の古い記録を削除
        float oldestAllowedTime = Time.time - limitTime;
        // sendTimes.RemoveAll(time => time < oldestAllowedTime); // Updateで実行

        // 制限時間内に発生した送信回数をカウント
        int recentSends = sendTimes.Count; // Updateで古い記録は削除済み
        
        if (recentSends < limitCount)
        {
            return true; // 制限回数内なら送信可能
        }
        else
        {
            return false; // 制限回数を超えているため送信不可
        }
    }

    // 🌟 送信時刻の記録
    private void RecordSendTime()
    {
        sendTimes.Add(Time.time);
    }


    /// <summary>
    /// 任意のOSCメッセージを送信
    /// </summary>
    public void sendOSC(OSCMessage message) {
        if (_transmitter == null)
        {
            Debug.LogError("OSC Transmitter has not been initialized. Call init() first.");
            return;
        }

        // 🌟 送信制限チェック
        if (!CanSend())
        {
            // Debug.LogWarning($"OSC message to {message.Address} skipped due to rate limit.");
            return; // 送信制限によりスキップ
        }

        // 🌟 送信時刻を記録
        RecordSendTime();
        
        _transmitter.Send(message);
    }

    // 💡 /paddle [1-4] 送信機能
    /// <summary>
    /// /paddle [1-4] の形式でOSCメッセージを送信
    /// </summary>
    /// <param name="paddleNumber">パドル番号 (1, 2, 3, 4)</param>
    public void SendPaddleOSC(int paddleNumber)
    {
        if (_transmitter == null)
        {
            Debug.LogError("OSC Transmitter has not been initialized. Call init() first.");
            return;
        }
        
        // 🌟 送信制限チェック
        if (!CanSend())
        {
            // Debug.LogWarning($"OSC message /paddle {paddleNumber} skipped due to rate limit.");
            return; // 送信制限によりスキップ
        }
        
        var message = new OSCMessage("/paddle");
        message.AddValue(OSCValue.Int(paddleNumber));

        // 🌟 sendOSCを経由せずに直接送信処理を行うため、ここでRecordSendTime()を呼び出す
        // 📝 sendOSC()を呼び出してしまうと、中で再度CanSend()が呼ばれ二重チェックになるため、
        // sendOSC()のロジックは使わず直接_transmitter.Send()を呼び出す方が適切です。
        
        RecordSendTime(); // 🌟 送信時刻を記録
        _transmitter.Send(message); // 直接送信
        
        Debug.Log($"OSC Sent: /paddle {paddleNumber}");
    }
}