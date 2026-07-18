Shader "Custom/TriplanarLit"
{
    Properties
    {
        _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor("Base Color Tint", Color) = (1,1,1,1)
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 0.2
        _TriplanarScale("Tiling / Escala (mas alto = textura mas chica repetida)", Float) = 2.0
        _SharpenBlend("Nitidez del blend entre ejes", Range(1,32)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _NormalStrength;
                float _Smoothness;
                float _TriplanarScale;
                float _SharpenBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float  fogCoord    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            float3 TriplanarWeights(float3 normalWS)
            {
                float3 blend = abs(normalWS);
                blend = pow(blend, _SharpenBlend);
                blend /= (blend.x + blend.y + blend.z + 1e-5);
                return blend;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 blend = TriplanarWeights(normalWS);

                // Proyeccion de UV desde 3 ejes usando la posicion en el mundo
                float2 uvX = IN.positionWS.zy * _TriplanarScale;
                float2 uvY = IN.positionWS.xz * _TriplanarScale;
                float2 uvZ = IN.positionWS.xy * _TriplanarScale;

                half4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);

                half3 albedo = (colX.rgb * blend.x + colY.rgb * blend.y + colZ.rgb * blend.z) * _BaseColor.rgb;

                // Normal map triplanar simplificado (blend en espacio mundo)
                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvX), _NormalStrength);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvY), _NormalStrength);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvZ), _NormalStrength);

                float3 nXw = float3(0, nX.y, nX.x) + normalWS;
                float3 nYw = float3(nY.x, 0, nY.y) + normalWS;
                float3 nZw = float3(nZ.x, nZ.y, 0) + normalWS;

                float3 blendedNormal = normalize(nXw * blend.x + nYw * blend.y + nZw * blend.z);

                // Iluminacion principal + sombras
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(blendedNormal, mainLight.direction));
                half3 diffuse = albedo * mainLight.color * NdotL * mainLight.shadowAttenuation;

                half3 ambient = albedo * SampleSH(blendedNormal);

                // Especular simple tipo Blinn-Phong, controlado por Smoothness
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specPower = lerp(4.0, 128.0, _Smoothness);
                half spec = pow(saturate(dot(blendedNormal, halfDir)), specPower) * _Smoothness;
                half3 specular = mainLight.color * spec * mainLight.shadowAttenuation;

                half3 finalColor = diffuse + ambient + specular;
                finalColor = MixFog(finalColor, IN.fogCoord);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // Pass para que el objeto proyecte sombras correctamente
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
