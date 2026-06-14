Shader "Pustynja/WorldSpaceSand"
{
    Properties
    {
        _BaseMap ("Sand Texture", 2D) = "white" {}
        _BaseColor ("Sand Color", Color) = (0.86, 0.58, 0.27, 1)
        _WorldTextureScale ("World Texture Scale", Range(0.01, 2)) = 0.2
        _NormalStrength ("Normal Lighting Strength", Range(0, 1)) = 0.45
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.42
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "World Space Sand"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _WorldTextureScale;
                half _NormalStrength;
                half _AmbientStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 worldUv = input.positionWS.xz * _WorldTextureScale;
                half4 sand = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldUv) * _BaseColor;

                half3 softenedNormal = normalize(lerp(half3(0, 1, 0), normalize(input.normalWS), _NormalStrength));
                Light mainLight = GetMainLight();
                half directLight = saturate(dot(softenedNormal, mainLight.direction));
                half lightAmount = _AmbientStrength + directLight * (1.0h - _AmbientStrength);
                half3 color = sand.rgb * mainLight.color * lightAmount;
                color = MixFog(color, input.fogFactor);

                return half4(color, sand.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
