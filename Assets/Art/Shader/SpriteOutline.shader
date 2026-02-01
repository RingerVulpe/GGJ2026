Shader "Ui/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size (px-ish)", Range(0,8)) = 1
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.01

        // ---- Added: UI stencil properties (so ScrollRect/Mask stops erroring) ----
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        // ---- Added: stencil block (Mask uses it; harmless if you only have RectMask2D) ----
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;

            fixed4 _OutlineColor;
            float _OutlineSize;
            float _AlphaThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                if (baseCol.a > _AlphaThreshold)
                    return baseCol;

                float2 texel = _MainTex_TexelSize.xy * _OutlineSize;

                float a = 0;
                a = max(a, tex2D(_MainTex, i.uv + float2( texel.x, 0)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-texel.x, 0)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(0,  texel.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(0, -texel.y)).a);

                a = max(a, tex2D(_MainTex, i.uv + float2( texel.x,  texel.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-texel.x,  texel.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2( texel.x, -texel.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-texel.x, -texel.y)).a);

                if (a > _AlphaThreshold)
                {
                    fixed4 o = _OutlineColor;
                    o.rgb *= o.a; // premultiplied alpha path
                    return o;
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}