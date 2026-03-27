using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class AutoHeightController : MonoBehaviour
{
    // --- 既存の Inspector Settings は完全に維持 ---
    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public QuizController quizController;
    public PaddleController paddleController;

    [Header("UI Output")]
    public TextMeshProUGUI resultTextMesh;
    public Text resultLegacyText;

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
    [SerializeField] private int _validSampleCount;

    private bool _isRecording = false;
    private List<float> _zSamples = new List<float>();
    private float _delayTimer = 0f;
    private float _recordingTimer = 0f;
    private int _lastState = -1;
    private bool _autoFinished = false;

    // --- 既存の OnValidate, Apply, UI更新 は完全に維持 ---
    private void OnValidate() { if (Application.isPlaying) ApplyValuesToControllers(); }
    public void ApplyValuesToControllers() { if (quizController != null) { quizController.SetZMin(lastCalculatedResult); quizController.SetZMax(testMaxResult); } if (paddleController != null) { paddleController.SetZMin(lastCalculatedResult); paddleController.SetZMax(testMaxResult); } UpdateResultUI(); }
    private void UpdateResultUI() { string displayText = lastCalculatedResult.ToString("F2"); if (resultTextMesh != null) resultTextMesh.text = displayText; if (resultLegacyText != null) resultLegacyText.text = displayText; }

    void Update()
    {
        if (oscManager == null) return;
        int currentState = oscManager.GetInt("State");
        if (currentState != _lastState) { OnStateChanged(currentState); _lastState = currentState; }
        HandleRecording();
    }

    private void OnStateChanged(int newState)
    {
        if (newState == 1) { _isRecording = false; _delayTimer = startDelay; _recordingTimer = 0f; _autoFinished = false; _zSamples.Clear(); _validSampleCount = 0; }
        else if (newState == 2) { if (!_autoFinished && _zSamples.Count > 0) ProcessResult(); _isRecording = false; }
    }

    private void HandleRecording()
    {
        if (_lastState == 1 && !_isRecording && !_autoFinished)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0) { _isRecording = true; _recordingTimer = 0f; }
        }

        if (_isRecording)
        {
            int count = oscManager.GetInt("Count");
            if (count >= 1) // 1点以上あればサンプリング対象とする
            {
                // 【アップグレード】全点から左右両端のZを特定
                float minX = 999f, maxX = -999f;
                float zAtMinX = 0f, zAtMaxX = 0f;
                bool foundLeft = false, foundRight = false;

                for (int i = 1; i <= count; i++)
                {
                    float px = oscManager.GetFloat("PositionX" + i);
                    float pz = oscManager.GetFloat("PositionZ" + i);
                    
                    if (px < minX) { minX = px; zAtMinX = pz; foundLeft = true; }
                    if (px > maxX) { maxX = px; zAtMaxX = pz; foundRight = true; }
                }

                // 両端（左と右）がしっかり分かれている（ある程度の幅がある）とき、
                // より高い方を「オールの手元」としてサンプリング
                if (foundLeft && foundRight)
                {
                    _zSamples.Add(Mathf.Max(zAtMinX, zAtMaxX));
                    _validSampleCount = _zSamples.Count;
                }
            }

            _recordingTimer += Time.deltaTime;
            if (useAutoDuration && _recordingTimer >= recordingDuration)
            {
                if (_zSamples.Count > 0) ProcessResult();
                _isRecording = false; _autoFinished = true;
            }
        }
    }

    // --- 既存の Calculation ロジックは維持 ---
    private void ProcessResult()
    {
        lastMedianZ = CalculateMedian(_zSamples);
        float rawResult = lastMedianZ + zOffset;
        float rounded = Mathf.Round(rawResult * 100f) / 100f;
        lastCalculatedResult = Mathf.Clamp(rounded, minResultFloor, maxResultCeiling);
        ApplyValuesToControllers();
    }

    private float CalculateMedian(List<float> list)
    {
        var sorted = list.OrderBy(n => n).ToList();
        int count = sorted.Count;
        if (count == 0) return 0f;
        if (count % 2 == 0) return (sorted[count / 2 - 1] + sorted[count / 2]) / 2f;
        return sorted[count / 2];
    }
}