Shader "Screen/InkDissolve"
{
    Properties
    {
        _MainTexA ("Texture A", 2D) = "white" {}
        _MainTexB("Texture B", 2D) = "black" { }
_MaskTex("Mask", 2D) = "white" { }
_Progress("Progress", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
        ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTexA;
sampler2D _MainTexB;
sampler2D _MaskTex;
float4 _MainTexA_ST;
float4 _MainTexB_ST;
float4 _MaskTex_ST;
float _Progress;

struct appdata
{
    float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

struct v2f
{
    float4 pos : SV_POSITION;
                float2 uvA : TEXCOORD0;
                float2 uvB : TEXCOORD1;
                float2 uvM : TEXCOORD2;
            };

v2f vert(appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uvA = TRANSFORM_TEX(v.uv, _MainTexA);
    o.uvB = TRANSFORM_TEX(v.uv, _MainTexB);
    o.uvM = TRANSFORM_TEX(v.uv, _MaskTex);
    return o;
}

fixed4 frag(v2f i) : SV_Target
            {
                fixed4 colA = tex2D(_MainTexA, i.uvA);
fixed4 colB = tex2D(_MainTexB, i.uvB);
fixed mask = tex2D(_MaskTex, i.uvM).r;   // 마스크 밝기
fixed t    = step(mask, _Progress);       // mask < progress ? 1 : 0
return lerp(colA, colB, t);
            }
            ENDCG
        }
    }
}