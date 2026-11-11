// UI-Default with an alpha-based outline drawn behind the sprite.
// UIHoverOutline drives _OutlineAmount/_OutlineWidth on a per-object material instance, enlarges simple quads so the
// outline is not cut off, and writes the sprite's original UV rect to TEXCOORD1 so enlarged quads never sample
// neighbouring sprites in an atlas. Without that component the whole texture counts as the sprite.
Shader "Game/UI Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 0.86, 0.12, 1)
        _OutlineWidth ("Outline Width (texels)", Range(0, 32)) = 4
        _OutlineAmount ("Outline Amount", Range(0, 1)) = 0

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
                float4 uvRect : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 uvRect : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineAmount;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                // A zero rect means no UV rect was supplied: use the whole texture.
                output.uvRect = input.uvRect.z > input.uvRect.x ? input.uvRect : float4(0, 0, 1, 1);
                return output;
            }

            float Inside(float2 uv, float4 rect)
            {
                float2 inside = step(rect.xy, uv) * step(uv, rect.zw);
                return inside.x * inside.y;
            }

            float NeighbourAlpha(float2 uv, float4 rect)
            {
                return tex2Dlod(_MainTex, float4(uv, 0, 0)).a * Inside(uv, rect);
            }

            fixed4 Fragment(Varyings input) : SV_Target
            {
                half4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                color.a *= Inside(input.texcoord, input.uvRect);

                // Widest neighbouring alpha in 12 directions becomes the outline, only where the sprite is transparent.
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                float outline = 0;
                [unroll] for (int i = 0; i < 12; i++)
                {
                    float angle = i * 0.5235988;
                    outline = max(outline, NeighbourAlpha(input.texcoord + float2(cos(angle), sin(angle)) * offset, input.uvRect));
                }
                outline *= saturate(_OutlineAmount) * _OutlineColor.a * input.color.a;

                half4 result;
                result.a = color.a + outline * (1 - color.a);
                result.rgb = (color.rgb * color.a + _OutlineColor.rgb * outline * (1 - color.a)) / max(result.a, 0.0001);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
