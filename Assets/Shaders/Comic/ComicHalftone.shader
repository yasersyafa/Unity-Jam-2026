Shader "Comic/ProceduralHalftone"
{
    Properties
    {
        [Header(Pattern)]
        _DotColor ("Dot Color", Color) = (0.055, 0.025, 0.09, 1)
        _PatternDensity ("Repetitions Across Reference Width", Range(1, 150)) = 30
        _PatternWidth ("Reference Sprite Width (Local Units)", Float) = 1
        _DotRadius ("Dot Radius (Cell Units)", Range(0.05, 0.45)) = 0.25
        _DotSoftness ("Dot Edge Softness (Cell Units)", Range(0, 0.15)) = 0.01
        _PatternRotation ("Pattern Rotation (Degrees)", Range(-180, 180)) = 0
        _AspectCorrection ("Pattern Aspect Correction Y", Range(0.1, 10)) = 1
        [Header(Animation)]
        _ScrollSpeedX ("Scroll X (Reference Widths Per Second)", Float) = 0.015
        _ScrollSpeedY ("Scroll Y (Reference Widths Per Second)", Float) = 0.008
        _AnimationSpeed ("Animation Speed Multiplier", Range(0, 5)) = 1
        [Header(Appearance)]
        _Opacity ("Overall Opacity", Range(0, 1)) = 0.30
        _PatternContrast ("Pattern Contrast", Range(0.25, 4)) = 1
        [Toggle] _InvertPattern ("Invert Pattern", Float) = 0
        [HideInInspector] [PerRendererData] _MainTex ("Sprite Texture (Not Sampled)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Name "ComicHalftone"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            // Core2D provides SpriteRenderer tint/flip helpers; this pass uses Universal Renderer, not 2D lighting.
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DotColor;
                float _PatternDensity;
                float _PatternWidth;
                float _DotRadius;
                float _DotSoftness;
                float _PatternRotation;
                float _AspectCorrection;
                float _ScrollSpeedX;
                float _ScrollSpeedY;
                float _AnimationSpeed;
                float _Opacity;
                float _PatternContrast;
                float _InvertPattern;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 patternPosition : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 position = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(position);
                // Local geometry avoids atlas UV distortion and remains attached to the sprite surface.
                output.patternPosition = input.positionOS.xy / max(_PatternWidth, 0.0001);
                output.color = input.color * unity_SpriteColor;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 coordinates = input.patternPosition;
                coordinates.y *= max(_AspectCorrection, 0.0001);
                float sine, cosine;
                sincos(radians(_PatternRotation), sine, cosine);
                coordinates = float2(cosine * coordinates.x - sine * coordinates.y,
                                     sine * coordinates.x + cosine * coordinates.y);
                float density = clamp(_PatternDensity, 1.0, 150.0);
                // Wrap only in whole cell periods; arbitrary wrapped time would introduce visible jumps.
                float2 scroll = frac(_Time.y * _AnimationSpeed * float2(_ScrollSpeedX, _ScrollSpeedY) * density);
                float2 grid = coordinates * density + scroll;
                float distanceToCenter = length(frac(grid + 0.5) - 0.5);
                float radius = clamp(_DotRadius, 0.05, 0.45);
                float edge = max(fwidth(distanceToCenter), max(_DotSoftness, 0.0001));
                float dots = 1.0 - smoothstep(radius - edge, radius + edge, distanceToCenter);
                dots = pow(saturate(dots), max(_PatternContrast, 0.25));
                // Fade unresolved dots to their analytic area coverage instead of shimmering at subpixel sizes.
                float footprint = max(length(ddx(grid)), length(ddy(grid)));
                dots = lerp(dots, PI * radius * radius, smoothstep(0.35, 1.0, footprint));
                dots = lerp(dots, 1.0 - dots, saturate(_InvertPattern));
                return half4(_DotColor.rgb * input.color.rgb,
                    saturate(dots * _Opacity * _DotColor.a * input.color.a));
            }
            ENDHLSL
        }
    }
}
