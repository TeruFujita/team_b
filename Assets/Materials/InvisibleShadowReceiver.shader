Shader "Custom/InvisibleShadow"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Lighting Off
        ZWrite Off
        ColorMask 0
        Pass {}
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
        }
    }
}
