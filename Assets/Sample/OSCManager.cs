using UnityEngine;
using UnityEngine.Events;
using extOSC;

/// <summary>
/// 汎用OSCマネージャー
/// x,y,z座標を受信して、値の変化方向に応じて異なる値を送信します
/// </summary>
public class OSCManager : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// 監視する軸の選択
    /// </summary>
    public enum AxisSelection
    {
        X,
        Y,
        Z
    }

    #endregion

    #region Public Vars

    [Header("Receiver Settings")]
    [Tooltip("OSC受信ポート")]
    public int receivePort = 7001;

    [Tooltip("位置情報を受信するOSCアドレス")]
    public string receiveAddress = "/position";

    [Header("Transmitter Settings")]
    [Tooltip("OSC送信先IPアドレス")]
    public string transmitHost = "127.0.0.1";

    [Tooltip("OSC送信ポート")]
    public int transmitPort = 7002;

    [Tooltip("値を送信するOSCアドレス")]
    public string transmitAddress = "/output";

    [Header("Axis Settings")]
    [Tooltip("監視する軸を選択")]
    public AxisSelection selectedAxis = AxisSelection.X;

    [Header("Negative Value Settings")]
    [Tooltip("負の値が増加した時（より負になった時）に送信する値")]
    public int negativeIncreaseValue = 0;

    [Tooltip("負の値が減少した時（0に近づいた時）に送信する値")]
    public int negativeDecreaseValue = 1;

    [Header("Positive Value Settings")]
    [Tooltip("正の値が増加した時（より正になった時）に送信する値")]
    public int positiveIncreaseValue = 2;

    [Tooltip("正の値が減少した時（0に近づいた時）に送信する値")]
    public int positiveDecreaseValue = 3;

    [Header("Conditional Axis Settings")]
    [Tooltip("条件軸を有効にする（チェックONで、指定した軸が範囲内の時のみメイン軸の変化を検出）")]
    public bool enableConditionalAxis = false;

    [Tooltip("条件軸の選択（この軸が範囲内にある時のみメイン軸の変化を検出）")]
    public AxisSelection conditionalAxis = AxisSelection.Z;

    [Tooltip("条件軸の最小値（この値以上の時に反応）")]
    public float conditionalAxisMin = -1.0f;

    [Tooltip("条件軸の最大値（この値以下の時に反応）")]
    public float conditionalAxisMax = 1.0f;

    [Header("Advanced Settings")]
    [Tooltip("値の変化がこの閾値以下の場合は無視する（ノイズ除去）")]
    public float changeThreshold = 0.001f;

    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Header("Events")]
    [Tooltip("値を送信した時に呼ばれるイベント")]
    public UnityEvent<int> onValueSent;

    #endregion

    #region Private Vars

    private OSCReceiver _receiver;
    private OSCTransmitter _transmitter;
    private OSCBind _currentBind;
    private float _previousValue = 0f;
    private bool _isFirstValue = true;

    #endregion

    #region Unity Methods

    void Start()
    {
        InitializeReceiver();
        InitializeTransmitter();

        LogDebug($"OSC Manager initialized");
        LogDebug($"Receiving on port {receivePort}, address: {receiveAddress}");
        LogDebug($"Transmitting to {transmitHost}:{transmitPort}, address: {transmitAddress}");
        LogDebug($"Monitoring axis: {selectedAxis}");
    }

    void OnDestroy()
    {
        if (_receiver != null)
        {
            _receiver.Close();
        }

        if (_transmitter != null)
        {
            _transmitter.Close();
        }
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// OSC Receiverの初期化
    /// </summary>
    private void InitializeReceiver()
    {
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = receivePort;
        _currentBind = _receiver.Bind(receiveAddress, OnPositionReceived);
    }

    /// <summary>
    /// OSC Transmitterの初期化
    /// </summary>
    private void InitializeTransmitter()
    {
        _transmitter = gameObject.AddComponent<OSCTransmitter>();
        _transmitter.RemoteHost = transmitHost;
        _transmitter.RemotePort = transmitPort;
    }

    #endregion

    #region OSC Callback Methods

    /// <summary>
    /// 位置情報のOSCメッセージを受信した時の処理
    /// </summary>
    private void OnPositionReceived(OSCMessage message)
    {
        if (message.Values.Count < 3)
        {
            LogDebug($"Invalid message format. Expected 3 values (x,y,z), got {message.Values.Count}");
            return;
        }

        // x, y, z 値を取得
        float x = message.Values[0].FloatValue;
        float y = message.Values[1].FloatValue;
        float z = message.Values[2].FloatValue;

        // 条件軸のチェック（有効な場合）
        if (enableConditionalAxis)
        {
            float conditionalValue = GetSelectedAxisValue(x, y, z, conditionalAxis);

            // 条件軸が範囲外の場合は処理をスキップ
            if (conditionalValue < conditionalAxisMin || conditionalValue > conditionalAxisMax)
            {
                LogDebug($"Conditional axis ({conditionalAxis}) value {conditionalValue:F3} is out of range [{conditionalAxisMin:F3}, {conditionalAxisMax:F3}]. Skipping processing.");
                return;
            }

            LogDebug($"Conditional axis ({conditionalAxis}) value {conditionalValue:F3} is within range [{conditionalAxisMin:F3}, {conditionalAxisMax:F3}]. Processing...");
        }

        // 選択された軸の値を取得
        float currentValue = GetSelectedAxisValue(x, y, z, selectedAxis);

        LogDebug($"Received position: ({x:F3}, {y:F3}, {z:F3}) -> Selected axis ({selectedAxis}): {currentValue:F3}");

        // 値を処理
        ProcessValue(currentValue);

        // 前回の値を更新
        _previousValue = currentValue;
        _isFirstValue = false;
    }

    #endregion

    #region Processing Methods

    /// <summary>
    /// 選択された軸の値を取得
    /// </summary>
    private float GetSelectedAxisValue(float x, float y, float z)
    {
        return GetSelectedAxisValue(x, y, z, selectedAxis);
    }

    /// <summary>
    /// 指定された軸の値を取得
    /// </summary>
    private float GetSelectedAxisValue(float x, float y, float z, AxisSelection axis)
    {
        switch (axis)
        {
            case AxisSelection.X:
                return x;
            case AxisSelection.Y:
                return y;
            case AxisSelection.Z:
                return z;
            default:
                return x;
        }
    }

    /// <summary>
    /// 値を処理して適切な出力を送信
    /// </summary>
    private void ProcessValue(float currentValue)
    {
        // 初回は比較できないのでスキップ
        if (_isFirstValue)
        {
            LogDebug($"First value received: {currentValue:F3}. Waiting for next value to compare.");
            return;
        }

        // 変化量を計算
        float delta = currentValue - _previousValue;

        // 閾値以下の変化は無視
        if (Mathf.Abs(delta) <= changeThreshold)
        {
            LogDebug($"Change ({delta:F3}) is below threshold ({changeThreshold:F3}). Ignoring.");
            return;
        }

        int outputValue;

        // 現在の値が負の場合
        if (currentValue < 0)
        {
            if (delta < 0)
            {
                // より負になった（増加）
                outputValue = negativeIncreaseValue;
                LogDebug($"Negative value increased (more negative): {_previousValue:F3} -> {currentValue:F3}, sending {outputValue}");
            }
            else
            {
                // 0に近づいた（減少）
                outputValue = negativeDecreaseValue;
                LogDebug($"Negative value decreased (closer to 0): {_previousValue:F3} -> {currentValue:F3}, sending {outputValue}");
            }
        }
        // 現在の値が正の場合
        else if (currentValue > 0)
        {
            if (delta > 0)
            {
                // より正になった（増加）
                outputValue = positiveIncreaseValue;
                LogDebug($"Positive value increased (more positive): {_previousValue:F3} -> {currentValue:F3}, sending {outputValue}");
            }
            else
            {
                // 0に近づいた（減少）
                outputValue = positiveDecreaseValue;
                LogDebug($"Positive value decreased (closer to 0): {_previousValue:F3} -> {currentValue:F3}, sending {outputValue}");
            }
        }
        // 現在の値がちょうど0の場合
        else
        {
            // 前回の値から判断
            if (_previousValue < 0)
            {
                outputValue = negativeDecreaseValue;
                LogDebug($"Value reached 0 from negative: {_previousValue:F3} -> {currentValue:F3}, sending {outputValue}");
            }
            else
            {
                outputValue = positiveDecreaseValue;
                LogDebug($"Value reached 0 from positive: {_previousValue:F3} -> {currentValue:F3}, sending {outputValue}");
            }
        }

        // 値を送信
        SendValue(outputValue);
    }

    /// <summary>
    /// OSCで値を送信
    /// </summary>
    private void SendValue(int value)
    {
        var message = new OSCMessage(transmitAddress);
        message.AddValue(OSCValue.Int(value));
        _transmitter.Send(message);

        LogDebug($"[SEND] {transmitAddress} -> {value}");

        // イベントを発火
        onValueSent?.Invoke(value);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// デバッグログを出力
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[OSCManager] {message}");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// カスタムアドレスに整数値を送信
    /// </summary>
    /// <param name="address">OSCアドレス</param>
    /// <param name="value">送信する整数値</param>
    public void SendOSC(string address, int value)
    {
        if (_transmitter == null)
        {
            LogDebug("Transmitter is not initialized");
            return;
        }

        var message = new OSCMessage(address);
        message.AddValue(OSCValue.Int(value));
        _transmitter.Send(message);

        LogDebug($"[SEND] {address} -> {value}");
    }

    /// <summary>
    /// /paddle アドレスに整数値を送信（RegionDetector用）
    /// </summary>
    /// <param name="paddleNumber">パドル番号</param>
    public void SendPaddleOSC(int paddleNumber)
    {
        SendOSC("/paddle", paddleNumber);
    }

    /// <summary>
    /// 監視する軸を変更
    /// </summary>
    public void SetAxis(AxisSelection axis)
    {
        selectedAxis = axis;
        _isFirstValue = true; // 軸を変更したので比較をリセット
        LogDebug($"Axis changed to: {axis}");
    }

    /// <summary>
    /// 受信アドレスを変更
    /// </summary>
    public void SetReceiveAddress(string address)
    {
        if (_receiver != null && _currentBind != null)
        {
            _receiver.Unbind(_currentBind);
            receiveAddress = address;
            _currentBind = _receiver.Bind(receiveAddress, OnPositionReceived);
            LogDebug($"Receive address changed to: {address}");
        }
    }

    /// <summary>
    /// 送信先を変更
    /// </summary>
    public void SetTransmitTarget(string host, int port)
    {
        if (_transmitter != null)
        {
            transmitHost = host;
            transmitPort = port;
            _transmitter.RemoteHost = host;
            _transmitter.RemotePort = port;
            LogDebug($"Transmit target changed to: {host}:{port}");
        }
    }

    /// <summary>
    /// 前回の値をリセット（テスト用）
    /// </summary>
    public void ResetPreviousValue()
    {
        _isFirstValue = true;
        LogDebug("Previous value reset");
    }

    /// <summary>
    /// 現在の設定情報を取得
    /// </summary>
    public string GetConfigInfo()
    {
        string info = $"Axis: {selectedAxis}\n" +
                      $"Negative: Increase={negativeIncreaseValue}, Decrease={negativeDecreaseValue}\n" +
                      $"Positive: Increase={positiveIncreaseValue}, Decrease={positiveDecreaseValue}\n";

        if (enableConditionalAxis)
        {
            info += $"Conditional: {conditionalAxis} in range [{conditionalAxisMin:F2}, {conditionalAxisMax:F2}]\n";
        }
        else
        {
            info += "Conditional: Disabled\n";
        }

        info += $"Receive: {receiveAddress}@{receivePort}\n" +
                $"Transmit: {transmitAddress}@{transmitHost}:{transmitPort}";

        return info;
    }

    #endregion
}
