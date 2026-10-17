Shader "AeroSim/ApronTarmac_RainPuddle"
{
    Properties
    {
        [Header(Base Asphalt Surface)]
        [MainTexture] _BaseMap("Asphalt Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Asphalt Tint", Color) = (0.22, 0.22, 0.24, 1.0)
        _BumpScale("Asphalt Normal Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _BumpMap("Asphalt Normal Map", 2D) = "bump" {}
        _Smoothness("Dry Tarmac Smoothness", Range(0.0, 1.0)) = 0.38
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.05
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion Map", 2D) = "white" {}

        [Header(Puddle Accumulation)]
        _PuddleMask("Puddle Distribution Mask", 2D) = "white" {}
        _PuddleTiling("Puddle Mask Tiling", Float) = 2.0
        _PuddleAmount("Puddle Coverage", Range(0.0, 1.0)) = 0.75
        _PuddleSmoothness("Puddle Mirror Smoothness", Range(0.8, 1.0)) = 0.98
        _PuddleTint("Puddle Water Depth Color", Color) = (0.06, 0.07, 0.09, 1.0)
        _PuddleEdgeWidth("Puddle Edge Transition", Range(0.01, 0.5)) = 0.12

        [Header(Rain Ripple Wave Simulation)]
        [Normal] _RippleNormalMap("Rain Ripple Normal Map", 2D) = "bump" {}
        _RippleTiling("Ripple Grid Density", Float) = 45.0
        _RippleSpeed("Ripple Animation Speed", Float) = 2.4
        _RippleIntensity("Ripple Wave Perturbation", Range(0.0, 3.0)) = 1.6

        [Header(Live Weather Control)]
        _RainIntensity("Rain Wetness & Ripple Active (0 to 1)", Range(0.0, 1.0)) = 1.0
        _WetDarkening("Wet Asphalt Darkening", Range(0.0, 1.0)) = 0.65

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);           SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);           SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);      SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_PuddleMask);        SAMPLER(sampler_PuddleMask);
            TEXTURE2D(_RippleNormalMap);   SAMPLER(sampler_RippleNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _PuddleTint;
                float _BumpScale;
                float _Smoothness;
                float _Metallic;
                float _OcclusionStrength;
                float _PuddleTiling;
                float _PuddleAmount;
                float _PuddleSmoothness;
                float _PuddleEdgeWidth;
                float _RippleTiling;
                float _RippleSpeed;
                float _RippleIntensity;
                float _RainIntensity;
                float _WetDarkening;
                float _ReceiveShadows;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float3 positionWS               : TEXCOORD0;
                float3 normalWS                 : TEXCOORD1;
                float4 tangentWS                : TEXCOORD2;
                float2 uv                       : TEXCOORD3;
                float4 shadowCoord              : TEXCOORD4;
                float  fogFactor                : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                output.shadowCoord = GetShadowCoord(posInputs);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            // Staggered multi-layer animated concentric wave ripple evaluation
            half3 ComputeRainRipples(float2 baseUV, float rainLevel)
            {
                if (rainLevel <= 0.005) return half3(0, 0, 1);

                float time = _Time.y * _RippleSpeed;
                float2 uv = baseUV * _RippleTiling;

                // 3 phase-shifted wave packets (staggered time & spatial offsets)
                float t0 = frac(time);
                float t1 = frac(time + 0.333);
                float t2 = frac(time + 0.667);

                // Smooth bell envelope for expanding rings
                half w0 = sin(t0 * PI);
                half w1 = sin(t1 * PI);
                half w2 = sin(t2 * PI);

                // Spatial offsets to break repetition across tiles
                float2 off0 = float2(0.13, 0.47) * floor(time);
                float2 off1 = float2(0.61, 0.29) * floor(time + 0.333);
                float2 off2 = float2(0.85, 0.74) * floor(time + 0.667);

                half3 n0 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormalMap, sampler_RippleNormalMap, uv + off0));
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormalMap, sampler_RippleNormalMap, uv * 1.28 + off1));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormalMap, sampler_RippleNormalMap, uv * 0.76 + off2));

                half3 rippleTS = (n0 * w0 + n1 * w1 + n2 * w2) / (w0 + w1 + w2 + 1e-4);
                rippleTS.xy *= _RippleIntensity * rainLevel;
                return normalize(rippleTS);
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float rainLevel = saturate(_RainIntensity);

                // 1. Sample Base Textures
                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half3 asphaltNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).r;
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);

                // 2. Sample Puddle Mask (using independent tiling)
                half puddleRaw = SAMPLE_TEXTURE2D(_PuddleMask, sampler_PuddleMask, uv * _PuddleTiling).r;

                // Calculate Puddle Factor: Threshold with edge width
                float puddleThreshold = saturate(1.0 - _PuddleAmount * rainLevel);
                half puddleFactor = smoothstep(puddleThreshold, puddleThreshold + _PuddleEdgeWidth, puddleRaw);
                puddleFactor *= rainLevel; // No puddles if rain is completely off

                // 3. Compute Animated Rain Ripples
                half3 rippleNormalTS = ComputeRainRipples(uv, rainLevel);

                // 4. Normal Blending
                // In dry areas: Asphalt pebble normal
                // In damp areas: Slightly smoothed asphalt
                // In deep puddles: Flat water surface perturbed by expanding rain ripples!
                half3 flatWaterNormal = half3(0, 0, 1);
                half3 waterSurfaceNormal = normalize(lerp(flatWaterNormal, rippleNormalTS, 0.85));

                // Also allow subtle ripples on damp asphalt surrounding puddles
                half3 dampAsphalt = normalize(lerp(asphaltNormalTS, rippleNormalTS, rainLevel * 0.35));
                half3 finalNormalTS = normalize(lerp(dampAsphalt, waterSurfaceNormal, puddleFactor));

                // 5. Albedo Wet Darkening & Puddle Color
                // Wet asphalt absorbs more light and appears significantly darker
                half3 dryColor = albedoSample.rgb;
                half3 wetColor = dryColor * lerp(1.0, 1.0 - _WetDarkening, rainLevel * 0.75);
                // In puddles, blend with deep standing water tint
                half3 finalAlbedo = lerp(wetColor, _PuddleTint.rgb, puddleFactor * 0.55);

                // 6. Smoothness & Specular Reflectivity
                // Dry tarmac: _Smoothness (0.38)
                // Wet tarmac: 0.65 - 0.75
                // Puddle surface: Mirror-like (_PuddleSmoothness = 0.98)
                half wetSmoothness = lerp(_Smoothness, 0.72, rainLevel);
                half finalSmoothness = lerp(wetSmoothness, _PuddleSmoothness, puddleFactor);

                half finalMetallic = lerp(_Metallic, 0.12, puddleFactor);

                // 7. Setup SurfaceData for URP PBR
                SurfaceData surfaceData;
                surfaceData.albedo = finalAlbedo;
                surfaceData.metallic = finalMetallic;
                surfaceData.specular = half3(0.04, 0.04, 0.04);
                surfaceData.smoothness = finalSmoothness;
                surfaceData.normalTS = finalNormalTS;
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                // 8. Setup InputData (World-Space Lighting Vectors)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;

                float sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);

                inputData.tangentToWorld = tangentToWorld;
                inputData.normalWS = TransformTangentToWorld(finalNormalTS, tangentToWorld);
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                inputData.shadowCoord = input.shadowCoord;
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                // 9. Evaluate Universal Fragment PBR
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // Standard Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Standard Depth Only Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
