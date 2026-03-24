using UnityEngine;
using UnityEngine.UI; // Legacy Text用
using TMPro;           // TextMeshPro用
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 中央値計算を行い、Z軸コントローラーやUIと連動させるクラス
/// 計測時は「2点ぴったり」検知されているフレームのみをサンプルとして採用します。
/// </summary>
public class AutoHeightController : MonoBehaviour
{
    [Header("References")]
    public MultiAddressOSCManager oscManager;
    public QuizController quizController;
    public PaddleController paddleController;

    [Header("UI Output")]
    [Tooltip("計算結果(lastCalculatedResult)を表示するTextMeshPro")]
    public TextMeshProUGUI resultTextMesh;
    
    [Tooltip("計算結果(lastCalculatedResult)を表示するLegacy Text")]
    public Text resultLegacyText;

    [Header("PositionZ Logic Settings")]
    public float startDelay = 0.5f;
    [Tooltip("中央値に加算するオフセット量")]
    public float zOffset = 0.05f;

    [Tooltip("計算結果の最小値（この値以下は全てこの値に固定）")]
    public float minResultFloor = 0.45f;

    [Tooltip("計算結果の最大値（この値以上は全てこの値に固定）")]
    public float maxResultCeiling = 0.89f;

    [Header("Recording Duration")]
    [Tooltip("ONにすると指定秒数で自動終了、OFFならState=2を待つ")]
    public bool useAutoDuration = false;

    [Tooltip("計測時間（秒）")]
    public float recordingDuration = 3.0f;

    [Header("Debug/Monitor (Values to Test)")]
    [SerializeField, Tooltip("計算された中央値")]
    private float lastMedianZ;
    
    [SerializeField, Tooltip("ここを書き換えると即座にMin(水色の帯の下端)が動きます。UIにも表示されます")]
    private float lastCalculatedResult;

    [SerializeField, Tooltip("ここを書き換えると即座にMax(水色の帯の上端)が動きます")]
    private float testMaxResult = 2.0f;

    [SerializeField, Tooltip("2点検知で正しくサンプリングされた回数")]
    private int _validSampleCount;

    private bool _isRecording = false;
    private List<float> _zSamples = new List<float>();
    private float _delayTimer = 0f;
    private float _recordingTimer = 0f;
    private int _lastState = -1;
    private bool _autoFinished = false; // 自動終了済みフラグ

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
    /// 計算結果をQuizController, PaddleController, および UI に適用する
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

        // UI表示を更新
        UpdateResultUI();
    }

    /// <summary>
    /// UIテキストを更新する
    /// </summary>
    private void UpdateResultUI()
    {
        string displayText = lastCalculatedResult.ToString("F2");

        if (resultTextMesh != null) resultTextMesh.text = displayText;
        if (resultLegacyText != null) resultLegacyText.text = displayText;
    }
    #endregion

    void Update()
    {
        if (oscManager == null) return;

        // OSCから現在のStateを取得
        int currentState = oscManager.GetInt("State");

        // Stateが変化した瞬間を検知（ループ対応：State 1 が来るたびにリセット）
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
            // 計測準備：前回のデータをリセットして待機
            _isRecording = false;
            _delayTimer = startDelay;
            _recordingTimer = 0f;
            _autoFinished = false;
            _zSamples.Clear();
            _validSampleCount = 0;
            Debug.Log("[AutoHeight] State 1: Ready to record next sample.");
        }
        else if (newState == 2)
        {
            // 計測終了：自動終了していなければ、ここで計算を実行
            if (!_autoFinished && _zSamples.Count > 0)
            {
                ProcessResult();
            }
            _isRecording = false;
            Debug.Log("[AutoHeight] State 2: Stopped recording.");
        }
    }

    private void HandleRecording()
    {
        // State 1 の間、開始ディレイを消化
        if (_lastState == 1 && !_isRecording && !_autoFinished)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0)
            {
                _isRecording = true;
                _recordingTimer = 0f;
                Debug.Log("[AutoHeight] Recording started...");
            }
        }

        // 記録中：リストにサンプルを溜める
        if (_isRecording)
        {
            // 【重要】2点ぴったり検知されているフレームのみサンプリング
            int count = oscManager.GetInt("Count");
            if (count == 2)
            {
                float z1 = oscManager.GetFloat("PositionZ1");
                float z2 = oscManager.GetFloat("PositionZ2");
                // 2点のうち、より高い方(オールの先端側)をサンプルとして採用する
                _zSamples.Add(Mathf.Max(z1, z2));
                _validSampleCount = _zSamples.Count;
            }

            _recordingTimer += Time.deltaTime;

            // 自動終了設定がある場合
            if (useAutoDuration && _recordingTimer >= recordingDuration)
            {
                Debug.Log($"[AutoHeight] Auto-stop after {recordingDuration}s ({_validSampleCount} valid samples)");
                if (_zSamples.Count > 0) ProcessResult();
                _isRecording = false;
                _autoFinished = true;
            }
        }
    }

    /// <summary>
    /// 溜まったサンプルから中央値を出し、各種オフセットとクランプを適用して最終結果を出す
    /// </summary>
    private void ProcessResult()
    {
        lastMedianZ = CalculateMedian(_zSamples);

        // 中央値 + offset を計算
        float rawResult = lastMedianZ + zOffset;

        // 小数点第3位を四捨五入（0.456 → 0.46）
        float rounded = Mathf.Round(rawResult * 100f) / 100f;

        // 最小値・最大値でクランプ
        lastCalculatedResult = Mathf.Clamp(rounded, minResultFloor, maxResultCeiling);

        Debug.Log($"[AutoHeight] Processed: Raw={rawResult:F3}, Final={lastCalculatedResult:F2}");

        // 結果をコントローラーとUIに即座に反映
        ApplyValuesToControllers();
    }

    private float CalculateMedian(List<float> list)
    {
        var sorted = list.OrderBy(n => n).ToList();
        int count = sorted.Count;
        if (count == 0) return 0f;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2f;
        }
        return sorted[count / 2];
    }
}