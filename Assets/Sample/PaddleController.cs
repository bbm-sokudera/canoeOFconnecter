using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 【最新検証版】符号反転検知 ＋ 旋回ロック付き二重チェック(Emergency Y)
/// 円運動の前進を救済しつつ、旋回時の精度を維持する
/// </summary>
public class PaddleController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    public MultiAddressOSCManager oscManager;

    [Header("Paddle Settings (Y-Flip Logic)")]
    public float xCenterPosition = 0f;
    [Tooltip("左右個別の送信クールダウン")]
    public float cooldownTime = 0.15f;

    [Header("Advanced Verification Modes")]
    [Tooltip("① 二重チェックモード: 符号反転を逃しても、Yベクトルが巨大なら発火")]
    public bool enableDoubleCheckMode = true;
    [Tooltip("救済発火させるYベクトルの最小値")]
    public float emergencyYThreshold = 0.8f;
    [Tooltip("同じ側を連打（旋回）した際、救済モードを無効化する時間")]
    public float turnOffsetTime = 0.5f;

    [Header("Z-Axis Filtering")]
    public bool enableConditionalAxis = true;
    public float conditionalAxisMin = 0.45f;
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

    // 旋回制御用
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
        
        // 旋回時の救済ロックタイマー
        if (_rightTurnBlockTimer > 0) _rightTurnBlockTimer -= Time.deltaTime;
        if (_leftTurnBlockTimer > 0) _leftTurnBlockTimer -= Time.deltaTime;
    }

    #endregion

    #region Core Paddle Logic

    void ProcessPaddleControl()
    {
        int count = oscManager.GetInt("Count");
        if (count <= 0) return;

        // 1. X軸両端抽出（ノイズ対策）
        HandData leftEnd = GetExtremeXPoint(count, true);
        HandData rightEnd = GetExtremeXPoint(count, false);

        // 2. 高Zターゲット選定
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

        // --- A. 基本ロジック: 符号反転 (- to +) ---
        // 円運動でもここを通るのが理想だが、漏れた場合をBで救済する
        if (prevY < 0 && currentY > 0)
        {
            triggered = true;
        }

        // --- B. ① 二重チェックモード (旋回ブロック機能付き) ---
        if (!triggered && enableDoubleCheckMode)
        {
            float turnBlockTimer = isRight ? _rightTurnBlockTimer : _leftTurnBlockTimer;

            // 旋回中でない（交互漕ぎである）かつ、Yが閾値を超えていれば救済発火
            if (turnBlockTimer <= 0 && currentY > emergencyYThreshold)
            {
                triggered = true;
                if (enableDebugLog) LogDebug($"救済発火: {(isRight ? "右" : "左")} (Y:{currentY:F2})");
            }
        }

        // --- 送信判定 ---
        if (triggered && cooldown <= 0)
        {
            int direction = DetermineDirection(isRight);

            // 旋回判定：前回と同じ側（右:1,3 / 左:2,4）を連続で漕いだか
            bool isSameSide = false;
            if (isRight && (_lastSentDirection == 1 || _lastSentDirection == 3)) isSameSide = true;
            if (!isRight && (_lastSentDirection == 2 || _lastSentDirection == 4)) isSameSide = true;

            SendPaddleDirection(direction, isRight);

            // 状態更新
            _lastSentDirection = direction;
            cooldown = cooldownTime;

            // 旋回（同側連打）なら救済モードを一定時間ロック
            if (isSameSide)
            {
                if (isRight) _rightTurnBlockTimer = turnOffsetTime;
                else _leftTurnBlockTimer = turnOffsetTime;
            }
        }

        prevY = currentY;
    }

    #endregion

    #region Sub Methods (Min/Max X & Target Selection)

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
        data.isValid = true;
        data.posX = x;
        data.posZ = z;
        data.vecY = vy;
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
        return isRight ? 1 : 2; // 現状は前進のみ
    }

    void SendPaddleDirection(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");
        onPaddleSent?.Invoke(direction);
    }

    public void SetZMin(float value) { conditionalAxisMin = value; _debugZMin = value; }
    public void SetZMax(float value) { conditionalAxisMax = value; _debugZMax = value; }
    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[Paddle] {message}"); }

    private struct HandData { public bool isValid; public float posX; public float posZ; public float vecY; }

    #endregion
}