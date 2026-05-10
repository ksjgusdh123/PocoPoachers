Shader "Custom/DarkOverlay"
{
    Properties
    {
        _Color   ("Dark Color", Color) = (0.2, 0.2, 0.2, 0.6)
        _FogMask ("Fog Mask",   2D)   = "black" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+2" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _FogMask_ST;
            CBUFFER_END

            TEXTURE2D(_FogMask);
            SAMPLER(sampler_FogMask);

            struct Attributes { float4 positionOS : POSITION; };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.screenPos   = posInputs.positionNDC;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv   = IN.screenPos.xy / IN.screenPos.w;
                float  mask = SAMPLE_TEXTURE2D(_FogMask, sampler_FogMask, uv).r;
                return half4(_Color.rgb, _Color.a * (1.0 - mask));
            }
            ENDHLSL
        }
    }
}
