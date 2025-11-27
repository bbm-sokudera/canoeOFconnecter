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
    [Tooltip("右側（X>=0）の時に送信する値")]
    public int rightValue = 0;

    [Tooltip("左側（X<0）の時に送信する値")]
    public int leftValue = 2;

    [Tooltip("右＋後方向の時に送信する値（後進のみモード時）")]
    public int rightBackwardValue = 1;

    [Tooltip("左＋後方向の時に送信する値（後進のみモード時）")]
    public int leftBackwardValue = 3;

    [Header("Conditional Axis Settings")]
    [Tooltip("条件軸を有効にする（チェックONで、Z軸が範囲内の時のみ判定を実行）")]
    public bool enableConditionalAxis = false;

    [Tooltip("条件軸（Z軸）の最小値（この値以上の時に反応）")]
    public float conditionalAxisMin = -1.0f;

    [Tooltip("条件軸（Z軸）の最大値（この値以下の時に反応）")]
    public float conditionalAxisMax = 1.0f;

    [Header("Cooldown Settings")]
    [Tooltip("送信のクールダウン時間（秒）：この時間経過後のみ次の送信を許可")]
    public float cooldownTime = 0.3f;

    [Header("Consecutive Side Detection")]
    [Tooltip("左右連続検出を有効にする")]
    public bool enableConsecutiveSideDetection = false;

    [Tooltip("同じ左右方向が何回続いたらその方向固定モードにするか")]
    public int consecutiveSideLimit = 5;

    [Tooltip("左右ロックモードを解除するために必要な反対側の回数")]
    public int oppositeSideRequiredCount = 2;

    [Header("Consecutive Backward Detection")]
    [Tooltip("後進連続検出を有効にする")]
    public bool enableConsecutiveBackwardDetection = false;

    [Tooltip("後進が何回続いたら後進のみモードにするか")]
    public int consecutiveBackwardLimit = 5;

    [Tooltip("後進のみモードを解除するために必要な前進の回数")]
    public int forwardRequiredCount = 2;

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

    // クールダウン用
    private float _lastSendTime = -999f;

    // 左右連続検出用
    private int _consecutiveLeftCount = 0;
    private int _consecutiveRightCount = 0;
    private bool _isLeftLocked = false;
    private bool _isRightLocked = false;
    private int _oppositeCountInLeftLock = 0;  // 左ロック中に右が来た回数
    private int _oppositeCountInRightLock = 0; // 右ロック中に左が来た回数

    // 後進連続検出用
    private int _consecutiveBackwardCount = 0;
    private bool _isBackwardOnlyMode = false;
    private int _forwardCountInBackwardMode = 0;

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
        // 1. クールダウンチェック
        if (Time.time - _lastSendTime < cooldownTime)
        {
            LogDebug($"Cooldown active. Skipping transmission. Time since last send: {Time.time - _lastSendTime:F3}s");
            return;
        }

        // 2. 左右連続検出の処理
        if (enableConsecutiveSideDetection)
        {
            UpdateConsecutiveSideCount(direction);
        }

        // 3. 後進連続検出の処理
        if (enableConsecutiveBackwardDetection)
        {
            UpdateConsecutiveBackwardCount(direction);
        }

        // 4. 送信する値を決定（後進のみモード・左右ロックを考慮）
        int value = DetermineValueToSend(direction);

        if (value == -1)
        {
            LogDebug("No clear direction. Skipping transmission.");
            return;
        }

        // 5. 値を送信
        SendValue(value);

        // 6. 最終送信時刻を更新
        _lastSendTime = Time.time;
    }

    /// <summary>
    /// 左右連続カウントを更新
    /// </summary>
    private void UpdateConsecutiveSideCount(CombinedDirection direction)
    {
        bool isLeft = (direction == CombinedDirection.LeftForward || direction == CombinedDirection.LeftBackward);
        bool isRight = (direction == CombinedDirection.RightForward || direction == CombinedDirection.RightBackward);

        // 左がロックされている場合
        if (_isLeftLocked)
        {
            // 右が来たらカウント
            if (isRight)
            {
                _oppositeCountInLeftLock++;
                LogDebug($"Left-locked mode: Right detected. Count: {_oppositeCountInLeftLock}/{oppositeSideRequiredCount}");

                // 必要回数に達したらロック解除
                if (_oppositeCountInLeftLock >= oppositeSideRequiredCount)
                {
                    _isLeftLocked = false;
                    _consecutiveLeftCount = 0;
                    _oppositeCountInLeftLock = 0;
                    _consecutiveRightCount = 0;
                    LogDebug("Left lock released. Counters reset.");
                }
            }
        }
        // 右がロックされている場合
        else if (_isRightLocked)
        {
            // 左が来たらカウント
            if (isLeft)
            {
                _oppositeCountInRightLock++;
                LogDebug($"Right-locked mode: Left detected. Count: {_oppositeCountInRightLock}/{oppositeSideRequiredCount}");

                // 必要回数に達したらロック解除
                if (_oppositeCountInRightLock >= oppositeSideRequiredCount)
                {
                    _isRightLocked = false;
                    _consecutiveRightCount = 0;
                    _oppositeCountInRightLock = 0;
                    _consecutiveLeftCount = 0;
                    LogDebug("Right lock released. Counters reset.");
                }
            }
        }
        // どちらもロックされていない場合
        else
        {
            if (isLeft)
            {
                _consecutiveLeftCount++;
                _consecutiveRightCount = 0; // 右カウントをリセット
                LogDebug($"Left direction count: {_consecutiveLeftCount}");

                // 閾値に達したら左をロック
                if (_consecutiveLeftCount >= consecutiveSideLimit)
                {
                    _isLeftLocked = true;
                    _oppositeCountInLeftLock = 0; // カウンターリセット
                    LogDebug($"Left locked due to consecutive detection ({_consecutiveLeftCount} times)");
                }
            }
            else if (isRight)
            {
                _consecutiveRightCount++;
                _consecutiveLeftCount = 0; // 左カウントをリセット
                LogDebug($"Right direction count: {_consecutiveRightCount}");

                // 閾値に達したら右をロック
                if (_consecutiveRightCount >= consecutiveSideLimit)
                {
                    _isRightLocked = true;
                    _oppositeCountInRightLock = 0; // カウンターリセット
                    LogDebug($"Right locked due to consecutive detection ({_consecutiveRightCount} times)");
                }
            }
        }
    }

    /// <summary>
    /// 後進連続カウントを更新
    /// </summary>
    private void UpdateConsecutiveBackwardCount(CombinedDirection direction)
    {
        bool isBackward = (direction == CombinedDirection.LeftBackward || direction == CombinedDirection.RightBackward);
        bool isForward = (direction == CombinedDirection.LeftForward || direction == CombinedDirection.RightForward);

        // 後進のみモード中の場合
        if (_isBackwardOnlyMode)
        {
            // 前進が来たらカウント
            if (isForward)
            {
                _forwardCountInBackwardMode++;
                LogDebug($"Backward-only mode: Forward detected. Count: {_forwardCountInBackwardMode}/{forwardRequiredCount}");

                // 必要回数に達したら後進のみモードを解除
                if (_forwardCountInBackwardMode >= forwardRequiredCount)
                {
                    _isBackwardOnlyMode = false;
                    _consecutiveBackwardCount = 0;
                    _forwardCountInBackwardMode = 0;
                    LogDebug("Backward-only mode deactivated. Counters reset.");
                }
            }
        }
        else
        {
            // 通常モード：後進の連続カウントを更新
            if (isBackward)
            {
                _consecutiveBackwardCount++;
                LogDebug($"Backward direction count: {_consecutiveBackwardCount}");

                // 閾値に達したら後進のみモードに移行
                if (_consecutiveBackwardCount >= consecutiveBackwardLimit)
                {
                    _isBackwardOnlyMode = true;
                    _forwardCountInBackwardMode = 0;
                    LogDebug($"Backward-only mode activated due to consecutive Backward ({_consecutiveBackwardCount} times)");
                }
            }
            else if (isForward)
            {
                // 前進が来たら後進カウントをリセット
                _consecutiveBackwardCount = 0;
            }
        }
    }

    /// <summary>
    /// 送信する値を決定（後進のみモード・左右ロックを考慮）
    /// </summary>
    private int DetermineValueToSend(CombinedDirection direction)
    {
        int value;

        // 基本は左右のみの判定
        bool isLeft = (direction == CombinedDirection.LeftForward || direction == CombinedDirection.LeftBackward);
        bool isRight = (direction == CombinedDirection.RightForward || direction == CombinedDirection.RightBackward);

        // 左右ロック機能が有効な場合のロックチェック
        if (enableConsecutiveSideDetection)
        {
            // 左がロックされている場合：右方向は無視
            if (_isLeftLocked && isRight)
            {
                LogDebug("Left is locked. Ignoring right direction.");
                return -1;
            }

            // 右がロックされている場合：左方向は無視
            if (_isRightLocked && isLeft)
            {
                LogDebug("Right is locked. Ignoring left direction.");
                return -1;
            }
        }

        if (_isBackwardOnlyMode)
        {
            // 後進のみモード：左右に応じて後進値を送信
            if (isLeft)
            {
                value = leftBackwardValue;
                LogDebug("Backward-only mode: Left → LeftBackward");
            }
            else if (isRight)
            {
                value = rightBackwardValue;
                LogDebug("Backward-only mode: Right → RightBackward");
            }
            else
            {
                return -1;
            }
        }
        else
        {
            // 通常モード：左右に応じて基本値を送信（前後は無視）
            if (isLeft)
            {
                value = leftValue;
            }
            else if (isRight)
            {
                value = rightValue;
            }
            else
            {
                return -1;
            }
        }

        return value;
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
        float zValue = _currentPosition.z;

        if (enableDebugLog && zValue > conditionalAxisMin)
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

        // 左右連続検出のカウンターリセット
        _consecutiveLeftCount = 0;
        _consecutiveRightCount = 0;
        _isLeftLocked = false;
        _isRightLocked = false;
        _oppositeCountInLeftLock = 0;
        _oppositeCountInRightLock = 0;

        // 後進連続検出のカウンターリセット
        _consecutiveBackwardCount = 0;
        _isBackwardOnlyMode = false;
        _forwardCountInBackwardMode = 0;

        _lastSendTime = -999f;
        LogDebug("Position and all counters reset");
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
                      $"Direction Values: Right={rightValue}, Left={leftValue}, RightBackward={rightBackwardValue}, LeftBackward={leftBackwardValue}\n";

        if (enableConditionalAxis)
        {
            info += $"Conditional Axis (Z): Enabled, Range [{conditionalAxisMin:F2}, {conditionalAxisMax:F2}]\n";
        }
        else
        {
            info += "Conditional Axis (Z): Disabled\n";
        }

        info += $"Cooldown Time: {cooldownTime:F2}s\n";

        if (enableConsecutiveSideDetection)
        {
            info += $"Consecutive Side Detection: Enabled (Limit={consecutiveSideLimit}, OppositeRequired={oppositeSideRequiredCount})\n";
            info += $"  State: LeftLocked={_isLeftLocked}, RightLocked={_isRightLocked}, LeftCount={_consecutiveLeftCount}, RightCount={_consecutiveRightCount}\n";
            if (_isLeftLocked)
            {
                info += $"  LeftLock OppositeCount: {_oppositeCountInLeftLock}\n";
            }
            if (_isRightLocked)
            {
                info += $"  RightLock OppositeCount: {_oppositeCountInRightLock}\n";
            }
        }
        else
        {
            info += "Consecutive Side Detection: Disabled\n";
        }

        if (enableConsecutiveBackwardDetection)
        {
            info += $"Consecutive Backward Detection: Enabled (Limit={consecutiveBackwardLimit}, ForwardRequired={forwardRequiredCount})\n";
            info += $"  State: BackwardOnlyMode={_isBackwardOnlyMode}, BackwardCount={_consecutiveBackwardCount}\n";
        }
        else
        {
            info += "Consecutive Backward Detection: Disabled\n";
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
    /// 現在のOSC受信位置を取得
    /// </summary>
    public Vector3 GetCurrentPosition()
    {
        return _currentPosition;
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

    /// <summary>
    /// テスト用：指定された値を手動でOSC送信
    /// クールダウンやモードチェックを無視して直接送信します
    /// </summary>
    public void SendTestValue(int value)
    {
        if (_transmitter == null)
        {
            Debug.LogWarning("[OSCXPositionYVectorManager] Transmitter is not initialized. Cannot send test value.");
            return;
        }

        var message = new OSCMessage(transmitAddress);
        message.AddValue(OSCValue.Int(value));
        _transmitter.Send(message);

        Debug.Log($"[OSCXPositionYVectorManager] [TEST SEND] {transmitAddress} -> {value}");

        // イベントを発火（ビジュアライザーが反応する）
        onValueSent?.Invoke(value);
    }

    #endregion
}
