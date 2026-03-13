using UnityEngine;
using UnityEngine.UI; // Legacy Textを使う場合
using TMPro;           // TextMeshProを使う場合
using System.Collections.Generic;
using System.Linq;

public class AutoHeightController : MonoBehaviour
{
    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public QuizController quizController;
    public PaddleController paddleController;

    [Header("UI Output")]
    [Tooltip("TextMeshProを使用する場合")]
    public TextMeshProUGUI medianTextMesh;
    
    [Tooltip("Legacy Text(旧テキスト)を使用する場合")]
    public Text medianLegacyText;

    [Header("PositionZ Logic Settings")]
    public float startDelay = 0.5f;
    public float zOffset = 0.05f;
    public float minResultFloor = 0.45f;
    public float maxResultCeiling = 0.89f;

    [Header("Recording Duration")]
    public bool useAutoDuration = false;
    public float recordingDuration = 3.0f;

    [Header("Debug/Monitor")]
    [SerializeField] private float lastMedianZ;
    [SerializeField] private float lastCalculatedResult;
    [SerializeField] private float testMaxResult = 2.0f;

    private bool _isRecording = false;
    private List<float> _zSamples = new List<float>();
    private float _delayTimer = 0f;
    private float _recordingTimer = 0f;
    private int _lastState = -1;
    private bool _autoFinished = false;

    private void OnValidate()
    {
        if (Application.isPlaying) ApplyValuesToControllers();
    }

    public void ApplyValuesToControllers()
    {
        if (quizController != null)
        {
            quizController.SetZMin(lastCalculatedResult);
            quizController.SetZMax(testMaxResult);
        }
        if (paddleController != null)
        {
            paddleController.SetZMin(lastCalculatedResult);
            paddleController.SetZMax(testMaxResult);
        }
        
        // UI表示を更新
        UpdateMedianUI();
    }

    void Update()
    {
        if (oscManager == null) return;

        int currentState = oscManager.GetInt("State");
        if (currentState != _lastState)
        {
            OnStateChanged(currentState);
            _lastState = currentState;
        }

        HandleRecording();
    }

    private void OnStateChanged(int newState)
    {
        if (newState == 1)
        {
            _isRecording = false;
            _delayTimer = startDelay;
            _recordingTimer = 0f;
            _autoFinished = false;
            _zSamples.Clear();
        }
        else if (newState == 2)
        {
            if (!_autoFinished && _zSamples.Count > 0) ProcessResult();
            _isRecording = false;
        }
    }

    private void HandleRecording()
    {
        if (_lastState == 1 && !_isRecording && !_autoFinished)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0)
            {
                _isRecording = true;
                _recordingTimer = 0f;
            }
        }

        if (_isRecording)
        {
            _zSamples.Add(oscManager.GetFloat("PositionZ"));
            _recordingTimer += Time.deltaTime;

            if (useAutoDuration && _recordingTimer >= recordingDuration)
            {
                if (_zSamples.Count > 0) ProcessResult();
                _isRecording = false;
                _autoFinished = true;
            }
        }
    }

    private void ProcessResult()
    {
        lastMedianZ = CalculateMedian(_zSamples);
        float rawResult = lastMedianZ + zOffset;
        float rounded = Mathf.Round(rawResult * 100f) / 100f;
        lastCalculatedResult = Mathf.Clamp(rounded, minResultFloor, maxResultCeiling);

        ApplyValuesToControllers();
    }

    // --- UI表示用のメソッドを追加 ---
    private void UpdateMedianUI()
    {
        string displayText = $"{lastMedianZ:F3}";

        // TextMeshProがセットされていれば反映
        if (medianTextMesh != null)
        {
            medianTextMesh.text = displayText;
        }

        // Legacy Textがセットされていれば反映
        if (medianLegacyText != null)
        {
            medianLegacyText.text = displayText;
        }
    }

    private float CalculateMedian(List<float> list)
    {
        var sorted = list.OrderBy(n => n).ToList();
        int count = sorted.Count;
        if (count % 2 == 0) return (sorted[count / 2 - 1] + sorted[sorted.Count / 2]) / 2f;
        return sorted[count / 2];
    }
}