// Frosted gel hex pad for URP (desktop), after the Intuitive Instruments Exquis pads.
//
// Two layered looks:
//   1. Gel surface: milky silicone under scene lighting. Wrapped (soft-terminator)
//      diffuse with a subsurface tint, back-light transmission through thin regions,
//      rough "frosted" specular, rim scatter and contact occlusion at the base.
//   2. LED glow: a virtual LED sits under the cap (object space). Light diffuses
//      through the gel with ~exp(-sigma*d)/d falloff, so the top centre and the
//      side-wall base glow hottest while the bevel and top corners fall off and stay
//      saturated. A hot core shifts toward white, and the top bevel catches a
//      thin internal-reflection highlight.
//
// Object-space assumptions: +Y up, cap centred on the Y axis. _CapBase = object-space Y of the
// cap's bottom (0 when the pivot is at the base, -height/2 when it is at the centre),
// _CapHeight = bottom-to-top height, _CapApothem = centre-to-flat-side distance.
//
// Runtime control: _Glow (0..1), _GlowColor and _BaseColor are meant to be set per cell
// through a MaterialPropertyBlock. All other properties are material-wide.

Shader "Aurumworks/GelHexPad"
{
    Properties
    {
        [Header(Gel Surface)]
        _BaseColor              ("Base Color", Color) = (0.86, 0.86, 0.88, 1)
        _SubsurfaceColor        ("Subsurface Color", Color) = (0.93, 0.87, 0.96, 1)
        _SubsurfaceStrength     ("Subsurface Strength", Range(0, 1)) = 0.65
        _Wrap                   ("Light Wrap", Range(0, 1)) = 0.55
        _Roughness              ("Roughness (frost)", Range(0.05, 1)) = 0.62
        _SpecularStrength       ("Specular Strength", Range(0, 2)) = 0.6
        _FrostStrength          ("Frost Normal Noise", Range(0, 0.5)) = 0.05
        _FrostScale             ("Frost Noise Scale", Float) = 80

        [Header(Back Light Transmission)]
        _TransmissionColor      ("Transmission Color", Color) = (1, 0.96, 0.96, 1)
        _TransmissionStrength   ("Transmission Strength", Range(0, 4)) = 0.8
        _TransmissionPower      ("Transmission Power", Range(1, 16)) = 3
        _TransmissionDistortion ("Transmission Normal Distortion", Range(0, 1)) = 0.35
        _ThicknessInfluence     ("Thickness Influence", Range(0, 1)) = 0.6

        [Header(Rim and Contact Shading)]
        _RimScatter             ("Rim Scatter (neg darkens)", Range(-1, 1)) = 0.3
        _RimPower               ("Rim Power", Range(0.5, 8)) = 3
        _BaseOcclusion          ("Base Occlusion", Range(0, 1)) = 0.35
        _BaseOcclusionHeight    ("Base Occlusion Height (0-1)", Range(0.01, 1)) = 0.3

        [Header(Cap Geometry Object Space)]
        _CapApothem             ("Cap Apothem (centre to flat)", Float) = 0.5
        _CapHeight              ("Cap Height", Float) = 0.4
        _CapBase                ("Cap Base Y (object space)", Float) = 0

        [Header(LED Glow Runtime)]
        _GlowColor              ("Glow Color", Color) = (0.55, 0.45, 1, 1)
        _Glow                   ("Glow Level", Range(0, 1)) = 0
        _GlowIntensity          ("Glow Intensity (HDR, top centre)", Range(0, 16)) = 2

        [Header(LED Glow Scattering)]
        _LedHeight              ("LED Height (x cap height)", Range(-1, 1)) = -0.1
        _LedSize                ("LED Size (x apothem)", Range(0.01, 1)) = 0.2
        _ScatterDensity         ("Scatter Density (contrast)", Range(0, 8)) = 2.4
        _MediumTint             ("Medium Tint (per-depth)", Color) = (1, 1, 1, 1)
        _Directionality         ("Exit Directionality", Range(0, 1)) = 0.4
        _GlowRim                ("Glow Rim (neg darkens edges)", Range(-1, 1)) = -0.25
        _CoreWhiteness          ("Hot Core Whiteness", Range(0, 1)) = 0.5
        _CoreThreshold          ("Hot Core Threshold (rel. top)", Range(0, 2)) = 0.6
        _CoreSoftness           ("Hot Core Softness", Range(0.01, 2)) = 0.35
        _EdgeCatch              ("Bevel Edge Catch", Range(0, 2)) = 0.2
        _EdgeBandCenter         ("Edge Band Centre (normal.y)", Range(0, 1)) = 0.75
        _EdgeBandWidth          ("Edge Band Width", Range(0.01, 0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            // Object space is required: batching would flatten it.
            "DisableBatching" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _SubsurfaceColor;
            float  _SubsurfaceStrength;
            float  _Wrap;
            float  _Roughness;
            float  _SpecularStrength;
            float  _FrostStrength;
            float  _FrostScale;

            float4 _TransmissionColor;
            float  _TransmissionStrength;
            float  _TransmissionPower;
            float  _TransmissionDistortion;
            float  _ThicknessInfluence;

            float  _RimScatter;
            float  _RimPower;
            float  _BaseOcclusion;
            float  _BaseOcclusionHeight;

            float  _CapApothem;
            float  _CapHeight;
            float  _CapBase;

            float4 _GlowColor;
            float  _Glow;
            float  _GlowIntensity;

            float  _LedHeight;
            float  _LedSize;
            float  _ScatterDensity;
            float4 _MediumTint;
            float  _Directionality;
            float  _GlowRim;
            float  _CoreWhiteness;
            float  _CoreThreshold;
            float  _CoreSoftness;
            float  _EdgeCatch;
            float  _EdgeBandCenter;
            float  _EdgeBandWidth;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------------
        // Forward lit
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex GelVert
            #pragma fragment GelFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // Forward+ keyword was renamed in Unity 6.1.
            #if UNITY_VERSION >= 600010
                #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #else
                #pragma multi_compile _ _FORWARD_PLUS
            #endif
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalOS   : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GelVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS   = input.normalOS;
                output.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            // ---------------- helpers ----------------

            float GelHash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float GelValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = GelHash13(i);
                float n100 = GelHash13(i + float3(1, 0, 0));
                float n010 = GelHash13(i + float3(0, 1, 0));
                float n110 = GelHash13(i + float3(1, 1, 0));
                float n001 = GelHash13(i + float3(0, 0, 1));
                float n101 = GelHash13(i + float3(1, 0, 1));
                float n011 = GelHash13(i + float3(0, 1, 1));
                float n111 = GelHash13(i + float3(1, 1, 1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            float3 GelFrostVector(float3 p)
            {
                return float3(GelValueNoise(p),
                              GelValueNoise(p + 17.31),
                              GelValueNoise(p - 29.17)) * 2.0 - 1.0;
            }

            // Diffusion-style fluence from the virtual LED at an object-space point.
            // Returns a value that is ~1 * exp(-density) one cap-height away.
            float GelFluence(float3 pOS, float3 nOS, out float distN)
            {
                float3 led  = float3(0.0, _LedHeight * _CapHeight, 0.0);
                float3 toP  = pOS - led;
                float  r    = _LedSize * _CapApothem;
                float  dist = sqrt(dot(toP, toP) + r * r);
                distN = dist / max(_CapHeight, 1e-4);

                float exitCos = dot(nOS, toP) / max(dist, 1e-5);
                float dirTerm = lerp(1.0, saturate(exitCos * 0.5 + 0.5), _Directionality);

                return exp(-_ScatterDensity * distN) / max(distN, 1e-3) * dirTerm;
            }

            struct GelSurface
            {
                float3 albedo;
                float3 N;
                float3 V;
                float  NdotV;
                float  roughness;      // alpha (perceptual^2)
                float3 F0;
                float  thinness;       // 0 thick .. 1 thin, already blended by _ThicknessInfluence
                float  occlusion;
            };

            void GelEvaluateLight(GelSurface s, Light light, float directAO,
                                  inout float3 diffuseAcc, inout float3 specularAcc)
            {
                float3 L        = light.direction;
                float3 radiance = light.color * light.distanceAttenuation;
                float  shadow   = light.shadowAttenuation;

                // Wrapped diffuse with a coloured terminator (cheap SSS).
                float NdotL   = dot(s.N, L);
                float lambert = saturate(NdotL);
                float wrap    = saturate((NdotL + _Wrap) / (1.0 + _Wrap));
                float3 diffuseTerm = lambert + _SubsurfaceStrength * _SubsurfaceColor.rgb * (wrap - lambert);
                // Soft shadows bleed a little inside a scattering medium.
                float sssShadow = lerp(shadow, 1.0, 0.25 * _SubsurfaceStrength);
                diffuseAcc += s.albedo * diffuseTerm * sssShadow * radiance * s.occlusion * directAO;

                // Back-light transmission (light passing through thin gel toward the viewer).
                float3 Ht = normalize(L + s.N * _TransmissionDistortion);
                float  t  = pow(saturate(dot(s.V, -Ht)), _TransmissionPower) * _TransmissionStrength * s.thinness;
                diffuseAcc += s.albedo * _TransmissionColor.rgb * t * radiance;

                // Rough GGX specular for the frosted skin.
                float3 H     = SafeNormalize(L + s.V);
                float  NdotH = saturate(dot(s.N, H));
                float  LdotH = saturate(dot(L, H));
                float  a2    = s.roughness * s.roughness;
                float  d     = (NdotH * a2 - NdotH) * NdotH + 1.0;
                float  D     = a2 / (PI * d * d + 1e-7);
                float  vis   = 0.5 / (s.NdotV * (lambert * (1.0 - s.roughness) + s.roughness)
                                    + lambert * (s.NdotV * (1.0 - s.roughness) + s.roughness) + 1e-5);
                float3 F     = s.F0 + (1.0 - s.F0) * pow(1.0 - LdotH, 5.0);
                specularAcc += F * (D * vis * lambert * shadow * _SpecularStrength) * radiance * directAO;
            }

            half4 GelFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Cap space: object space with the cap's bottom at y = 0.
                float3 pOS = input.positionOS - float3(0.0, _CapBase, 0.0);
                float3 nOS = normalize(input.normalOS);
                float3 positionWS = input.positionWS;
                float3 V = GetWorldSpaceNormalizeViewDir(positionWS);

                float3 N = normalize(input.normalWS);
                N = normalize(N + GelFrostVector(pOS * _FrostScale) * _FrostStrength);
                float NdotV = saturate(dot(N, V)) + 1e-4;

                float radialN = length(pOS.xz) / max(_CapApothem, 1e-4);
                float heightN = saturate(pOS.y / max(_CapHeight, 1e-4));

                // Thin toward outer top edges, thick at the base centre.
                float thin01   = saturate(0.5 * radialN + 0.5 * heightN);
                float thinness = lerp(1.0, thin01, _ThicknessInfluence);

                // Contact darkening where the cap meets the panel.
                float baseAO = lerp(1.0 - _BaseOcclusion, 1.0, smoothstep(0.0, _BaseOcclusionHeight, heightN));

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                AmbientOcclusionFactor ssao = GetScreenSpaceAmbientOcclusion(screenUV);

                GelSurface s;
                s.albedo    = _BaseColor.rgb;
                s.N         = N;
                s.V         = V;
                s.NdotV     = NdotV;
                s.roughness = max(_Roughness * _Roughness, 0.002);
                s.F0        = float3(0.03, 0.03, 0.03); // silicone, IOR ~1.4
                s.thinness  = thinness;
                s.occlusion = baseAO;

                float3 diffuseAcc  = 0;
                float3 specularAcc = 0;

                // Main light
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord, positionWS, half4(1, 1, 1, 1));
                GelEvaluateLight(s, mainLight, ssao.directAmbientOcclusion, diffuseAcc, specularAcc);

                // Additional lights (classic forward and Forward+ / cluster loop)
                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                {
                    InputData inputData = (InputData)0;
                    inputData.positionWS = positionWS;
                    inputData.normalizedScreenSpaceUV = screenUV;

                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP || USE_FORWARD_PLUS
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dirLight = GetAdditionalLight(dirIndex, positionWS, half4(1, 1, 1, 1));
                        GelEvaluateLight(s, dirLight, ssao.directAmbientOcclusion, diffuseAcc, specularAcc);
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1));
                        GelEvaluateLight(s, addLight, ssao.directAmbientOcclusion, diffuseAcc, specularAcc);
                    LIGHT_LOOP_END
                }
                #endif

                // Ambient: front SH plus a little back-side SH scattered through the gel.
                float indirectAO = baseAO * ssao.indirectAmbientOcclusion;
                float3 ambient = SampleSH(N) * s.albedo * indirectAO;
                ambient += SampleSH(-N) * s.albedo * _SubsurfaceColor.rgb * (0.3 * _SubsurfaceStrength * thinness) * indirectAO;
                diffuseAcc += ambient;

                // Rim scatter: light bleeding at silhouettes (positive) or darker limbs (negative).
                float fres = pow(1.0 - NdotV, _RimPower);
                diffuseAcc *= max(0.0, 1.0 + _RimScatter * fres);

                // Frosted environment reflection.
                float3 R = reflect(-V, N);
                float3 env = GlossyEnvironmentReflection(R, positionWS, _Roughness, indirectAO, screenUV);
                float  surfaceReduction = 1.0 / (s.roughness * s.roughness + 1.0);
                float3 Fenv = s.F0 + (max(1.0 - _Roughness, s.F0) - s.F0) * pow(1.0 - NdotV, 5.0);
                specularAcc += env * Fenv * surfaceReduction * _SpecularStrength;

                // ---------------- LED glow ----------------
                float3 emission = 0;
                if (_Glow > 1e-4)
                {
                    // Normalise so the top-centre of the cap = 1. _GlowIntensity is then the
                    // HDR brightness there, and _ScatterDensity only changes contrast.
                    float distTop;
                    float fluTop = GelFluence(float3(0, _CapHeight, 0), float3(0, 1, 0), distTop);

                    float distN;
                    float flu = GelFluence(pOS, nOS, distN) / max(fluTop, 1e-6);

                    // Hot core drifts toward white; dim presses stay saturated.
                    float hot = smoothstep(0.0, 1.0, saturate((flu * _Glow - _CoreThreshold) / max(_CoreSoftness, 1e-3)));
                    float3 col = lerp(_GlowColor.rgb, float3(1, 1, 1), hot * _CoreWhiteness);

                    // Optional per-depth colour absorption of the gel.
                    float3 tint = pow(max(_MediumTint.rgb, 1e-3), distN);

                    // Silhouette darkening/brightening of the glow.
                    float rimGlow = max(0.0, 1.0 + _GlowRim * fres);

                    emission = col * tint * flu * rimGlow;

                    // Thin internal-reflection highlight on the top bevel.
                    float band = 1.0 - smoothstep(0.0, _EdgeBandWidth, abs(nOS.y - _EdgeBandCenter));
                    emission += _GlowColor.rgb * band * _EdgeCatch;

                    emission *= _GlowIntensity * _Glow;
                }

                float3 color = diffuseAcc + specularAcc + emission;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Shadow caster
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth only
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth + normals (SSAO, etc.)
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    float2 remapped = saturate(oct * 0.5 + 0.5);
                    return half4(PackFloat2To888(remapped), 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
