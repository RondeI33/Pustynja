Shader "Pustynja/HeatHazeOverlay"
{
    Properties
    {
        _Strength ("Distortion Strength", Range(0, 0.02)) = 0.0014
        _Speed ("Shimmer Speed", Range(0, 12)) = 0.75
        _Scale ("Shimmer Scale", Range(1, 80)) = 22
        _WarmTint ("Warm Tint", Color) = (1, 0.72, 0.42, 0.25)
        _Opacity ("Opacity", Range(0, 1)) = 0.24
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Heat Haze"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Speed;
                float _Scale;
                half4 _WarmTint;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPosition = ComputeScreenPos(output.positionHCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float time = _Time.y * _Speed;

                float waveX = sin((screenUv.y * _Scale + time) * 6.2831853);
                float waveY = sin(((screenUv.x + screenUv.y) * _Scale * 0.45 - time * 0.8) * 6.2831853);
                float smallRipple = sin((screenUv.x * _Scale * 1.7 + time * 1.4) * 6.2831853);

                float2 offset = float2(waveX + smallRipple * 0.35, waveY) * _Strength;
                half3 sceneColor = SampleSceneColor(saturate(screenUv + offset));
                half3 warmColor = lerp(sceneColor, sceneColor * _WarmTint.rgb, _WarmTint.a);

                return half4(warmColor, _Opacity);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
