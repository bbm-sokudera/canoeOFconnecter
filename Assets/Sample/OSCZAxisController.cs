using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Z軸設定を補助するコントローラー
/// 現在のZ位置をワンクリックでMin/Max値に設定できる
/// </summary>
public class OSCZAxisController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("設定対象のOSCマネージャー")]
    public OSCXPositionYVectorManager targetManager;

    [Tooltip("Z軸ビジュアライザー")]
    public OSCZAxisVisualizer zAxisVisualizer;

    [Header("UI Elements - Min Settings")]
    [Tooltip("Z軸最小値の入力フィールド")]
    public InputField minInputField;

    [Tooltip("現在のZ値をMinに設定するボタン")]
    public Button setMinButton;

    [Header("UI Elements - Max Settings")]
    [Tooltip("Z軸最大値の入力フィールド")]
    public InputField maxInputField;

    [Tooltip("現在のZ値をMaxに設定するボタン")]
    public Button setMaxButton;

    [Header("Display")]
    [Tooltip("現在のZ値を表示するText")]
    public Text currentZValueText;

    [Header("Save/Load")]
    [Tooltip("設定を保存するボタン")]
    public Button saveButton;

    [Tooltip("設定をリセットするボタン")]
    public Button resetButton;

    // PlayerPrefsのキー
    private const string PREFS_KEY_MIN = "OSC_ZAxis_Min";
    private const string PREFS_KEY_MAX = "OSC_ZAxis_Max";
    private const float DEFAULT_MIN = 0.0f;
    private const float DEFAULT_MAX = 2.0f;

    void Start()
    {
        // ボタンのイベント設定
        if (setMinButton != null)
            setMinButton.onClick.AddListener(OnSetMinClicked);

        if (setMaxButton != null)
            setMaxButton.onClick.AddListener(OnSetMaxClicked);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        // 入力フィールドのイベント設定
        if (minInputField != null)
            minInputField.onEndEdit.AddListener(OnMinValueChanged);

        if (maxInputField != null)
            maxInputField.onEndEdit.AddListener(OnMaxValueChanged);

        // 保存された値をロード
        LoadSettings();
    }

    void Update()
    {
        // 現在のZ値を表示
        UpdateCurrentZDisplay();
    }

    /// <summary>
    /// 現在のZ値表示を更新
    /// </summary>
    void UpdateCurrentZDisplay()
    {
        if (currentZValueText == null || targetManager == null)
            return;

        Vector3 position = targetManager.GetCurrentPosition();
        currentZValueText.text = $"Current Z: {position.z:F3}";
    }

    /// <summary>
    /// 保存された設定値をロード（なければデフォルト値）
    /// </summary>
    void LoadSettings()
    {
        if (targetManager == null)
            return;

        // PlayerPrefsから読み込み（なければデフォルト値）
        float minValue = PlayerPrefs.GetFloat(PREFS_KEY_MIN, DEFAULT_MIN);
        float maxValue = PlayerPrefs.GetFloat(PREFS_KEY_MAX, DEFAULT_MAX);

        // Managerに設定
        targetManager.conditionalAxisMin = minValue;
        targetManager.conditionalAxisMax = maxValue;

        // UIに反映
        if (minInputField != null)
            minInputField.text = minValue.ToString("F3");

        if (maxInputField != null)
            maxInputField.text = maxValue.ToString("F3");

        Debug.Log($"[OSCZAxisController] Settings loaded: Min={minValue:F3}, Max={maxValue:F3}");
    }

    /// <summary>
    /// 現在の設定を保存
    /// </summary>
    void SaveSettings()
    {
        if (targetManager == null)
            return;

        float minValue = targetManager.conditionalAxisMin;
        float maxValue = targetManager.conditionalAxisMax;

        // PlayerPrefsに保存
        PlayerPrefs.SetFloat(PREFS_KEY_MIN, minValue);
        PlayerPrefs.SetFloat(PREFS_KEY_MAX, maxValue);
        PlayerPrefs.Save();

        Debug.Log($"[OSCZAxisController] Settings saved: Min={minValue:F3}, Max={maxValue:F3}");
    }

    /// <summary>
    /// 設定をデフォルト値にリセット
    /// </summary>
    void ResetSettings()
    {
        if (targetManager == null)
            return;

        // デフォルト値を設定
        targetManager.conditionalAxisMin = DEFAULT_MIN;
        targetManager.conditionalAxisMax = DEFAULT_MAX;

        // UIに反映
        if (minInputField != null)
            minInputField.text = DEFAULT_MIN.ToString("F3");

        if (maxInputField != null)
            maxInputField.text = DEFAULT_MAX.ToString("F3");

        Debug.Log($"[OSCZAxisController] Settings reset to defaults: Min={DEFAULT_MIN:F3}, Max={DEFAULT_MAX:F3}");
    }

    /// <summary>
    /// Minボタンがクリックされた時
    /// </summary>
    void OnSetMinClicked()
    {
        if (targetManager == null)
        {
            Debug.LogWarning("[OSCZAxisController] Target Manager is not assigned!");
            return;
        }

        // 現在のZ値を取得
        float currentZ = GetCurrentZValue();

        // Managerに設定
        targetManager.conditionalAxisMin = currentZ;

        // 入力フィールドに反映
        if (minInputField != null)
        {
            minInputField.text = currentZ.ToString("F3");
            // フォーカスをクリアしてUI更新を妨げないようにする
            EventSystem.current.SetSelectedGameObject(null);
        }

        Debug.Log($"[OSCZAxisController] Conditional Axis Min set to: {currentZ:F3}");
    }

    /// <summary>
    /// Maxボタンがクリックされた時
    /// </summary>
    void OnSetMaxClicked()
    {
        if (targetManager == null)
        {
            Debug.LogWarning("[OSCZAxisController] Target Manager is not assigned!");
            return;
        }

        // 現在のZ値を取得
        float currentZ = GetCurrentZValue();

        // Managerに設定
        targetManager.conditionalAxisMax = currentZ;

        // 入力フィールドに反映
        if (maxInputField != null)
        {
            maxInputField.text = currentZ.ToString("F3");
            // フォーカスをクリアしてUI更新を妨げないようにする
            EventSystem.current.SetSelectedGameObject(null);
        }

        Debug.Log($"[OSCZAxisController] Conditional Axis Max set to: {currentZ:F3}");
    }

    /// <summary>
    /// Min入力値が変更された時
    /// </summary>
    void OnMinValueChanged(string value)
    {
        if (targetManager == null)
            return;

        if (float.TryParse(value, out float result))
        {
            targetManager.conditionalAxisMin = result;
            Debug.Log($"[OSCZAxisController] Conditional Axis Min changed to: {result:F3}");
        }
    }

    /// <summary>
    /// Max入力値が変更された時
    /// </summary>
    void OnMaxValueChanged(string value)
    {
        if (targetManager == null)
            return;

        if (float.TryParse(value, out float result))
        {
            targetManager.conditionalAxisMax = result;
            Debug.Log($"[OSCZAxisController] Conditional Axis Max changed to: {result:F3}");
        }
    }

    /// <summary>
    /// 保存ボタンがクリックされた時
    /// </summary>
    void OnSaveClicked()
    {
        SaveSettings();
    }

    /// <summary>
    /// リセットボタンがクリックされた時
    /// </summary>
    void OnResetClicked()
    {
        ResetSettings();
    }

    /// <summary>
    /// 現在のZ値を取得
    /// </summary>
    float GetCurrentZValue()
    {
        // ビジュアライザー経由で取得を試みる
        if (zAxisVisualizer != null)
        {
            return zAxisVisualizer.GetCurrentZValue();
        }

        // 直接マネージャーから取得
        if (targetManager != null)
        {
            Vector3 position = targetManager.GetCurrentPosition();
            return position.z;
        }

        return 0f;
    }

    /// <summary>
    /// Z軸条件フィルタのON/OFF切替
    /// </summary>
    public void ToggleConditionalAxis(bool enabled)
    {
        if (targetManager != null)
        {
            targetManager.enableConditionalAxis = enabled;
            Debug.Log($"[OSCZAxisController] Conditional Axis: {(enabled ? "Enabled" : "Disabled")}");
        }
    }
}
