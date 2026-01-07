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
    [Tooltip("選択判定のX座標中心")]
    public float xCenterPosition = 0f;

    [Tooltip("X軸の最小値（この値未満は範囲外）")]
    public float xMin = -1.0f;

    [Tooltip("X軸の最大値（この値より大きいと範囲外）")]
    public float xMax = 1.0f;

    [Tooltip("選択確定のキー（テスト用・自動モードOFFの時のみ）")]
    public KeyCode confirmKey = KeyCode.Space;

    [Tooltip("自動選択モード（位置で自動的に選択を送信）")]
    public bool autoSelectMode = true;

    [Tooltip("自動選択のクールダウン時間（秒）")]
    public float autoSelectCooldown = 0.5f;

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
    /// 現在の選択を更新（範囲判定あり）
    /// </summary>
    void UpdateCurrentChoice()
    {
        float posX = oscManager.GetFloat("PositionX");

        // 範囲外チェック
        if (posX < xMin || posX > xMax)
        {
            _currentChoice = QuizChoice.None; // 0: 選択していない
            return;
        }

        // 範囲内での左右判定
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
