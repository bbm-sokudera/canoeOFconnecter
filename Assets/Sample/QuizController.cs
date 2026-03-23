using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// クイズモードの選択を管理するコントローラー
/// 同一フレーム内の複数データから最も高い(Z大)点を採用して判定する
/// </summary>
public class QuizController : MonoBehaviour
{
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
    public bool autoSelectMode = true;
    public float autoSelectCooldown = 0.5f;

    [Header("Z-Axis Filter (AutoHeight)")]
    public bool enableZAxisFilter = true;
    public float zMin = 0f; // Base + Offset
    public float zMax = 2.0f;

    [Header("Quiz Offset Settings")]
    [Tooltip("AutoHeightからの基準値に加算するクイズ専用のオフセット")]
    public float quizZOffset = 0.05f;

    [Header("Events")]
    public UnityEvent<QuizChoice> onQuizSelected;

    [Header("Debug Monitor")]
    [SerializeField] private float _baseZMin;
    [SerializeField] private QuizChoice _debugCurrentChoice;
    public bool enableDebugLog = true;
    #endregion

    private float _lastSelectTime = -999f;
    private QuizChoice _currentChoice = QuizChoice.None;

    void Update()
    {
        if (oscManager == null) return;

        // クイズモード(State=4)の時のみ動作
        int currentState = oscManager.GetInt("State");
        if (stateController != null && !stateController.IsState(StateController.GameState.Quiz)) return;
        if (stateController == null && currentState != 4) return;

        UpdateCurrentChoice();
        UpdateChoiceDisplay();

        if (autoSelectMode) HandleAutoSelect();
    }

    /// <summary>
    /// 同一フレーム内のデータから「最も高い手」を選んで選択肢を決定
    /// </summary>
    void UpdateCurrentChoice()
    {
        int count = oscManager.GetInt("Count");

        // 3点以上のノイズ時は「選択なし」として安全側に倒す
        if (count <= 0 || count > 2)
        {
            if (count > 2) LogDebug($"<color=orange>[制限] 検知数過多({count})のため無視します</color>");
            _currentChoice = QuizChoice.None;
            return;
        }

        float boxZ = oscManager.GetFloat("BoxZ");
        float bestZ = -999f;
        float bestX = 0f;
        bool foundValid = false;

        // 1点目と2点目を比較
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
                // より高い(Zが大きい)方を採用
                if (!foundValid || actualZ > bestZ)
                {
                    bestZ = actualZ;
                    bestX = posX;
                    foundValid = true;
                }
            }
        }

        if (!foundValid)
        {
            _currentChoice = QuizChoice.None;
        }
        else
        {
            // 最も高かった手のX座標で回答を決定
            _currentChoice = (bestX >= xCenterPosition) ? QuizChoice.Right : QuizChoice.Left;
        }

        _debugCurrentChoice = _currentChoice;
    }

    void UpdateChoiceDisplay()
    {
        string choiceName = GetChoiceName(_currentChoice);
        string displayText = $"Quiz: {choiceName}\nThreshold: {zMin:F2} ({_baseZMin:F2} + {quizZOffset:F2})";

        if (choiceDisplayText != null) choiceDisplayText.text = displayText;
        if (choiceDisplayTextTMP != null) choiceDisplayTextTMP.text = displayText;
    }

    string GetChoiceName(QuizChoice choice)
    {
        switch (choice) {
            case QuizChoice.None: return "選択なし";
            case QuizChoice.Right: return "右選択";
            case QuizChoice.Left: return "左選択";
            default: return "不明";
        }
    }

    void HandleAutoSelect()
    {
        if (Time.time - _lastSelectTime < autoSelectCooldown) return;
        SendQuizChoice(_currentChoice);
    }

    void SendQuizChoice(QuizChoice choice)
    {
        oscManager.SetInt("QuizChoice", (int)choice);
        oscManager.SendMessage("/quiz");
        _lastSelectTime = Time.time;
        onQuizSelected?.Invoke(choice);
    }

    public void SetZMin(float value)
    {
        _baseZMin = value;
        zMin = _baseZMin + quizZOffset;
        LogDebug($"しきい値更新: {zMin:F3}");
    }

    public void SetZMax(float value) { zMax = value; }

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[QuizController] {message}"); }
}