Shader "Custom/Blur"
{
    Properties
    {
        _MainTex  ("Texture",   2D)    = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        ZWrite Off
        ZTest Always
        Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_TexelSize;
        float     _BlurSize;
        ENDCG

        // Pass 0: Horizontal
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragH

            fixed4 fragH(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * 0.227027;

                float off1 = 1.0 * _BlurSize * _MainTex_TexelSize.x;
                col += tex2D(_MainTex, i.uv + float2(off1, 0)) * 0.1945946;
                col += tex2D(_MainTex, i.uv - float2(off1, 0)) * 0.1945946;

                float off2 = 2.0 * _BlurSize * _MainTex_TexelSize.x;
                col += tex2D(_MainTex, i.uv + float2(off2, 0)) * 0.1216216;
                col += tex2D(_MainTex, i.uv - float2(off2, 0)) * 0.1216216;

                float off3 = 3.0 * _BlurSize * _MainTex_TexelSize.x;
                col += tex2D(_MainTex, i.uv + float2(off3, 0)) * 0.054054;
                col += tex2D(_MainTex, i.uv - float2(off3, 0)) * 0.054054;

                float off4 = 4.0 * _BlurSize * _MainTex_TexelSize.x;
                col += tex2D(_MainTex, i.uv + float2(off4, 0)) * 0.016216;
                col += tex2D(_MainTex, i.uv - float2(off4, 0)) * 0.016216;

                return col;
            }
            ENDCG
        }

        // Pass 1: Vertical
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragV

            fixed4 fragV(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * 0.227027;

                float off1 = 1.0 * _BlurSize * _MainTex_TexelSize.y;
                col += tex2D(_MainTex, i.uv + float2(0, off1)) * 0.1945946;
                col += tex2D(_MainTex, i.uv - float2(0, off1)) * 0.1945946;

                float off2 = 2.0 * _BlurSize * _MainTex_TexelSize.y;
                col += tex2D(_MainTex, i.uv + float2(0, off2)) * 0.1216216;
                col += tex2D(_MainTex, i.uv - float2(0, off2)) * 0.1216216;

                float off3 = 3.0 * _BlurSize * _MainTex_TexelSize.y;
                col += tex2D(_MainTex, i.uv + float2(0, off3)) * 0.054054;
                col += tex2D(_MainTex, i.uv - float2(0, off3)) * 0.054054;

                float off4 = 4.0 * _BlurSize * _MainTex_TexelSize.y;
                col += tex2D(_MainTex, i.uv + float2(0, off4)) * 0.016216;
                col += tex2D(_MainTex, i.uv - float2(0, off4)) * 0.016216;

                return col;
            }
            ENDCG
        }
    }
}
