using UnityEngine;

public class DepthTextureGenerator : MonoBehaviour
{
    public int depthWidth = 640;
    public int depthHeight = 480;
    public Texture2D depthTexture;

    [Header("Shader Settings")]
    public Material colorDepthMaterial; // シェーダーマテリアル
    [Tooltip("色付けの最小深度（ミリメートル）")]
    [Range(0, 10000)] public float minDepthMap = 0; // mm
    [Tooltip("色付けの最大深度（ミリメートル）")]
    [Range(0, 10000)] public float maxDepthMap = 5000; // mm
    [Tooltip("トリミングの最小深度（ミリメートル）")]
    [Range(0, 10000)] public float clipMinDepth = 0; // mm
    [Tooltip("トリミングの最大深度（ミリメートル）")]
    [Range(0, 10000)] public float clipMaxDepth = 5000; // mm

    private ushort[] _depthData;
    private float[] _floatDepthBuffer;

    void Start()
    {
        // ダミーデータ（デバッグ）
        _depthData = new ushort[depthWidth * depthHeight];
        for (int i = 0; i < _depthData.Length; i++)
        {
            _depthData[i] = (ushort)Random.Range(0, 5000);
        }

        EnsureTextureInitialized();
        UpdateDepthTexture(); // 最初に一度適用
    }

    void EnsureTextureInitialized()
    {
        // 解像度変更時や未作成時にテクスチャを再生成
        if (depthTexture == null || depthTexture.width != depthWidth || depthTexture.height != depthHeight)
        {
#if UNITY_2019_3_OR_NEWER
            // 新しいUnityではGraphicsFormatが明示的で安全
            var gfxFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
            depthTexture = new Texture2D(depthWidth, depthHeight, gfxFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
#else
            depthTexture = new Texture2D(depthWidth, depthHeight, TextureFormat.RFloat, false);
#endif
            depthTexture.filterMode = FilterMode.Point;
            depthTexture.wrapMode = TextureWrapMode.Clamp;
        }
        // Floatバッファを一度だけ確保（GCを防ぐ）
        if (_floatDepthBuffer == null || _floatDepthBuffer.Length != depthWidth * depthHeight)
        {
            _floatDepthBuffer = new float[depthWidth * depthHeight];
        }

        // マテリアルにテクスチャをセット（シェーダーのプロパティ名に合わせて変更可）
        if (colorDepthMaterial != null)
        {
            colorDepthMaterial.SetTexture("_MainTex", depthTexture);
            UpdateMaterialDepthParams();
        }
    }

    // Inspector上の mm 値をメートルに変換してマテリアルへ送る
    void UpdateMaterialDepthParams()
    {
        if (colorDepthMaterial == null) return;
        colorDepthMaterial.SetFloat("_MinDepth", minDepthMap * 0.001f);
        colorDepthMaterial.SetFloat("_MaxDepth", maxDepthMap * 0.001f);
        colorDepthMaterial.SetFloat("_ClipMinDepth", clipMinDepth * 0.001f);
        colorDepthMaterial.SetFloat("_ClipMaxDepth", clipMaxDepth * 0.001f);
    }

    public void UpdateDepthData(ushort[] newDepthData)
    {
        if (newDepthData == null || newDepthData.Length != depthWidth * depthHeight)
        {
            Debug.LogError("Depth data size mismatch!");
            return;
        }

        _depthData = newDepthData;
        UpdateDepthTexture();
    }

    void UpdateDepthTexture()
    {
        if (_depthData == null || _depthData.Length == 0) return;

        EnsureTextureInitialized();

        // mm -> m に変換してバッファへ（毎フレーム新しい配列を作らない）
        int len = _depthData.Length;
        for (int i = 0; i < len; i++)
        {
            // 深度が0なら0m（センサの無効値扱い）にするなどの処理をここで行える
            _floatDepthBuffer[i] = _depthData[i] * 0.001f;
        }

        // R32_SFloat (単一チャンネルfloat) テクスチャへ一括で転送
        depthTexture.SetPixelData(_floatDepthBuffer, 0);
        depthTexture.Apply(false, false); // ミップマップ不要、リード可能なまま

        UpdateMaterialDepthParams();
    }

    void Update()
    {
        // デバッグ：毎5フレームで値を少し変える
        if (Application.isPlaying && Time.frameCount % 5 == 0)
        {
            if (_depthData == null) return;
            for (int i = 0; i < _depthData.Length; i++)
            {
                int v = _depthData[i] + Random.Range(-100, 101);
                _depthData[i] = (ushort)Mathf.Clamp(v, 0, 5000);
            }
            UpdateDepthTexture();
        }
    }

    void OnValidate()
    {
        // エディタで値が変わったらマテリアルへ反映
        UpdateMaterialDepthParams();

        // 解像度変更時にエディタ実行中ならテクスチャ再作成
        if (Application.isPlaying)
        {
            if (depthTexture == null || depthTexture.width != depthWidth || depthTexture.height != depthHeight)
            {
                EnsureTextureInitialized();
                UpdateDepthTexture();
            }
        }
    }
}