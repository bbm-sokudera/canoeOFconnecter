using System.Collections.Generic;
using Orbbec;
using OrbbecUnity;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

// 深度データから3D点群を生成し、描画するためのコンポーネント
public class PointCloudRenderer : MonoBehaviour
{
    // --- 参照と設定 ---
    [Header("Orbbec Data Source")]
    public OrbbecFrameSource frameSource;
    
    [Header("Rendering Settings")]
    [Tooltip("点群の描画に使用するマテリアル（ポイント描画可能なカスタムシェーダーが必要）")]
    public Material pointCloudMaterial;
    
    [Tooltip("描画する点のサイズ")]
    public float pointSize = 0.003f; // 3mm 

    [Header("Camera Intrinsic Parameters (Approximate Values)")]
    // キャリブレーションパラメーター（使用するカメラと解像度に合わせて手動で設定してください）
    // 例: Astra Pro (640x480)の場合の一般的な値
    public float fx = 525.0f; 
    public float fy = 525.0f;
    public float cx = 319.5f; 
    public float cy = 239.5f;

    // --- 内部状態 ---
    private int _depthWidth = 0;
    private int _depthHeight = 0;
    private ComputeBuffer _pointBuffer; // 3D点を格納するバッファ
    private Vector3[] _points;          // CPUで計算する場合の一時配列

    // データの構造 (X, Y, Z座標)
    private const int SIZE_OF_VECTOR3 = 3 * sizeof(float); 

    void Start()
    {
        if (frameSource == null)
        {
            Debug.LogError("OrbbecFrameSourceが設定されていません！");
            enabled = false;
        }
    }

    void OnDestroy()
    {
        // ComputeBufferを解放
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
        }
    }

    // メインの処理ループ
    void Update()
    {
        var obDepthFrame = frameSource.GetDepthFrame();

        if (obDepthFrame == null || obDepthFrame.data == null || obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
        {
            return;
        }

        // フレームサイズのチェックとバッファの再初期化
        if (obDepthFrame.width != _depthWidth || obDepthFrame.height != _depthHeight)
        {
            _depthWidth = obDepthFrame.width;
            _depthHeight = obDepthFrame.height;
            InitializeBuffers(_depthWidth, _depthHeight);
        }

        // 深度データを3D座標に変換
        // CPU処理のため大規模点群ではパフォーマンスに注意
        ConvertDepthToPointCloud(obDepthFrame.data, _depthWidth, _depthHeight);
        
        // ComputeBufferにデータをアップロード
        if (_pointBuffer != null)
        {
            _pointBuffer.SetData(_points);
        }
    }

    // 深度データから3D座標を計算するデプロジェクション処理（CPU処理）
    private void ConvertDepthToPointCloud(byte[] depthData, int width, int height)
    {
        if (_points == null) return;
        
        // 元のスクリプトから深度スケールを仮定 (Orbbecの深度単位は通常mm)
        const float DEPTH_SCALE_FACTOR = 1000.0f; // 深度値を[mm]から[m]に変換すると仮定

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pointIndex = y * width + x;
                int dataIndex = pointIndex * 2;
                
                if (dataIndex + 1 >= depthData.Length) continue;

                // 1. 16ビット深度値 (ushort) を取得
                ushort rawDepth = (ushort)(depthData[dataIndex + 1] << 8 | depthData[dataIndex]);
                
                // 2. 深度をメートル単位に変換
                float z = (float)rawDepth / DEPTH_SCALE_FACTOR; 
                
                // 3. デプロジェクション（カメラ座標から3D座標へ）
                // Pinhole Camera Model: 
                // X = (u - cx) * Z / fx
                // Y = (v - cy) * Z / fy
                
                // 深度が0（無効データ）または遠すぎる場合はスキップ
                if (rawDepth == 0 || z > 5.0f || z < 0.1f) 
                {
                    _points[pointIndex] = Vector3.zero; // ゼロを設定して描画されないようにする
                    continue;
                }
                
                // 2Dピクセル座標 (u, v)
                float u = (float)x;
                float v = (float)y;

                // 3D座標を計算（XとYはZ方向に正の方向へ向かって、Unityの左手座標系を意識して変換）
                float X = (u - cx) * z / fx;
                float Y = (v - cy) * z / fy;

                // Orbbec/Kinect V2は通常、Z軸が前、X軸が右、Y軸が下
                // Unityでは通常、Z軸が前、X軸が右、Y軸が上
                // 変換: Z -> Z, X -> X, Y -> -Y (Y軸を反転)
                // Y座標はフレームの上部から計算されるため、Y軸を反転しなくてもUnity座標系に近づくことが多いが、ここでは単純な変換を行う
                
                // 多くのSDKではY軸が下向きなので、Yを反転させてUnityの上向きに合わせる
                _points[pointIndex] = new Vector3(-X, -Y, z); // Xを反転させると見やすくなることが多い
            }
        }
    }

    // ComputeBufferと配列の初期化/再構築
    private void InitializeBuffers(int width, int height)
    {
        int totalPoints = width * height;

        // 既存のバッファを解放
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
        }

        // 新しいバッファと配列を作成
        _pointBuffer = new ComputeBuffer(totalPoints, SIZE_OF_VECTOR3);
        _points = new Vector3[totalPoints];
        Debug.Log($"Point Cloud Buffers initialized: {width}x{height} = {totalPoints} points.");
    }

    // レンダリングパイプラインにフックして描画
    // このコンポーネントがアタッチされたオブジェクトのトランスフォームを基準に描画されます。
    void OnRenderObject()
    {
        if (_pointBuffer == null || pointCloudMaterial == null || _points == null || _depthWidth == 0) return;

        // マテリアルの設定
        pointCloudMaterial.SetPass(0);
        pointCloudMaterial.SetBuffer("PointCloudBuffer", _pointBuffer);
        pointCloudMaterial.SetFloat("_PointSize", pointSize);

        // トランスフォームをシェーダーに渡す（シェーダー側で描画座標をワールド変換するため）
        pointCloudMaterial.SetMatrix("LocalToWorldMatrix", transform.localToWorldMatrix);

        // プリミティブ（点）として描画
        Graphics.DrawProceduralNow(
            MeshTopology.Points, 
            _pointBuffer.count, 
            1 // インスタンス数
        );
    }
}