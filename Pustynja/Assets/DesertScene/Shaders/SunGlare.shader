Shader "Pustynja/SunGlare"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.72, 0.32, 0.8)
        _RingColor ("Ring Color", Color) = (1, 0.45, 0.18, 0.35)
        _Intensity ("Intensity", Range(0, 4)) = 1.4
        _Radius ("Radius", Range(0.01, 0.6)) = 0.2
        _Softness ("Softness", Range(0.01, 1)) = 0.4
        _RingRadius ("Ring Radius", Range(0.01, 0.8)) = 0.32
        _RingWidth ("Ring Width", Range(0.005, 0.2)) = 0.035
        _Alpha ("Alpha", Range(0, 1)) = 1
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
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RingColor;
                half _Intensity;
                half _Radius;
                half _Softness;
                half _RingRadius;
                half _RingWidth;
                half _Alpha;
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
                float2 centeredUv = input.uv - 0.5;
                float distanceFromCenter = length(centeredUv);

                float core = smoothstep(_Radius, 0.0, distanceFromCenter);
                float glow = smoothstep(_Radius + _Softness, 0.02, distanceFromCenter) * 0.45;
                float ring = smoothstep(_RingWidth, 0.0, abs(distanceFromCenter - _RingRadius)) * 0.22;

                half3 color = _Color.rgb * (core + glow) + _RingColor.rgb * ring;
                half alpha = saturate((core + glow + ring) * _Color.a * _Alpha);
                return half4(color * _Intensity, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
