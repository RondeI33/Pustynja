Shader "Pustynja/FootprintMark"
{
    Properties
    {
        _Color ("Color", Color) = (0.12, 0.065, 0.025, 0.62)
        _SoftEdge ("Soft Edge", Range(0.05, 0.48)) = 0.24
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Footprint Mark"
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _SoftEdge;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float distanceFromCenter = distance(input.uv, float2(0.5, 0.5));
                float alpha = saturate((0.5 - distanceFromCenter) / max(0.001, 0.5 - _SoftEdge));
                alpha = alpha * alpha * (3.0 - 2.0 * alpha);
                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
