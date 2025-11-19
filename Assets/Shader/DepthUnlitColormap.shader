Shader "Custom/DepthUnlitColormap"
{
    Properties
    {
        _MainTex ("Depth Texture (R float)", 2D) = "white" {}
        _ColorMap ("Colormap (1D tex)", 2D) = "white" {}
        _UseColormap ("Use Colormap (0/1)", Float) = 1
        _MinDepth ("Min Depth (m)", Float) = 0.0
        _MaxDepth ("Max Depth (m)", Float) = 5.0
        _ClipMinDepth ("Clip Min (m)", Float) = 0.0
        _ClipMaxDepth ("Clip Max (m)", Float) = 5.0
        _LowColor ("Low Color", Color) = (0,0,0.8,1)
        _MidColor ("Mid Color", Color) = (0,0.8,0,1)
        _HighColor ("High Color", Color) = (0.8,0,0,1)
        _OutOfRangeColor ("Out of Range Color", Color) = (0,0,0,0) // alpha 0 = transparent
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _ColorMap;
            float _UseColormap;
            float _MinDepth;
            float _MaxDepth;
            float _ClipMinDepth;
            float _ClipMaxDepth;
            fixed4 _LowColor;
            fixed4 _MidColor;
            fixed4 _HighColor;
            fixed4 _OutOfRangeColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // simple three-color gradient fallback: low -> mid -> high
            fixed4 GradientFallback(float t)
            {
                if (t <= 0.5)
                {
                    float tt = t / 0.5;
                    return lerp(_LowColor, _MidColor, tt);
                }
                else
                {
                    float tt = (t - 0.5) / 0.5;
                    return lerp(_MidColor, _HighColor, tt);
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample single-channel depth (should be in meters)
                float d = tex2D(_MainTex, i.uv).r;

                // Handle sensor invalid value (e.g., 0) as out-of-range
                if (d <= 0.0)
                {
                    return _OutOfRangeColor;
                }

                // Clip range: if outside clip range -> out-of-range color
                if (d < _ClipMinDepth || d > _ClipMaxDepth)
                {
                    return _OutOfRangeColor;
                }

                // Normalize between min/max
                float denom = max(0.000001, (_MaxDepth - _MinDepth));
                float t = saturate((d - _MinDepth) / denom);

                fixed4 col;
                if (_UseColormap > 0.5)
                {
                    // Sample colormap texture: expect a 1D-like texture (e.g., 256x1 or 256x2)
                    // Use clamp on u, sample at v=0.5
                    col = tex2D(_ColorMap, float2(t, 0.5));
                    // If color map texture includes alpha channel, keep it; otherwise set alpha=1
                    col.a = col.a > 0.0 ? col.a : 1.0;
                }
                else
                {
                    col = GradientFallback(t);
                    col.a = 1.0;
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
