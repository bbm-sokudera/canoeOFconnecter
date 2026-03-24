using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 2点検知フレーム限定で中央値を計測するキャリブレーター
/// </summary>
public class AutoHeightController : MonoBehaviour
{
    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public QuizController quizController;
    public PaddleController paddleController;

    [Header("Settings")]
    public float startDelay = 0.5f;
    public float zOffset = 0.05f;
    public float minResultFloor = 0.45f;
    public float maxResultCeiling = 0.89f;
    public float recordingDuration = 3.0f;

    private bool _isRecording = false;
    private List<float> _zSamples = new List<float>();
    private float _recordingTimer = 0f;
    private float _delayTimer = 0f;
    private int _lastState = -1;

    void Update()
    {
        if (oscManager == null) return;
        int state = oscManager.GetInt("State");
        if (state != _lastState) { OnStateChanged(state); _lastState = state; }
        HandleRecording();
    }

    void OnStateChanged(int newState)
    {
        if (newState == 1) { // 開始
            _isRecording = false; _delayTimer = startDelay; _recordingTimer = 0f; _zSamples.Clear();
        } else if (newState == 2) { // 終了
            if (_isRecording && _zSamples.Count > 0) ProcessResult();
            _isRecording = false;
        }
    }

    void HandleRecording()
    {
        if (_lastState == 1 && !_isRecording) {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0) _isRecording = true;
        }

        if (_isRecording) {
            int count = oscManager.GetInt("Count");
            if (count == 2) { // 2点検知時のみ
                _zSamples.Add(Mathf.Max(oscManager.GetFloat("PositionZ1"), oscManager.GetFloat("PositionZ2")));
            }
            _recordingTimer += Time.deltaTime;
            if (_recordingTimer >= recordingDuration) {
                if (_zSamples.Count > 0) ProcessResult();
                _isRecording = false;
            }
        }
    }

    void ProcessResult()
    {
        var sorted = _zSamples.OrderBy(n => n).ToList();
        float median = (sorted.Count % 2 == 0) ? (sorted[sorted.Count/2-1] + sorted[sorted.Count/2])/2f : sorted[sorted.Count/2];
        float result = Mathf.Clamp(Mathf.Round((median + zOffset) * 100f) / 100f, minResultFloor, maxResultCeiling);
        
        if (quizController != null) quizController.SetZMin(result);
        if (paddleController != null) paddleController.SetZMin(result);
        Debug.Log($"[AutoHeight] Result: {result} (Samples: {_zSamples.Count})");
    }
}