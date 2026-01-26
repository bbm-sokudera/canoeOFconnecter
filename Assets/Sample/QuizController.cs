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

    #endregion

    #region Private Variables

    private float _lastSelectTime = -999f;
    private QuizChoice _currentChoice = QuizChoice.None;

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        if (oscManager == null)
            return;

        // クイズモード(State=4)の時のみ動作
        if (stateController != null && !stateController.IsState(StateController.GameState.Quiz))
            return;

        // State直接チェック（stateControllerがない場合）
        if (stateController == null && oscManager.GetInt("State") != 4)
            return;

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
    /// 現在の選択を更新（Z軸範囲 + X軸左右判定）
    /// </summary>
    void UpdateCurrentChoice()
    {
        float posX = oscManager.GetFloat("PositionX");
        float posZ = oscManager.GetFloat("PositionZ");
        float boxZ = oscManager.GetFloat("BoxZ");

        // Z軸の実際の位置（ボックス上端）
        float actualZ = posZ + boxZ * 0.5f;

        // Z軸範囲チェック（有効時）
        if (enableZAxisFilter)
        {
            if (actualZ < zMin || actualZ > zMax)
            {
                _currentChoice = QuizChoice.None; // 0: 範囲外
                LogDebug($"Z={actualZ:F3} out of range [{zMin:F3}, {zMax:F3}]");
                return;
            }
        }

        // X軸の左右判定（centerより右か左か）
        if (posX >= xCenterPosition)
        {
            _currentChoice = QuizChoice.Right; // 1: 右選択
        }
        else
        {
            _currentChoice = QuizChoice.Left; // 2: 左選択
        }
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
        LogDebug($"Z Min set to: {value:F3}");
    }

    /// <summary>
    /// Z軸の最大値を設定（AutoHeightControllerから呼び出し用）
    /// </summary>
    public void SetZMax(float value)
    {
        zMax = value;
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
