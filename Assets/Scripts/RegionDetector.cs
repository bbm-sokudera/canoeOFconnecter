using System.Collections.Generic;
using Orbbec;
using OrbbecUnity;
using UnityEngine;
using System;
using System.IO;
using extOSC;


public class NormLowSequenceDetector : MonoBehaviour
{
    // --- 参照 ---
    public OrbbecFrameSource frameSource;
    [Tooltip("Orbbecデバイスへの参照（デバイス接続制御用）")]
    public OrbbecUnity.OrbbecDevice orbbecDevice;

    [Header("OSC Output")]
    public OSCManager oscManager;

    [Header("Input Source Selection")]
    [Tooltip("FrameSourceから深度データを使用する（チェックを外すとOSC入力を使用）")]
    public bool useFrameSource = true;

    [Header("OSC Input Settings")]
    [Tooltip("OSC入力を受信するポート番号")]
    public int oscReceivePort = 7003;

    [Tooltip("領域アクティブ化を受信するOSCアドレス")]
    public string regionActiveAddress = "/region/active";

    // --- Depth Settings (NormLow 判定ロジック) ---
    [Header("Depth Settings (NormLow Range)")]
    [Tooltip("深度のスケールファクター (例: 2700f)")]
    public float depthScale = 2700f; 
    [Tooltip("深度の最小閾値 (メートル)")]
    public float minDepthThreshold = 0.5f; 
    [Tooltip("深度の最大閾値 (メートル)")]
    public float maxDepthThreshold = 1.28f; 
    
    // NormLowの正規化範囲
    const float NormLowStart = 0.1f;
    const float NormLowEnd = 0.3f;

    readonly float DEPTH_RANGE = 0.73f;

    [Header("Detection Settings")]
    [Tooltip("NormLowと判定されるために必要な最小ピクセル数")]
    public int minNormLowPixelsToTrigger = 50; 

    [Tooltip("シーケンス検知の最大時間差 (秒)")]
    public float sequenceMaxTimeGap = 0.5f;
    
    // 監視対象の矩形範囲を定義する構造体
    [System.Serializable]
    public struct DepthRegion
    {
        public string name;
        [Tooltip("領域の左上のX座標 (ピクセル)")]
        public int x;
        [Tooltip("領域の左上のY座標 (ピクセル)")]
        public int y;
        [Tooltip("領域の幅 (ピクセル)")]
        public int width;
        [Tooltip("領域の高さ (ピクセル)")]
        public int height;
        [HideInInspector]
        public int index; // 領域のインデックス（0〜3）
    }

    // 監視する領域
    public DepthRegion[] regions = new DepthRegion[4]
    {
        new DepthRegion { name = "Region 0 (Top-Left)", x = 100, y = 100, width = 50, height = 50, index = 0 },
        new DepthRegion { name = "Region 1 (Top-Right)", x = 490, y = 100, width = 50, height = 50, index = 1 },
        new DepthRegion { name = "Region 2 (Bottom-Left)", x = 100, y = 330, width = 50, height = 50, index = 2 },
        new DepthRegion { name = "Region 3 (Bottom-Right)", x = 490, y = 330, width = 50, height = 50, index = 3 }
    };
    
    // --- シーケンス追跡用内部状態 ---
    private int _depthWidth = 0;
    private int _depthHeight = 0;

    private int _sequenceStartArea = -1;    // シーケンス開始領域のインデックス
    private float _sequenceStartTime = 0f;  // シーケンス開始時刻

    // シーケンス定義マップ: (開始領域, 終了領域, /paddle 番号)
    private readonly (int start, int end, int paddleNum)[] _sequences = {
        (2, 3, 1), // 領域2 -> 領域3 : /paddle 1 右前進
        (0, 1, 2), // 領域0 -> 領域1 : /paddle 2 左前進
        (3, 2, 3), // 領域3 -> 領域2 : /paddle 3 右後進
        (1, 0, 4)  // 領域1 -> 領域0 : /paddle 4 左後進
    };

    // OSC入力用
    private OSCReceiver _oscReceiver;
    private List<int> _oscActiveRegions = new List<int>();
    private float _lastOscReceiveTime = 0f;
    private const float OSC_TIMEOUT = 0.2f; // OSC入力のタイムアウト（秒）

    void Start()
    {
        // OSCManagerは自動的に初期化されるため、追加の初期化は不要
        if (oscManager == null)
        {
            Debug.LogWarning("OSCManager is not assigned in RegionDetector!");
        }

        // useFrameSourceの状態に応じてデバイス接続を制御
        if (useFrameSource)
        {
            // FrameSourceを使用する場合、デバイス接続を開始
            if (orbbecDevice != null)
            {
                Debug.Log("[RegionDetector] useFrameSource=true: Starting Orbbec device connection...");
                orbbecDevice.StartDeviceConnection();
            }
            else
            {
                Debug.LogWarning("[RegionDetector] orbbecDevice is not assigned. Cannot control device connection.");
            }
        }
        else
        {
            // OSC入力を使用する場合、デバイス接続を停止してOSCレシーバーを初期化
            if (orbbecDevice != null)
            {
                Debug.Log("[RegionDetector] useFrameSource=false: Stopping Orbbec device connection...");
                orbbecDevice.StopDeviceConnection();
            }
            InitializeOSCReceiver();
        }
    }

    void OnDestroy()
    {
        if (_oscReceiver != null)
        {
            _oscReceiver.Close();
        }

        // デバイス接続を停止（念のため）
        if (orbbecDevice != null)
        {
            orbbecDevice.StopDeviceConnection();
        }
    }

    /// <summary>
    /// OSC受信機能の初期化
    /// </summary>
    private void InitializeOSCReceiver()
    {
        _oscReceiver = gameObject.AddComponent<OSCReceiver>();
        _oscReceiver.LocalPort = oscReceivePort;
        _oscReceiver.Bind(regionActiveAddress, OnRegionActiveReceived);
        Debug.Log($"[RegionDetector] OSC Receiver initialized on port {oscReceivePort}, address: {regionActiveAddress}");
    }

    /// <summary>
    /// OSCで領域アクティブ化メッセージを受信
    /// 形式: /region/active [領域番号]
    /// 例: /region/active 0 → 領域0がアクティブ
    /// </summary>
    private void OnRegionActiveReceived(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            int regionIndex = message.Values[0].IntValue;

            // 有効な領域番号かチェック
            if (regionIndex >= 0 && regionIndex < regions.Length)
            {
                // 既にリストにない場合のみ追加
                if (!_oscActiveRegions.Contains(regionIndex))
                {
                    _oscActiveRegions.Add(regionIndex);
                    _lastOscReceiveTime = Time.time;
                    Debug.Log($"[RegionDetector] OSC input: Region {regionIndex} activated");
                }
            }
            else
            {
                Debug.LogWarning($"[RegionDetector] Invalid region index received via OSC: {regionIndex}");
            }
        }
    }

    // --- OSC通知メソッド (OSCManagerを経由) ----
    public void SendPaddleOSC(int paddleNumber)    
    {
        if (oscManager != null)
        {
            oscManager.SendPaddleOSC(paddleNumber);
        }
    }

    void Update()
    {
        List<int> currentActiveRegions = new List<int>();

        // FrameSourceを使用する場合
        if (useFrameSource)
        {
            if (frameSource == null)
            {
                Debug.LogWarning("[RegionDetector] FrameSource is not assigned!");
                return;
            }

            var obDepthFrame = frameSource.GetDepthFrame();

            if (obDepthFrame == null || obDepthFrame.width == 0 || obDepthFrame.height == 0 || obDepthFrame.data == null)
            {
                return;
            }
            if (obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
            {
                return;
            }

            // フレームサイズの更新
            if (obDepthFrame.width != _depthWidth || obDepthFrame.height != _depthHeight)
            {
                _depthWidth = obDepthFrame.width;
                _depthHeight = obDepthFrame.height;
            }

            // どの領域がNormLowでアクティブかを判定
            for (int i = 0; i < regions.Length; i++)
            {
                int normLowCount = CountNormLowPixelsInRegion(ref regions[i], obDepthFrame.data, _depthWidth, _depthHeight);

                if (normLowCount >= minNormLowPixelsToTrigger)
                {
                    currentActiveRegions.Add(i);
                }
            }
        }
        // OSC入力を使用する場合
        else
        {
            // OSCレシーバーが初期化されていない場合は初期化
            if (_oscReceiver == null)
            {
                InitializeOSCReceiver();
            }

            // OSC入力のタイムアウトチェック
            if (Time.time - _lastOscReceiveTime > OSC_TIMEOUT)
            {
                // タイムアウト: アクティブリストをクリア
                _oscActiveRegions.Clear();
            }

            // OSCから受信したアクティブ領域を使用
            currentActiveRegions = new List<int>(_oscActiveRegions);

            // 処理後、OSCアクティブリストをクリア（次のフレームまで保持しない）
            _oscActiveRegions.Clear();
        }

        // シーケンス検知ロジックを実行
        CheckNormLowSequence(currentActiveRegions.ToArray());
    }

    /// ====================================================================================================================
    /// 特定の領域内のNormLow条件を満たすピクセル数をカウントする
    /// ====================================================================================================================
    private int CountNormLowPixelsInRegion(ref DepthRegion region, byte[] depthData, int frameWidth, int frameHeight)
    {
        int normLowCount = 0;
        
        // 領域の範囲をフレーム内にクランプ
        int startX = Mathf.Max(0, region.x);
        int startY = Mathf.Max(0, region.y);
        int endX = Mathf.Min(frameWidth, region.x + region.width);
        int endY = Mathf.Min(frameHeight, region.y + region.height);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int dataIndex = (y * frameWidth + x) * 2;
                
                // 1. 16ビット深度値の取得
                // データのインデックスチェック（範囲外アクセス防止）
                if (dataIndex + 1 >= depthData.Length) continue;

                ushort depthValue = (ushort)(depthData[dataIndex + 1] << 8 | depthData[dataIndex]);
                
                // 2. 実際の深度計算 (メートル)
                float depth = (float)depthValue / depthScale;

                // 3. 深度が有効範囲内かどうか (depthValue == 0 は通常無効)
                if (depthValue == 0 || depth < minDepthThreshold || depth > maxDepthThreshold)
                {
                    continue; 
                }
                
                // 4. 深度を正規化 (0.0 から 1.0 の範囲)
                float normalizedDepth = (depth - minDepthThreshold) / DEPTH_RANGE;

                // 5. NormLow範囲のチェック
                if (normalizedDepth >= NormLowStart && normalizedDepth <= NormLowEnd)
                {
                    normLowCount++;
                }
            }
        }

        return normLowCount;
    }

    /// ====================================================================================================================
    /// NormLow検出に基づくシーケンス検知ロジック
    /// ====================================================================================================================
    private void CheckNormLowSequence(int[] activeRegions)
    {
        // 1. 時間切れのチェック
        if (_sequenceStartArea != -1 && Time.time > _sequenceStartTime + sequenceMaxTimeGap)
        {
            Debug.Log($"Sequence timed out. Resetting tracking.");
            _sequenceStartArea = -1;
            _sequenceStartTime = 0f;
        }

        // 2. 現在アクティブな領域を処理
        foreach (int currentArea in activeRegions)
        {
            // A. シーケンス追跡中でない場合 (追跡の起点を探す)
            if (_sequenceStartArea == -1)
            {
                // 現在アクティブな領域が、いずれかのシーケンスの開始点と一致するかチェック
                foreach (var seq in _sequences)
                {
                    if (seq.start == currentArea)
                    {
                        _sequenceStartArea = currentArea;
                        _sequenceStartTime = Time.time;
                        Debug.Log($"Sequence tracking started at Region {currentArea}.");
                        return;
                    }
                }
            }
            // B. シーケンス追跡中の場合 (終了エリアを待っている)
            else if (currentArea != _sequenceStartArea)
            {
                // 現在アクティブな領域が、追跡中のシーケンスの終了点と一致するかチェック
                foreach (var seq in _sequences)
                {
                    if (seq.start == _sequenceStartArea && seq.end == currentArea)
                    {
                        // シーケンス成功！
                        SendPaddleOSC(seq.paddleNum);
                        Debug.Log($"SUCCESS: Sequence {seq.start} -> {seq.end} detected. Sending /paddle {seq.paddleNum}.");

                        // 最終通知後、追跡を完全にリセット
                        _sequenceStartArea = -1;
                        _sequenceStartTime = 0f;
                        return;
                    }
                }
            }
        }
    }


    // ==========================================================
    // Gizmos表示ロジック
    // ==========================================================
    private void OnDrawGizmos()
    {
        float W = (_depthWidth > 0) ? _depthWidth : 640;
        float H = (_depthHeight > 0) ? _depthHeight : 480;

        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-W / 2f, -H / 2f, 0));
        
        // 外枠
        Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(W / 2, H / 2, 0), new Vector3(W, H, 0));

        // 4つの矩形領域を描画
        for (int i = 0; i < regions.Length; i++)
        {
            DepthRegion region = regions[i];
            
            Vector3 center = new Vector3(region.x + region.width / 2f, H - (region.y + region.height / 2f), 0);
            Vector3 size = new Vector3(region.width, region.height, 0);

            // シーケンス追跡中の領域は色を変える
            if (Application.isPlaying && _sequenceStartArea == i)
            {
                // 追跡開始エリア: 赤色で点滅
                float pulse = Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f;
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, pulse);
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                // 通常の描画: 青色のワイヤーフレーム
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireCube(center, size);
            }
            
            // 矩形番号の表示 (Sceneビュー上)
            // 実行時のみ番号を表示（Handlesを使うのが正式だが、ここでは簡易的に）
            #if UNITY_EDITOR
            if (!Application.isPlaying || _sequenceStartArea == i)
            {
                UnityEditor.Handles.Label(Gizmos.matrix.MultiplyPoint(center), $"R{i}", new GUIStyle { normal = { textColor = Color.yellow } });
            }
            #endif
        }

        // 行列を元に戻す
        Gizmos.matrix = oldGizmosMatrix;
    }
}