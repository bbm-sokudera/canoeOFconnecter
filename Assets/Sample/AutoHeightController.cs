using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 中央値計算を行い、Z軸コントローラーやオシロスコープと連動させるクラス
/// </summary>
public class AutoHeightController : MonoBehaviour
{
    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public QuizController quizController;
    public PaddleController paddleController;

    [Header("PositionZ Logic Settings")]
    public float startDelay = 0.5f;
    public float zOffset = 0.05f;

    [Header("Debug/Monitor (Values to Test)")]
    [SerializeField, Tooltip("計算された中央値")]
    private float lastMedianZ;
    
    [SerializeField, Tooltip("ここを書き換えると即座にMin(水色の帯の下端)が動きます")]
    private float lastCalculatedResult;

    [SerializeField, Tooltip("ここを書き換えると即座にMax(水色の帯の上端)が動きます")]
    private float testMaxResult = 2.0f;

    private bool _isRecording = false;
    private List<float> _zSamples = new List<float>();
    private float _delayTimer = 0f;
    private int _lastState = -1;

    #region Unity Editor Logic
    
    // インスペクターで値が変更されたときに自動で呼ばれる（再生中のみ反映）
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyValuesToControllers();
        }
    }

    /// <summary>
    /// 計算結果をQuizControllerとPaddleControllerに適用する
    /// </summary>
    [ContextMenu("Apply Inspector Values Now")]
    public void ApplyValuesToControllers()
    {
        // QuizControllerに反映
        if (quizController != null)
        {
            quizController.SetZMin(lastCalculatedResult);
            quizController.SetZMax(testMaxResult);
        }

        // PaddleControllerに反映
        if (paddleController != null)
        {
            paddleController.SetZMin(lastCalculatedResult);
            paddleController.SetZMax(testMaxResult);
        }

        Debug.Log($"[AutoHeight] Applied to controllers: Min={lastCalculatedResult:F3}, Max={testMaxResult:F3}");
    }
    #endregion

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
            _zSamples.Clear();
        }
        else if (newState == 2)
        {
            if (_zSamples.Count > 0) ProcessResult();
            _isRecording = false;
        }
    }

    private void HandleRecording()
    {
        if (_lastState == 1 && !_isRecording)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0) _isRecording = true;
        }

        if (_isRecording)
        {
            _zSamples.Add(oscManager.GetFloat("PositionZ"));
        }
    }

    private void ProcessResult()
    {
        lastMedianZ = CalculateMedian(_zSamples);
        lastCalculatedResult = lastMedianZ + zOffset;
        
        // 自動計算後もUIに反映
        ApplyValuesToControllers();
        
        Debug.Log($"[AutoHeight] Auto-calculated Result: {lastCalculatedResult:F3}");
    }

    private float CalculateMedian(List<float> list)
    {
        var sorted = list.OrderBy(n => n).ToList();
        int count = sorted.Count;
        if (count % 2 == 0) return (sorted[count / 2 - 1] + sorted[sorted.Count / 2]) / 2f;
        return sorted[count / 2];
    }
}