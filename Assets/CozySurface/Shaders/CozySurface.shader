Shader "Restoration/CozySurface"
{
    Properties
    {
        [Header(Clean surface)]
        _BaseMap ("Clean Texture", 2D) = "white" {}
        _BaseColor ("Clean Color (texture tint)", Color) = (1, 1, 1, 1)
        _BareGloss ("Soft Highlight Strength", Range(0, 1)) = 0.26
        _GlintColor ("Highlight Color", Color) = (1, 0.96, 0.9, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.01

        [Header(Soft lighting)]
        _ShadowTint ("Shadow Tint", Color) = (0.67, 0.64, 0.69, 1)
        _ShadowAmount ("Shadow Strength", Range(0, 1)) = 0.6
        _ShadowStep ("Light Transition Position", Range(0, 1)) = 0.5
        _ShadowSoft ("Light Transition Softness", Range(0.001, 0.5)) = 0.22
        _LightContribution ("Light Color Contribution", Range(0, 1)) = 0.5

        [Header(Dirt)]
        _DirtAmount ("Dirt Amount (static objects)", Range(0,1)) = 0
        _GrimeColor ("Opaque Dirt Color", Color) = (0.23,0.21,0.18,1)
        _GrimeOpacity ("Dirt Opacity", Range(0,1)) = 0.88
        _GrimeDesaturation ("Color Hidden By Dirt", Range(0,1)) = 0.75

        [Header(Cleaning component connection)]
        [Toggle] _UseWearMask ("Use Cleaning Masks (RestorableItem)", Float) = 0
        _WearMask ("Cleaning Mask (R dust, G dirt)", 2D) = "black" {}
        _MaskUVSet ("Mask UV: 0 = UV0, 1 = UV1", Float) = 1

        [HideInInspector] _WoodGrain ("Procedural Wood Preview", Range(0,1)) = 0
        [HideInInspector] _StudyGraphic ("Graphic shadow style", Range(0,1)) = 0
        [HideInInspector] _BrushScale ("Brush Patch Scale", Range(1, 35)) = 9
        [HideInInspector] _BrushStrength ("Painted Color Variation", Range(0, 1)) = 0.035
        [HideInInspector] _StrokeStrength ("Long Brush Strokes", Range(0, 0.4)) = 0.12
        [HideInInspector] _BrushDark ("Paint Shadow Pigment", Color) = (0.79, 0.82, 0.88, 1)
        [HideInInspector] _BrushLight ("Paint Light Pigment", Color) = (1.10, 1.05, 0.91, 1)
        [HideInInspector] _ChipBreakup ("Irregular Coating Edge", Range(0, 0.8)) = 0.35
        [HideInInspector] _ChipShadow ("Coating Edge Shadow", Range(0, 0.5)) = 0.12
        [HideInInspector] _PaintMask ("Paint Mask: R new paint, G varnish", 2D) = "black" {}
        [HideInInspector] _DustColor ("Dust", Color) = (0.80, 0.76, 0.72, 1)
        [HideInInspector] _DirtColor ("Dirt", Color) = (0.48, 0.33, 0.27, 1)
        [HideInInspector] _OldPaintColor ("Old Paint", Color) = (0.88, 0.44, 0.37, 1)
        [HideInInspector] [Toggle] _TexturedCoating ("Original Texture On Coating", Float) = 0
        [HideInInspector] _SubstrateColor ("Exposed Ceramic", Color) = (0.64, 0.48, 0.34, 1)
        [HideInInspector] _RustColor ("Rust", Color) = (0.81, 0.42, 0.17, 1)
        [HideInInspector] _DustOpacity ("Dust Opacity", Range(0, 1)) = 0.8
        [HideInInspector] _DustPixels ("Dust Pixel Grid (0 = smooth)", Float) = 0
        [HideInInspector] _DustRagged ("Dust Ragged Edge", Range(0, 1)) = 0.5
        [HideInInspector] _CleanGlow ("Clean Edge Glow", Range(0, 1)) = 0.5
        [HideInInspector] _DetailScale ("Wear Detail Scale", Float) = 30
        [HideInInspector] _DetailStrength ("Wear Detail Strength", Range(0, 1)) = 0.6
        [HideInInspector] _WearCoordinates ("Wear Coordinates", Vector) = (0,0,0,1)
        [HideInInspector] _WearDetailOffset ("Wear Seed Offset", Vector) = (0,0,0,0)
        [HideInInspector] _NewPaintColor ("New Paint", Color) = (0.55, 0.80, 0.75, 1)
        [HideInInspector] _EdgeLo ("Mask Edge Low", Range(0, 1)) = 0.42
        [HideInInspector] _EdgeHi ("Mask Edge High", Range(0, 1)) = 0.58
        [HideInInspector] _ShadingBlend ("Toon Flattening", Range(0, 1)) = 0.25
        [HideInInspector] _SkyAmbient ("Ambient Tint In Shadow", Range(0, 1)) = 0.2
        [HideInInspector] _LampBoost ("Lamp Strength", Range(0, 2)) = 1
        [HideInInspector] _FloorY ("Floor Height", Float) = 0
        [HideInInspector] _FloorHeight ("Fade Height", Float) = 1.2
        [HideInInspector] _FloorDark ("Brightness At Floor", Range(0, 1)) = 1
        [HideInInspector] _GlintSize ("Glint Size", Range(0, 1)) = 0.2
        [HideInInspector] _GlintSoft ("Glint Softness", Range(0, 1)) = 0.1
        [HideInInspector] _PaintGloss ("Fresh Paint Gloss", Range(0, 1)) = 0.15
        [HideInInspector] _WearRelief ("Coating Relief", Range(0, 0.003)) = 0.0002
        [HideInInspector] _RimSize ("Rim Size", Range(0, 1)) = 0.45
        [HideInInspector] _RimSoft ("Rim Softness", Range(0.01, 1)) = 0.3
        [HideInInspector] _OutlineColor ("Outline Color", Color) = (0.357, 0.247, 0.290, 1)
        [HideInInspector] _OutlineWidth ("Outline Width px", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
        TEXTURE2D(_WearMask);  SAMPLER(sampler_WearMask);
        TEXTURE2D(_PaintMask); SAMPLER(sampler_PaintMask);

        CBUFFER_START(UnityPerMaterial)
            half _DirtAmount; half _WoodGrain; half4 _GrimeColor; half _GrimeOpacity; half _GrimeDesaturation; half _StudyGraphic; half _UseWearMask;
            half _BrushScale;
            half _BrushStrength;
            half _StrokeStrength;
            half4 _BrushDark;
            half4 _BrushLight;
            half _ChipBreakup;
            half _ChipShadow;
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _MaskUVSet;
            half4 _DustColor;
            half _DustOpacity;
            half _DustPixels;
            half _DustRagged;
            half _CleanGlow;
            half _DetailScale;
            half _DetailStrength;
            float4 _WearCoordinates;
            float4 _WearDetailOffset;
            half4 _DirtColor;
            half4 _OldPaintColor;
            half _TexturedCoating;
            half4 _SubstrateColor;
            half4 _RustColor;
            half4 _NewPaintColor;
            half _EdgeLo;
            half _EdgeHi;
            half4 _ShadowTint;
            half _ShadowAmount;
            half _ShadowStep;
            half _ShadowSoft;
            half _ShadingBlend;
            half _LightContribution;
            half _SkyAmbient;
            half _LampBoost;
            half _FloorY;
            half _FloorHeight;
            half _FloorDark;
            half4 _GlintColor;
            half _GlintSize;
            half _GlintSoft;
            half _PaintGloss;
            half _BareGloss;
            half _WearRelief;
            half _RimSize;
            half _RimSoft;
            half _RimStrength;
            half4 _OutlineColor;
            half _OutlineWidth;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CozySurfaceLighting.hlsl"
            #include "CozySurfaceLayers.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 uv1 : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                float3 posOS : TEXCOORD5;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uv1 = _MaskUVSet > 0.5 ? v.uv1 : v.uv;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                o.posOS = v.positionOS.xyz;
                return o;
            }

            half EdgeMask(half x)
            {
                return smoothstep(_EdgeLo, _EdgeHi, x);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // ---- Слои состояния ----
                half3 bare = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                bare = PHBrush(bare, i.posOS * _WearCoordinates.w + _WearCoordinates.xyz);
                half4 w = SAMPLE_TEXTURE2D(_WearMask, sampler_WearMask, i.uv1) * _UseWearMask;
                w.r = max(w.r, _DirtAmount * (1.0h - _UseWearMask));
                half3 p = SAMPLE_TEXTURE2D(_PaintMask, sampler_PaintMask, i.uv1).rgb * _UseWearMask;

                half newP = EdgeMask(p.r);
                half varn = saturate(p.g);
                half wetV = p.b;   // влажность от губки

                WearColors C;
                C.dust = _DustColor.rgb;
                C.dirt = _DirtColor.rgb;
                C.oldPaint = _OldPaintColor.rgb;
                C.texturedCoating = _TexturedCoating;
                C.substrate = _SubstrateColor.rgb;
                C.rust = _RustColor.rgb;
                C.dustOpacity = _DustOpacity;
                C.detailStrength = _DetailStrength;
                C.dustRagged = _DustRagged;
                C.cleanGlow = _CleanGlow;
                WearNoise N = WearNoiseAt((i.posOS * _WearCoordinates.w + _WearCoordinates.xyz
                    + _WearDetailOffset.xyz) * _DetailScale);
                N.broad = lerp(0.5h, N.broad, C.detailStrength);
                N.mid = lerp(0.5h, N.mid, C.detailStrength);
                N.fine = lerp(0.5h, N.fine, C.detailStrength);

                // снизу вверх: материал → ржавчина → старая краска → новая краска → грязь → пыль
                half rustA, paintA, dirtA, dustA;
                half3 albedo = WearBelow(bare, w, N, C, rustA, paintA);
                albedo = lerp(albedo, _NewPaintColor.rgb, newP);
                albedo = WearAbove(albedo, w, N, C, dirtA, dustA);

                albedo *= 1.0h - wetV * 0.10h;   // мокрое темнее

                // насколько поверхность "потускнела": износ гасит блик и рим
                half coatingDullness = paintA * lerp(1.0h, 0.18h, _TexturedCoating);
                half wear = max(max(dustA, dirtA), max(coatingDullness, rustA) * (1.0h - newP));

                // ---- Освещение (общее для всех шейдеров игры) ----
                FWParams P;
                P.shadowTint = _ShadowTint.rgb;
                P.shadowAmount = _ShadowAmount;
                P.shadowStep = _ShadowStep;
                P.shadowSoft = _ShadowSoft;
                P.shadingBlend = _ShadingBlend;
                P.lightMix = _LightContribution;
                P.skyAmbient = _SkyAmbient;
                P.lampBoost = _LampBoost;
                P.floorY = _FloorY;
                P.floorHeight = _FloorHeight;
                P.floorDark = _FloorDark;

                float3 nWS = normalize(i.normalWS);
                // Surface gradient: a small raised edge on the remaining coating.
                // Derivatives follow the mask on the actual surface, independent of UV orientation.
                float coating = (paintA * (1.0h - newP) + rustA * 0.35h + dirtA * 0.1h) * _WearRelief;
                float3 dpdx = ddx(i.positionWS), dpdy = ddy(i.positionWS);
                float3 r1 = cross(dpdy, nWS), r2 = cross(nWS, dpdx);
                float det = dot(dpdx, r1);
                float3 gradient = (r1 * ddx(coating) + r2 * ddy(coating))
                    * (sign(det) / max(abs(det), 1e-8));
                nWS = normalize(nWS - gradient);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                half lit;
                Light mainLight;
                half3 col = FWShade(albedo, nWS, i.positionWS, i.positionCS, P, lit, mainLight);

                // ---- Блик: лак + чуть свежей краски, износ гасит ----
                half substrateGloss = _BareGloss * lerp(1.0h, paintA, _TexturedCoating);
                half surfaceGloss = lerp(substrateGloss, _PaintGloss, newP);
                half gloss = saturate(surfaceGloss * (1.0h - wear) + varn * (1.0h - wear) + wetV * 0.7h);
                float3 h = SafeNormalize(mainLight.direction + viewDir);
                half ndh = saturate(dot(nWS, h));
                half thr = 1.0h - _GlintSize * 0.5h;
                half soft = max(0.0005h, _GlintSoft * 0.2h);
                half glint = 0;
                half broadGlint = pow(ndh, 24.0h) * lit * gloss * 0.75h;
                col += _GlintColor.rgb * (glint + broadGlint) * mainLight.color;

                // ---- Рим ----
                half rim = 1.0h - saturate(dot(nWS, viewDir));
                half rimThr = 1.0h - _RimSize;
                rim = smoothstep(rimThr, rimThr + _RimSoft, rim);
                col += _GlintColor.rgb * rim * _RimStrength * (1.0h - wear) * lit;

                col = FWFog(col, i.positionWS);
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag

            struct OA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct OV { float4 positionCS : SV_POSITION; };

            OV OutlineVert(OA v)
            {
                OV o;
                float4 clip = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = TransformWorldToHClipDir(nWS).xy;
                nCS = normalize(nCS + float2(1e-5, 1e-5));
                // ширина постоянна в пикселях
                clip.xy += nCS / _ScreenParams.xy * _OutlineWidth * clip.w * 2.0;
                o.positionCS = clip;
                return o;
            }

            half4 OutlineFrag(OV i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct SA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SV { float4 positionCS : SV_POSITION; };

            SV ShadowVert(SA v)
            {
                SV o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE * cs.w);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE * cs.w);
                #endif
                o.positionCS = cs;
                return o;
            }

            half4 ShadowFrag(SV i) : SV_Target { return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DA { float4 positionOS : POSITION; };
            struct DV { float4 positionCS : SV_POSITION; };

            DV DepthVert(DA v)
            {
                DV o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 DepthFrag(DV i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}






