using UnityEngine;
using UnityEngine.Events;

public class QuizController : MonoBehaviour
{
    // --- 既存のEnum, Inspector Settings, Variables は完全に維持 ---
    #region Quiz Choice Enum
    public enum QuizChoice { None = 0, Right = 2, Left = 1 }
    #endregion

    #region Inspector Settings
    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public StateController stateController;

    [Header("UI")]
    public UnityEngine.UI.Text choiceDisplayText;
    public TMPro.TextMeshProUGUI choiceDisplayTextTMP;

    [Header("Quiz Settings")]
    public float xCenterPosition = 0f;
    public KeyCode confirmKey = KeyCode.Space;
    public bool autoSelectMode = true;
    public float autoSelectCooldown = 0.1f;

    [Header("Z-Axis Filter (AutoHeight)")]
    public bool enableZAxisFilter = true;
    public float zMin = 0f;
    public float zMax = 2.0f;

    [Header("Quiz Offset Settings")]
    public float quizZOffset = 0.05f;

    [Header("Events")]
    public UnityEvent<QuizChoice> onQuizSelected;

    [Header("Debug")]
    public bool enableDebugLog = true;

    [Header("Debug Monitor")]
    [SerializeField] private int _debugState;
    [SerializeField] private bool _debugIsQuizMode;
    [SerializeField] private float _baseZMin;
    [SerializeField] private float _debugBestX;
    [SerializeField] private float _debugBestZ;
    [SerializeField] private QuizChoice _debugCurrentChoice;
    [SerializeField] private float _debugZMin;
    [SerializeField] private float _debugZMax;
    #endregion

    private float _lastSelectTime = -999f;
    private QuizChoice _currentChoice = QuizChoice.None;

    #region Unity Lifecycle
    void Update()
    {
        if (oscManager == null) return;
        _debugState = oscManager.GetInt("State");

        if (stateController != null && !stateController.IsState(StateController.GameState.Quiz)) { _debugIsQuizMode = false; return; }
        if (stateController == null && _debugState != 4) { _debugIsQuizMode = false; return; }

        _debugIsQuizMode = true;
        UpdateCurrentChoice();
        UpdateChoiceDisplay();

        if (autoSelectMode) HandleAutoSelect();
        else if (Input.GetKeyDown(confirmKey)) HandleManualSelect();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) { zMin = _baseZMin + quizZOffset; _debugZMin = zMin; }
    }
#endif
    #endregion

    #region Quiz Logic (Upgraded with Min/Max X)

    void UpdateCurrentChoice()
    {
        int count = oscManager.GetInt("Count");
        if (count <= 0) 
        {
            // 【維持】完全ロスト時は前回の選択をキープ（瞬断対策）
            return; 
        }

        // 1. 全点の中からX軸の最小(左端)・最大(右端)を抽出
        // これにより3点以上のノイズ（体など）があってもオールの先端を特定できる
        HandData leftEnd = GetExtremeXPoint(count, true);
        HandData rightEnd = GetExtremeXPoint(count, false);

        // 2. 抽出した両端のうち、より高い方をアクション対象とする
        HandData target = (leftEnd.posZ > rightEnd.posZ) ? leftEnd : rightEnd;

        // 3. 有効判定と状態決定
        if (target.isValid)
        {
            // 【即座に選択】
            _currentChoice = (target.posX >= xCenterPosition) ? QuizChoice.Right : QuizChoice.Left;
            _debugBestX = target.posX;
            _debugBestZ = target.posZ;
        }
        else
        {
            // 有効な高度に点がない場合
            // 両端ともしっかり見えている（count >= 2相当）がどちらも低いなら「選択なし」
            // 1点しか見えていない状態での低高度は「瞬断/死角」の可能性が高いため維持する
            if (count >= 2)
            {
                _currentChoice = QuizChoice.None;
                _debugBestX = 0f;
                _debugBestZ = 0f;
            }
        }

        _debugCurrentChoice = _currentChoice;
    }

    // X軸の極値を抽出する汎用メソッド
    HandData GetExtremeXPoint(int count, bool findMin)
    {
        HandData extreme = new HandData { isValid = false, posX = findMin ? 999f : -999f };
        float boxZ = oscManager.GetFloat("BoxZ");

        for (int i = 1; i <= count; i++)
        {
            string s = i.ToString();
            float px = oscManager.GetFloat("PositionX" + s);
            float pz = oscManager.GetFloat("PositionZ" + s) + (boxZ * 0.5f);

            // 高度フィルタ
            if (enableZAxisFilter && (pz < zMin || pz > zMax)) continue;

            if (findMin) { if (px < extreme.posX) SetHand(ref extreme, px, pz); }
            else { if (px > extreme.posX) SetHand(ref extreme, px, pz); }
        }
        return extreme;
    }

    void SetHand(ref HandData h, float x, float z) { h.isValid = true; h.posX = x; h.posZ = z; }

    // --- 以下、既存の Display, Send, Public メソッドは完全に継承 ---
    void UpdateChoiceDisplay() { /* 既存通り */ string choiceName = GetChoiceName(_currentChoice); string displayText = $"Quiz: {(int)_currentChoice} - {choiceName}\nThreshold: {zMin:F2} ({_baseZMin:F2} + {quizZOffset:F2})"; if (choiceDisplayText != null) choiceDisplayText.text = displayText; if (choiceDisplayTextTMP != null) choiceDisplayTextTMP.text = displayText; }
    string GetChoiceName(QuizChoice choice) { switch (choice) { case QuizChoice.None: return "選択していない"; case QuizChoice.Right: return "右選択"; case QuizChoice.Left: return "左選択"; default: return "Unknown"; } }
    void HandleAutoSelect() { if (Time.time - _lastSelectTime < autoSelectCooldown) return; SendQuizChoice(_currentChoice); }
    void HandleManualSelect() { if (_currentChoice == QuizChoice.None) return; SendQuizChoice(_currentChoice); }
    void SendQuizChoice(QuizChoice choice) { oscManager.SetInt("QuizChoice", (int)choice); oscManager.SendMessage("/quiz"); _lastSelectTime = Time.time; LogDebug($"Quiz selected: {choice}"); onQuizSelected?.Invoke(choice); }
    public QuizChoice GetCurrentChoice() => _currentChoice;
    public void ForceSelect(QuizChoice choice) => SendQuizChoice(choice);
    public void ConfirmCurrentChoice() { if (_currentChoice != QuizChoice.None) SendQuizChoice(_currentChoice); }
    public void SetZMin(float value) { _baseZMin = value; zMin = _baseZMin + quizZOffset; _debugZMin = zMin; }
    public void SetZMax(float value) { zMax = value; _debugZMax = value; }
    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[QuizController] {message}"); }

    private struct HandData { public bool isValid; public float posX; public float posZ; }
    #endregion
}