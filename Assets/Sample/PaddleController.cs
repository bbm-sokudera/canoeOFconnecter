using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 一括送信OSC形式に対応したパドルコントローラー
/// 2点ぴったり検知されたフレームのみを採用し、高い方を優先して送信する
/// </summary>
public class PaddleController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    public MultiAddressOSCManager oscManager;

    [Header("Paddle Settings")]
    public float vectorThreshold = 0.1f;
    public float xCenterPosition = 0f;
    public float cooldownTime = 0.3f;

    [Header("Z-Axis Filter")]
    public bool enableConditionalAxis = true;
    public float conditionalAxisMin = 0.45f;
    public float conditionalAxisMax = 2.0f;

    [Header("Mode Settings")]
    public bool ignoreForwardBackward = true;
    public bool invertLeftRight = false;

    [Header("Events")]
    public UnityEvent<int> onPaddleSent;

    [Header("Debug")]
    public bool enableDebugLog = true;

    [Header("Monitor")]
    [SerializeField] private float _debugZMin;
    [SerializeField] private float _debugZMax;

    #endregion

    private float _lastSendTime = -999f;

    void Update()
    {
        if (oscManager == null) return;

        // チュートリアル(3)またはゲーム(5)の時のみ動作
        int state = oscManager.GetInt("State");
        if (state != 3 && state != 5) return;

        ProcessPaddleControl();
    }

    void ProcessPaddleControl()
    {
        // 【重要】検知数が「2」の時以外はノイズ・不完全データとして無視
        int count = oscManager.GetInt("Count");
        if (count != 2) return;

        float boxZ = oscManager.GetFloat("BoxZ");
        
        // 2点のデータを取得
        float z1 = oscManager.GetFloat("PositionZ1") + boxZ * 0.5f;
        float z2 = oscManager.GetFloat("PositionZ2") + boxZ * 0.5f;
        float x1 = oscManager.GetFloat("PositionX1");
        float x2 = oscManager.GetFloat("PositionX2");
        float v1 = oscManager.GetFloat("VectorY1");
        float v2 = oscManager.GetFloat("VectorY2");

        // 両方の点がZ範囲内かチェック
        bool z1Valid = !enableConditionalAxis || (z1 >= conditionalAxisMin && z1 <= conditionalAxisMax);
        bool z2Valid = !enableConditionalAxis || (z2 >= conditionalAxisMin && z2 <= conditionalAxisMax);

        // どちらかが範囲外なら、不完全な構えとして処理しない
        if (!z1Valid || !z2Valid) return;

        // 高い方のデータを採用
        bool isFirstBest = z1 > z2;
        float bestX = isFirstBest ? x1 : x2;
        float bestVecY = isFirstBest ? v1 : v2;

        // ベクトルの閾値チェック
        if (Mathf.Abs(bestVecY) < vectorThreshold) return;

        // クールダウンチェック
        if (Time.time - _lastSendTime < cooldownTime) return;

        // 左右・前後判定
        bool isRight = bestX >= xCenterPosition;
        if (invertLeftRight) isRight = !isRight;

        int direction = 0;
        if (ignoreForwardBackward) {
            direction = isRight ? 1 : 2;
        } else {
            bool isForward = bestVecY > 0;
            if (isForward) direction = isRight ? 1 : 2;
            else direction = isRight ? 3 : 4;
        }

        SendPaddleOSC(direction, isRight);
    }

    void SendPaddleOSC(int direction, bool isRight)
    {
        oscManager.SetInt("PaddleDirection", direction);
        oscManager.SendMessage("/paddle");
        _lastSendTime = Time.time;

        string side = isRight ? "【右】" : "【左】";
        LogDebug($"<color=white><b>[送信] {side} を送信！方向:{direction}</b></color>");
        onPaddleSent?.Invoke(direction);
    }

    public void SetZMin(float value) { conditionalAxisMin = value; _debugZMin = value; }
    public void SetZMax(float value) { conditionalAxisMax = value; _debugZMax = value; }

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[PaddleController] {message}"); }
}