Shader "Custom/SkinnedOccludedOutlineMask"
{
    // Mask 전용 셰이더 — 캐릭터 실루엣을 Stencil 비트1(Ref=2)에 기록
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "MASK"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Stencil
            {
                Ref       2
                WriteMask 2
                Comp      Always
                Pass      Replace
            }

            ColorMask 0
            ZWrite    Off
            ZTest     LEqual
            Cull      Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher 호환 (빈 CBUFFER라도 선언 필요)
            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
