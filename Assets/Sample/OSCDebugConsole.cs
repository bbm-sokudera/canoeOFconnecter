using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/// <summary>
/// ゲームビュー内でConsoleのようなログ表示を行うスクリプト
/// OSC関連のログや一般的なDebug.Logをゲーム内で確認できます
/// </summary>
public class OSCDebugConsole : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ログを表示するText（またはTextMeshPro）")]
    public Text logText;

    [Tooltip("スクロール用のScrollRect（オプション）")]
    public ScrollRect scrollRect;

    [Header("Console Settings")]
    [Tooltip("表示する最大ログ行数")]
    public int maxLogLines = 50;

    [Tooltip("Unity標準のDebug.Logをキャプチャする")]
    public bool captureUnityLog = true;

    [Tooltip("通常ログを表示する")]
    public bool showInfoLogs = true;

    [Tooltip("警告ログを表示する")]
    public bool showWarningLogs = true;

    [Tooltip("エラーログを表示する")]
    public bool showErrorLogs = true;

    [Header("OSC Manager Reference (Optional)")]
    [Tooltip("OSCマネージャーのイベントを監視する場合に設定")]
    public OSCXPositionYVectorManager oscManager;

    [Header("Log Colors")]
    [Tooltip("通常ログの色")]
    public Color infoColor = Color.white;

    [Tooltip("警告ログの色")]
    public Color warningColor = Color.yellow;

    [Tooltip("エラーログの色")]
    public Color errorColor = Color.red;

    [Tooltip("OSC送信ログの色")]
    public Color oscSendColor = Color.green;

    [Tooltip("OSC受信ログの色")]
    public Color oscReceiveColor = Color.cyan;

    [Header("Auto Scroll")]
    [Tooltip("新しいログが追加されたら自動で下にスクロール")]
    public bool autoScrollToBottom = true;

    // ログのリスト
    private Queue<string> _logQueue = new Queue<string>();
    private bool _needsUpdate = false;

    void Start()
    {
        // Unity標準ログのキャプチャ
        if (captureUnityLog)
        {
            Application.logMessageReceived += HandleUnityLog;
        }

        // OSCマネージャーのイベントに登録
        if (oscManager != null)
        {
            oscManager.onValueSent.AddListener(OnOSCValueSent);
            oscManager.onDirectionDetected.AddListener(OnDirectionDetected);
        }

        // 初期メッセージ
        AddLog("[Console] OSC Debug Console started.", infoColor);
    }

    void OnDestroy()
    {
        // イベントから登録解除
        if (captureUnityLog)
        {
            Application.logMessageReceived -= HandleUnityLog;
        }

        if (oscManager != null)
        {
            oscManager.onValueSent.RemoveListener(OnOSCValueSent);
            oscManager.onDirectionDetected.RemoveListener(OnDirectionDetected);
        }
    }

    void Update()
    {
        // ログが更新されている場合、UIを更新
        if (_needsUpdate)
        {
            UpdateLogDisplay();
            _needsUpdate = false;

            // 自動スクロール
            if (autoScrollToBottom && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    /// <summary>
    /// Unity標準のログをハンドル
    /// </summary>
    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        Color color = infoColor;
        bool shouldShow = false;

        switch (type)
        {
            case LogType.Log:
                color = infoColor;
                shouldShow = showInfoLogs;
                break;

            case LogType.Warning:
                color = warningColor;
                shouldShow = showWarningLogs;
                break;

            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                color = errorColor;
                shouldShow = showErrorLogs;
                break;
        }

        if (shouldShow)
        {
            string prefix = type == LogType.Log ? "" : $"[{type}] ";
            AddLog($"{prefix}{logString}", color);
        }
    }

    /// <summary>
    /// OSC値が送信された時
    /// </summary>
    private void OnOSCValueSent(int value)
    {
        string directionName = GetDirectionName(value);
        AddLog($"[OSC SEND] Value: {value} ({directionName})", oscSendColor);
    }

    /// <summary>
    /// 方向が検出された時
    /// </summary>
    private void OnDirectionDetected(OSCXPositionYVectorManager.CombinedDirection direction)
    {
        AddLog($"[OSC DETECT] Direction: {direction}", oscReceiveColor);
    }

    /// <summary>
    /// ログを追加
    /// </summary>
    public void AddLog(string message, Color color)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string formattedLog = $"<color=#{colorHex}>[{timestamp}] {message}</color>";

        _logQueue.Enqueue(formattedLog);

        // 最大行数を超えたら古いログを削除
        while (_logQueue.Count > maxLogLines)
        {
            _logQueue.Dequeue();
        }

        _needsUpdate = true;
    }

    /// <summary>
    /// ログ表示を更新
    /// </summary>
    private void UpdateLogDisplay()
    {
        if (logText == null)
            return;

        logText.text = string.Join("\n", _logQueue);
    }

    /// <summary>
    /// ログをクリア
    /// </summary>
    public void ClearLogs()
    {
        _logQueue.Clear();
        _needsUpdate = true;
        AddLog("[Console] Logs cleared.", infoColor);
    }

    /// <summary>
    /// 値から方向名を取得
    /// </summary>
    private string GetDirectionName(int value)
    {
        switch (value)
        {
            case 0: return "Right";
            case 1: return "Right+Backward";
            case 2: return "Left";
            case 3: return "Left+Backward";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// カスタムログを追加（外部から使用可能）
    /// </summary>
    public void LogInfo(string message)
    {
        AddLog(message, infoColor);
    }

    public void LogWarning(string message)
    {
        AddLog($"[Warning] {message}", warningColor);
    }

    public void LogError(string message)
    {
        AddLog($"[Error] {message}", errorColor);
    }

    public void LogOSCSend(string message)
    {
        AddLog($"[OSC SEND] {message}", oscSendColor);
    }

    public void LogOSCReceive(string message)
    {
        AddLog($"[OSC RECV] {message}", oscReceiveColor);
    }
}
