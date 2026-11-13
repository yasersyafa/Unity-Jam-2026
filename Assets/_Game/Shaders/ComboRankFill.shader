Shader "Game/UI Combo Rank"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _FillBounds ("Vertical Bounds (Bottom, Top, Softness, EdgeWidth)", Vector) = (0.24, 0.78, 0.015, 0.035)
        _EmptyColor ("Unfilled Tint", Color) = (0.42, 0.42, 0.45, 1)
        _EmptyDesaturation ("Unfilled Desaturation", Range(0, 1)) = 0.12
        _EdgeGlowColor ("Fill Edge Color", Color) = (1, 1, 1, 0.9)
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
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
            Name "Default"
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _FillAmount;
            float4 _FillBounds;
            fixed4 _EmptyColor;
            float _EmptyDesaturation;
            fixed4 _EdgeGlowColor;
            fixed4 _FlashColor;
            float _FlashAmount;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 Fragment(Varyings input) : SV_Target
            {
                half4 tex = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;

                float boundsSpan = max(0.0001, _FillBounds.y - _FillBounds.x);
                float y01 = saturate((input.texcoord.y - _FillBounds.x) / boundsSpan);
                float softness = max(0.0005, _FillBounds.z);
                float fill = saturate(_FillAmount);
                float fillMask = (1.0 - smoothstep(fill - softness, fill + softness, y01)) * step(0.0001, fill);
                fillMask = lerp(fillMask, 1.0, step(0.9999, fill));

                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                half3 emptyRgb = lerp(tex.rgb, half3(lum, lum, lum), _EmptyDesaturation) * _EmptyColor.rgb * input.color.rgb;
                half emptyAlpha = tex.a * _EmptyColor.a;

                float glyphMask = smoothstep(0.22, 0.85, tex.a);
                float activeBand = step(0.01, fill) * (1.0 - step(0.99, fill));
                float edgeBand = (1.0 - smoothstep(0.0, max(0.001, _FillBounds.w), abs(y01 - fill))) * activeBand * glyphMask;
                half3 filledRgb = lerp(tex.rgb * input.color.rgb, _EdgeGlowColor.rgb * input.color.rgb, edgeBand * _EdgeGlowColor.a);

                half3 rgb = lerp(emptyRgb, filledRgb, fillMask);
                float flash = saturate(_FlashAmount) * glyphMask;
                rgb = lerp(rgb, _FlashColor.rgb * input.color.rgb, flash);

                half alpha = lerp(emptyAlpha, tex.a, fillMask);
                alpha = saturate(alpha + flash * 0.25) * input.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return half4(rgb, alpha);
            }
            ENDCG
        }
    }
}
