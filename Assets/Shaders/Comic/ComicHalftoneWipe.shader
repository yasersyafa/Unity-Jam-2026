// Full-screen UI wipe: halftone dots grow along a sweep direction until the screen is covered.
// _Progress 0 = clear, 1 = fully covered.
Shader "Comic/HalftoneWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (Not Sampled)", 2D) = "white" {}
        _Color ("Ink Color", Color) = (0.055, 0.025, 0.09, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _CellSize ("Cell Size (Pixels)", Float) = 48
        _Angle ("Sweep Angle (Degrees)", Float) = 30
        _Softness ("Sweep Softness", Range(0.01, 1)) = 0.15

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            float _Progress;
            float _CellSize;
            float _Angle;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pixel = i.vertex.xy;
                float2 cell = pixel / max(_CellSize, 1.0);
                float2 center = (floor(cell) + 0.5) * max(_CellSize, 1.0);
                float d = length(frac(cell) - 0.5);

                // Sweep position of this cell's center, normalised to 0..1 across the screen.
                float rad = radians(_Angle);
                float2 dir = float2(cos(rad), sin(rad));
                float proj = dot(center / _ScreenParams.xy, dir);
                float minP = min(0.0, dir.x) + min(0.0, dir.y);
                float maxP = max(0.0, dir.x) + max(0.0, dir.y);
                float t = saturate((proj - minP) / max(maxP - minP, 0.0001));

                // 0.75 exceeds the cell's half-diagonal (~0.707), so progress 1 covers everything.
                float r = saturate((_Progress * (1.0 + _Softness) - t) / _Softness) * 0.75;
                float aa = max(fwidth(d), 0.0001);
                float alpha = 1.0 - smoothstep(r - aa, r, d);
                alpha *= step(0.0001, r);
                return fixed4(i.color.rgb, i.color.a * alpha);
            }
            ENDCG
        }
    }
}
