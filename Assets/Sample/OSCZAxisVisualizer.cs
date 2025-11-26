using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Z軸の値をリアルタイムで履歴表示するビジュアライザー
/// 過去3コマ分を表示し、最新のみ赤色で強調表示
/// </summary>
public class OSCZAxisVisualizer : MonoBehaviour
{
    [Header("Target Manager")]
    [Tooltip("監視対象のOSCXPositionYVectorManager")]
    public OSCXPositionYVectorManager targetManager;

    [Header("UI Images for History (最新→過去の順)")]
    [Tooltip("最新のZ値を表示するImage（赤色）")]
    public Image currentImage;

    [Tooltip("1コマ前のZ値を表示するImage（グレー）")]
    public Image history1Image;

    [Tooltip("2コマ前のZ値を表示するImage（グレー）")]
    public Image history2Image;

    [Header("UI Text for Z Values")]
    [Tooltip("最新のZ値を表示するText")]
    public Text currentText;

    [Tooltip("1コマ前のZ値を表示するText")]
    public Text history1Text;

    [Tooltip("2コマ前のZ値を表示するText")]
    public Text history2Text;

    [Header("Color Settings")]
    [Tooltip("最新フレームの色（赤）")]
    public Color currentColor = Color.red;

    [Tooltip("過去フレームの色（グレー）")]
    public Color historyColor = Color.gray;

    [Header("Update Settings")]
    [Tooltip("更新頻度（秒）：この間隔でZ値を取得")]
    public float updateInterval = 0.1f;

    // Z値の履歴（最新が先頭）
    private Queue<float> _zHistory = new Queue<float>();
    private const int MAX_HISTORY = 3;

    // 更新タイマー
    private float _updateTimer = 0f;

    void Start()
    {
        // 初期化：履歴を0で埋める
        for (int i = 0; i < MAX_HISTORY; i++)
        {
            _zHistory.Enqueue(0f);
        }

        UpdateDisplay();
    }

    void Update()
    {
        if (targetManager == null)
            return;

        _updateTimer += Time.deltaTime;

        if (_updateTimer >= updateInterval)
        {
            _updateTimer = 0f;

            // 現在のZ値を取得
            Vector3 movement = targetManager.GetMovementVector();
            float currentZ = movement.z;

            // 履歴に追加
            _zHistory.Enqueue(currentZ);

            // 古いデータを削除
            if (_zHistory.Count > MAX_HISTORY)
            {
                _zHistory.Dequeue();
            }

            // 表示更新
            UpdateDisplay();
        }
    }

    /// <summary>
    /// 表示を更新
    /// </summary>
    void UpdateDisplay()
    {
        float[] history = _zHistory.ToArray();

        // 最新（インデックス2 = 最後）
        if (history.Length > 2)
        {
            UpdateFrame(currentImage, currentText, history[2], currentColor);
        }

        // 1コマ前（インデックス1）
        if (history.Length > 1)
        {
            UpdateFrame(history1Image, history1Text, history[1], historyColor);
        }

        // 2コマ前（インデックス0 = 最初）
        if (history.Length > 0)
        {
            UpdateFrame(history2Image, history2Text, history[0], historyColor);
        }
    }

    /// <summary>
    /// 個別フレームの更新
    /// </summary>
    void UpdateFrame(Image image, Text text, float zValue, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }

        if (text != null)
        {
            text.text = $"Z: {zValue:F3}";
        }
    }

    /// <summary>
    /// 現在のZ値を取得（外部から使用可能）
    /// </summary>
    public float GetCurrentZValue()
    {
        if (targetManager == null)
            return 0f;

        Vector3 movement = targetManager.GetMovementVector();
        return movement.z;
    }

    /// <summary>
    /// 履歴をクリア
    /// </summary>
    public void ClearHistory()
    {
        _zHistory.Clear();

        for (int i = 0; i < MAX_HISTORY; i++)
        {
            _zHistory.Enqueue(0f);
        }

        UpdateDisplay();
    }
}
