using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// /cropbox/autoheight の送信を管理するコントローラー
/// </summary>
public class AutoHeightController : MonoBehaviour
{
    #region Inspector Settings

    [Header("References")]
    [Tooltip("MultiAddressOSCManager")]
    public MultiAddressOSCManager oscManager;

    [Header("Auto Height Settings")]
    [Tooltip("自動高さ調整の値")]
    public int autoHeightValue = 100;

    [Header("Events")]
    [Tooltip("AutoHeightを送信した時のイベント")]
    public UnityEvent<int> onAutoHeightSent;

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    #endregion

    #region Public Methods

    /// <summary>
    /// AutoHeightを送信
    /// </summary>
    public void SendAutoHeight(int value)
    {
        if (oscManager == null)
        {
            Debug.LogWarning("[AutoHeightController] OSC Manager is not assigned!");
            return;
        }

        oscManager.SetInt("AutoHeight", value);
        oscManager.SendMessage("/cropbox/autoheight");

        LogDebug($"AutoHeight sent: {value}");

        // イベント発火
        onAutoHeightSent?.Invoke(value);
    }

    /// <summary>
    /// 現在設定されている値を送信
    /// </summary>
    public void SendCurrentValue()
    {
        SendAutoHeight(autoHeightValue);
    }

    /// <summary>
    /// 値を設定して送信
    /// </summary>
    public void SetAndSendAutoHeight(int value)
    {
        autoHeightValue = value;
        SendAutoHeight(value);
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[AutoHeightController] {message}");
        }
    }

    #endregion
}
