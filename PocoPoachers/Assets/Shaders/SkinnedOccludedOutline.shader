Shader "Custom/SkinnedOccludedOutline"
{
    // Outline 전용 셰이더 — Mask보다 Queue+1이 보장되므로 반드시 Mask 다음에 실행됨
    Properties
    {
        _OutlineColor     ("Outline Color",  Color)  = (1, 1, 0, 1)
        _OutlineWidth     ("Outline Width",  Float)  = 0.02
        _WaveSpeed        ("Wave Speed",     Float)  = 3.0
        _WaveFrequency    ("Wave Frequency", Float)  = 5.0
        _WaveAmplitude    ("Wave Amplitude", Float)  = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+1" }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Stencil
            {
                Ref      2
                ReadMask 2
                Comp     NotEqual
            }

            ZWrite Off
            ZTest  Greater
            Cull   Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveAmplitude;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 로컬 좌표 기준 — 메시에 파동이 고정되어 이동해도 속도가 일정함
                float  phase  = IN.positionOS.x * _WaveFrequency
                              + IN.positionOS.y * _WaveFrequency * 0.7
                              + _Time.y          * _WaveSpeed;
                float  wave   = sin(phase) * _WaveAmplitude;

                float3 inflated  = IN.positionOS.xyz + IN.normalOS * (_OutlineWidth + wave);
                OUT.positionHCS = TransformObjectToHClip(inflated);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
