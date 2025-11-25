using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OSCXPositionYVectorConfigを使用したGUI調整ツールのサンプル実装
/// このスクリプトを参考にして、独自のGUIツールを作成してください
/// </summary>
public class OSCConfigGUISample : MonoBehaviour
{
    [Header("References")]
    [Tooltip("設定インターフェース")]
    public OSCXPositionYVectorConfig config;

    [Header("UI Elements - Receiver Settings")]
    public InputField receivePortInput;
    public InputField receiveAddressInput;

    [Header("UI Elements - Transmitter Settings")]
    public InputField transmitHostInput;
    public InputField transmitPortInput;
    public InputField transmitAddressInput;

    [Header("UI Elements - Position & Vector Settings")]
    public Slider xCenterSlider;
    public Text xCenterText;
    public Slider yVectorThresholdSlider;
    public Text yVectorThresholdText;

    [Header("UI Elements - Direction Values")]
    public InputField rightValueInput;
    public InputField leftValueInput;
    public InputField rightBackwardValueInput;
    public InputField leftBackwardValueInput;

    [Header("UI Elements - Conditional Axis")]
    public Toggle enableConditionalAxisToggle;
    public Slider conditionalAxisMinSlider;
    public Text conditionalAxisMinText;
    public Slider conditionalAxisMaxSlider;
    public Text conditionalAxisMaxText;

    [Header("UI Elements - Cooldown")]
    public Slider cooldownSlider;
    public Text cooldownText;

    [Header("UI Elements - Consecutive Side Detection")]
    public Toggle enableSideDetectionToggle;
    public Slider sideLimitSlider;
    public Text sideLimitText;
    public Slider oppositeRequiredSlider;
    public Text oppositeRequiredText;

    [Header("UI Elements - Consecutive Backward Detection")]
    public Toggle enableBackwardDetectionToggle;
    public Slider backwardLimitSlider;
    public Text backwardLimitText;
    public Slider forwardRequiredSlider;
    public Text forwardRequiredText;

    [Header("UI Elements - Advanced Settings")]
    public Slider yAngleThresholdSlider;
    public Text yAngleThresholdText;
    public Toggle enableDebugLogToggle;
    public Toggle showGizmoToggle;

    [Header("UI Elements - Info & Control")]
    public Text currentStatusText;
    public Button resetButton;
    public Button saveButton;
    public Button loadButton;

    void Start()
    {
        // UIイベントリスナーを設定
        SetupUIListeners();

        // 初期値を読み込み
        LoadValuesFromConfig();

        // リセットボタン
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        // セーブボタン（例：PlayerPrefsに保存）
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);

        // ロードボタン（例：PlayerPrefsから読み込み）
        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);
    }

    void Update()
    {
        // リアルタイムステータス表示
        UpdateStatusDisplay();
    }

    /// <summary>
    /// UIリスナーをセットアップ
    /// </summary>
    void SetupUIListeners()
    {
        if (config == null)
            return;

        // Receiver Settings
        if (receivePortInput != null)
            receivePortInput.onEndEdit.AddListener(val => config.ReceivePort = int.TryParse(val, out int result) ? result : config.ReceivePort);

        if (receiveAddressInput != null)
            receiveAddressInput.onEndEdit.AddListener(val => config.ReceiveAddress = val);

        // Transmitter Settings
        if (transmitHostInput != null)
            transmitHostInput.onEndEdit.AddListener(val => config.TransmitHost = val);

        if (transmitPortInput != null)
            transmitPortInput.onEndEdit.AddListener(val => config.TransmitPort = int.TryParse(val, out int result) ? result : config.TransmitPort);

        if (transmitAddressInput != null)
            transmitAddressInput.onEndEdit.AddListener(val => config.TransmitAddress = val);

        // Position & Vector Settings
        if (xCenterSlider != null)
        {
            xCenterSlider.onValueChanged.AddListener(val =>
            {
                config.XCenterPosition = val;
                if (xCenterText != null) xCenterText.text = $"X Center: {val:F2}";
            });
        }

        if (yVectorThresholdSlider != null)
        {
            yVectorThresholdSlider.onValueChanged.AddListener(val =>
            {
                config.YVectorMagnitudeThreshold = val;
                if (yVectorThresholdText != null) yVectorThresholdText.text = $"Y Threshold: {val:F3}";
            });
        }

        // Direction Values
        if (rightValueInput != null)
            rightValueInput.onEndEdit.AddListener(val => config.RightValue = int.TryParse(val, out int result) ? result : config.RightValue);

        if (leftValueInput != null)
            leftValueInput.onEndEdit.AddListener(val => config.LeftValue = int.TryParse(val, out int result) ? result : config.LeftValue);

        if (rightBackwardValueInput != null)
            rightBackwardValueInput.onEndEdit.AddListener(val => config.RightBackwardValue = int.TryParse(val, out int result) ? result : config.RightBackwardValue);

        if (leftBackwardValueInput != null)
            leftBackwardValueInput.onEndEdit.AddListener(val => config.LeftBackwardValue = int.TryParse(val, out int result) ? result : config.LeftBackwardValue);

        // Conditional Axis
        if (enableConditionalAxisToggle != null)
            enableConditionalAxisToggle.onValueChanged.AddListener(val => config.EnableConditionalAxis = val);

        if (conditionalAxisMinSlider != null)
        {
            conditionalAxisMinSlider.onValueChanged.AddListener(val =>
            {
                config.ConditionalAxisMin = val;
                if (conditionalAxisMinText != null) conditionalAxisMinText.text = $"Z Min: {val:F2}";
            });
        }

        if (conditionalAxisMaxSlider != null)
        {
            conditionalAxisMaxSlider.onValueChanged.AddListener(val =>
            {
                config.ConditionalAxisMax = val;
                if (conditionalAxisMaxText != null) conditionalAxisMaxText.text = $"Z Max: {val:F2}";
            });
        }

        // Cooldown
        if (cooldownSlider != null)
        {
            cooldownSlider.onValueChanged.AddListener(val =>
            {
                config.CooldownTime = val;
                if (cooldownText != null) cooldownText.text = $"Cooldown: {val:F2}s";
            });
        }

        // Consecutive Side Detection
        if (enableSideDetectionToggle != null)
            enableSideDetectionToggle.onValueChanged.AddListener(val => config.EnableConsecutiveSideDetection = val);

        if (sideLimitSlider != null)
        {
            sideLimitSlider.onValueChanged.AddListener(val =>
            {
                config.ConsecutiveSideLimit = Mathf.RoundToInt(val);
                if (sideLimitText != null) sideLimitText.text = $"Side Limit: {Mathf.RoundToInt(val)}";
            });
        }

        if (oppositeRequiredSlider != null)
        {
            oppositeRequiredSlider.onValueChanged.AddListener(val =>
            {
                config.OppositeSideRequiredCount = Mathf.RoundToInt(val);
                if (oppositeRequiredText != null) oppositeRequiredText.text = $"Opposite Required: {Mathf.RoundToInt(val)}";
            });
        }

        // Consecutive Backward Detection
        if (enableBackwardDetectionToggle != null)
            enableBackwardDetectionToggle.onValueChanged.AddListener(val => config.EnableConsecutiveBackwardDetection = val);

        if (backwardLimitSlider != null)
        {
            backwardLimitSlider.onValueChanged.AddListener(val =>
            {
                config.ConsecutiveBackwardLimit = Mathf.RoundToInt(val);
                if (backwardLimitText != null) backwardLimitText.text = $"Backward Limit: {Mathf.RoundToInt(val)}";
            });
        }

        if (forwardRequiredSlider != null)
        {
            forwardRequiredSlider.onValueChanged.AddListener(val =>
            {
                config.ForwardRequiredCount = Mathf.RoundToInt(val);
                if (forwardRequiredText != null) forwardRequiredText.text = $"Forward Required: {Mathf.RoundToInt(val)}";
            });
        }

        // Advanced Settings
        if (yAngleThresholdSlider != null)
        {
            yAngleThresholdSlider.onValueChanged.AddListener(val =>
            {
                config.YAngleThreshold = val;
                if (yAngleThresholdText != null) yAngleThresholdText.text = $"Y Angle: {val:F1}°";
            });
        }

        if (enableDebugLogToggle != null)
            enableDebugLogToggle.onValueChanged.AddListener(val => config.EnableDebugLog = val);

        if (showGizmoToggle != null)
            showGizmoToggle.onValueChanged.AddListener(val => config.ShowGizmo = val);
    }

    /// <summary>
    /// Configから現在の値を読み込んでUIに反映
    /// </summary>
    void LoadValuesFromConfig()
    {
        if (config == null)
            return;

        // Receiver Settings
        if (receivePortInput != null) receivePortInput.text = config.ReceivePort.ToString();
        if (receiveAddressInput != null) receiveAddressInput.text = config.ReceiveAddress;

        // Transmitter Settings
        if (transmitHostInput != null) transmitHostInput.text = config.TransmitHost;
        if (transmitPortInput != null) transmitPortInput.text = config.TransmitPort.ToString();
        if (transmitAddressInput != null) transmitAddressInput.text = config.TransmitAddress;

        // Position & Vector Settings
        if (xCenterSlider != null) xCenterSlider.value = config.XCenterPosition;
        if (yVectorThresholdSlider != null) yVectorThresholdSlider.value = config.YVectorMagnitudeThreshold;

        // Direction Values
        if (rightValueInput != null) rightValueInput.text = config.RightValue.ToString();
        if (leftValueInput != null) leftValueInput.text = config.LeftValue.ToString();
        if (rightBackwardValueInput != null) rightBackwardValueInput.text = config.RightBackwardValue.ToString();
        if (leftBackwardValueInput != null) leftBackwardValueInput.text = config.LeftBackwardValue.ToString();

        // Conditional Axis
        if (enableConditionalAxisToggle != null) enableConditionalAxisToggle.isOn = config.EnableConditionalAxis;
        if (conditionalAxisMinSlider != null) conditionalAxisMinSlider.value = config.ConditionalAxisMin;
        if (conditionalAxisMaxSlider != null) conditionalAxisMaxSlider.value = config.ConditionalAxisMax;

        // Cooldown
        if (cooldownSlider != null) cooldownSlider.value = config.CooldownTime;

        // Consecutive Side Detection
        if (enableSideDetectionToggle != null) enableSideDetectionToggle.isOn = config.EnableConsecutiveSideDetection;
        if (sideLimitSlider != null) sideLimitSlider.value = config.ConsecutiveSideLimit;
        if (oppositeRequiredSlider != null) oppositeRequiredSlider.value = config.OppositeSideRequiredCount;

        // Consecutive Backward Detection
        if (enableBackwardDetectionToggle != null) enableBackwardDetectionToggle.isOn = config.EnableConsecutiveBackwardDetection;
        if (backwardLimitSlider != null) backwardLimitSlider.value = config.ConsecutiveBackwardLimit;
        if (forwardRequiredSlider != null) forwardRequiredSlider.value = config.ForwardRequiredCount;

        // Advanced Settings
        if (yAngleThresholdSlider != null) yAngleThresholdSlider.value = config.YAngleThreshold;
        if (enableDebugLogToggle != null) enableDebugLogToggle.isOn = config.EnableDebugLog;
        if (showGizmoToggle != null) showGizmoToggle.isOn = config.ShowGizmo;
    }

    /// <summary>
    /// リアルタイムステータス表示を更新
    /// </summary>
    void UpdateStatusDisplay()
    {
        if (config == null || currentStatusText == null)
            return;

        var xPos = config.GetCurrentXPosition();
        var yDir = config.GetCurrentYDirection();
        var movement = config.GetMovementVector();

        currentStatusText.text = $"Current Status:\n" +
                                $"X Position: {xPos}\n" +
                                $"Y Direction: {yDir}\n" +
                                $"Movement Vector: {movement:F3}";
    }

    /// <summary>
    /// リセットボタンが押された時
    /// </summary>
    void OnResetButtonClicked()
    {
        if (config != null)
        {
            config.ResetPosition();
            Debug.Log("Position and counters reset.");
        }
    }

    /// <summary>
    /// セーブボタンが押された時（例：JSONでPlayerPrefsに保存）
    /// </summary>
    void OnSaveButtonClicked()
    {
        if (config == null)
            return;

        var data = config.GetConfigData();
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString("OSCConfig", json);
        PlayerPrefs.Save();
        Debug.Log("Configuration saved to PlayerPrefs.");
    }

    /// <summary>
    /// ロードボタンが押された時（例：PlayerPrefsから読み込み）
    /// </summary>
    void OnLoadButtonClicked()
    {
        if (config == null)
            return;

        if (PlayerPrefs.HasKey("OSCConfig"))
        {
            string json = PlayerPrefs.GetString("OSCConfig");
            var data = JsonUtility.FromJson<OSCXPositionYVectorConfig.ConfigData>(json);
            config.ApplyConfigData(data);
            LoadValuesFromConfig(); // UIを更新
            Debug.Log("Configuration loaded from PlayerPrefs.");
        }
        else
        {
            Debug.LogWarning("No saved configuration found in PlayerPrefs.");
        }
    }
}
