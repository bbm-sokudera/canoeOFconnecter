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

    [Header("Quiz Settings")]
    [Tooltip("選択判定のX座標閾値")]
    public float xThreshold = 0f;

    [Tooltip("選択確定のキー（テスト用）")]
    public KeyCode confirmKey = KeyCode.Space;

    [Tooltip("自動選択モード（位置で自動的に選択を送信）")]
    public bool autoSelectMode = false;

    [Tooltip("自動選択のクールダウン時間（秒）")]
    public float autoSelectCooldown = 1.0f;

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
    /// 現在の選択を更新
    /// </summary>
    void UpdateCurrentChoice()
    {
        float posX = oscManager.GetFloat("PositionX");

        if (posX >= xThreshold)
        {
            _currentChoice = QuizChoice.Right;
        }
        else
        {
            _currentChoice = QuizChoice.Left;
        }
    }

    /// <summary>
    /// 自動選択処理
    /// </summary>
    void HandleAutoSelect()
    {
        // クールダウンチェック
        if (Time.time - _lastSelectTime < autoSelectCooldown)
            return;

        // 選択なしはスキップ
        if (_currentChoice == QuizChoice.None)
            return;

        // 選択を送信
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
