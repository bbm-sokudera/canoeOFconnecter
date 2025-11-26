using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OSC送信値に反応してUI-Imageの色を変更するビジュアライザー
/// OSCXPositionYVectorManagerのイベントに反応します
/// </summary>
public class OSCValueVisualizer : MonoBehaviour
{
    [Header("Target Manager")]
    [Tooltip("監視対象のOSCXPositionYVectorManager")]
    public OSCXPositionYVectorManager targetManager;

    [Header("UI Images for Each Direction")]
    [Tooltip("右方向（通常）を表示するImage")]
    public Image rightImage;

    [Tooltip("左方向（通常）を表示するImage")]
    public Image leftImage;

    [Tooltip("右方向（後進のみモード）を表示するImage")]
    public Image rightBackwardImage;

    [Tooltip("左方向（後進のみモード）を表示するImage")]
    public Image leftBackwardImage;

    [Header("Active Colors (値が送信された時の色)")]
    [Tooltip("右方向（値=0）がアクティブな時の色")]
    public Color rightActiveColor = new Color(1f, 0.2f, 0.2f, 1f); // 明るい赤

    [Tooltip("左方向（値=2）がアクティブな時の色")]
    public Color leftActiveColor = new Color(0.2f, 0.2f, 1f, 1f); // 明るい青

    [Tooltip("右＋後進（値=1）がアクティブな時の色")]
    public Color rightBackwardActiveColor = new Color(1f, 0.5f, 0f, 1f); // オレンジ

    [Tooltip("左＋後進（値=3）がアクティブな時の色")]
    public Color leftBackwardActiveColor = new Color(0.5f, 0f, 1f, 1f); // 紫

    [Header("Inactive Color (非アクティブ時の色)")]
    [Tooltip("非アクティブな時の色")]
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // グレー（半透明）

    [Header("Flash Settings")]
    [Tooltip("色が光ってから元に戻るまでの時間（秒）")]
    public float flashDuration = 0.3f;

    [Tooltip("フェードアウト効果を有効にする")]
    public bool enableFadeOut = true;

    // 現在アクティブな値
    private int _currentActiveValue = -1;

    // フラッシュタイマー
    private float _flashTimer = 0f;
    private int _flashingValue = -1;

    void Start()
    {
        // 全てのImageを非アクティブ色に初期化
        ResetAllColors();

        // OSCマネージャーのイベントに登録
        if (targetManager != null)
        {
            targetManager.onValueSent.AddListener(OnOSCValueSent);
        }
        else
        {
            Debug.LogWarning("[OSCValueVisualizer] Target Manager is not assigned!");
        }
    }

    void OnDestroy()
    {
        // イベントから登録解除
        if (targetManager != null)
        {
            targetManager.onValueSent.RemoveListener(OnOSCValueSent);
        }
    }

    void Update()
    {
        // フラッシュ効果の更新
        if (enableFadeOut && _flashingValue != -1)
        {
            _flashTimer += Time.deltaTime;

            if (_flashTimer >= flashDuration)
            {
                // フラッシュ終了：全て非アクティブ色に戻す
                ResetAllColors();
                _flashingValue = -1;
                _flashTimer = 0f;
            }
            else
            {
                // フェードアウト中
                float t = _flashTimer / flashDuration;
                UpdateColorWithFade(_flashingValue, 1f - t);
            }
        }
    }

    /// <summary>
    /// OSC値が送信された時に呼ばれる
    /// </summary>
    private void OnOSCValueSent(int value)
    {
        _currentActiveValue = value;
        _flashingValue = value;
        _flashTimer = 0f;

        // すべてを非アクティブ色にリセット
        ResetAllColors();

        // 該当する値のImageをアクティブ色にする
        SetActiveColor(value);

        // デバッグログ
        Debug.Log($"[OSCValueVisualizer] Value {value} sent. Updating UI.");
    }

    /// <summary>
    /// 指定された値に対応するImageをアクティブ色にする
    /// </summary>
    private void SetActiveColor(int value)
    {
        if (targetManager == null)
            return;

        // 設定値と比較して対応するImageを光らせる
        if (value == targetManager.rightValue)
        {
            // 右（通常）
            if (rightImage != null)
                rightImage.color = rightActiveColor;
        }
        else if (value == targetManager.rightBackwardValue)
        {
            // 右（後進のみ）
            if (rightBackwardImage != null)
                rightBackwardImage.color = rightBackwardActiveColor;
        }
        else if (value == targetManager.leftValue)
        {
            // 左（通常）
            if (leftImage != null)
                leftImage.color = leftActiveColor;
        }
        else if (value == targetManager.leftBackwardValue)
        {
            // 左（後進のみ）
            if (leftBackwardImage != null)
                leftBackwardImage.color = leftBackwardActiveColor;
        }
    }

    /// <summary>
    /// フェードアウト中の色を更新
    /// </summary>
    private void UpdateColorWithFade(int value, float alpha)
    {
        if (targetManager == null)
            return;

        Color activeColor = Color.white;

        // 設定値と比較して対応するImageをフェード
        if (value == targetManager.rightValue)
        {
            activeColor = rightActiveColor;
            if (rightImage != null)
                rightImage.color = Color.Lerp(inactiveColor, activeColor, alpha);
        }
        else if (value == targetManager.rightBackwardValue)
        {
            activeColor = rightBackwardActiveColor;
            if (rightBackwardImage != null)
                rightBackwardImage.color = Color.Lerp(inactiveColor, activeColor, alpha);
        }
        else if (value == targetManager.leftValue)
        {
            activeColor = leftActiveColor;
            if (leftImage != null)
                leftImage.color = Color.Lerp(inactiveColor, activeColor, alpha);
        }
        else if (value == targetManager.leftBackwardValue)
        {
            activeColor = leftBackwardActiveColor;
            if (leftBackwardImage != null)
                leftBackwardImage.color = Color.Lerp(inactiveColor, activeColor, alpha);
        }
    }

    /// <summary>
    /// すべてのImageを非アクティブ色にリセット
    /// </summary>
    private void ResetAllColors()
    {
        if (rightImage != null)
            rightImage.color = inactiveColor;

        if (leftImage != null)
            leftImage.color = inactiveColor;

        if (rightBackwardImage != null)
            rightBackwardImage.color = inactiveColor;

        if (leftBackwardImage != null)
            leftBackwardImage.color = inactiveColor;
    }

    /// <summary>
    /// 現在アクティブな値を取得
    /// </summary>
    public int GetCurrentActiveValue()
    {
        return _currentActiveValue;
    }

    /// <summary>
    /// 手動でビジュアライズをトリガー（テスト用）
    /// </summary>
    public void TestVisualize(int value)
    {
        OnOSCValueSent(value);
    }
}
