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

        [Header(Organic World Space Puddles)]
        _PuddleMask("Multi-Octave Puddle Mask (RGBA)", 2D) = "white" {}
        _PuddleAmount("Puddle Water Level", Range(0.0, 1.0)) = 0.78
        _PuddleSmoothness("Puddle Mirror Smoothness", Range(0.85, 1.0)) = 0.985
        _PuddleTint("Puddle Water Depth Color", Color) = (0.05, 0.06, 0.08, 1.0)
        _PuddleEdgeWidth("Puddle Edge Transition", Range(0.01, 0.4)) = 0.10
        _PebbleInfiltration("Pebble Crevice Infiltration", Range(0.0, 1.0)) = 0.45

        [Header(Reactive Raindrop Ripple Simulation)]
        _RippleSpeed("Ripple Expansion Speed", Float) = 1.25
        _RippleIntensity("Ripple Wave Perturbation", Range(0.0, 3.0)) = 1.8
        _RippleDensity("Raindrop Arrival Frequency", Range(0.2, 3.0)) = 1.2

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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _PuddleTint;
                float _BumpScale;
                float _Smoothness;
                float _Metallic;
                float _OcclusionStrength;
                float _PuddleAmount;
                float _PuddleSmoothness;
                float _PuddleEdgeWidth;
                float _PebbleInfiltration;
                float _RippleSpeed;
                float _RippleIntensity;
                float _RippleDensity;
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

            // ═══════════════════════════════════════════════════════════════════════
            //  GPU PROCEDURAL RAINDROP RIPPLE ENGINE
            //  Analytical concentric capillary wave packets synthesized in real time
            //  Completely eliminates repetition and locks to actual raindrop physics
            // ═══════════════════════════════════════════════════════════════════════

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Evaluates single raindrop impact cell
            half2 EvaluateRaindropCell(float2 cell, float2 posWS, float cellSize, float time, float rainLevel, float speed, float waveK)
            {
                float2 dropHash = Hash22(cell);
                float timeHash = Hash21(cell * 17.13 + 5.37);

                // Reactivity: In light rain, only low-hash cells trigger; in heavy storm, all cells fire
                float dropGating = dropHash.x * 1.05;
                if (dropGating > rainLevel) return half2(0, 0);

                // Raindrop impact center within cell
                float2 dropCenterWS = (cell + dropHash * 0.70 + 0.15) * cellSize;
                float2 diff = posWS - dropCenterWS;
                float dist = length(diff);

                // Drop lifecycle progress (0 = just impacted, 1 = fully dissipated)
                float dropProgress = frac(time * speed + timeHash);

                // Expanding wavefront radius
                float maxRadius = cellSize * 0.75;
                float waveRadius = dropProgress * maxRadius;
                float d = dist - waveRadius;

                // Wave amplitude envelope: sharp initial splash peak then smooth outward decay
                float impactFade = smoothstep(0.0, 0.06, dropProgress) * pow(saturate(1.0 - dropProgress), 1.6);
                impactFade *= saturate(1.0 - dist / (cellSize * 0.92)); // confine to cell influence

                // Analytical concentric capillary wave equation
                float sigma = 75.0;
                float expDecay = exp(-sigma * d * d);
                float waveDerivative = (waveK * cos(waveK * d) - 2.0 * sigma * d * sin(waveK * d)) * expDecay;

                // Droplet central splash rebound bead on fresh impacts
                float splashCenter = exp(-dist * dist * 140.0) * sin(dropProgress * 22.0) * saturate(1.0 - dropProgress * 4.0);

                float totalDisplacement = (waveDerivative + splashCenter * 15.0) * impactFade * 0.038;

                // Radial direction vector (tangent space X/Y gradient)
                float2 radialDir = diff / (dist + 1e-4);
                return half2(radialDir * totalDisplacement);
            }

            // Evaluates 2x2 neighboring cells for seamless wave propagation across cell seams
            half2 SampleRainGrid(float2 posWS, float cellSize, float time, float rainLevel, float speed, float waveK)
            {
                float2 gridPos = posWS / cellSize;
                float2 cellBase = floor(gridPos - 0.5);

                half2 rippleSum = half2(0, 0);
                [unroll]
                for (int ox = 0; ox <= 1; ox++)
                {
                    [unroll]
                    for (int oy = 0; oy <= 1; oy++)
                    {
                        float2 cell = cellBase + float2(ox, oy);
                        rippleSum += EvaluateRaindropCell(cell, posWS, cellSize, time, rainLevel, speed, waveK);
                    }
                }
                return rippleSum;
            }

            // Dual-layer stochastic raindrop simulation
            half3 ComputeProceduralRainRipples(float3 posWS, float rainLevel)
            {
                if (rainLevel <= 0.005) return half3(0, 0, 1);

                float time = _Time.y * _RippleSpeed;
                float2 p = posWS.xz;

                // Layer 1: Primary fat raindrops (~0.72m cell size, expanding waves)
                half2 ripple1 = SampleRainGrid(p, 0.72, time, rainLevel, 1.35 * _RippleDensity, 46.0);

                // Layer 2: Interleaved rotated smaller & faster raindrops (~0.50m cell size, rotated 37.5 deg)
                float2 rotP = float2(p.x * 0.793 - p.y * 0.609, p.x * 0.609 + p.y * 0.793);
                half2 ripple2 = SampleRainGrid(rotP + float2(12.7, 34.1), 0.50, time * 1.25, rainLevel, 1.85 * _RippleDensity, 58.0);

                // Superposition of wave packets
                half2 combinedXY = (ripple1 + ripple2 * 0.75) * (_RippleIntensity * rainLevel);

                // Analytical unit normal
                half nz = sqrt(saturate(1.0 - dot(combinedXY, combinedXY)));
                return half3(combinedXY, nz);
            }

            // ═══════════════════════════════════════════════════════════════════════
            //  ORGANIC NON-REPETITIVE PUDDLE DISTRIBUTION
            //  Evaluated in World Space with continuous domain warping
            // ═══════════════════════════════════════════════════════════════════════

            half CalculateOrganicPuddleMask(float3 posWS, float rainLevel, float pebbleHeight)
            {
                if (rainLevel <= 0.005) return 0.0;

                float2 p = posWS.xz;

                // Continuous sinusoidal domain warp: breaks any rectangular grid or pattern recognition!
                float2 warp = float2(
                    sin(p.y * 0.038 + p.x * 0.022),
                    cos(p.x * 0.032 - p.y * 0.026)
                ) * 5.5; // Up to 5.5m organic warping
                float2 warpedP = p + warp;

                // Channel R: Macro terrain drainage basins (scale ~80m across apron)
                half macroDepression = SAMPLE_TEXTURE2D(_PuddleMask, sampler_PuddleMask, warpedP * 0.0125).r;

                // Channel G: Medium swales & puddles (scale ~22m, rotated 43 degrees)
                float2 rotP = float2(warpedP.x * 0.731 - warpedP.y * 0.682, warpedP.x * 0.682 + warpedP.y * 0.731);
                half mediumDepression = SAMPLE_TEXTURE2D(_PuddleMask, sampler_PuddleMask, rotP * 0.045).g;

                // Channel B: Micro fissures & local roughness (scale ~5.5m)
                half microDepression = SAMPLE_TEXTURE2D(_PuddleMask, sampler_PuddleMask, warpedP * 0.18).b;

                // Multi-scale terrain depression
                half terrainDepression = macroDepression * 0.52 + mediumDepression * 0.33 + microDepression * 0.15;

                // Infiltrate pebble crevices: water pools between asphalt stones first!
                // Pebble peaks (high pebbleHeight) stay above water longer than low cavities
                half surfaceHeight = lerp(terrainDepression, terrainDepression * (1.0 - pebbleHeight * _PebbleInfiltration), 0.65);

                // Water level rises with rain intensity and configured coverage
                float waterLevel = saturate(_PuddleAmount * rainLevel);
                float threshold = saturate(1.0 - waterLevel);

                half puddleFactor = smoothstep(threshold, threshold + _PuddleEdgeWidth, surfaceHeight);
                return saturate(puddleFactor * rainLevel);
            }

            // ═══════════════════════════════════════════════════════════════════════
            //  FRAGMENT SHADER
            // ═══════════════════════════════════════════════════════════════════════

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float3 posWS = input.positionWS;
                float rainLevel = saturate(_RainIntensity);

                // 1. Sample Asphalt Textures
                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half3 asphaltNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).r;
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);

                // 2. Evaluate Non-Repetitive Organic Puddles in World Space
                float pebbleHeight = saturate(asphaltNormalTS.z * 1.3 - 0.3);
                half puddleFactor = CalculateOrganicPuddleMask(posWS, rainLevel, pebbleHeight);

                // 3. Compute Truly Reactive Procedural Raindrop Ripples
                half3 rippleNormalTS = ComputeProceduralRainRipples(posWS, rainLevel);

                // 4. Normal Blending
                // In dry areas: Crisp rough asphalt pebble normal
                // In damp surrounding areas: Damp asphalt with subtle micro-disturbance
                // In standing puddles: Flat glassy water normal perturbed by active expanding raindrop ripples
                half3 flatWaterNormal = half3(0, 0, 1);
                half3 waterNormal = normalize(lerp(flatWaterNormal, rippleNormalTS, 0.90));

                half3 dampAsphaltNormal = normalize(lerp(asphaltNormalTS, rippleNormalTS, rainLevel * 0.25));
                half3 finalNormalTS = normalize(lerp(dampAsphaltNormal, waterNormal, puddleFactor));

                // 5. Albedo Wet Darkening & Puddle Depth Color
                // Wet asphalt absorbs more light and appears significantly darker
                half3 dryColor = albedoSample.rgb;
                half3 wetColor = dryColor * lerp(1.0, 1.0 - _WetDarkening, rainLevel * 0.80);

                // Inside puddles, blend with deep standing water tint
                half3 finalAlbedo = lerp(wetColor, _PuddleTint.rgb, puddleFactor * 0.60);

                // 6. Smoothness & Specular Reflectivity
                // Dry tarmac: _Smoothness (0.38)
                // Wet tarmac: 0.65 - 0.75
                // Puddles: Mirror-like (_PuddleSmoothness = 0.985)
                half wetSmoothness = lerp(_Smoothness, 0.74, rainLevel);
                half finalSmoothness = lerp(wetSmoothness, _PuddleSmoothness, puddleFactor);

                half finalMetallic = lerp(_Metallic, 0.15, puddleFactor);

                // 7. Surface Data
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

                // 8. Lighting Vectors
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

                // 9. URP Forward PBR Lighting Calculation
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // Depth & Shadow Passes
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
