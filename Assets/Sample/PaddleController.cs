using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 【最終最適化版】符号反転検知 ＋ 左右交互漕ぎ救済ロジック搭載
/// 円を描く前進モーションの取りこぼしを、二重チェックモードで補完する
/// </summary>
public class PaddleController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    public MultiAddressOSCManager oscManager;

    [Header("Paddle Settings (Y-Flip Logic)")]
    public float xCenterPosition = 0f;
    public float cooldownTime = 0.1f;

    [Header("Advanced Verification (救済モード)")]
    [Tooltip("符号反転を逃してもYベクトルが巨大なら発火")]
    public bool enableDoubleCheckMode = true;
    [Tooltip("救済を発動させるYベクトルの閾値")]
    public float emergencyYThreshold = 0.8f;
    [Tooltip("同じ側を連打した際、救済を封印する時間")]
    public float turnOffsetTime = 0.5f;

    [Header("Z-Axis Filtering")]
    public bool enableConditionalAxis = true;
    public float conditionalAxisMin = 0f;
    public float conditionalAxisMax = 2.0f;

    [Header("Mode Settings")]
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

    // 救済モード制御用
    private int _lastSentDirection = 0; // 1:右前, 2:左前, 3:右後, 4:左後
    private float _rightTurnBlockTimer = 0f;
    private float _leftTurnBlockTimer = 0f;

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

        // 旋回ブロックタイマー（救済制限時間）の更新
        if (_rightTurnBlockTimer > 0) _rightTurnBlockTimer -= Time.deltaTime;
        if (_leftTurnBlockTimer > 0) _leftTurnBlockTimer -= Time.deltaTime;
    }

    #endregion

    #region Core Paddle Logic

    void ProcessPaddleControl()
    {
        int count = oscManager.GetInt("Count");
        if (count <= 0) return;

        // 1. 全点群からX軸の最小・最大の2点を抽出
        HandData leftEnd = GetExtremeXPoint(count, true);
        HandData rightEnd = GetExtremeXPoint(count, false);

        // 2. 高い方をアクション対象として選別
        HandData targetPoint = SelectHigherTarget(leftEnd, rightEnd);
        if (!targetPoint.isValid) return;

        // 3. 左右判定
        bool isRightSide = targetPoint.posX >= xCenterPosition;

        // 4. 判定ロジックへ
        if (isRightSide)
        {
            CheckFlipAndSend(ref _prevRightY, ref _rightCooldownTimer, targetPoint.vecY, true);
        }
        else
        {
            CheckFlipAndSend(ref _prevLeftY, ref _leftCooldownTimer, targetPoint.vecY, false);
        }
    }

    void CheckFlipAndSend(ref float prevY, ref float cooldown, float currentY, bool isRight)
    {
        bool triggered = false;
        bool isFlipTriggered = false;

        // --- A. 基本ロジック: 符号反転 (- to +) ---
        if (prevY >= 0 && currentY < 0)
        {
            triggered = true;
            isFlipTriggered = true;
        }

        // --- B. ① 二重チェックモード (救済) ---
        if (!triggered && enableDoubleCheckMode)
        {
            float blockTimer = isRight ? _rightTurnBlockTimer : _leftTurnBlockTimer;
            
            // 制限時間が切れている時のみ救済を発動
            if (blockTimer <= 0 && currentY > emergencyYThreshold)
            {
                triggered = true;
                if (enableDebugLog) LogDebug($"救済発火: {(isRight ? "右" : "左")} (Y:{currentY:F2})");
            }
        }

        // --- 送信判定 ---
        if (triggered && cooldown <= 0)
        {
            int direction = DetermineDirection(isRight);
            
            // 旋回判定：前回と同じ側か？
            bool isSameSide = false;
            if (isRight && (_lastSentDirection == 1 || _lastSentDirection == 3)) isSameSide = true;
            if (!isRight && (_lastSentDirection == 2 || _lastSentDirection == 4)) isSameSide = true;

            // 送信
            SendPaddleDirection(direction, isRight);

            // 状態更新
            _lastSentDirection = direction;
            cooldown = cooldownTime;

            if (isSameSide)
            {
                // 旋回（連打）時はタイマーをセットして救済を一時封印
                if (isRight) _rightTurnBlockTimer = turnOffsetTime;
                else _leftTurnBlockTimer = turnOffsetTime;
            }
            else
            {
                // 左右交互に来た場合は、両方の救済ブロックを即座に解除（救済復活）
                _rightTurnBlockTimer = 0;
                _leftTurnBlockTimer = 0;
                if (enableDebugLog) LogDebug("左右交互入力を検知：救済ブロックをリセットしました");
            }
        }
        
        prevY = currentY;
    }

    // --- 以下、ユーティリティメソッド (既存を維持) ---

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
            if (findMin) { if (px < extreme.posX) SetData(ref extreme, px, pz, vy); }
            else { if (px > extreme.posX) SetData(ref extreme, px, pz, vy); }
        }
        return extreme;
    }

    void SetData(ref HandData data, float x, float z, float vy)
    {
        data.isValid = true; data.posX = x; data.posZ = z; data.vecY = vy;
    }

    HandData SelectHigherTarget(HandData a, HandData b)
    {
        if (!a.isValid) return b; if (!b.isValid) return a;
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
    }

    public void SetZMin(float value) { conditionalAxisMin = value; _debugZMin = value; }
    public void SetZMax(float value) { conditionalAxisMax = value; _debugZMax = value; }

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[PaddleController] {message}"); }

    private struct HandData { public bool isValid; public float posX; public float posZ; public float vecY; }
    #endregion
}