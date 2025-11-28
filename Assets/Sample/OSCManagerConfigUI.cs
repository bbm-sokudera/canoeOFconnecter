using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OSCXPositionYVectorManagerの設定をUIで操作するコントローラー
/// トグルとInputFieldを使って、主要なパラメータをリアルタイムで変更可能
/// </summary>
public class OSCManagerConfigUI : MonoBehaviour
{
    #region Inspector Settings

    [Header("Target Manager")]
    [Tooltip("設定対象のOSCマネージャー")]
    public OSCXPositionYVectorManager targetManager;

    [Header("Toggle UI Elements")]
    [Tooltip("前後無視モードのToggle（ON=前後無視、OFF=前後区別）")]
    public Toggle ignoreForwardBackwardToggle;

    [Tooltip("後進のみモードのToggle")]
    public Toggle enableBackwardModeToggle;

    [Tooltip("左右連続検出のToggle")]
    public Toggle enableSideDetectionToggle;

    [Header("InputField UI Elements")]
    [Tooltip("クールダウン時間（秒）のInputField")]
    public InputField cooldownTimeInput;

    [Tooltip("Y軸角度閾値（度）のInputField")]
    public InputField yAngleThresholdInput;

    [Tooltip("Y軸ベクトル最小閾値のInputField")]
    public InputField yVectorThresholdInput;

    [Header("Settings")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        SetupToggles();
        SetupInputFields();
        LoadCurrentValues();
    }

    #endregion

    #region Setup

    /// <summary>
    /// トグルのセットアップ
    /// </summary>
    void SetupToggles()
    {
        if (ignoreForwardBackwardToggle != null)
        {
            ignoreForwardBackwardToggle.onValueChanged.AddListener(OnIgnoreForwardBackwardChanged);
            LogDebug("Ignore Forward/Backward Toggle setup");
        }

        if (enableBackwardModeToggle != null)
        {
            enableBackwardModeToggle.onValueChanged.AddListener(OnBackwardModeChanged);
            LogDebug("Backward Mode Toggle setup");
        }

        if (enableSideDetectionToggle != null)
        {
            enableSideDetectionToggle.onValueChanged.AddListener(OnSideDetectionChanged);
            LogDebug("Side Detection Toggle setup");
        }
    }

    /// <summary>
    /// InputFieldのセットアップ
    /// </summary>
    void SetupInputFields()
    {
        if (cooldownTimeInput != null)
        {
            cooldownTimeInput.onEndEdit.AddListener(OnCooldownTimeChanged);
            LogDebug("Cooldown Time Input setup");
        }

        if (yAngleThresholdInput != null)
        {
            yAngleThresholdInput.onEndEdit.AddListener(OnYAngleThresholdChanged);
            LogDebug("Y Angle Threshold Input setup");
        }

        if (yVectorThresholdInput != null)
        {
            yVectorThresholdInput.onEndEdit.AddListener(OnYVectorThresholdChanged);
            LogDebug("Y Vector Threshold Input setup");
        }
    }

    /// <summary>
    /// 現在の設定値をUIにロード
    /// </summary>
    void LoadCurrentValues()
    {
        if (targetManager == null)
        {
            Debug.LogWarning("[OSCManagerConfigUI] Target Manager is not assigned!");
            return;
        }

        // Toggleの値をロード
        if (ignoreForwardBackwardToggle != null)
            ignoreForwardBackwardToggle.isOn = targetManager.ignoreForwardBackward;

        if (enableBackwardModeToggle != null)
            enableBackwardModeToggle.isOn = targetManager.enableConsecutiveBackwardDetection;

        if (enableSideDetectionToggle != null)
            enableSideDetectionToggle.isOn = targetManager.enableConsecutiveSideDetection;

        // InputFieldの値をロード
        if (cooldownTimeInput != null)
            cooldownTimeInput.text = targetManager.cooldownTime.ToString("F2");

        if (yAngleThresholdInput != null)
            yAngleThresholdInput.text = targetManager.yAngleThreshold.ToString("F1");

        if (yVectorThresholdInput != null)
            yVectorThresholdInput.text = targetManager.yVectorMagnitudeThreshold.ToString("F3");

        LogDebug("Current values loaded from manager");
    }

    #endregion

    #region Toggle Event Handlers

    /// <summary>
    /// 前後無視モードの変更
    /// </summary>
    void OnIgnoreForwardBackwardChanged(bool isOn)
    {
        if (targetManager == null)
            return;

        targetManager.ignoreForwardBackward = isOn;
        LogDebug($"Ignore Forward/Backward: {(isOn ? "ON (無視)" : "OFF (区別)")}");

        // 後進のみモードのToggleの有効/無効を切り替え
        if (enableBackwardModeToggle != null)
        {
            enableBackwardModeToggle.interactable = !isOn;
            LogDebug($"Backward Mode Toggle: {(!isOn ? "Enabled" : "Disabled")}");
        }
    }

    /// <summary>
    /// 後進のみモードの変更
    /// </summary>
    void OnBackwardModeChanged(bool isOn)
    {
        if (targetManager == null)
            return;

        targetManager.enableConsecutiveBackwardDetection = isOn;
        LogDebug($"Backward Mode: {(isOn ? "ON" : "OFF")}");
    }

    /// <summary>
    /// 左右連続検出の変更
    /// </summary>
    void OnSideDetectionChanged(bool isOn)
    {
        if (targetManager == null)
            return;

        targetManager.enableConsecutiveSideDetection = isOn;
        LogDebug($"Side Detection: {(isOn ? "ON" : "OFF")}");
    }

    #endregion

    #region InputField Event Handlers

    /// <summary>
    /// クールダウン時間の変更
    /// </summary>
    void OnCooldownTimeChanged(string value)
    {
        if (targetManager == null)
            return;

        if (float.TryParse(value, out float result))
        {
            // 0.01秒〜10秒の範囲にクランプ
            result = Mathf.Clamp(result, 0.01f, 10f);
            targetManager.cooldownTime = result;
            cooldownTimeInput.text = result.ToString("F2");
            LogDebug($"Cooldown Time: {result:F2}s");
        }
        else
        {
            // 無効な値の場合、元の値に戻す
            cooldownTimeInput.text = targetManager.cooldownTime.ToString("F2");
        }
    }

    /// <summary>
    /// Y軸角度閾値の変更
    /// </summary>
    void OnYAngleThresholdChanged(string value)
    {
        if (targetManager == null)
            return;

        if (float.TryParse(value, out float result))
        {
            // 0〜90度の範囲にクランプ
            result = Mathf.Clamp(result, 0f, 90f);
            targetManager.yAngleThreshold = result;
            yAngleThresholdInput.text = result.ToString("F1");
            LogDebug($"Y Angle Threshold: {result:F1}°");
        }
        else
        {
            // 無効な値の場合、元の値に戻す
            yAngleThresholdInput.text = targetManager.yAngleThreshold.ToString("F1");
        }
    }

    /// <summary>
    /// Y軸ベクトル最小閾値の変更
    /// </summary>
    void OnYVectorThresholdChanged(string value)
    {
        if (targetManager == null)
            return;

        if (float.TryParse(value, out float result))
        {
            // 0.001〜1.0の範囲にクランプ
            result = Mathf.Clamp(result, 0.001f, 1.0f);
            targetManager.yVectorMagnitudeThreshold = result;
            yVectorThresholdInput.text = result.ToString("F3");
            LogDebug($"Y Vector Threshold: {result:F3}");
        }
        else
        {
            // 無効な値の場合、元の値に戻す
            yVectorThresholdInput.text = targetManager.yVectorMagnitudeThreshold.ToString("F3");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 設定をリセット（デフォルト値に戻す）
    /// </summary>
    public void ResetToDefaults()
    {
        if (targetManager == null)
            return;

        // デフォルト値を設定
        targetManager.ignoreForwardBackward = true;
        targetManager.enableConsecutiveBackwardDetection = false;
        targetManager.enableConsecutiveSideDetection = false;
        targetManager.cooldownTime = 0.3f;
        targetManager.yAngleThreshold = 45f;
        targetManager.yVectorMagnitudeThreshold = 0.1f;

        // UIに反映
        LoadCurrentValues();

        LogDebug("Settings reset to defaults");
    }

    /// <summary>
    /// 現在の設定を再読み込み
    /// </summary>
    public void RefreshUI()
    {
        LoadCurrentValues();
        LogDebug("UI refreshed");
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[OSCManagerConfigUI] {message}");
        }
    }

    #endregion
}
