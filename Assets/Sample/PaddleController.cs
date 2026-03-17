using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MultiAddressOSCManagerを使ったパドル操作コントローラー
/// 両手同時検知時の高Z優先ロジックを追加
/// </summary>
public class PaddleController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    [Tooltip("MultiAddressOSCManager")]
    public MultiAddressOSCManager oscManager;

    [Header("Paddle Settings")]
    [Tooltip("VectorYの最小閾値（この値以上で前後を判定）")]
    public float vectorThreshold = 0.1f;

    [Tooltip("X軸の中心位置（この値より右か左かで判定）")]
    public float xCenterPosition = 0f;

    [Tooltip("送信のクールダウン時間（秒）")]
    public float cooldownTime = 0.3f;

    [Header("Z-Axis Conditional Filter")]
    [Tooltip("Z軸条件フィルタを有効にする")]
    public bool enableConditionalAxis = false;

    [Tooltip("Z軸の最小値（この値以上の時に反応）")]
    public float conditionalAxisMin = 0f;

    [Tooltip("Z軸の最大値（この値以下の時に反応）")]
    public float conditionalAxisMax = 2.0f;

    [Header("Mode Settings")]
    [Tooltip("前後の方向を無視する")]
    public bool ignoreForwardBackward = true;

    [Tooltip("左右を反転する（1↔2, 3↔4）")]
    public bool invertLeftRight = false;

    [Header("High-Priority Dual Hand Logic")]
    [Tooltip("両手が範囲内の時、より高い(Zが大きい)方を優先する（クイズモード以外で有効）")]
    public bool enableHighZPriority = true;

    [Tooltip("左右のデータを比較対象とする有効時間（秒）")]
    public float handDataExpiry = 0.1f;

    [Header("Events")]
    [Tooltip("パドル操作を送信した時のイベント")]
    public UnityEvent<int> onPaddleSent;

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Header("AutoHeight設定値 (実行中に確認)")]
    [SerializeField, Tooltip("AutoHeightから設定されたZ最小値")]
    private float _debugZMin;
    [SerializeField, Tooltip("AutoHeightから設定されたZ最大値")]
    private float _debugZMax;

    #endregion

    #region Private Variables

    // 左右別々のクールダウンタイマー
    private float _lastRightSendTime = -999f;
    private float _lastLeftSendTime = -999f;

    // 高Z優先ロジック用の内部保持変数
    private float _lastRightZ = -1f;
    private float _lastLeftZ = -1f;
    private float _lastRightTimestamp = -1f;
    private float _lastLeftTimestamp = -1f;

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        if (oscManager == null)
            return;

        // チュートリアル(State=3)またはゲームモード(State=5)の時のみ動作
        int state = oscManager.GetInt("State");
        if (state != 3 && state != 5)
            return;

        ProcessPaddleControl();
    }

    #endregion

    #region Paddle Control

    /// <summary>
    /// パドル操作の処理
    /// </summary>
    void ProcessPaddleControl()
    {
        // 位置とベクトルを取得
        Vector3 position = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
        float vectorY = oscManager.GetFloat("VectorY");
        float boxZ = oscManager.GetFloat("BoxZ");

        // Z位置判定（ボックス上端）
        float actualZ = position.z + boxZ * 0.5f;
        float currentTime = Time.time;
        bool isRight = position.x >= xCenterPosition;

        // --- 高Z優先ロジックの判定 ---
        if (enableHighZPriority)
        {
            // 今回のデータを記録
            if (isRight) {
                _lastRightZ = actualZ;
                _lastRightTimestamp = currentTime;
            } else {
                _lastLeftZ = actualZ;
                _lastLeftTimestamp = currentTime;
            }

            // 反対側の手が有効時間内に存在するかチェック
            if (isRight) {
                bool leftIsActive = (currentTime - _lastLeftTimestamp) < handDataExpiry;
                if (leftIsActive && _lastLeftZ > actualZ) {
                    LogDebug($"[Priority] Right hand IGNORED. (Right Z:{actualZ:F3} < Left Z:{_lastLeftZ:F3})");
                    return;
                }
                if (leftIsActive && _lastLeftZ <= actualZ) {
                    LogDebug($"[Priority] Right hand SELECTED. (Right Z:{actualZ:F3} >= Left Z:{_lastLeftZ:F3})");
                }
            } else {
                bool rightIsActive = (currentTime - _lastRightTimestamp) < handDataExpiry;
                if (rightIsActive && _lastRightZ > actualZ) {
                    LogDebug($"[Priority] Left hand IGNORED. (Left Z:{actualZ:F3} < Right Z:{_lastRightZ:F3})");
                    return;
                }
                if (rightIsActive && _lastRightZ <= actualZ) {
                    LogDebug($"[Priority] Left hand SELECTED. (Left Z:{actualZ:F3} >= Right Z:{_lastRightZ:F3})");
                }
            }
        }

        // Z軸条件フィルタ
        if (enableConditionalAxis)
        {
            if (actualZ < conditionalAxisMin || actualZ > conditionalAxisMax)
            {
                LogDebug($"Z={actualZ:F3} is out of range [{conditionalAxisMin:F3}, {conditionalAxisMax:F3}]. Skipping.");
                return;
            }
        }

        // ベクトルの閾値チェック
        if (Mathf.Abs(vectorY) < vectorThreshold)
        {
            LogDebug($"VectorY={vectorY:F3} is below threshold {vectorThreshold:F3}. Skipping.");
            return;
        }

        // 左右別クールダウンチェック
        if (isRight)
        {
            if (Time.time - _lastRightSendTime < cooldownTime)
            {
                LogDebug($"Right cooldown active. Skipping send.");
                return;
            }
        }
        else
        {
            if (Time.time - _lastLeftSendTime < cooldownTime)
            {
                LogDebug($"Left cooldown active. Skipping send.");
                return;
            }
        }

        // 前後判定
        bool isForward = vectorY > 0;

        // パドル方向を決定
        int direction = DeterminePaddleDirection(isRight, isForward);

        // 送信
        SendPaddleDirection(direction, isRight);
    }

    /// <summary>
    /// パドル方向を決定
    /// </summary>
    int DeterminePaddleDirection(bool isRight, bool isForward)
    {
        // 左右反転オプション
        if (invertLeftRight)
        {
            isRight = !isRight;
        }

        if (ignoreForwardBackward)
        {
            // 前後を無視（左右のみ）
            return isRight ? 1 : 2; // 1:右前進, 2:左前進
        }
        else
        {
            // 前後を区別
            if (isForward)
            {
                return isRight ? 1 : 2; // 1:右前進, 2:左前進
            }
            else
            {
                return isRight ? 3 : 4; // 3:右後進, 4:左後進
            }
        }
    }

    /// <summary>
    /// パドル方向を送信
    /// </summary>
    void SendPaddleDirection(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");

        // 左右別々にクールダウンタイマーを更新
        if (isRight)
        {
            _lastRightSendTime = Time.time;
        }
        else
        {
            _lastLeftSendTime = Time.time;
        }

        LogDebug($"Paddle sent: {direction} ({GetDirectionName(direction)}) - {(isRight ? "Right" : "Left")} side");

        // イベント発火
        onPaddleSent?.Invoke(direction);
    }

    /// <summary>
    /// 方向名を取得
    /// </summary>
    string GetDirectionName(int direction)
    {
        switch (direction)
        {
            case 1: return "右前進";
            case 2: return "左前進";
            case 3: return "右後進";
            case 4: return "左後進";
            default: return "Unknown";
        }
    }

    #endregion

    #region Public Methods

    public Vector3 GetCurrentPosition()
    {
        if (oscManager == null)
            return Vector3.zero;

        Vector3 position = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
        float boxZ = oscManager.GetFloat("BoxZ");

        return new Vector3(
            position.x,
            position.y,
            position.z + boxZ * 0.5f
        );
    }

    public Vector3 GetCurrentVector()
    {
        if (oscManager == null)
            return Vector3.zero;

        return oscManager.GetVector3("VectorX", "VectorY", "VectorZ");
    }

    public void SetZMin(float value)
    {
        conditionalAxisMin = value;
        _debugZMin = value;
        LogDebug($"Z Min set to: {value:F3}");
    }

    public void SetZMax(float value)
    {
        conditionalAxisMax = value;
        _debugZMax = value;
        LogDebug($"Z Max set to: {value:F3}");
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PaddleController] {message}");
        }
    }

    #endregion
}