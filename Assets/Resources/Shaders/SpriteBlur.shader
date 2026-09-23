Shader "Custom/SpriteBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 _Color;
            float _BlurSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _BlurSize;

                fixed4 col = 0;

                col += tex2D(_MainTex, i.uv) * 0.20;

                col += tex2D(_MainTex, i.uv + float2(offset.x, 0)) * 0.12;
                col += tex2D(_MainTex, i.uv - float2(offset.x, 0)) * 0.12;
                col += tex2D(_MainTex, i.uv + float2(0, offset.y)) * 0.12;
                col += tex2D(_MainTex, i.uv - float2(0, offset.y)) * 0.12;

                col += tex2D(_MainTex, i.uv + offset) * 0.08;
                col += tex2D(_MainTex, i.uv - offset) * 0.08;
                col += tex2D(_MainTex, i.uv + float2(offset.x, -offset.y)) * 0.08;
                col += tex2D(_MainTex, i.uv + float2(-offset.x, offset.y)) * 0.08;

                return col * i.color;
            }

            ENDCG
        }
    }
}