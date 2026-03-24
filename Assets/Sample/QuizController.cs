using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// クイズモードの選択を管理するコントローラー
/// 同一フレームの一括OSCに対応し、フレームレート差による瞬断を防ぐ「遅延ゼロの非対称ロジック」を搭載
/// </summary>
public class QuizController : MonoBehaviour
{
    #region Quiz Choice Enum

    public enum QuizChoice
    {
        None = 0,   // 選択していない
        Right = 2,  // 右選択
        Left = 1    // 左選択
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

    [Tooltip("Z軸の最小値（この値以上の時に有効。※実行時はBase+Offsetが適用される）")]
    public float zMin = 0f;

    [Tooltip("Z軸の最大値（この値以下の時に有効）")]
    public float zMax = 2.0f;

    [Header("Quiz Offset Settings")]
    [Tooltip("AutoHeightからの基準値に加算するクイズ専用のオフセット")]
    public float quizZOffset = 0.05f;

    [Header("Events")]
    [Tooltip("選択を送信した時のイベント")]
    public UnityEvent<QuizChoice> onQuizSelected;

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Header("Debug Monitor (実行中に確認)")]
    [SerializeField] private int _debugState;
    [SerializeField] private bool _debugIsQuizMode;
    [SerializeField] private float _baseZMin; // AutoHeightから受け取った素の値
    [SerializeField] private float _debugBestX; // 採用された手のX座標
    [SerializeField] private float _debugBestZ; // 採用された手の実際のZ座標
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 実行中にインスペクターのOffsetをいじった際、即座に zMin を更新する
        if (Application.isPlaying)
        {
            zMin = _baseZMin + quizZOffset;
            _debugZMin = zMin;
        }
    }
#endif

    #endregion

    #region Quiz Logic

    /// <summary>
    /// 現在の選択を更新（遅延ゼロの非対称ロジック）
    /// </summary>
    void UpdateCurrentChoice()
    {
        int count = oscManager.GetInt("Count");

        // 3点以上はノイズとしてスキップ（前回の選択を維持）
        if (count > 2) return;

        float boxZ = oscManager.GetFloat("BoxZ");
        float bestZ = -999f;
        float bestX = 0f;
        bool foundValid = false;

        // 1. まず「ラインを超えている有効な手」を探す
        for (int i = 1; i <= count; i++)
        {
            string suffix = i.ToString();
            float posX = oscManager.GetFloat("PositionX" + suffix);
            float posZ = oscManager.GetFloat("PositionZ" + suffix);
            float actualZ = posZ + boxZ * 0.5f;

            // Z軸範囲フィルタ
            bool zInRange = !enableZAxisFilter || (actualZ >= zMin && actualZ <= zMax);

            if (zInRange)
            {
                // 有効な点の中で最も高い(Z大)ものを記録
                if (!foundValid || actualZ > bestZ)
                {
                    bestZ = actualZ;
                    bestX = posX;
                    foundValid = true;
                }
            }
        }

        // 2. 状態の決定（最善策コアロジック）
        if (foundValid)
        {
            // 【即座に選択】ラインを超えている手があれば、高い方で1か2を決定
            _currentChoice = (bestX >= xCenterPosition) ? QuizChoice.Right : QuizChoice.Left;
            
            // デバッグ表示用
            _debugBestX = bestX;
            _debugBestZ = bestZ;
        }
        else
        {
            // ラインを超えている手がない場合、0にするか維持するかを判定
            if (count == 2 || count == 0)
            {
                // 【即座にキャンセル】両手(2点)がしっかり見えていて両方ライン下の場合、
                // または誰もいなくなった(0点)の場合 → 0(None)にする
                _currentChoice = QuizChoice.None;
                
                // デバッグ表示リセット
                _debugBestX = 0f;
                _debugBestZ = 0f;
            }
            else
            {
                // 【維持】1点しか見えない場合
                // → センサーの瞬断や死角に入っただけとみなし、前回の選択(_currentChoice)を維持！
                LogDebug("<color=yellow>トラッキング不安定(1点のみ)のため、リセット(0)を保留し前回の姿勢を維持します</color>");
            }
        }

        _debugCurrentChoice = _currentChoice;
    }

    /// <summary>
    /// 選択表示を更新
    /// </summary>
    void UpdateChoiceDisplay()
    {
        string choiceName = GetChoiceName(_currentChoice);
        // 判定しきい値(zMin)とその内訳を表示
        string displayText = $"Quiz: {(int)_currentChoice} - {choiceName}\nThreshold: {zMin:F2} ({_baseZMin:F2} + {quizZOffset:F2})";

        if (choiceDisplayText != null) choiceDisplayText.text = displayText;
        if (choiceDisplayTextTMP != null) choiceDisplayTextTMP.text = displayText;
    }

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

    void HandleAutoSelect()
    {
        if (Time.time - _lastSelectTime < autoSelectCooldown) return;
        SendQuizChoice(_currentChoice);
    }

    void HandleManualSelect()
    {
        if (_currentChoice == QuizChoice.None) return;
        SendQuizChoice(_currentChoice);
    }

    void SendQuizChoice(QuizChoice choice)
    {
        oscManager.SetInt("QuizChoice", (int)choice);
        oscManager.SendMessage("/quiz");

        _lastSelectTime = Time.time;

        LogDebug($"Quiz selected: {choice}");
        onQuizSelected?.Invoke(choice);
    }

    #endregion

    #region Public Methods

    public QuizChoice GetCurrentChoice()
    {
        return _currentChoice;
    }

    public void ForceSelect(QuizChoice choice)
    {
        SendQuizChoice(choice);
    }

    public void ConfirmCurrentChoice()
    {
        if (_currentChoice != QuizChoice.None)
        {
            SendQuizChoice(_currentChoice);
        }
    }

    public void SetZMin(float value)
    {
        _baseZMin = value; 
        zMin = _baseZMin + quizZOffset; 
        _debugZMin = zMin;
        LogDebug($"Quiz ZMin updated: {zMin:F3} (Base:{_baseZMin:F3} + Offset:{quizZOffset:F3})");
    }

    public void SetZMax(float value)
    {
        zMax = value;
        _debugZMax = value;
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