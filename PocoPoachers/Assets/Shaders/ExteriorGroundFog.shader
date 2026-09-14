Shader "PocoPoachers/ExteriorGroundFog"
{
    Properties
    {
        _Mask("영역", 2D) = "black" {}
        _FogColor("안개 색", Color) = (0.49,0.43,0.33,1)
        _Density("농도", Range(0,1)) = 0.85
        _NoiseScale("구름 크기", Float) = 35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Mask); SAMPLER(sampler_Mask);
            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Density;
                float _NoiseScale;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                o.uv = v.uv;
                return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                return lerp(lerp(Hash(i), Hash(i+float2(1,0)), f.x),
                    lerp(Hash(i+float2(0,1)), Hash(i+1), f.x), f.y);
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.uv).r;
                float2 p = i.world.xz / max(_NoiseScale,1);
                float n = Noise(p + _Time.y*float2(0.012,0.007))*0.65
                    + Noise(p*2.3 - _Time.y*float2(0.008,0.01))*0.35;
                return half4(_FogColor.rgb * lerp(0.85,1.12,n),
                    smoothstep(0.15,0.95,mask)*_Density*lerp(0.65,1,n)*_FogColor.a);
            }
            ENDHLSL
        }
    }
}
