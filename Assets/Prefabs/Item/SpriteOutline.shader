Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 0.5, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : v2f_COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata_t IN) {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target {
                // ★ 추가: 외곽선 두께가 0이면 계산 없이 바로 원래 픽셀 반환
                if (_OutlineWidth <= 0) {
                    return tex2D(_MainTex, IN.texcoord) * IN.color;
                }
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 주변 픽셀 확인하여 알파 경계 감지
                float2 texel = _MainTex_TexelSize.xy;
                float alpha = col.a;
                alpha += tex2D(_MainTex, IN.texcoord + float2(texel.x, 0) * _OutlineWidth).a;
                alpha += tex2D(_MainTex, IN.texcoord + float2(-texel.x, 0) * _OutlineWidth).a;
                alpha += tex2D(_MainTex, IN.texcoord + float2(0, texel.y) * _OutlineWidth).a;
                alpha += tex2D(_MainTex, IN.texcoord + float2(0, -texel.y) * _OutlineWidth).a;

                // 원래 픽셀이 투명하고 주변에 알파값이 있다면 외곽선 색상 출력
                if (col.a < 0.1 && alpha > 0.1) {
                    return _OutlineColor;
                }
                
                return col;
            }
            ENDCG
        }
    }
}