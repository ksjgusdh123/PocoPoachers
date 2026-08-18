// 반투명 본체 + 불투명 외곽선을 한 번에 그리는 UI 셰이더.
//
// uGUI의 Outline 컴포넌트는 원본 메시를 복제해 뒤에 깐다. 그래서 본체가 반투명하면 뒤에 깔린
// 외곽선 색이 비쳐 본체까지 물든다. 여기서는 한 픽셀 안에서 본체와 외곽선을 직접 나눠 그리므로
// 둘이 겹치지 않는다. 본체 알파가 얼마든 외곽선은 지정한 알파 그대로 나온다.
//
// 모양은 둥근 사각형 거리장으로 잡는다. 텍스처 샘플 없이 두께를 얼마든 키울 수 있고,
// 안티에일리어싱이 화면 픽셀 기준으로 공짜로 나온다.
//
// 외곽선은 사각형 경계 바깥에 그려진다. RectTransform 밖이므로 UIRoundedOutline 컴포넌트가
// 메시에 여백 띠를 덧붙여 그릴 자리를 만든다.
//
// Canvas는 배칭하면서 정점을 루트 캔버스 공간으로 구워 넣는다. 그래서 POSITION으로는 사각형
// 기준 좌표를 알 수 없다. 컴포넌트가 uv1에 그 좌표를 실어 보내므로 거리장은 uv1로 잰다.
// (그 대신 Canvas의 Additional Shader Channels에 TexCoord1이 켜져 있어야 한다)
//
// 알파가 픽셀마다 달라지므로 프리멀티플라이드 알파로 내보낸다.
Shader "Custom/UI-RoundedOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Body)]
        _BodyAlpha ("본체 불투명도", Range(0, 1)) = 0.6

        [Header(Outline)]
        _OutlineColor ("외곽선 색", Color) = (0.31, 0.847, 0.961, 1)
        _Thickness ("외곽선 두께 px", Float) = 2

        [Header(Glow)]
        _GlowColor ("야광 색", Color) = (0.31, 0.847, 0.961, 1)
        _GlowWidth ("번짐 폭 px", Float) = 16
        _GlowIntensity ("발광 세기 (0이면 끔)", Float) = 1
        _PulseSpeed ("맥동 속도 (0이면 끔)", Float) = 0
        _PulseAmount ("맥동 폭", Range(0, 1)) = 0.25

        [Header(Shape)]
        _RectSize ("사각형 크기 px (스크립트가 채움)", Vector) = (400, 300, 0, 0)
        _Radius ("모서리 둥글기 px", Float) = 12

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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex    : POSITION;
                float4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float2 local         : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _BodyAlpha;

            fixed4 _OutlineColor;
            float _Thickness;

            fixed4 _GlowColor;
            float _GlowWidth;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmount;

            float4 _RectSize;
            float _Radius;

            // 둥근 사각형까지의 부호 있는 거리. 안쪽이 음수, 경계가 0, 바깥이 양수다.
            float RoundedBoxSDF(float2 p, float2 halfSize, float radius)
            {
                radius = min(radius, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.local = v.texcoord1;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 사각형 중심을 원점으로 하는 좌표. 컴포넌트가 uv1에 실어 보낸 값이라
                // 캔버스 배칭이나 패널 이동에 흔들리지 않는다.
                float2 halfSize = max(_RectSize.xy * 0.5, 1.0);
                float2 p = IN.local;

                float d = RoundedBoxSDF(p, halfSize, _Radius);
                float aa = max(fwidth(d), 1e-4);

                // 본체는 경계 안쪽, 외곽선은 경계에서 두께만큼 바깥. 두 영역은 겹치지 않는다.
                float inside = 1.0 - smoothstep(-aa, aa, d);
                float outside = 1.0 - smoothstep(_Thickness - aa, _Thickness + aa, d);
                float edge = saturate(outside - inside);

                half4 tex = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // 본체만 반투명해진다. 외곽선은 아래에서 따로 계산하므로 영향받지 않는다.
                float bodyAlpha = tex.a * _BodyAlpha * inside;

                // 정점 알파는 CanvasGroup 페이드다. 외곽선도 같이 사라져야 하므로 곱한다.
                float outlineAlpha = _OutlineColor.a * IN.color.a * edge;

                float pulse = 1.0;
                if (_PulseSpeed > 0.0)
                    pulse = 1.0 - _PulseAmount * (0.5 - 0.5 * sin(_Time.y * _PulseSpeed));

                // 외곽선 바깥으로 지수 감쇠하는 번짐. 외곽선 띠 위에서는 세기가 1이라 선 자체가 빛나 보인다.
                // 그래픽 색을 곱하면 어두운 패널에서 발광이 죽으므로 페이드용 알파만 받는다.
                float halo = exp(-max(d - _Thickness, 0.0) / max(_GlowWidth, 1e-3))
                           * smoothstep(-aa, aa, d)
                           * _GlowIntensity * _GlowColor.a * IN.color.a * pulse;

                #ifdef UNITY_UI_CLIP_RECT
                float clipping = UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                bodyAlpha *= clipping;
                outlineAlpha *= clipping;
                halo *= clipping;
                #endif

                float alpha = bodyAlpha + outlineAlpha;

                #ifdef UNITY_UI_ALPHACLIP
                clip(max(alpha, halo) - 0.001);
                #endif

                // 본체와 외곽선은 배타적이라 각자 알파를 곱해 더하면 서로 물들지 않는다.
                // 번짐은 알파에 더하지 않는다. 알파 0인 채로 색만 얹히므로 가산 합성이 된다.
                half3 rgb = tex.rgb * bodyAlpha + _OutlineColor.rgb * outlineAlpha + _GlowColor.rgb * halo;
                return half4(rgb, alpha);
            }
            ENDCG
        }
    }
}
