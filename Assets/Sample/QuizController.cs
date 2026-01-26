using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// クイズモードの選択を管理するコントローラー
/// </summary>
public class QuizController : MonoBehaviour
{
    #region Quiz Choice Enum

    public enum QuizChoice
    {
        None = 0,   // 選択していない
        Right = 1,  // 右選択
        Left = 2    // 左選択
    }

    #endregion

    #region Inspector Settings

    [Header("References")]
    [Tooltip("MultiAddressOSCManager")]
    public MultiAddressOSCManager oscManager;

    [Tooltip("StateController（クイズモード判定用）")]
    public StateController stateController;

    [Header("UI")]
    [Tooltip("現在の選択を表示するText（UI.Text使用時）")]
    public UnityEngine.UI.Text choiceDisplayText;

    [Tooltip("現在の選択を表示するText（TextMeshPro使用時）")]
    public TMPro.TextMeshProUGUI choiceDisplayTextTMP;

    [Header("Quiz Settings")]
    [Tooltip("選択判定のX座標中心")]
    public float xCenterPosition = 0f;

    [Tooltip("選択確定のキー（テスト用・自動モードOFFの時のみ）")]
    public KeyCode confirmKey = KeyCode.Space;

    [Tooltip("自動選択モード（位置で自動的に選択を送信）")]
    public bool autoSelectMode = true;

    [Tooltip("自動選択のクールダウン時間（秒）")]
    public float autoSelectCooldown = 0.5f;

    [Header("Z-Axis Filter (AutoHeight)")]
    [Tooltip("Z軸範囲フィルタを有効にする")]
    public bool enableZAxisFilter = true;

    [Tooltip("Z軸の最小値（この値以上の時に有効）")]
    public float zMin = 0f;

    [Tooltip("Z軸の最大値（この値以下の時に有効）")]
    public float zMax = 2.0f;

    [Header("Events")]
    [Tooltip("選択を送信した時のイベント")]
    public UnityEvent<QuizChoice> onQuizSelected;

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Header("Debug Monitor (実行中に確認)")]
    [SerializeField] private int _debugState;
    [SerializeField] private bool _debugIsQuizMode;
    [SerializeField] private float _debugPositionX;
    [SerializeField] private float _debugPositionZ;
    [SerializeField] private float _debugBoxZ;
    [SerializeField] private float _debugActualZ;
    [SerializeField] private bool _debugZInRange;
    [SerializeField] private bool _debugRightZInRange;
    [SerializeField] private bool _debugLeftZInRange;
    [SerializeField] private QuizChoice _debugCurrentChoice;

    [Header("AutoHeight設定値 (実行中に確認)")]
    [SerializeField, Tooltip("AutoHeightから設定されたZ最小値")]
    private float _debugZMin;
    [SerializeField, Tooltip("AutoHeightから設定されたZ最大値")]
    private float _debugZMax;

    #endregion

    #region Private Variables

    private float _lastSelectTime = -999f;
    private QuizChoice _currentChoice = QuizChoice.None;

    // 左右それぞれのZ範囲状態を追跡
    private bool _rightZInRange = false;
    private bool _leftZInRange = false;

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        if (oscManager == null)
            return;

        // デバッグ: State値を取得
        _debugState = oscManager.GetInt("State");

        // クイズモード(State=4)の時のみ動作
        if (stateController != null && !stateController.IsState(StateController.GameState.Quiz))
        {
            _debugIsQuizMode = false;
            return;
        }

        // State直接チェック（stateControllerがない場合）
        if (stateController == null && _debugState != 4)
        {
            _debugIsQuizMode = false;
            return;
        }

        _debugIsQuizMode = true;

        // 現在の選択を更新
        UpdateCurrentChoice();

        // UI更新
        UpdateChoiceDisplay();

        // 自動選択モード
        if (autoSelectMode)
        {
            HandleAutoSelect();
        }
        // 手動選択モード（確定キー）
        else if (Input.GetKeyDown(confirmKey))
        {
            HandleManualSelect();
        }
    }

    #endregion

    #region Quiz Logic

    /// <summary>
    /// 現在の選択を更新（左右交互データ対応・両方範囲外でNone）
    /// </summary>
    void UpdateCurrentChoice()
    {
        float posX = oscManager.GetFloat("PositionX");
        float posZ = oscManager.GetFloat("PositionZ");
        float boxZ = oscManager.GetFloat("BoxZ");

        // Z軸の実際の位置（ボックス上端）
        float actualZ = posZ + boxZ * 0.5f;

        // 今回のデータがZ範囲内かどうか
        bool currentZInRange = !enableZAxisFilter || (actualZ >= zMin && actualZ <= zMax);

        // X座標で左右を判定し、該当側のZ範囲状態を更新
        bool isRight = posX >= xCenterPosition;
        if (isRight)
        {
            _rightZInRange = currentZInRange;
        }
        else
        {
            _leftZInRange = currentZInRange;
        }

        // デバッグ値を更新
        _debugPositionX = posX;
        _debugPositionZ = posZ;
        _debugBoxZ = boxZ;
        _debugActualZ = actualZ;
        _debugZInRange = currentZInRange;
        _debugRightZInRange = _rightZInRange;
        _debugLeftZInRange = _leftZInRange;

        // 両方とも範囲外の場合のみNone
        if (enableZAxisFilter && !_rightZInRange && !_leftZInRange)
        {
            _currentChoice = QuizChoice.None; // 0: 両方範囲外
            _debugCurrentChoice = _currentChoice;
            LogDebug($"Both sides out of Z range. Right:{_rightZInRange}, Left:{_leftZInRange}");
            return;
        }

        // どちらか片方でも範囲内なら、範囲内の側を選択
        if (_rightZInRange && !_leftZInRange)
        {
            _currentChoice = QuizChoice.Right; // 右のみ範囲内
        }
        else if (!_rightZInRange && _leftZInRange)
        {
            _currentChoice = QuizChoice.Left; // 左のみ範囲内
        }
        else
        {
            // 両方範囲内の場合、今回のデータの側を選択
            _currentChoice = isRight ? QuizChoice.Right : QuizChoice.Left;
        }

        _debugCurrentChoice = _currentChoice;
    }

    /// <summary>
    /// 選択表示を更新
    /// </summary>
    void UpdateChoiceDisplay()
    {
        string choiceName = GetChoiceName(_currentChoice);
        string displayText = $"Quiz: {(int)_currentChoice} - {choiceName}";

        // UI.Text対応
        if (choiceDisplayText != null)
        {
            choiceDisplayText.text = displayText;
        }

        // TextMeshPro対応
        if (choiceDisplayTextTMP != null)
        {
            choiceDisplayTextTMP.text = displayText;
        }
    }

    /// <summary>
    /// 選択の日本語名を取得
    /// </summary>
    string GetChoiceName(QuizChoice choice)
    {
        switch (choice)
        {
            case QuizChoice.None: return "選択していない";
            case QuizChoice.Right: return "右選択";
            case QuizChoice.Left: return "左選択";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// 自動選択処理（範囲外も含めて常に送信）
    /// </summary>
    void HandleAutoSelect()
    {
        // クールダウンチェック
        if (Time.time - _lastSelectTime < autoSelectCooldown)
            return;

        // 現在の選択を送信（None=0も含む）
        SendQuizChoice(_currentChoice);
    }

    /// <summary>
    /// 手動選択処理
    /// </summary>
    void HandleManualSelect()
    {
        if (_currentChoice == QuizChoice.None)
            return;

        // 選択を送信
        SendQuizChoice(_currentChoice);
    }

    /// <summary>
    /// クイズ選択を送信
    /// </summary>
    void SendQuizChoice(QuizChoice choice)
    {
        oscManager.SetInt("QuizChoice", (int)choice);
        oscManager.SendMessage("/quiz");

        _lastSelectTime = Time.time;

        LogDebug($"Quiz selected: {choice}");

        // イベント発火
        onQuizSelected?.Invoke(choice);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 現在の選択を取得
    /// </summary>
    public QuizChoice GetCurrentChoice()
    {
        return _currentChoice;
    }

    /// <summary>
    /// 選択を強制送信（外部から呼び出し可能）
    /// </summary>
    public void ForceSelect(QuizChoice choice)
    {
        SendQuizChoice(choice);
    }

    /// <summary>
    /// 現在の選択を送信
    /// </summary>
    public void ConfirmCurrentChoice()
    {
        if (_currentChoice != QuizChoice.None)
        {
            SendQuizChoice(_currentChoice);
        }
    }

    /// <summary>
    /// Z軸の最小値を設定（AutoHeightControllerから呼び出し用）
    /// </summary>
    public void SetZMin(float value)
    {
        zMin = value;
        _debugZMin = value;
        LogDebug($"Z Min set to: {value:F3}");
    }

    /// <summary>
    /// Z軸の最大値を設定（AutoHeightControllerから呼び出し用）
    /// </summary>
    public void SetZMax(float value)
    {
        zMax = value;
        _debugZMax = value;
        LogDebug($"Z Max set to: {value:F3}");
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuizController] {message}");
        }
    }

    #endregion
}
