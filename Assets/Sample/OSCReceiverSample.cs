using UnityEngine;
using extOSC;

/// <summary>
/// OSC受信のサンプルスクリプト
/// このスクリプトはOSCメッセージを受信してログに出力します
/// </summary>
public class OSCReceiverSample : MonoBehaviour
{
    #region Public Vars

    [Header("OSC Settings")]
    [Tooltip("OSC受信ポート番号")]
    public int receivePort = 7001;

    [Header("OSC Addresses")]
    [Tooltip("float値を受信するOSCアドレス")]
    public string floatAddress = "/float";

    [Tooltip("int値を受信するOSCアドレス")]
    public string intAddress = "/int";

    [Tooltip("string値を受信するOSCアドレス")]
    public string stringAddress = "/string";

    [Tooltip("複数の値を受信するOSCアドレス")]
    public string multipleAddress = "/multiple";

    #endregion

    #region Private Vars

    private OSCReceiver _receiver;

    #endregion

    #region Unity Methods

    void Start()
    {
        // OSC Receiverの作成
        _receiver = gameObject.AddComponent<OSCReceiver>();

        // ローカルポートの設定
        _receiver.LocalPort = receivePort;

        // Float値を受信するバインド
        _receiver.Bind(floatAddress, OnFloatMessageReceived);

        // Int値を受信するバインド
        _receiver.Bind(intAddress, OnIntMessageReceived);

        // String値を受信するバインド
        _receiver.Bind(stringAddress, OnStringMessageReceived);

        // 複数の値を受信するバインド
        _receiver.Bind(multipleAddress, OnMultipleMessageReceived);

        // すべてのメッセージを受信するバインド（ワイルドカード）
        _receiver.Bind("/*", OnAnyMessageReceived);

        Debug.Log($"OSC Receiver started on port {receivePort}");
        Debug.Log($"Listening for messages on addresses:");
        Debug.Log($"  - {floatAddress} (float)");
        Debug.Log($"  - {intAddress} (int)");
        Debug.Log($"  - {stringAddress} (string)");
        Debug.Log($"  - {multipleAddress} (multiple values)");
    }

    void OnDestroy()
    {
        if (_receiver != null)
        {
            _receiver.Close();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Float値のOSCメッセージを受信した時の処理
    /// </summary>
    private void OnFloatMessageReceived(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            float value = message.Values[0].FloatValue;
            Debug.Log($"[OSC] Float received: {value} from address: {message.Address}");
        }
    }

    /// <summary>
    /// Int値のOSCメッセージを受信した時の処理
    /// </summary>
    private void OnIntMessageReceived(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            int value = message.Values[0].IntValue;
            Debug.Log($"[OSC] Int received: {value} from address: {message.Address}");
        }
    }

    /// <summary>
    /// String値のOSCメッセージを受信した時の処理
    /// </summary>
    private void OnStringMessageReceived(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            string value = message.Values[0].StringValue;
            Debug.Log($"[OSC] String received: {value} from address: {message.Address}");
        }
    }

    /// <summary>
    /// 複数の値を持つOSCメッセージを受信した時の処理
    /// </summary>
    private void OnMultipleMessageReceived(OSCMessage message)
    {
        Debug.Log($"[OSC] Multiple values received from address: {message.Address}");
        for (int i = 0; i < message.Values.Count; i++)
        {
            var value = message.Values[i];
            Debug.Log($"  [{i}] Type: {value.Type}, Value: {value}");
        }
    }

    /// <summary>
    /// すべてのOSCメッセージを受信した時の処理（デバッグ用）
    /// </summary>
    private void OnAnyMessageReceived(OSCMessage message)
    {
        // この関数は全てのメッセージに対して呼ばれるため、
        // 必要に応じてコメントアウトしてください
        // Debug.Log($"[OSC] Message received: {message.Address} with {message.Values.Count} values");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 受信ポートを動的に変更する
    /// </summary>
    public void ChangeReceivePort(int newPort)
    {
        if (_receiver != null)
        {
            _receiver.LocalPort = newPort;
            receivePort = newPort;
            Debug.Log($"OSC Receiver port changed to {newPort}");
        }
    }

    #endregion
}
