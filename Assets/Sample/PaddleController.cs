using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 【検証モード搭載】Yベクトルの符号反転検知 ＋ 二重チェック ＋ 減少継続検知
/// 円運動パドリングの取りこぼしを防ぐための最適化モデル
/// </summary>
public class PaddleController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    public MultiAddressOSCManager oscManager;

    [Header("Paddle Settings (Y-Flip Logic)")]
    [Tooltip("判定に使うX軸の中心位置")]
    public float xCenterPosition = 0f;

    [Tooltip("左右個別の送信クールダウン（秒）")]
    public float cooldownTime = 0.1f;

    [Header("Advanced Verification Modes (円運動対策)")]
    [Tooltip("① 二重チェックモード: 符号反転を逃しても、Yベクトルが巨大なら発火")]
    public bool enableDoubleCheckMode = false;
    public float emergencyYThreshold = 0.8f;

    [Tooltip("② Y-Point 減少継続モード: 2フレーム連続減少を『溜め』として拾う")]
    public bool enableYPointDecreaseMode = false;

    [Header("Z-Axis Filtering")]
    [Tooltip("Z軸条件フィルタを有効にする")]
    public bool enableConditionalAxis = true;
    public float conditionalAxisMin = 0f;
    public float conditionalAxisMax = 2.0f;

    [Header("Mode Settings")]
    [Tooltip("左右を反転する（1↔2, 3↔4）")]
    public bool invertLeftRight = false;

    [Header("Events")]
    public UnityEvent<int> onPaddleSent;

    [Header("Debug")]
    public bool enableDebugLog = true;
    [SerializeField] private float _debugZMin;
    [SerializeField] private float _debugZMax;

    #endregion

    #region Private Variables

    private float _prevRightY = 0f;
    private float _prevLeftY = 0f;
    private float _rightCooldownTimer = 0f;
    private float _leftCooldownTimer = 0f;

    // ②モード用：連続減少カウント
    private int _rightDecreaseCount = 0;
    private int _leftDecreaseCount = 0;

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        if (oscManager == null) return;

        int state = oscManager.GetInt("State");
        if (state != 3 && state != 5) return;

        ProcessPaddleControl();
        UpdateTimers();
    }

    private void UpdateTimers()
    {
        if (_rightCooldownTimer > 0) _rightCooldownTimer -= Time.deltaTime;
        if (_leftCooldownTimer > 0) _leftCooldownTimer -= Time.deltaTime;
    }

    #endregion

    #region Core Paddle Logic

    void ProcessPaddleControl()
    {
        int count = oscManager.GetInt("Count");
        if (count <= 0) return;

        // 1. 全点群からX軸の「一番左」と「一番右」の2点を抽出
        HandData leftEnd = GetExtremeXPoint(count, true);
        HandData rightEnd = GetExtremeXPoint(count, false);

        // 2. 抽出した2点のうち「より高い点（手元）」をアクション対象として選別
        HandData targetPoint = SelectHigherTarget(leftEnd, rightEnd);

        if (!targetPoint.isValid) return;

        // 3. 左右どちらのパドル領域か判定
        bool isRightSide = targetPoint.posX >= xCenterPosition;

        // 4. 左右独立した判定処理
        if (isRightSide)
        {
            CheckAndSend(ref _prevRightY, ref _rightCooldownTimer, ref _rightDecreaseCount, targetPoint.vecY, true);
        }
        else
        {
            CheckAndSend(ref _prevLeftY, ref _leftCooldownTimer, ref _leftDecreaseCount, targetPoint.vecY, false);
        }
    }

    /// <summary>
    /// 各種検証モードを統合した送信判定
    /// </summary>
    void CheckAndSend(ref float prevY, ref float cooldown, ref int decreaseCount, float currentY, bool isRight)
    {
        bool triggered = false;

        // --- 基本: 符号反転検知 (負から正へ) ---
        // ※現状の極性が「正から負」なら prevY > 0 && currentY < 0 に書き換えてください
        if (prevY < 0 && currentY > 0)
        {
            triggered = true;
            if (enableDebugLog) LogDebug($"{(isRight ? "右" : "左")}: 符号反転で検知");
        }

        // --- ① 二重チェックモード (絶対値による救済) ---
        if (!triggered && enableDoubleCheckMode)
        {
            if (currentY > emergencyYThreshold)
            {
                triggered = true;
                if (enableDebugLog) LogDebug($"{(isRight ? "右" : "左")}: 二重チェック(閾値超え)で救済");
            }
        }

        // --- ② Y-Point 減少継続モード (予兆検知) ---
        if (!triggered && enableYPointDecreaseMode)
        {
            // 「減少（負の方向への加速）」をカウント
            if (currentY < prevY)
            {
                decreaseCount++;
                if (decreaseCount >= 2)
                {
                    triggered = true;
                    decreaseCount = 0;
                    if (enableDebugLog) LogDebug($"{(isRight ? "右" : "左")}: 2フレーム連続減少で救済");
                }
            }
            else
            {
                decreaseCount = 0;
            }
        }

        // --- 送信実行 ---
        if (triggered && cooldown <= 0)
        {
            int direction = DetermineDirection(isRight);
            SendPaddleDirection(direction, isRight);
            cooldown = cooldownTime;
        }

        prevY = currentY;
    }

    HandData GetExtremeXPoint(int count, bool findMin)
    {
        HandData extreme = new HandData { isValid = false, posX = findMin ? 999f : -999f };
        float boxZ = oscManager.GetFloat("BoxZ");

        for (int i = 1; i <= count; i++)
        {
            string s = i.ToString();
            float px = oscManager.GetFloat("PositionX" + s);
            float pz = oscManager.GetFloat("PositionZ" + s) + (boxZ * 0.5f);
            float vy = oscManager.GetFloat("VectorY" + s);

            if (pz < conditionalAxisMin || pz > conditionalAxisMax) continue;

            if (findMin) { if (px < extreme.posX) { SetData(ref extreme, px, pz, vy, s); } }
            else { if (px > extreme.posX) { SetData(ref extreme, px, pz, vy, s); } }
        }
        return extreme;
    }

    void SetData(ref HandData data, float x, float z, float vy, string id)
    {
        data.isValid = true; data.posX = x; data.posZ = z; data.vecY = vy; data.idSuffix = id;
    }

    HandData SelectHigherTarget(HandData a, HandData b)
    {
        if (!a.isValid && !b.isValid) return a;
        if (!a.isValid) return b;
        if (!b.isValid) return a;
        return (a.posZ > b.posZ) ? a : b;
    }

    int DetermineDirection(bool isRight)
    {
        if (invertLeftRight) isRight = !isRight;
        return isRight ? 1 : 2;
    }

    void SendPaddleDirection(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");
        onPaddleSent?.Invoke(direction);

        if (enableDebugLog)
        {
            string side = isRight ? "【右】" : "【左】";
            Debug.Log($"<color=lime><b>[送信] {side} (ID: {direction})</b></color>");
        }
    }

    #endregion

    #region Public / Utility

    public void SetZMin(float value) { conditionalAxisMin = value; _debugZMin = value; }
    public void SetZMax(float value) { conditionalAxisMax = value; _debugZMax = value; }

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[PaddleController] {message}"); }

    private struct HandData
    {
        public bool isValid;
        public float posX;
        public float posZ;
        public float vecY;
        public string idSuffix;
    }
    #endregion
}