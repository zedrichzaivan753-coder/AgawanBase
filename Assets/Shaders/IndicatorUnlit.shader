// Indicator-only unlit shader.
//
// WHY THIS EXISTS AT ALL:
// URP/Unlit (which Mat_Blob already uses) does not expose _ZTest, so a marker drawn with it
// is hidden behind anything with depth - and the arena contains two 7 m tall BasePost_* props
// that genuinely occlude a character standing behind them. This shader is URP/Unlit plus one
// extra material property, _ZTest, so a marker can be told to draw over props.
//
// It is deliberately tiny: no lighting, no shadows, no texture, one colour. It writes no depth,
// so overlapping markers blend instead of cutting holes in each other.
//
// SRP BATCHER: every per-material uniform lives in the UnityPerMaterial CBUFFER. _ZTest is
// shader STATE, not a uniform, so it belongs outside the buffer - exactly how URP's own Lit
// shader treats _ZWrite / _Cull / _SrcBlend. That keeps the SRP Batcher path intact, which is
// what lets one shared material serve many characters without breaking batching.

Shader "Agawan/IndicatorUnlit"
{
    Properties
    {
        _BaseColor("Base Colour", Color) = (1, 1, 1, 1)

        // 8 = Always, so a marker draws over rooftops, walls and the base posts.
        // Set per material, never per frame.
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Indicator"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
