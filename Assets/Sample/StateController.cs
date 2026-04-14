using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// OSCのState値を管理するコントローラー
/// State変更時にイベントを発火
/// </summary>
public class StateController : MonoBehaviour
{
    #region State Enum

    public enum GameState
    {
        Other = 0,              // その他
        CalibrationStart = 1,   // キャリブレーション開始
        CalibrationEnd = 2,     // キャリブレーション終了
        Tutorial = 3,           // チュートリアル
        Quiz = 4,               // クイズ
        Game = 5                // ゲーム
    }

    #endregion

    #region Inspector Settings

    [Header("References")]
    [Tooltip("MultiAddressOSCManager")]
    public MultiAddressOSCManager oscManager;

    [Header("UI")]
    [Tooltip("現在のStateを表示するText（UI.Text使用時）")]
    public UnityEngine.UI.Text stateDisplayText;

    [Tooltip("現在のStateを表示するText（TextMeshPro使用時）")]
    public TMPro.TextMeshProUGUI stateDisplayTextTMP;

    [Header("Events")]
    [Tooltip("State変更時に呼ばれるイベント（新しいState値を渡す）")]
    public UnityEvent<GameState> onStateChanged;

    [Tooltip("キャリブレーション開始時")]
    public UnityEvent onCalibrationStart;

    [Tooltip("キャリブレーション終了時")]
    public UnityEvent onCalibrationEnd;

    [Tooltip("チュートリアル開始時")]
    public UnityEvent onTutorialStart;

    [Tooltip("クイズ開始時")]
    public UnityEvent onQuizStart;

    [Tooltip("ゲーム開始時")]
    public UnityEvent onGameStart;

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    #endregion

    #region Private Variables

    private GameState _currentState = GameState.Other;

    #endregion

    #region Properties

    /// <summary>
    /// 現在のState
    /// </summary>
    public GameState CurrentState => _currentState;

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        if (oscManager == null)
            return;

        // State値を取得
        int stateValue = oscManager.GetInt("State");
        GameState newState = (GameState)stateValue;

        // State変更チェック
        if (newState != _currentState)
        {
            OnStateChanged(_currentState, newState);
            _currentState = newState;
        }

        // UI更新
        UpdateStateDisplay();
    }

    #endregion

    #region State Management

    /// <summary>
    /// State変更時の処理
    /// </summary>
    void OnStateChanged(GameState oldState, GameState newState)
    {
        LogDebug($"State changed: {oldState} -> {newState}");

        // 汎用イベント発火
        onStateChanged?.Invoke(newState);

        // 各State専用のイベント発火
        switch (newState)
        {
            case GameState.CalibrationStart:
                onCalibrationStart?.Invoke();
                break;

            case GameState.CalibrationEnd:
                onCalibrationEnd?.Invoke();
                break;

            case GameState.Tutorial:
                onTutorialStart?.Invoke();
                break;

            case GameState.Quiz:
                onQuizStart?.Invoke();
                break;

            case GameState.Game:
                onGameStart?.Invoke();
                break;
        }
    }

    /// <summary>
    /// State表示を更新
    /// </summary>
    void UpdateStateDisplay()
    {
        string stateName = GetStateName(_currentState);
        string displayText = $" {(int)_currentState} - {stateName}";

        // UI.Text対応
        if (stateDisplayText != null)
        {
            stateDisplayText.text = displayText;
        }

        // TextMeshPro対応
        if (stateDisplayTextTMP != null)
        {
            stateDisplayTextTMP.text = displayText;
        }
    }

    /// <summary>
    /// Stateの日本語名を取得
    /// </summary>
    string GetStateName(GameState state)
    {
        switch (state)
        {
            case GameState.Other: return "その他";
            case GameState.CalibrationStart: return "キャリブレーション開始";
            case GameState.CalibrationEnd: return "キャリブレーション終了";
            case GameState.Tutorial: return "チュートリアル";
            case GameState.Quiz: return "クイズ";
            case GameState.Game: return "ゲーム";
            default: return "Unknown";
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 現在のStateを取得
    /// </summary>
    public GameState GetCurrentState()
    {
        return _currentState;
    }

    /// <summary>
    /// 特定のStateかどうかチェック
    /// </summary>
    public bool IsState(GameState state)
    {
        return _currentState == state;
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[StateController] {message}");
        }
    }

    #endregion
}
