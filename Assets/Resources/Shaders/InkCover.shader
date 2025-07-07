Shader "Post/InkCover"
{
    Properties
    {
        _MainTex  ("Source", 2D) = "white" {}     // 카메라가 넘겨주는 화면
        _MaskTex("Ink Mask", 2D) = "white" { }   // 번짐 형태 마스크
_Progress("Progress", Range(0, 1)) = 0    // 0=안 덮임, 1=완전 암전
        _InkColor("Ink Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
sampler2D _MaskTex;
fixed4 _InkColor;
float _Progress;

fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
fixed m   = tex2D(_MaskTex, i.uv).r;      // 밝을수록 빨리 덮음
fixed t   = step(m, _Progress);           // 0 또는 1
return lerp(src, _InkColor, t);            // 잉크색으로 덮기
            }
            ENDCG
        }
    }
}