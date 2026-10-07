Shader "Restoration/ToonRestorable"
{
    Properties
    {
        [Header(Base bare material UV0)]
        _BaseMap ("Bare Material Texture", 2D) = "white" {}
        _BaseColor ("Bare Material Color", Color) = (0.87, 0.66, 0.47, 1)

        [Header(Masks)]
        _WearMask ("Wear Mask: R dust, G dirt, B old paint, A rust", 2D) = "black" {}
        _PaintMask ("Paint Mask: R new paint, G varnish", 2D) = "black" {}
        _MaskUVSet ("Mask UV: 0 = UV0, 1 = UV1", Float) = 1

        [Header(Layer colors)]
        _DustColor ("Dust", Color) = (0.62, 0.56, 0.50, 1)
        _DirtColor ("Dirt", Color) = (0.40, 0.33, 0.26, 1)
        _OldPaintColor ("Old Paint", Color) = (0.78, 0.47, 0.42, 1)
        _RustColor ("Rust", Color) = (0.66, 0.31, 0.16, 1)
        _NewPaintColor ("New Paint", Color) = (0.55, 0.80, 0.75, 1)
        _EdgeLo ("Mask Edge Low", Range(0, 1)) = 0.42
        _EdgeHi ("Mask Edge High", Range(0, 1)) = 0.58

        [Header(Cel shading)]
        _ShadowTint ("Shadow Tint", Color) = (0.557, 0.424, 0.604, 1)
        _ShadowAmount ("Shadow Amount", Range(0, 1)) = 0.38
        _ShadowStep ("Shadow Step", Range(0, 1)) = 0.5
        _ShadowSoft ("Shadow Softness", Range(0.001, 0.5)) = 0.04
        _LightContribution ("Light Color Contribution", Range(0, 1)) = 0.3

        [Header(Glint and rim)]
        _GlintColor ("Glint and Rim Color", Color) = (1, 0.96, 0.9, 1)
        _GlintSize ("Glint Size", Range(0, 1)) = 0.2
        _GlintSoft ("Glint Softness", Range(0, 1)) = 0.1
        _PaintGloss ("Fresh Paint Gloss", Range(0, 1)) = 0.15
        _RimSize ("Rim Size", Range(0, 1)) = 0.45
        _RimSoft ("Rim Softness", Range(0.01, 1)) = 0.3
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.25

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.357, 0.247, 0.290, 1)
        _OutlineWidth ("Outline Width px", Float) = 2.5
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
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _MaskUVSet;
            half4 _DustColor;
            half4 _DirtColor;
            half4 _OldPaintColor;
            half4 _RustColor;
            half4 _NewPaintColor;
            half _EdgeLo;
            half _EdgeHi;
            half4 _ShadowTint;
            half _ShadowAmount;
            half _ShadowStep;
            half _ShadowSoft;
            half _LightContribution;
            half4 _GlintColor;
            half _GlintSize;
            half _GlintSoft;
            half _PaintGloss;
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                half4 w = SAMPLE_TEXTURE2D(_WearMask, sampler_WearMask, i.uv1);
                half2 p = SAMPLE_TEXTURE2D(_PaintMask, sampler_PaintMask, i.uv1).rg;

                half dust = EdgeMask(w.r);
                half dirt = EdgeMask(w.g);
                half oldP = EdgeMask(w.b);
                half rust = EdgeMask(w.a);
                half newP = EdgeMask(p.r);
                half varn = saturate(p.g);

                // снизу вверх: материал → ржавчина → старая краска → новая краска → грязь → пыль
                half3 albedo = bare;
                albedo = lerp(albedo, _RustColor.rgb, rust);
                albedo = lerp(albedo, _OldPaintColor.rgb, oldP);
                albedo = lerp(albedo, _NewPaintColor.rgb, newP);
                albedo = lerp(albedo, _DirtColor.rgb, dirt);
                albedo = lerp(albedo, _DustColor.rgb, dust);

                // насколько поверхность "потускнела": износ гасит блик и рим
                half wear = max(max(dust, dirt), max(oldP, rust) * (1.0h - newP));

                // ---- Cel-освещение ----
                float3 nWS = normalize(i.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndl = dot(nWS, mainLight.direction) * 0.5h + 0.5h;
                half lit = smoothstep(_ShadowStep - _ShadowSoft, _ShadowStep + _ShadowSoft, ndl);
                lit *= mainLight.shadowAttenuation;

                half3 shadedCol = lerp(albedo, _ShadowTint.rgb, _ShadowAmount);
                half3 lightTint = lerp(half3(1, 1, 1), mainLight.color, _LightContribution);
                half3 col = lerp(shadedCol, albedo, lit) * lightTint;

                // ---- Блик: лак + чуть свежей краски, износ гасит ----
                half gloss = saturate(varn + newP * _PaintGloss) * (1.0h - wear);
                float3 h = normalize(mainLight.direction + viewDir);
                half ndh = saturate(dot(nWS, h));
                half thr = 1.0h - _GlintSize * 0.25h;
                half soft = max(0.0005h, _GlintSoft * 0.05h);
                half glint = smoothstep(thr - soft, thr + soft, ndh) * lit * gloss;
                col += _GlintColor.rgb * glint;

                // ---- Рим ----
                half rim = 1.0h - saturate(dot(nWS, viewDir));
                half rimThr = 1.0h - _RimSize;
                rim = smoothstep(rimThr, rimThr + _RimSoft, rim);
                col += _GlintColor.rgb * rim * _RimStrength * (1.0h - wear);

                return half4(col, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        Pass
        {
            Name "Outline"
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
