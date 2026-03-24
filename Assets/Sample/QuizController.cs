using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// クイズモードの選択を管理するコントローラー
/// 2点検知時のみ動作し、高い方の手で回答を決定する
/// </summary>
public class QuizController : MonoBehaviour
{
    public enum QuizChoice { None = 0, Right = 2, Left = 1 }

    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public StateController stateController;

    [Header("UI")]
    public UnityEngine.UI.Text choiceDisplayText;
    public TMPro.TextMeshProUGUI choiceDisplayTextTMP;

    [Header("Settings")]
    public float xCenterPosition = 0f;
    public bool autoSelectMode = true;
    public float autoSelectCooldown = 0.5f;

    [Header("Z-Axis Filter")]
    public bool enableZAxisFilter = true;
    public float zMin = 0f;
    public float zMax = 2.0f;
    public float quizZOffset = 0.05f;

    [Header("Events")]
    public UnityEvent<QuizChoice> onQuizSelected;

    [Header("Debug")]
    public bool enableDebugLog = true;
    [SerializeField] private float _baseZMin;
    [SerializeField] private QuizChoice _currentChoice = QuizChoice.None;

    private float _lastSelectTime = -999f;

    void Update()
    {
        if (oscManager == null) return;

        int currentState = oscManager.GetInt("State");
        if (stateController != null && !stateController.IsState(StateController.GameState.Quiz)) return;
        if (stateController == null && currentState != 4) return;

        UpdateCurrentChoice();
        UpdateChoiceDisplay();

        if (autoSelectMode) HandleAutoSelect();
    }

    void UpdateCurrentChoice()
    {
        // 2点検知時のみ更新
        int count = oscManager.GetInt("Count");
        if (count != 2) return;

        float boxZ = oscManager.GetFloat("BoxZ");
        float z1 = oscManager.GetFloat("PositionZ1") + boxZ * 0.5f;
        float z2 = oscManager.GetFloat("PositionZ2") + boxZ * 0.5f;

        // 両方が範囲内か
        bool z1Valid = !enableZAxisFilter || (z1 >= zMin && z1 <= zMax);
        bool z2Valid = !enableZAxisFilter || (z2 >= zMin && z2 <= zMax);

        if (!z1Valid || !z2Valid) return;

        // 高い方のX座標で判定
        float bestX = (z1 > z2) ? oscManager.GetFloat("PositionX1") : oscManager.GetFloat("PositionX2");
        _currentChoice = (bestX >= xCenterPosition) ? QuizChoice.Right : QuizChoice.Left;
    }

    void UpdateChoiceDisplay()
    {
        string choiceName = (_currentChoice == QuizChoice.Right) ? "右選択" : (_currentChoice == QuizChoice.Left) ? "左選択" : "選択なし";
        string displayText = $"Quiz: {choiceName}\nThreshold: {zMin:F2} ({_baseZMin:F2} + {quizZOffset:F2})";

        if (choiceDisplayText != null) choiceDisplayText.text = displayText;
        if (choiceDisplayTextTMP != null) choiceDisplayTextTMP.text = displayText;
    }

    void HandleAutoSelect()
    {
        if (Time.time - _lastSelectTime < autoSelectCooldown) return;
        oscManager.SetInt("QuizChoice", (int)_currentChoice);
        oscManager.SendMessage("/quiz");
        _lastSelectTime = Time.time;
        onQuizSelected?.Invoke(_currentChoice);
    }

    public void SetZMin(float value) { _baseZMin = value; zMin = _baseZMin + quizZOffset; }
    public void SetZMax(float value) { zMax = value; }

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[QuizController] {message}"); }
}