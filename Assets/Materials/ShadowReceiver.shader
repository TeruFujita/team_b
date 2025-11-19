Shader "Custom/ShadowOnly"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        ColorMask 0
        Lighting Off

        // このPassでは何も描画しない（透明）
        Pass {}

        // 影だけを受け取るためのパス
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
        }
    }
    Fallback Off
}
