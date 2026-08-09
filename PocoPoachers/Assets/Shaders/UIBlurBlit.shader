// UIBlurFeature가 카메라 컬러를 블러할 때 쓰는 블릿 셰이더.
// URP RenderGraph의 Blitter는 _BlitTexture / Vert(전체화면 삼각형)를 요구하므로
// 빌트인 파이프라인용 Custom/Blur와 달리 Blit.hlsl 기반으로 따로 둔다.
Shader "Hidden/UIBlurBlit"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurSize;

        // 선형 샘플링으로 9탭 가우시안을 5탭에 접은 형태.
        half4 BlurAlong(float2 uv, float2 dir)
        {
            float2 texel = _BlitTexture_TexelSize.xy * dir * _BlurSize;

            half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * 0.2270270270;

            float2 off1 = texel * 1.3846153846;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + off1) * 0.3162162162;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - off1) * 0.3162162162;

            float2 off2 = texel * 3.2307692308;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + off2) * 0.0702702703;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - off2) * 0.0702702703;

            return col;
        }
        ENDHLSL

        // Pass 0: Horizontal
        Pass
        {
            Name "UIBlurHorizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return BlurAlong(input.texcoord, float2(1, 0));
            }
            ENDHLSL
        }

        // Pass 1: Vertical
        Pass
        {
            Name "UIBlurVertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return BlurAlong(input.texcoord, float2(0, 1));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
