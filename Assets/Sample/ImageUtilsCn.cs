using System;

public class ImageUtilsCn
{
    public static void ConvertDepthToColorData(byte[] depthData, float depthScale, ref byte[] colorData)
    {
        // ⚠ ここに設定したい最大・最小の深度（メートルなどの単位）
        const float MIN_DEPTH_THRESHOLD = 0.5f;
        const float MAX_DEPTH_THRESHOLD = 1.28f;

        const float DEPTH_RANGE = MAX_DEPTH_THRESHOLD - MIN_DEPTH_THRESHOLD;

        const float NormHighStart = 0.7f;
        const float NormHighEnd = 0.96f;
        const float NormLowStart = 0.1f;
        const float NormLowEnd = 0.4f;

        for (int i = 0; i < depthData.Length; i += 2)
        {
            // 1. 16ビット深度値の取得
            ushort depthValue = (ushort)(depthData[i + 1] << 8 | depthData[i]);
            // 2. 実際の深度計算
            float depth = (float)depthValue / depthScale;
            int index = (i / 2) * 3;

            // 3. 深度が有効範囲外かどうか(depthValue == 0 は通常、深度が取得不可

            if (depthValue == 0 || depth < MIN_DEPTH_THRESHOLD || depth > MAX_DEPTH_THRESHOLD)
            {
                colorData[index] = 50;      // Red
                colorData[index + 1] = 0;    // Green
                colorData[index + 2] = 0;    // Blue
            }
            else
            {
                // 4. 深度を正規化
                float normalizedDepth = (depth - MIN_DEPTH_THRESHOLD) / DEPTH_RANGE;

                if (normalizedDepth >= NormHighStart && normalizedDepth <= NormHighEnd)
                {
                    colorData[index] = 0;      // Red
                    colorData[index + 1] = 0;    // Green
                    colorData[index + 2] = 255;    // Blue
                }
                if (normalizedDepth >= NormLowStart && normalizedDepth <= NormLowEnd)
                {
                    colorData[index] = 255;      // Red
                    colorData[index + 1] = 0;    // Green
                    colorData[index + 2] = 0;    // Blue
                }

    

                // 5. 正規化された深度を0から255のバイト値にマッピング
                //byte depthByte = (byte)(Math.Max(0, Math.Min(255, normalizedDepth * 255)));

                // 6. グレースケール設定
                //colorData[index] = depthByte;      // Red
                //colorData[index + 1] = depthByte;  // Green
                //colorData[index + 2] = depthByte;  // Blue
            }
        }
    }

    public static void Convert8BitIrToByteArray(byte[] irData, ref byte[] colorData)
    {
        int colorDataLength = irData.Length * 3;
        if (colorData == null || colorData.Length != colorDataLength)
        {
            colorData = new byte[colorDataLength];
        }

        for (int i = 0; i < irData.Length; i++)
        {
            byte irValue = irData[i];
            int index = i * 3;
            colorData[index] = irValue; // Red
            colorData[index + 1] = irValue; // Green
            colorData[index + 2] = irValue; // Blue
        }
    }

    public static void Convert16BitIrToColorData(byte[] irData, ref byte[] colorData)
    {
        int colorDataLength = (irData.Length / 2) * 3;
        if (colorData == null || colorData.Length != colorDataLength)
        {
            colorData = new byte[colorDataLength];
        }

        for (int i = 0; i < irData.Length; i += 2)
        {
            ushort irValue = (ushort)(irData[i + 1] << 8 | irData[i]);
            byte irByte = (byte)(irValue >> 8); // Scale down to 8 bits

            int index = (i / 2) * 3;
            colorData[index] = irByte; // Red
            colorData[index + 1] = irByte; // Green
            colorData[index + 2] = irByte; // Blue
        }
    }
}