// 가장자리가 서서히 투명해지는 UI 셰이더. 미니맵처럼 잘린 사각형 느낌을 없앨 때 쓴다.
// 알파만 깎으므로 뒤에 깔린 블러 배경이 그대로 비쳐 자연스럽게 녹아든다.
//
// 경계를 극좌표(중심 + 각도별 반지름)로 잡는다.
//  - 중심/반지름: 지형이 한쪽으로 치우친 맵을 맞춘다
//  - 흔들림(Wobble): 각도에 따라 반지름을 변조해 잉크가 번진 듯한 불규칙한 경계를 만든다
//  - 마스크 텍스처(선택): 슬라이더로 안 되는 모양은 흑백 이미지로 직접 그려 넣는다
Shader "Custom/UI-EdgeFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shape)]
        _CenterX ("중심 X", Range(0, 1)) = 0.5
        _CenterY ("중심 Y", Range(0, 1)) = 0.5
        _RadiusX ("가로 반지름", Range(0.05, 1.2)) = 0.5
        _RadiusY ("세로 반지름", Range(0.05, 1.2)) = 0.5
        _Softness ("페이드 폭", Range(0.01, 1)) = 0.3

        [Header(Wobble)]
        _WobbleAmount ("흔들림 세기", Range(0, 0.6)) = 0.12
        _WobbleFrequency ("흔들림 개수", Range(1, 8)) = 3
        _WobbleSeed ("흔들림 시드", Range(0, 10)) = 0

        [Header(Optional Mask)]
        _MaskTex ("마스크 (흑백, 흰색=보임)", 2D) = "white" {}

        [Header(Outline)]
        _OutlineColor ("외곽선 색 (알파 0이면 끔)", Color) = (0.9, 0.15, 0.15, 0)
        _OutlineSharpness ("외곽선 두께 (클수록 얇음)", Range(1, 16)) = 6
        _OutlineGlow ("안쪽 번짐", Range(0, 1)) = 0.35

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _CenterX, _CenterY;
            float _RadiusX, _RadiusY;
            float _Softness;
            float _WobbleAmount, _WobbleFrequency, _WobbleSeed;

            fixed4 _OutlineColor;
            float _OutlineSharpness;
            float _OutlineGlow;

            // 각도에 따라 경계를 밀고 당긴다. 배음을 정수로 맞춰야 각도 0도와 360도가 이어진다.
            float Wobble(float angle)
            {
                float f = max(1.0, floor(_WobbleFrequency));
                return sin(angle * f + _WobbleSeed) * 0.6
                     + sin(angle * f * 2.0 + _WobbleSeed * 1.7) * 0.3
                     + sin(angle * f * 3.0 + _WobbleSeed * 2.3) * 0.1;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // 중심 기준 타원 좌표. 경계에서 1이 되도록 반지름으로 나눈다.
                float2 offset = IN.texcoord - float2(_CenterX, _CenterY);
                offset.x /= max(_RadiusX, 1e-4);
                offset.y /= max(_RadiusY, 1e-4);

                float radius = length(offset);
                radius *= 1.0 - Wobble(atan2(offset.y, offset.x)) * _WobbleAmount;

                // 0=완전 투명, 1=완전 불투명. 마스크까지 곱해두면 외곽선이 마스크 모양도 따라간다.
                float coverage = 1.0 - smoothstep(1.0 - _Softness, 1.0, radius);
                coverage *= tex2D(_MaskTex, IN.texcoord).r;
                color.a *= coverage;

                // coverage가 0도 1도 아닌 전이 구간에서만 1에 가까워지는 띠.
                float band = saturate(coverage * (1.0 - coverage) * 4.0);
                float outline = saturate(pow(band, _OutlineSharpness) + band * _OutlineGlow);
                float outlineAlpha = outline * _OutlineColor.a;

                color.rgb = lerp(color.rgb, _OutlineColor.rgb, outlineAlpha);
                color.a = max(color.a, outlineAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
