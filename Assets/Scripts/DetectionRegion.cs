// ファイル名: DetectionRegion.cs

using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public struct DetectionRegion
{
    // 3D空間における境界ボックスの定義 (単位: メートル)
    [Tooltip("X軸 (左右) の最小値 (メートル)")]
    public float minX;
    [Tooltip("X軸 (左右) の最大値 (メートル)")]
    public float maxX;
    [Tooltip("Y軸 (高さ) の最小値 (メートル)")]
    public float minY;
    [Tooltip("Y軸 (高さ) の最大値 (メートル)")]
    public float maxY;
    [Tooltip("Z軸 (奥行き) の最小値 (メートル)")]
    public float minZ;
    [Tooltip("Z軸 (奥行き) の最大値 (メートル)")]
    public float maxZ;

    // 検知ロジックで使用するパラメータ
    [Tooltip("検知された場合に SendPaddleOSC に送る番号")]
    public int paddleNumber;

    [Tooltip("物体検知と判定する最小ポイント数")]
    public int detectionThreshold;
}
