using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MultiAddressOSCManagerを使ったパドル操作コントローラー
/// OSCXPositionYVectorManagerの機能を再実装
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

    private float _lastSendTime = -999f;

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

        // 左右判定
        bool isRight = position.x >= xCenterPosition;

        // 前後判定
        bool isForward = vectorY > 0;

        // パドル方向を決定
        int direction = DeterminePaddleDirection(isRight, isForward);

        // クールダウンチェック
        if (Time.time - _lastSendTime < cooldownTime)
        {
            LogDebug($"Cooldown active. Skipping send.");
            return;
        }

        // 送信
        SendPaddleDirection(direction);
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
    void SendPaddleDirection(int direction)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");

        _lastSendTime = Time.time;

        LogDebug($"Paddle sent: {direction} ({GetDirectionName(direction)})");

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

    /// <summary>
    /// 現在の位置を取得（ボックス上端のZ値）
    /// </summary>
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

    /// <summary>
    /// 現在のベクトルを取得
    /// </summary>
    public Vector3 GetCurrentVector()
    {
        if (oscManager == null)
            return Vector3.zero;

        return oscManager.GetVector3("VectorX", "VectorY", "VectorZ");
    }

    /// <summary>
    /// Z軸の最小値を設定（AutoHeightControllerから呼び出し用）
    /// </summary>
    public void SetZMin(float value)
    {
        conditionalAxisMin = value;
        _debugZMin = value;
        LogDebug($"Z Min set to: {value:F3}");
    }

    /// <summary>
    /// Z軸の最大値を設定（AutoHeightControllerから呼び出し用）
    /// </summary>
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
