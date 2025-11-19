using UnityEngine;
using UnityEngine.Events;
using extOSC;

/// <summary>
/// ベクトルベースOSCマネージャー
/// x,y,z座標から移動ベクトルを計算し、方向と大きさに応じて値を送信します
/// </summary>
public class OSCVectorManager : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// ベクトル判定モード
    /// </summary>
    public enum VectorMode
    {
        Horizontal2D,   // 水平2D (X-Z平面)
        Vertical2D,     // 垂直2D (X-Y平面)
        Full3D          // 完全3D (X-Y-Z)
    }

    /// <summary>
    /// 方向判定
    /// </summary>
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down,
        Forward,
        Backward,
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
    public string transmitAddress = "/vector";

    [Header("Vector Settings")]
    [Tooltip("ベクトル判定モード")]
    public VectorMode vectorMode = VectorMode.Horizontal2D;

    [Tooltip("ベクトルの大きさの最小閾値（この値以上で判定を行う）")]
    public float magnitudeThreshold = 0.1f;

    [Header("Direction Value Settings - Horizontal")]
    [Tooltip("左方向に移動した時に送信する値")]
    public int leftValue = 0;

    [Tooltip("右方向に移動した時に送信する値")]
    public int rightValue = 1;

    [Header("Direction Value Settings - Vertical")]
    [Tooltip("上方向に移動した時に送信する値")]
    public int upValue = 2;

    [Tooltip("下方向に移動した時に送信する値")]
    public int downValue = 3;

    [Header("Direction Value Settings - Depth")]
    [Tooltip("前方向に移動した時に送信する値")]
    public int forwardValue = 4;

    [Tooltip("後方向に移動した時に送信する値")]
    public int backwardValue = 5;

    [Header("Advanced Settings")]
    [Tooltip("角度の閾値（度）：この角度以内なら主方向として判定")]
    public float angleThreshold = 45f;

    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Tooltip("ベクトルをGizmoで表示する")]
    public bool showVectorGizmo = true;

    [Header("Events")]
    [Tooltip("値を送信した時に呼ばれるイベント")]
    public UnityEvent<int> onValueSent;

    [Tooltip("ベクトルを検出した時に呼ばれるイベント（方向、大きさ）")]
    public UnityEvent<Direction, float> onVectorDetected;

    #endregion

    #region Private Vars

    private OSCReceiver _receiver;
    private OSCTransmitter _transmitter;
    private OSCBind _currentBind;

    private Vector3 _previousPosition = Vector3.zero;
    private Vector3 _currentPosition = Vector3.zero;
    private Vector3 _movementVector = Vector3.zero;
    private bool _isFirstPosition = true;

    #endregion

    #region Unity Methods

    void Start()
    {
        InitializeReceiver();
        InitializeTransmitter();

        LogDebug($"Vector OSC Manager initialized");
        LogDebug($"Mode: {vectorMode}, Magnitude Threshold: {magnitudeThreshold}");
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
        if (!showVectorGizmo || _movementVector.magnitude < 0.001f)
            return;

        // ベクトルを可視化
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position + _currentPosition;
        Vector3 end = start + _movementVector * 2f; // スケールアップして見やすく
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.05f);

        // 大きさを表示
        Gizmos.color = _movementVector.magnitude >= magnitudeThreshold ? Color.green : Color.red;
        Gizmos.DrawWireSphere(start, magnitudeThreshold);
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

        // 初回は前回位置を設定するだけ
        if (_isFirstPosition)
        {
            _previousPosition = _currentPosition;
            _isFirstPosition = false;
            LogDebug("First position set. Waiting for next position to calculate vector.");
            return;
        }

        // 移動ベクトルを計算
        CalculateAndProcessVector();

        // 前回位置を更新
        _previousPosition = _currentPosition;
    }

    #endregion

    #region Vector Processing Methods

    /// <summary>
    /// 移動ベクトルを計算して処理
    /// </summary>
    private void CalculateAndProcessVector()
    {
        // 移動ベクトル = 現在位置 - 前回位置
        _movementVector = _currentPosition - _previousPosition;

        // モードに応じてベクトルを調整
        Vector3 processedVector = GetProcessedVector(_movementVector);

        // ベクトルの大きさを計算
        float magnitude = processedVector.magnitude;

        LogDebug($"Movement vector: {_movementVector}, Processed: {processedVector}, Magnitude: {magnitude:F3}");

        // 大きさが閾値未満の場合はスキップ
        if (magnitude < magnitudeThreshold)
        {
            LogDebug($"Magnitude {magnitude:F3} is below threshold {magnitudeThreshold:F3}. Skipping.");
            return;
        }

        // 方向を判定
        Direction direction = DetermineDirection(processedVector);

        LogDebug($"Direction detected: {direction}, Magnitude: {magnitude:F3}");

        // イベントを発火
        onVectorDetected?.Invoke(direction, magnitude);

        // 方向に応じた値を送信
        SendDirectionValue(direction);
    }

    /// <summary>
    /// モードに応じてベクトルを処理
    /// </summary>
    private Vector3 GetProcessedVector(Vector3 vector)
    {
        switch (vectorMode)
        {
            case VectorMode.Horizontal2D:
                // X-Z平面のみ（Y軸を無視）
                return new Vector3(vector.x, 0, vector.z);

            case VectorMode.Vertical2D:
                // X-Y平面のみ（Z軸を無視）
                return new Vector3(vector.x, vector.y, 0);

            case VectorMode.Full3D:
                // すべての軸を使用
                return vector;

            default:
                return vector;
        }
    }

    /// <summary>
    /// ベクトルから方向を判定
    /// </summary>
    private Direction DetermineDirection(Vector3 vector)
    {
        // ベクトルが0の場合
        if (vector.magnitude < 0.001f)
            return Direction.None;

        // 正規化
        Vector3 normalized = vector.normalized;

        switch (vectorMode)
        {
            case VectorMode.Horizontal2D:
                return DetermineHorizontalDirection(normalized);

            case VectorMode.Vertical2D:
                return DetermineVerticalDirection(normalized);

            case VectorMode.Full3D:
                return Determine3DDirection(normalized);

            default:
                return Direction.None;
        }
    }

    /// <summary>
    /// 水平方向（X-Z平面）の判定
    /// </summary>
    private Direction DetermineHorizontalDirection(Vector3 normalized)
    {
        // X軸との角度を計算
        float angleX = Vector3.Angle(Vector3.right, normalized);
        float angleNegX = Vector3.Angle(Vector3.left, normalized);
        float angleZ = Vector3.Angle(Vector3.forward, normalized);
        float angleNegZ = Vector3.Angle(Vector3.back, normalized);

        // 最も近い方向を選択
        float minAngle = Mathf.Min(angleX, angleNegX, angleZ, angleNegZ);

        if (minAngle > angleThreshold)
        {
            LogDebug($"No clear direction. Minimum angle: {minAngle:F1}° > threshold {angleThreshold:F1}°");
            return Direction.None;
        }

        if (minAngle == angleX)
            return Direction.Right;
        else if (minAngle == angleNegX)
            return Direction.Left;
        else if (minAngle == angleZ)
            return Direction.Forward;
        else
            return Direction.Backward;
    }

    /// <summary>
    /// 垂直方向（X-Y平面）の判定
    /// </summary>
    private Direction DetermineVerticalDirection(Vector3 normalized)
    {
        float angleX = Vector3.Angle(Vector3.right, normalized);
        float angleNegX = Vector3.Angle(Vector3.left, normalized);
        float angleY = Vector3.Angle(Vector3.up, normalized);
        float angleNegY = Vector3.Angle(Vector3.down, normalized);

        float minAngle = Mathf.Min(angleX, angleNegX, angleY, angleNegY);

        if (minAngle > angleThreshold)
            return Direction.None;

        if (minAngle == angleX)
            return Direction.Right;
        else if (minAngle == angleNegX)
            return Direction.Left;
        else if (minAngle == angleY)
            return Direction.Up;
        else
            return Direction.Down;
    }

    /// <summary>
    /// 3D方向の判定
    /// </summary>
    private Direction Determine3DDirection(Vector3 normalized)
    {
        // 6つの主要方向との角度を計算
        float angleRight = Vector3.Angle(Vector3.right, normalized);
        float angleLeft = Vector3.Angle(Vector3.left, normalized);
        float angleUp = Vector3.Angle(Vector3.up, normalized);
        float angleDown = Vector3.Angle(Vector3.down, normalized);
        float angleForward = Vector3.Angle(Vector3.forward, normalized);
        float angleBackward = Vector3.Angle(Vector3.back, normalized);

        float minAngle = Mathf.Min(angleRight, angleLeft, angleUp, angleDown, angleForward, angleBackward);

        if (minAngle > angleThreshold)
            return Direction.None;

        if (minAngle == angleRight)
            return Direction.Right;
        else if (minAngle == angleLeft)
            return Direction.Left;
        else if (minAngle == angleUp)
            return Direction.Up;
        else if (minAngle == angleDown)
            return Direction.Down;
        else if (minAngle == angleForward)
            return Direction.Forward;
        else
            return Direction.Backward;
    }

    /// <summary>
    /// 方向に応じた値を送信
    /// </summary>
    private void SendDirectionValue(Direction direction)
    {
        int value;

        switch (direction)
        {
            case Direction.Left:
                value = leftValue;
                break;
            case Direction.Right:
                value = rightValue;
                break;
            case Direction.Up:
                value = upValue;
                break;
            case Direction.Down:
                value = downValue;
                break;
            case Direction.Forward:
                value = forwardValue;
                break;
            case Direction.Backward:
                value = backwardValue;
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
            Debug.Log($"[OSCVectorManager] {message}");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ベクトルモードを変更
    /// </summary>
    public void SetVectorMode(VectorMode mode)
    {
        vectorMode = mode;
        _isFirstPosition = true;
        LogDebug($"Vector mode changed to: {mode}");
    }

    /// <summary>
    /// 大きさの閾値を変更
    /// </summary>
    public void SetMagnitudeThreshold(float threshold)
    {
        magnitudeThreshold = threshold;
        LogDebug($"Magnitude threshold changed to: {threshold}");
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
    /// 現在の設定情報を取得
    /// </summary>
    public string GetConfigInfo()
    {
        return $"Vector Mode: {vectorMode}\n" +
               $"Magnitude Threshold: {magnitudeThreshold:F3}\n" +
               $"Angle Threshold: {angleThreshold:F1}°\n" +
               $"Direction Values: L={leftValue}, R={rightValue}, U={upValue}, D={downValue}, F={forwardValue}, B={backwardValue}\n" +
               $"Receive: {receiveAddress}@{receivePort}\n" +
               $"Transmit: {transmitAddress}@{transmitHost}:{transmitPort}";
    }

    /// <summary>
    /// 現在の移動ベクトル情報を取得
    /// </summary>
    public Vector3 GetMovementVector()
    {
        return _movementVector;
    }

    /// <summary>
    /// 現在のベクトルの大きさを取得
    /// </summary>
    public float GetMagnitude()
    {
        return _movementVector.magnitude;
    }

    #endregion
}
