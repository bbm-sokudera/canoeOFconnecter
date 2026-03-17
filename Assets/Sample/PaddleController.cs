using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MultiAddressOSCManagerを使ったパドル操作コントローラー
/// 両手同時検知時の高Z優先ロジック（日本語ログ対応）を追加
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
    [Tooltip("両手が範囲内の時、より高い(Zが大きい)方を優先する")]
    public bool enableHighZPriority = true;

    [Tooltip("左右のデータを比較対象とする有効時間（秒）。SELECTEDが出ない場合はここを上げてください")]
    public float handDataExpiry = 0.2f;

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

        // チュートリアル(3)またはゲーム(5)の時のみ動作
        int state = oscManager.GetInt("State");
        if (state != 3 && state != 5)
            return;

        ProcessPaddleControl();
    }

    #endregion

    #region Paddle Control

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
        LogDebug($"<color=yellow>{position.x:F2})");

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

            // 反対側の手との比較
            if (isRight) {
                bool leftIsActive = (currentTime - _lastLeftTimestamp) < handDataExpiry;
                if (leftIsActive) {
                    if (_lastLeftZ > actualZ) {
                        //LogDebug($"<color=yellow>[優先判定] 左右両方を検知：左が高い(Z:{_lastLeftZ:F2})ため、右(Z:{actualZ:F2})を無視します</color>");
                        return;
                    } else {
                       // LogDebug($"<color=cyan>[優先判定] 左右両方を検知：右が高い(Z:{actualZ:F2})ため、右を優先します！</color>");
                    }
                }
            } else {
                bool rightIsActive = (currentTime - _lastRightTimestamp) < handDataExpiry;
                if (rightIsActive) {
                    if (_lastRightZ > actualZ) {
                       // LogDebug($"<color=cyan>[優先判定] 左右両方を検知：右が高い(Z:{_lastRightZ:F2})ため、左(Z:{actualZ:F2})を無視します</color>");
                        return;
                    } else {
                       // LogDebug($"<color=yellow>[優先判定] 左右両方を検知：左が高い(Z:{actualZ:F2})ため、左を優先します！</color>");
                    }
                }
            }
        }

        // Z軸条件フィルタ
        if (enableConditionalAxis)
        {
            if (actualZ < conditionalAxisMin || actualZ > conditionalAxisMax)
            {
                LogDebug($"Z={actualZ:F3} は範囲外です [{conditionalAxisMin:F3}, {conditionalAxisMax:F3}]");
                return;
            }
        }

        // ベクトルの閾値チェック
        if (Mathf.Abs(vectorY) < vectorThreshold)
        {
            return;
        }

        // 左右別クールダウンチェック
        if (isRight)
        {
            if (Time.time - _lastRightSendTime < cooldownTime) return;
        }
        else
        {
            if (Time.time - _lastLeftSendTime < cooldownTime) return;
        }

        bool isForward = vectorY > 0;
        int direction = DeterminePaddleDirection(isRight, isForward);
        SendPaddleDirection(direction, isRight);
    }

    int DeterminePaddleDirection(bool isRight, bool isForward)
    {
        if (invertLeftRight) isRight = !isRight; //

        if (ignoreForwardBackward) return isRight ? 1 : 2; //
        
        if (isForward) return isRight ? 1 : 2; //
        return isRight ? 3 : 4; //
    }

    void SendPaddleDirection(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction); //
        oscManager.SendMessage("/paddle"); //

        if (isRight) _lastRightSendTime = Time.time;
        else _lastLeftSendTime = Time.time;

        string side = isRight ? "【右】" : "【左】";
        string dirName = GetDirectionName(direction);
        LogDebug($"<color=white><b>[送信] {side} を送信しました！ (方向: {dirName})</b></color>");

        onPaddleSent?.Invoke(direction); //
    }

    string GetDirectionName(int direction)
    {
        switch (direction)
        {
            case 1: return "右前進";
            case 2: return "左前進";
            case 3: return "右後進";
            case 4: return "左後進";
            default: return "不明";
        }
    }

    #endregion

    #region Public Methods

    public Vector3 GetCurrentPosition() //
    {
        if (oscManager == null) return Vector3.zero;
        Vector3 position = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
        float boxZ = oscManager.GetFloat("BoxZ");
        return new Vector3(position.x, position.y, position.z + boxZ * 0.5f);
    }

    public void SetZMin(float value) //
    {
        conditionalAxisMin = value;
        _debugZMin = value;
    }

    public void SetZMax(float value) //
    {
        conditionalAxisMax = value;
        _debugZMax = value;
    }

    #endregion

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[PaddleController] {message}"); }
}