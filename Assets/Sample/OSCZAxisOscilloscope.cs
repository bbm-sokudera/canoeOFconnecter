using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// OSCのZ軸データをオシロスコープ形式でリアルタイム可視化
/// UI.Imageとテクスチャを使って波形グラフを描画
/// 表示/非表示切り替え時にパフォーマンス負荷を最小化
/// </summary>
public class OSCZAxisOscilloscope : MonoBehaviour
{
    #region Inspector Settings

    [Header("Data Source")]
    [Tooltip("Z値を取得するOSCマネージャー")]
    public OSCXPositionYVectorManager targetManager;

    [Header("UI References")]
    [Tooltip("グラフを描画するUI Image")]
    public Image graphImage;

    [Tooltip("表示/非表示を切り替えるToggle")]
    public Toggle visibilityToggle;

    [Header("Graph Settings")]
    [Tooltip("グラフに表示する最大データポイント数")]
    [Range(10, 500)]
    public int maxDataPoints = 100;

    [Tooltip("サンプリング間隔（秒）")]
    [Range(0.01f, 5f)]
    public float samplingInterval = 1f;

    [Header("Graph Dimensions")]
    [Tooltip("グラフの幅（ピクセル）")]
    public int graphWidth = 512;

    [Tooltip("グラフの高さ（ピクセル）")]
    public int graphHeight = 256;

    [Header("Y-Axis Scale")]
    [Tooltip("Y軸の最小値（固定スケール）")]
    public float yMin = -1f;

    [Tooltip("Y軸の最大値（固定スケール）")]
    public float yMax = 1f;

    [Header("Appearance")]
    [Tooltip("背景色")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Tooltip("波形の線の色")]
    public Color waveformColor = Color.green;

    [Tooltip("波形の線の太さ（ピクセル）")]
    [Range(1, 5)]
    public int lineThickness = 2;

    [Tooltip("グリッド線を表示する")]
    public bool showGrid = true;

    [Tooltip("グリッド線の色")]
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Tooltip("横グリッド線の数")]
    [Range(0, 10)]
    public int horizontalGridLines = 4;

    [Tooltip("縦グリッド線の数")]
    [Range(0, 20)]
    public int verticalGridLines = 10;

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = false;

    #endregion

    #region Private Variables

    private List<float> _dataPoints = new List<float>();
    private Texture2D _graphTexture;
    private float _samplingTimer = 0f;
    private bool _isVisible = true;
    private bool _isInitialized = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeGraph();
        SetupToggle();
    }

    void Update()
    {
        // 非表示時は全処理をスキップ（パフォーマンス最適化）
        if (!_isVisible || !_isInitialized)
            return;

        // データ収集
        CollectData();

        // グラフ描画
        DrawGraph();
    }

    void OnDestroy()
    {
        // テクスチャを破棄してメモリリーク防止
        if (_graphTexture != null)
        {
            Destroy(_graphTexture);
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// グラフの初期化
    /// </summary>
    void InitializeGraph()
    {
        if (graphImage == null)
        {
            Debug.LogError("[OSCZAxisOscilloscope] Graph Image is not assigned!");
            return;
        }

        if (targetManager == null)
        {
            Debug.LogError("[OSCZAxisOscilloscope] Target Manager is not assigned!");
            return;
        }

        // テクスチャ作成
        _graphTexture = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);
        _graphTexture.filterMode = FilterMode.Point; // ピクセルパーフェクト
        _graphTexture.wrapMode = TextureWrapMode.Clamp;

        // UI Imageに適用
        graphImage.sprite = Sprite.Create(
            _graphTexture,
            new Rect(0, 0, graphWidth, graphHeight),
            new Vector2(0.5f, 0.5f)
        );

        // 初期描画（背景のみ）
        ClearGraph();
        _graphTexture.Apply();

        _isInitialized = true;
        LogDebug("Graph initialized");
    }

    /// <summary>
    /// Toggleのセットアップ
    /// </summary>
    void SetupToggle()
    {
        if (visibilityToggle != null)
        {
            // 現在の状態を反映
            visibilityToggle.isOn = _isVisible;

            // イベント登録
            visibilityToggle.onValueChanged.AddListener(OnVisibilityToggled);

            LogDebug("Toggle setup complete");
        }
    }

    #endregion

    #region Data Collection

    /// <summary>
    /// データ収集処理
    /// </summary>
    void CollectData()
    {
        _samplingTimer += Time.deltaTime;

        if (_samplingTimer >= samplingInterval)
        {
            _samplingTimer = 0f;

            // 現在のZ値を取得
            if (targetManager != null)
            {
                Vector3 position = targetManager.GetCurrentPosition();
                float currentZ = position.z;

                // データポイントに追加
                _dataPoints.Add(currentZ);

                // 最大数を超えたら古いデータを削除
                if (_dataPoints.Count > maxDataPoints)
                {
                    _dataPoints.RemoveAt(0);
                }

                LogDebug($"Data collected: Z={currentZ:F3}, Total points: {_dataPoints.Count}");
            }
        }
    }

    #endregion

    #region Graph Drawing

    /// <summary>
    /// グラフを描画
    /// </summary>
    void DrawGraph()
    {
        if (_graphTexture == null)
            return;

        // 背景クリア
        ClearGraph();

        // グリッド描画
        if (showGrid)
        {
            DrawGrid();
        }

        // 波形描画
        DrawWaveform();

        // テクスチャ更新
        _graphTexture.Apply();
    }

    /// <summary>
    /// グラフをクリア（背景色で塗りつぶし）
    /// </summary>
    void ClearGraph()
    {
        Color[] pixels = new Color[graphWidth * graphHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        _graphTexture.SetPixels(pixels);
    }

    /// <summary>
    /// グリッド線を描画
    /// </summary>
    void DrawGrid()
    {
        // 横グリッド線
        for (int i = 0; i <= horizontalGridLines; i++)
        {
            int y = Mathf.RoundToInt((float)i / horizontalGridLines * (graphHeight - 1));
            DrawHorizontalLine(y, gridColor);
        }

        // 縦グリッド線
        for (int i = 0; i <= verticalGridLines; i++)
        {
            int x = Mathf.RoundToInt((float)i / verticalGridLines * (graphWidth - 1));
            DrawVerticalLine(x, gridColor);
        }
    }

    /// <summary>
    /// 波形を描画
    /// </summary>
    void DrawWaveform()
    {
        if (_dataPoints.Count < 2)
            return;

        // データポイント間を線で結ぶ
        for (int i = 0; i < _dataPoints.Count - 1; i++)
        {
            // X座標計算（左から右へ）
            float t1 = (float)i / (maxDataPoints - 1);
            float t2 = (float)(i + 1) / (maxDataPoints - 1);
            int x1 = Mathf.RoundToInt(t1 * (graphWidth - 1));
            int x2 = Mathf.RoundToInt(t2 * (graphWidth - 1));

            // Y座標計算（値を正規化）
            float normalizedY1 = Mathf.InverseLerp(yMin, yMax, _dataPoints[i]);
            float normalizedY2 = Mathf.InverseLerp(yMin, yMax, _dataPoints[i + 1]);
            int y1 = Mathf.RoundToInt(normalizedY1 * (graphHeight - 1));
            int y2 = Mathf.RoundToInt(normalizedY2 * (graphHeight - 1));

            // 線を描画
            DrawLine(x1, y1, x2, y2, waveformColor);
        }
    }

    /// <summary>
    /// 2点間に線を描画（Bresenhamアルゴリズム）
    /// </summary>
    void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // 太さを適用して描画
            DrawThickPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// 太さを持つピクセルを描画
    /// </summary>
    void DrawThickPixel(int x, int y, Color color)
    {
        int halfThickness = lineThickness / 2;
        for (int dy = -halfThickness; dy <= halfThickness; dy++)
        {
            for (int dx = -halfThickness; dx <= halfThickness; dx++)
            {
                SetPixelSafe(x + dx, y + dy, color);
            }
        }
    }

    /// <summary>
    /// 横線を描画
    /// </summary>
    void DrawHorizontalLine(int y, Color color)
    {
        for (int x = 0; x < graphWidth; x++)
        {
            SetPixelSafe(x, y, color);
        }
    }

    /// <summary>
    /// 縦線を描画
    /// </summary>
    void DrawVerticalLine(int x, Color color)
    {
        for (int y = 0; y < graphHeight; y++)
        {
            SetPixelSafe(x, y, color);
        }
    }

    /// <summary>
    /// 範囲チェック付きでピクセルをセット
    /// </summary>
    void SetPixelSafe(int x, int y, Color color)
    {
        if (x >= 0 && x < graphWidth && y >= 0 && y < graphHeight)
        {
            _graphTexture.SetPixel(x, y, color);
        }
    }

    #endregion

    #region Visibility Control

    /// <summary>
    /// 表示/非表示切り替え時のコールバック
    /// </summary>
    void OnVisibilityToggled(bool isOn)
    {
        _isVisible = isOn;

        if (!_isVisible)
        {
            // 非表示時はデータをクリア
            _dataPoints.Clear();
            _samplingTimer = 0f;

            // グラフもクリア
            if (_graphTexture != null)
            {
                ClearGraph();
                _graphTexture.Apply();
            }

            LogDebug("Oscilloscope hidden - data cleared");
        }
        else
        {
            LogDebug("Oscilloscope visible");
        }

        // UI Imageの表示/非表示
        if (graphImage != null)
        {
            graphImage.enabled = _isVisible;
        }
    }

    /// <summary>
    /// プログラムから表示/非表示を切り替え
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visibilityToggle != null)
        {
            visibilityToggle.isOn = visible;
        }
        else
        {
            OnVisibilityToggled(visible);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// データをクリア
    /// </summary>
    public void ClearData()
    {
        _dataPoints.Clear();
        _samplingTimer = 0f;

        if (_graphTexture != null)
        {
            ClearGraph();
            _graphTexture.Apply();
        }

        LogDebug("Data cleared manually");
    }

    /// <summary>
    /// 現在のデータポイント数を取得
    /// </summary>
    public int GetDataPointCount()
    {
        return _dataPoints.Count;
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[OSCZAxisOscilloscope] {message}");
        }
    }

    #endregion
}
