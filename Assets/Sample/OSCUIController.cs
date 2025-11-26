using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OSCビジュアライザーとデバッグコンソールを統合管理するコントローラー
/// UI全体の制御とテスト機能を提供します
/// </summary>
public class OSCUIController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("OSC値ビジュアライザー")]
    public OSCValueVisualizer visualizer;

    [Tooltip("デバッグコンソール")]
    public OSCDebugConsole debugConsole;

    [Tooltip("OSCマネージャー")]
    public OSCXPositionYVectorManager oscManager;

    [Tooltip("設定インターフェース")]
    public OSCXPositionYVectorConfig config;

    [Header("UI Panel References")]
    [Tooltip("ビジュアライザーパネル")]
    public GameObject visualizerPanel;

    [Tooltip("コンソールパネル")]
    public GameObject consolePanel;

    [Tooltip("設定パネル")]
    public GameObject configPanel;

    [Header("UI Buttons")]
    [Tooltip("コンソールクリアボタン")]
    public Button clearConsoleButton;

    [Tooltip("位置リセットボタン")]
    public Button resetPositionButton;

    [Tooltip("ビジュアライザー表示切替ボタン")]
    public Button toggleVisualizerButton;

    [Tooltip("コンソール表示切替ボタン")]
    public Button toggleConsoleButton;

    [Header("Status Display")]
    [Tooltip("現在の状態を表示するText")]
    public Text statusText;

    [Tooltip("現在のモードを表示するText")]
    public Text modeText;

    [Header("Test Buttons (Optional)")]
    [Tooltip("テスト用：右を送信")]
    public Button testRightButton;

    [Tooltip("テスト用：左を送信")]
    public Button testLeftButton;

    [Tooltip("テスト用：右後進を送信")]
    public Button testRightBackwardButton;

    [Tooltip("テスト用：左後進を送信")]
    public Button testLeftBackwardButton;

    [Header("Alternate Test Buttons (Optional)")]
    [Tooltip("テスト用：右左を交互に送信（前進）")]
    public Button testForwardAlternateButton;

    [Tooltip("テスト用：右後ろ左後ろを交互に送信（後進）")]
    public Button testBackwardAlternateButton;

    private bool _visualizerVisible = true;
    private bool _consoleVisible = true;

    // 交互送信用の状態
    private bool _forwardAlternateIsRight = true;  // true=右, false=左
    private bool _backwardAlternateIsRight = true; // true=右後ろ, false=左後ろ

    void Start()
    {
        // ボタンにイベントを登録
        SetupButtons();

        // 初期ログ
        if (debugConsole != null)
        {
            debugConsole.LogInfo("OSC UI Controller initialized.");
        }
    }

    void Update()
    {
        // ステータス表示を更新
        UpdateStatusDisplay();
    }

    /// <summary>
    /// ボタンのイベントをセットアップ
    /// </summary>
    void SetupButtons()
    {
        if (clearConsoleButton != null)
            clearConsoleButton.onClick.AddListener(OnClearConsole);

        if (resetPositionButton != null)
            resetPositionButton.onClick.AddListener(OnResetPosition);

        if (toggleVisualizerButton != null)
            toggleVisualizerButton.onClick.AddListener(OnToggleVisualizer);

        if (toggleConsoleButton != null)
            toggleConsoleButton.onClick.AddListener(OnToggleConsole);

        // テストボタン（設定値を使用）
        if (testRightButton != null)
            testRightButton.onClick.AddListener(() => TestSendValue("right"));

        if (testLeftButton != null)
            testLeftButton.onClick.AddListener(() => TestSendValue("left"));

        if (testRightBackwardButton != null)
            testRightBackwardButton.onClick.AddListener(() => TestSendValue("rightBackward"));

        if (testLeftBackwardButton != null)
            testLeftBackwardButton.onClick.AddListener(() => TestSendValue("leftBackward"));

        // 交互送信ボタン
        if (testForwardAlternateButton != null)
            testForwardAlternateButton.onClick.AddListener(OnForwardAlternateClicked);

        if (testBackwardAlternateButton != null)
            testBackwardAlternateButton.onClick.AddListener(OnBackwardAlternateClicked);
    }

    /// <summary>
    /// ステータス表示を更新
    /// </summary>
    void UpdateStatusDisplay()
    {
        if (config == null)
            return;

        // ステータステキスト
        if (statusText != null)
        {
            var xPos = config.GetCurrentXPosition();
            var yDir = config.GetCurrentYDirection();
            var movement = config.GetMovementVector();

            statusText.text = $"X: {xPos} | Y: {yDir}\n" +
                             $"Vector: ({movement.x:F2}, {movement.y:F2}, {movement.z:F2})";
        }

        // モードテキスト
        if (modeText != null)
        {
            string modeInfo = "Mode: Normal";

            // 左右ロック状態をチェック（config経由で取得できないため、ここでは簡易表示）
            if (config.EnableConsecutiveSideDetection)
            {
                modeInfo += " | Side Detection: ON";
            }

            if (config.EnableConsecutiveBackwardDetection)
            {
                modeInfo += " | Backward Detection: ON";
            }

            modeText.text = modeInfo;
        }
    }

    /// <summary>
    /// コンソールクリアボタン
    /// </summary>
    void OnClearConsole()
    {
        if (debugConsole != null)
        {
            debugConsole.ClearLogs();
        }
    }

    /// <summary>
    /// 位置リセットボタン
    /// </summary>
    void OnResetPosition()
    {
        if (config != null)
        {
            config.ResetPosition();

            if (debugConsole != null)
            {
                debugConsole.LogInfo("Position and counters reset.");
            }
        }
    }

    /// <summary>
    /// ビジュアライザー表示切替
    /// </summary>
    void OnToggleVisualizer()
    {
        _visualizerVisible = !_visualizerVisible;

        if (visualizerPanel != null)
        {
            visualizerPanel.SetActive(_visualizerVisible);
        }

        if (debugConsole != null)
        {
            debugConsole.LogInfo($"Visualizer panel: {(_visualizerVisible ? "Visible" : "Hidden")}");
        }
    }

    /// <summary>
    /// コンソール表示切替
    /// </summary>
    void OnToggleConsole()
    {
        _consoleVisible = !_consoleVisible;

        if (consolePanel != null)
        {
            consolePanel.SetActive(_consoleVisible);
        }
    }

    /// <summary>
    /// テスト用：指定された方向を実際にOSC送信
    /// </summary>
    void TestSendValue(string direction)
    {
        if (oscManager == null)
        {
            Debug.LogWarning("[OSCUIController] OSC Manager is not assigned!");
            return;
        }

        int value = -1;

        // 方向に応じて設定値を取得
        switch (direction.ToLower())
        {
            case "right":
                value = oscManager.rightValue;
                break;

            case "left":
                value = oscManager.leftValue;
                break;

            case "rightbackward":
                value = oscManager.rightBackwardValue;
                break;

            case "leftbackward":
                value = oscManager.leftBackwardValue;
                break;
        }

        if (value == -1)
        {
            Debug.LogWarning($"[OSCUIController] Unknown direction: {direction}");
            return;
        }

        // 実際にOSC送信（これでイベントも発火されるのでビジュアライザーも自動的に反応する）
        oscManager.SendTestValue(value);

        // ログ出力
        if (debugConsole != null)
        {
            debugConsole.LogOSCSend($"Test {direction}: value={value}");
        }
    }

    /// <summary>
    /// 前進（右左交互）ボタンがクリックされた時
    /// </summary>
    void OnForwardAlternateClicked()
    {
        if (oscManager == null)
        {
            Debug.LogWarning("[OSCUIController] OSC Manager is not assigned!");
            return;
        }

        // 現在の状態に応じて値を決定
        int value;
        string direction;

        if (_forwardAlternateIsRight)
        {
            value = oscManager.rightValue;
            direction = "Right";
        }
        else
        {
            value = oscManager.leftValue;
            direction = "Left";
        }

        // OSC送信
        oscManager.SendTestValue(value);

        // ログ出力
        if (debugConsole != null)
        {
            debugConsole.LogOSCSend($"Forward Alternate: {direction} (value={value})");
        }

        // 状態を反転
        _forwardAlternateIsRight = !_forwardAlternateIsRight;
    }

    /// <summary>
    /// 後進（右後ろ左後ろ交互）ボタンがクリックされた時
    /// </summary>
    void OnBackwardAlternateClicked()
    {
        if (oscManager == null)
        {
            Debug.LogWarning("[OSCUIController] OSC Manager is not assigned!");
            return;
        }

        // 現在の状態に応じて値を決定
        int value;
        string direction;

        if (_backwardAlternateIsRight)
        {
            value = oscManager.rightBackwardValue;
            direction = "Right+Backward";
        }
        else
        {
            value = oscManager.leftBackwardValue;
            direction = "Left+Backward";
        }

        // OSC送信
        oscManager.SendTestValue(value);

        // ログ出力
        if (debugConsole != null)
        {
            debugConsole.LogOSCSend($"Backward Alternate: {direction} (value={value})");
        }

        // 状態を反転
        _backwardAlternateIsRight = !_backwardAlternateIsRight;
    }

    /// <summary>
    /// 外部から呼び出し可能：ログを追加
    /// </summary>
    public void AddLog(string message)
    {
        if (debugConsole != null)
        {
            debugConsole.LogInfo(message);
        }
    }

    /// <summary>
    /// UIパネルの表示/非表示を設定
    /// </summary>
    public void SetPanelVisible(string panelName, bool visible)
    {
        switch (panelName.ToLower())
        {
            case "visualizer":
                if (visualizerPanel != null)
                    visualizerPanel.SetActive(visible);
                _visualizerVisible = visible;
                break;

            case "console":
                if (consolePanel != null)
                    consolePanel.SetActive(visible);
                _consoleVisible = visible;
                break;

            case "config":
                if (configPanel != null)
                    configPanel.SetActive(visible);
                break;
        }
    }
}
