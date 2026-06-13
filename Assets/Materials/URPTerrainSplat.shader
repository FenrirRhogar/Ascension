Shader "Custom/URPTerrainSplat"
{
    Properties
    {
        _Layer1("Ground (R)", 2D) = "white" {}
        _Layer2("Rock (G)", 2D) = "white" {}
        _Layer3("Peak (B)", 2D) = "white" {}
        _Tiling("Tiling", Float) = 10.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Splat weights
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 worldUV : TEXCOORD0;
                float4 splatWeights : COLOR;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1);
            TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2);
            TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3);

            CBUFFER_START(UnityPerMaterial)
                float _Tiling;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // Use world position for UVs to avoid tiling issues on procedural meshes
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldUV = worldPos.xz / _Tiling;
                
                output.splatWeights = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 tex1 = SAMPLE_TEXTURE2D(_Layer1, sampler_Layer1, input.worldUV);
                float4 tex2 = SAMPLE_TEXTURE2D(_Layer2, sampler_Layer2, input.worldUV);
                float4 tex3 = SAMPLE_TEXTURE2D(_Layer3, sampler_Layer3, input.worldUV);

                // Blend based on vertex colors
                float3 finalRGB = tex1.rgb * input.splatWeights.r + 
                                  tex2.rgb * input.splatWeights.g + 
                                  tex3.rgb * input.splatWeights.b;

                // Simple lighting
                float3 lightDir = _MainLightPosition.xyz;
                float3 normal = normalize(input.normalWS);
                float diffuse = saturate(dot(normal, lightDir));
                
                finalRGB *= (diffuse + 0.3); // Add some ambient

                return float4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}
