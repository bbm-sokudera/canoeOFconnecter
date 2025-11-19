using UnityEngine;
using UnityEngine.Events;
using extOSC;

/// <summary>
/// X位置+Yベクトル方向OSCマネージャー
/// X軸の位置（左右）とY軸の移動ベクトル方向（前後）を組み合わせて判定します
/// </summary>
public class OSCXPositionYVectorManager : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// X軸の位置
    /// </summary>
    public enum XPosition
    {
        Left,   // X < 0
        Right   // X >= 0
    }

    /// <summary>
    /// Y軸のベクトル方向
    /// </summary>
    public enum YVectorDirection
    {
        Forward,   // Y+方向
        Backward,  // Y-方向
        None       // 不明確
    }

    /// <summary>
    /// 組み合わせ判定結果
    /// </summary>
    public enum CombinedDirection
    {
        LeftForward,
        RightForward,
        LeftBackward,
        RightBackward,
        None
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
    public string transmitAddress = "/direction";

    [Header("Position & Vector Settings")]
    [Tooltip("X軸の中心位置（この値より右か左かで判定）")]
    public float xCenterPosition = 0f;

    [Tooltip("Y軸ベクトルの最小閾値（この値以上で方向を判定）")]
    public float yVectorMagnitudeThreshold = 0.1f;

    [Header("Direction Value Settings")]
    [Tooltip("左＋前方向の時に送信する値")]
    public int leftForwardValue = 0;

    [Tooltip("右＋前方向の時に送信する値")]
    public int rightForwardValue = 2;

    [Tooltip("左＋後方向の時に送信する値")]
    public int leftBackwardValue = 1;

    [Tooltip("右＋後方向の時に送信する値")]
    public int rightBackwardValue = 3;

    [Header("Conditional Axis Settings")]
    [Tooltip("条件軸を有効にする（チェックONで、Z軸が範囲内の時のみ判定を実行）")]
    public bool enableConditionalAxis = false;

    [Tooltip("条件軸（Z軸）の最小値（この値以上の時に反応）")]
    public float conditionalAxisMin = -1.0f;

    [Tooltip("条件軸（Z軸）の最大値（この値以下の時に反応）")]
    public float conditionalAxisMax = 1.0f;

    [Header("Advanced Settings")]
    [Tooltip("Y軸ベクトル方向の角度閾値（度）：Y軸からこの角度以内なら前後として判定")]
    public float yAngleThreshold = 45f;

    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Tooltip("Gizmoで位置とベクトルを表示する")]
    public bool showGizmo = true;

    [Header("Events")]
    [Tooltip("値を送信した時に呼ばれるイベント")]
    public UnityEvent<int> onValueSent;

    [Tooltip("方向を検出した時に呼ばれるイベント")]
    public UnityEvent<CombinedDirection> onDirectionDetected;

    #endregion

    #region Private Vars

    private OSCReceiver _receiver;
    private OSCTransmitter _transmitter;
    private OSCBind _currentBind;

    private Vector3 _previousPosition = Vector3.zero;
    private Vector3 _currentPosition = Vector3.zero;
    private Vector3 _movementVector = Vector3.zero;
    private bool _isFirstPosition = true;

    private XPosition _currentXPosition = XPosition.Right;
    private YVectorDirection _currentYDirection = YVectorDirection.None;

    #endregion

    #region Unity Methods

    void Start()
    {
        InitializeReceiver();
        InitializeTransmitter();

        LogDebug($"X-Position + Y-Vector OSC Manager initialized");
        LogDebug($"X Center: {xCenterPosition}, Y Vector Threshold: {yVectorMagnitudeThreshold}");
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

    void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        // 現在位置を表示
        Vector3 worldPos = transform.position + _currentPosition;

        // X軸の中心線を表示
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(
            transform.position + new Vector3(xCenterPosition, -10, 0),
            transform.position + new Vector3(xCenterPosition, 10, 0)
        );

        // 現在位置を表示
        Gizmos.color = _currentXPosition == XPosition.Left ? Color.blue : Color.red;
        Gizmos.DrawSphere(worldPos, 0.1f);

        // Y軸ベクトルを表示
        if (_movementVector.magnitude > 0.001f)
        {
            Gizmos.color = Color.yellow;
            Vector3 yVectorOnly = new Vector3(0, _movementVector.y, 0) * 2f;
            Gizmos.DrawLine(worldPos, worldPos + yVectorOnly);
            Gizmos.DrawSphere(worldPos + yVectorOnly, 0.05f);

            // 閾値円を表示
            Gizmos.color = Mathf.Abs(_movementVector.y) >= yVectorMagnitudeThreshold ? Color.green : Color.red;
            Gizmos.DrawWireSphere(worldPos, yVectorMagnitudeThreshold);
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

        // 現在の位置を更新
        _currentPosition = new Vector3(
            message.Values[0].FloatValue,
            message.Values[1].FloatValue,
            message.Values[2].FloatValue
        );

        LogDebug($"Received position: {_currentPosition}");

        // 条件軸（Z軸）のチェック（有効な場合）
        if (enableConditionalAxis)
        {
            float zValue = _currentPosition.z;

            // Z軸が範囲外の場合は処理をスキップ（ログは出力しない）
            if (zValue < conditionalAxisMin || zValue > conditionalAxisMax)
            {
                return;
            }

            LogDebug($"Conditional axis (Z) value {zValue:F3} is within range [{conditionalAxisMin:F3}, {conditionalAxisMax:F3}]. Processing...");
        }

        // 初回は前回位置を設定するだけ
        if (_isFirstPosition)
        {
            _previousPosition = _currentPosition;
            _isFirstPosition = false;
            LogDebug("First position set. Waiting for next position to calculate vector.");
            return;
        }

        // 判定処理を実行
        ProcessPositionAndVector();

        // 前回位置を更新
        _previousPosition = _currentPosition;
    }

    #endregion

    #region Processing Methods

    /// <summary>
    /// 位置とベクトルを処理
    /// </summary>
    private void ProcessPositionAndVector()
    {
        // 1. X軸の位置を判定（現在のX座標が中心より左か右か）
        _currentXPosition = DetermineXPosition(_currentPosition.x);

        // 2. 移動ベクトルを計算
        _movementVector = _currentPosition - _previousPosition;

        // 3. Y軸のベクトル方向を判定
        _currentYDirection = DetermineYVectorDirection(_movementVector);

        LogDebug($"X Position: {_currentXPosition} (x={_currentPosition.x:F3})");
        LogDebug($"Y Vector: {_movementVector.y:F3}, Direction: {_currentYDirection}");

        // 4. Y軸ベクトルが閾値未満または方向不明の場合はスキップ
        if (_currentYDirection == YVectorDirection.None)
        {
            LogDebug($"Y vector magnitude {Mathf.Abs(_movementVector.y):F3} is below threshold {yVectorMagnitudeThreshold:F3} or unclear direction. Skipping.");
            return;
        }

        // 5. X位置とY方向を組み合わせて判定
        CombinedDirection combinedDirection = CombineXPositionAndYDirection(_currentXPosition, _currentYDirection);

        LogDebug($"Combined Direction: {combinedDirection}");

        // 6. イベントを発火
        onDirectionDetected?.Invoke(combinedDirection);

        // 7. 方向に応じた値を送信
        SendDirectionValue(combinedDirection);
    }

    /// <summary>
    /// X軸の位置を判定（中心より左か右か）
    /// </summary>
    private XPosition DetermineXPosition(float x)
    {
        return x < xCenterPosition ? XPosition.Left : XPosition.Right;
    }

    /// <summary>
    /// Y軸のベクトル方向を判定
    /// </summary>
    private YVectorDirection DetermineYVectorDirection(Vector3 vector)
    {
        // Y成分の大きさをチェック
        float yMagnitude = Mathf.Abs(vector.y);

        if (yMagnitude < yVectorMagnitudeThreshold)
        {
            return YVectorDirection.None;
        }

        // Y軸方向との角度をチェック（オプション）
        Vector3 yAxisForward = Vector3.up;    // Y+方向
        Vector3 yAxisBackward = Vector3.down; // Y-方向

        float angleForward = Vector3.Angle(yAxisForward, vector);
        float angleBackward = Vector3.Angle(yAxisBackward, vector);

        // どちらか近い方を選択
        if (angleForward < angleBackward && angleForward <= yAngleThreshold)
        {
            return YVectorDirection.Forward;
        }
        else if (angleBackward <= yAngleThreshold)
        {
            return YVectorDirection.Backward;
        }

        // 角度閾値を超えている場合は、単純にY成分の正負で判定
        if (vector.y > 0)
            return YVectorDirection.Forward;
        else if (vector.y < 0)
            return YVectorDirection.Backward;

        return YVectorDirection.None;
    }

    /// <summary>
    /// X位置とY方向を組み合わせる
    /// </summary>
    private CombinedDirection CombineXPositionAndYDirection(XPosition xPos, YVectorDirection yDir)
    {
        if (xPos == XPosition.Left && yDir == YVectorDirection.Forward)
            return CombinedDirection.LeftForward;
        else if (xPos == XPosition.Right && yDir == YVectorDirection.Forward)
            return CombinedDirection.RightForward;
        else if (xPos == XPosition.Left && yDir == YVectorDirection.Backward)
            return CombinedDirection.LeftBackward;
        else if (xPos == XPosition.Right && yDir == YVectorDirection.Backward)
            return CombinedDirection.RightBackward;
        else
            return CombinedDirection.None;
    }

    /// <summary>
    /// 方向に応じた値を送信
    /// </summary>
    private void SendDirectionValue(CombinedDirection direction)
    {
        int value;

        switch (direction)
        {
            case CombinedDirection.LeftForward:
                value = leftForwardValue;
                break;
            case CombinedDirection.RightForward:
                value = rightForwardValue;
                break;
            case CombinedDirection.LeftBackward:
                value = leftBackwardValue;
                break;
            case CombinedDirection.RightBackward:
                value = rightBackwardValue;
                break;
            default:
                LogDebug("No clear direction. Skipping transmission.");
                return;
        }

        SendValue(value);
    }

    /// <summary>
    /// OSCで値を送信
    /// </summary>
    private void SendValue(int value)
    {
        if (_transmitter == null)
        {
            LogDebug("Transmitter is not initialized");
            return;
        }

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
            Debug.Log($"[OSCXPositionYVectorManager] {message}");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// X軸の中心位置を変更
    /// </summary>
    public void SetXCenterPosition(float center)
    {
        xCenterPosition = center;
        LogDebug($"X center position changed to: {center}");
    }

    /// <summary>
    /// Y軸ベクトルの閾値を変更
    /// </summary>
    public void SetYVectorThreshold(float threshold)
    {
        yVectorMagnitudeThreshold = threshold;
        LogDebug($"Y vector threshold changed to: {threshold}");
    }

    /// <summary>
    /// 前回位置をリセット
    /// </summary>
    public void ResetPosition()
    {
        _isFirstPosition = true;
        _movementVector = Vector3.zero;
        LogDebug("Position reset");
    }

    /// <summary>
    /// 条件軸（Z軸）の有効/無効を設定
    /// </summary>
    public void SetConditionalAxisEnabled(bool enabled)
    {
        enableConditionalAxis = enabled;
        LogDebug($"Conditional axis (Z) {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 条件軸（Z軸）の範囲を設定
    /// </summary>
    public void SetConditionalAxisRange(float min, float max)
    {
        conditionalAxisMin = min;
        conditionalAxisMax = max;
        LogDebug($"Conditional axis (Z) range set to [{min:F3}, {max:F3}]");
    }

    /// <summary>
    /// 現在の設定情報を取得
    /// </summary>
    public string GetConfigInfo()
    {
        string info = $"X Center Position: {xCenterPosition:F3}\n" +
                      $"Y Vector Threshold: {yVectorMagnitudeThreshold:F3}\n" +
                      $"Y Angle Threshold: {yAngleThreshold:F1}°\n" +
                      $"Direction Values: LF={leftForwardValue}, RF={rightForwardValue}, LB={leftBackwardValue}, RB={rightBackwardValue}\n";

        if (enableConditionalAxis)
        {
            info += $"Conditional Axis (Z): Enabled, Range [{conditionalAxisMin:F2}, {conditionalAxisMax:F2}]\n";
        }
        else
        {
            info += "Conditional Axis (Z): Disabled\n";
        }

        info += $"Receive: {receiveAddress}@{receivePort}\n" +
                $"Transmit: {transmitAddress}@{transmitHost}:{transmitPort}";

        return info;
    }

    /// <summary>
    /// 現在の移動ベクトルを取得
    /// </summary>
    public Vector3 GetMovementVector()
    {
        return _movementVector;
    }

    /// <summary>
    /// 現在のX位置を取得
    /// </summary>
    public XPosition GetCurrentXPosition()
    {
        return _currentXPosition;
    }

    /// <summary>
    /// 現在のY方向を取得
    /// </summary>
    public YVectorDirection GetCurrentYDirection()
    {
        return _currentYDirection;
    }

    #endregion
}
