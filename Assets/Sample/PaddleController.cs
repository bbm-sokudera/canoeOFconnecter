using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 【最新最適解】Yベクトルの符号反転検知 ＆ X軸両端抽出による
/// 超低遅延・高耐性パドルコントローラー
/// </summary>
public class PaddleController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    public MultiAddressOSCManager oscManager;

    [Header("Paddle Settings (Y-Flip Logic)")]
    [Tooltip("判定に使うX軸の中心位置")]
    public float xCenterPosition = 0f;

    [Tooltip("左右個別の送信クールダウン（秒）※0で符号反転のみに依存")]
    public float cooldownTime = 0.1f;

    [Header("Z-Axis Filtering")]
    [Tooltip("Z軸条件フィルタを有効にする")]
    public bool enableConditionalAxis = true;

    [Tooltip("Z軸の最小値（AutoHeightから上書きされる）")]
    public float conditionalAxisMin = 0f;

    [Tooltip("Z軸の最大値（この高さ以上は動作を無視）")]
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

    // 左右独立した符号管理とタイマー
    private float _prevRightY = 0f;
    private float _prevLeftY = 0f;
    private float _rightCooldownTimer = 0f;
    private float _leftCooldownTimer = 0f;

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

        // 1. 全点群からX軸の「一番左」と「一番右」の2点を抽出（内側のノイズを排除）
        HandData leftEnd = GetExtremeXPoint(count, true);  // Min X
        HandData rightEnd = GetExtremeXPoint(count, false); // Max X

        // 2. 抽出した2点のうち「より高い点（手元）」をアクション対象として選別
        HandData targetPoint = SelectHigherTarget(leftEnd, rightEnd);

        if (!targetPoint.isValid) return;

        // 3. 左右どちらのパドル領域か判定
        bool isRightSide = targetPoint.posX >= xCenterPosition;

        // 4. 左右独立した符号反転判定と送信
        if (isRightSide)
        {
            CheckFlipAndSend(ref _prevRightY, ref _rightCooldownTimer, targetPoint.vecY, true);
        }
        else
        {
            CheckFlipAndSend(ref _prevLeftY, ref _leftCooldownTimer, targetPoint.vecY, false);
        }
    }

    /// <summary>
    /// Yベクトルの「負（戻し）」から「正（押し出し）」への切り替わりを最速検知
    /// </summary>
    void CheckFlipAndSend(ref float prevY, ref float cooldown, float currentY, bool isRight)
    {
        // 条件: クールダウン中ではなく、前回がプラスで、今回がマイナスになった瞬間を
        if (cooldown <= 0 && prevY > 0 && currentY < 0)
        {
            int direction = DetermineDirection(isRight);
            SendPaddleDirection(direction, isRight);
            cooldown = cooldownTime; // 送信後にその側の窓口を閉鎖
        }
        
        prevY = currentY; // 前回の符号を更新
    }

    /// <summary>
    /// 全データの中からX座標が最小(Min)または最大(Max)の点を抽出する
    /// </summary>
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

            // Z高度制限内かチェック
            if (pz < conditionalAxisMin || pz > conditionalAxisMax) continue;

            if (findMin)
            {
                if (px < extreme.posX) { SetData(ref extreme, px, pz, vy, s); }
            }
            else
            {
                if (px > extreme.posX) { SetData(ref extreme, px, pz, vy, s); }
            }
        }
        return extreme;
    }

    void SetData(ref HandData data, float x, float z, float vy, string id)
    {
        data.isValid = true;
        data.posX = x;
        data.posZ = z;
        data.vecY = vy;
        data.idSuffix = id;
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
        // 「負→正」の反転で入ってくるため、ここは常に前進(1 or 2)を想定
        return isRight ? 1 : 2;
    }

    void SendPaddleDirection(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");

        string side = isRight ? "【右】" : "【左】";
        LogDebug($"<color=lime><b>[最速送信] {side} 符号反転を検知！ (ID: {direction})</b></color>");

        onPaddleSent?.Invoke(direction);
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