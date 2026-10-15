// Универсальный шейдер для всех предметов, которые не чистятся (мебель, пропсы, стены).
// Для предметов с износом используй Restoration/ToonRestorable: у него то же освещение.
Shader "Restoration/FirewatchLit"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Painted detail)]
        _DetailMap ("Brush Detail (gray)", 2D) = "gray" {}
        _DetailTiling ("Detail Tiling", Float) = 3
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.25

        [Header(Light and shadow)]
        _ShadowTint ("Shadow Tint", Color) = (0.48, 0.35, 0.63, 1)
        _ShadowAmount ("Shadow Amount", Range(0, 1)) = 0.5
        _ShadowStep ("Shadow Step", Range(0, 1)) = 0.5
        _ShadowSoft ("Shadow Softness", Range(0.001, 0.5)) = 0.12
        _ShadingBlend ("Toon Flattening", Range(0, 1)) = 0.35
        _LightMix ("Light Color Contribution", Range(0, 1)) = 0.5
        _SkyAmbient ("Ambient Tint In Shadow", Range(0, 1)) = 0.6
        _LampBoost ("Lamp Strength", Range(0, 2)) = 1

        [Header(Floor darkening)]
        _FloorY ("Floor Height", Float) = 0
        _FloorHeight ("Fade Height", Float) = 1.2
        _FloorDark ("Brightness At Floor", Range(0, 1)) = 0.8

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 0.86, 0.7, 1)
        _RimSize ("Rim Size", Range(0, 1)) = 0.45
        _RimSoft ("Rim Softness", Range(0.01, 1)) = 0.4
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
        TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _DetailTiling;
            half _DetailStrength;
            half4 _ShadowTint;
            half _ShadowAmount;
            half _ShadowStep;
            half _ShadowSoft;
            half _ShadingBlend;
            half _LightMix;
            half _SkyAmbient;
            half _LampBoost;
            half _FloorY;
            half _FloorHeight;
            half _FloorDark;
            half4 _RimColor;
            half _RimSize;
            half _RimSoft;
            half _RimStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "FirewatchLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, i.uv * _DetailTiling).r;
                albedo *= 1.0h + (detail - 0.5h) * _DetailStrength * 2.0h;

                FWParams P;
                P.shadowTint = _ShadowTint.rgb;
                P.shadowAmount = _ShadowAmount;
                P.shadowStep = _ShadowStep;
                P.shadowSoft = _ShadowSoft;
                P.shadingBlend = _ShadingBlend;
                P.lightMix = _LightMix;
                P.skyAmbient = _SkyAmbient;
                P.lampBoost = _LampBoost;
                P.floorY = _FloorY;
                P.floorHeight = _FloorHeight;
                P.floorDark = _FloorDark;

                float3 nWS = normalize(i.normalWS);
                half lit;
                Light mainLight;
                half3 col = FWShade(albedo, nWS, i.positionWS, i.positionCS, P, lit, mainLight);

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                half rim = 1.0h - saturate(dot(nWS, viewDir));
                half rimThr = 1.0h - _RimSize;
                rim = smoothstep(rimThr, rimThr + _RimSoft, rim);
                col += _RimColor.rgb * rim * _RimStrength * saturate(lit + 0.3h);

                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

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
