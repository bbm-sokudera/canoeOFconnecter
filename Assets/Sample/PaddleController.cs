using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一括送信OSC形式に対応したパドルコントローラー
/// 同一フレーム内の複数データから最適な1点を選出し、日本語ログで通知する
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
        // 1. 同一フレーム内の検知数を取得
        int count = oscManager.GetInt("Count");

        // 【懸念対策】2点以下のみ検知。3点以上はノイズとして無視
        if (count <= 0 || count > 2)
        {
            if (count > 2) LogDebug($"<color=orange>[制限] 検知数過多({count})のため、ノイズとして無視します</color>");
            return;
        }

        // 2. 1点目と2点目のデータを取得し、有効な（Z範囲内の）ものを選別
        HandData bestHand = GetBestHandInFrame(count);

        // 有効な手が1つもなければ終了
        if (!bestHand.isValid) return;

        // 3. ベクトルの閾値チェック
        if (Mathf.Abs(bestHand.vecY) < vectorThreshold)
            return;

        // 4. クールダウンチェック
        if (Time.time - _lastSendTime < cooldownTime)
            return;

        // 5. 送信処理へ
        bool isRight = bestHand.posX >= xCenterPosition;
        bool isForward = bestHand.vecY > 0;
        int direction = DeterminePaddleDirection(isRight, isForward);
        
        SendPaddleDirection(direction, isRight);
    }

    /// <summary>
    /// 同一フレーム内のデータから、Z軸範囲内で最もZが高い1点を選び出す
    /// </summary>
    HandData GetBestHandInFrame(int count)
    {
        HandData best = new HandData { isValid = false, posZ = -999f };
        float boxZ = oscManager.GetFloat("BoxZ");

        // 2点（まで）をループで確認
        for (int i = 1; i <= count; i++)
        {
            // パラメータ名が ID1, PositionX1... となっている想定
            string suffix = i.ToString();
            float posX = oscManager.GetFloat("PositionX" + suffix);
            float posZ = oscManager.GetFloat("PositionZ" + suffix);
            float vecY = oscManager.GetFloat("VectorY" + suffix);
            float actualZ = posZ + boxZ * 0.5f;

            // Z軸条件フィルタ
            bool zInRange = !enableConditionalAxis || (actualZ >= conditionalAxisMin && actualZ <= conditionalAxisMax);

            if (zInRange)
            {
                // 今持っている候補よりZが高い、もしくは最初の1点目なら更新
                if (!best.isValid || actualZ > best.posZ)
                {
                    if (best.isValid && count == 2)
                    {
                        LogDebug($"<color=cyan>[優先判定] 左右両方を検知：Zが高い方(Z:{actualZ:F2})を優先します</color>");
                    }

                    best.isValid = true;
                    best.posX = posX;
                    best.posZ = actualZ;
                    best.vecY = vecY;
                    best.idSuffix = suffix;
                }
            }
            else
            {
                LogDebug($"点{suffix}(Z:{actualZ:F2}) は範囲外のため無視");
            }
        }

        return best;
    }

    int DeterminePaddleDirection(bool isRight, bool isForward)
    {
        if (invertLeftRight) isRight = !isRight;
        if (ignoreForwardBackward) return isRight ? 1 : 2;
        if (isForward) return isRight ? 1 : 2;
        return isRight ? 3 : 4;
    }

    void SendPaddleDirection(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");

        _lastSendTime = Time.time;

        string side = isRight ? "【右】" : "【左】";
        string dirName = GetDirectionName(direction);
        LogDebug($"<color=white><b>[送信] {side} を送信しました！ (方向: {dirName})</b></color>");

        onPaddleSent?.Invoke(direction);
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

    public Vector3 GetCurrentPosition()
    {
        if (oscManager == null) return Vector3.zero;
        // 基本的に1点目の位置を返す
        float boxZ = oscManager.GetFloat("BoxZ");
        return new Vector3(
            oscManager.GetFloat("PositionX1"),
            oscManager.GetFloat("PositionY1"),
            oscManager.GetFloat("PositionZ1") + boxZ * 0.5f
        );
    }

    public void SetZMin(float value)
    {
        conditionalAxisMin = value;
        _debugZMin = value;
    }

    public void SetZMax(float value)
    {
        conditionalAxisMax = value;
        _debugZMax = value;
    }

    #endregion

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[PaddleController] {message}"); }

    // フレーム内データ保持用
    private struct HandData
    {
        public bool isValid;
        public float posX;
        public float posZ;
        public float vecY;
        public string idSuffix;
    }
}