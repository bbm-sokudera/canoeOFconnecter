Shader "Custom/PointCloudRenderShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _PointSize ("Point Size", Range(0.001, 0.05)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // 半透明にする場合
            Cull Off // 裏面も描画
            ZWrite On // Zバッファ書き込み

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR; // 頂点カラーもあれば
            };

            fixed4 _Color;
            float _PointSize;

            v2g vert (appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex); // オブジェクト空間からクリップ空間へ
                return o;
            }

            // Geometry Shader
            // 各頂点を小さなクアッド（四角形）に展開
            [maxvertexcount(4)]
            void geom(point v2g p[1], inout TriangleStream<g2f> triStream)
            {
                float4 pPos = p[0].pos; // 頂点のクリップ空間座標

                // スクリーン空間でのポイントサイズを計算
                // Unity_StereoEyeIndex はVR向けだが、ここでは通常の表示にも有効
                float pointScaleScreen = _PointSize * 0.01; // 必要に応じて調整

                float halfSizeX = pointScaleScreen * pPos.w * _ScreenParams.x / _ScreenParams.y; // アスペクト比考慮
                float halfSizeY = pointScaleScreen * pPos.w; 
                
                // 4つの頂点を定義してクアッドを作成
                float4 v[4];
                v[0] = float4( pPos.x - halfSizeX, pPos.y + halfSizeY, pPos.z, pPos.w); // Top-Left
                v[1] = float4( pPos.x + halfSizeX, pPos.y + halfSizeY, pPos.z, pPos.w); // Top-Right
                v[2] = float4( pPos.x - halfSizeX, pPos.y - halfSizeY, pPos.z, pPos.w); // Bottom-Left
                v[3] = float4( pPos.x + halfSizeX, pPos.y - halfSizeY, pPos.z, pPos.w); // Bottom-Right

                // 各頂点をtriStreamに送る
                g2f o;
                o.color = _Color;

                o.pos = v[0]; triStream.Append(o);
                o.pos = v[1]; triStream.Append(o);
                o.pos = v[2]; triStream.Append(o);
                o.pos = v[3]; triStream.Append(o);

                triStream.RestartStrip(); // 次のポイントのためにストリップをリセット
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}