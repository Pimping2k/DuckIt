#ifndef PAINTERLY_LIGHTING_INCLUDED
#define PAINTERLY_LIGHTING_INCLUDED

// Общее освещение + туман. Подключать после Core.hlsl и Lighting.hlsl. URP 17, Forward+.

// Глобальные параметры. Выставляет FWAtmosphere.cs
float4 _FWFogNear;    // цвет тумана вблизи
float4 _FWFogFar;     // цвет тумана вдали (он же горизонт неба)
float4 _FWSunGlow;    // подсветка тумана в сторону солнца
float4 _FWFogParams;  // x=start, y=end, z=высота базы, w=спад по высоте
float4 _FWSunDir;     // направление НА солнце (из твоего неба)

struct FWParams
{
    half3 shadowTint;
    half shadowAmount;
    half shadowStep;
    half shadowSoft;
    half shadingBlend;
    half lightMix;
    half skyAmbient;
    half lampBoost;
    float floorY;
    half floorHeight;
    half floorDark;
};

half FWBand(half ndl01, half stepV, half soft, half blend)
{
    return lerp(ndl01, smoothstep(stepV - soft, stepV + soft, ndl01), blend);
}

half3 FWAdditional(Light l, float3 n, FWParams P)
{
    half ndl = dot(n, l.direction) * 0.5h + 0.5h;
    half b = FWBand(ndl, P.shadowStep, P.shadowSoft, P.shadingBlend);
    return l.color * (b * l.distanceAttenuation * l.shadowAttenuation);
}

// Гранёные нормали из производных позиции (amount: 0 = как в меше, 1 = low-poly)
float3 FWFlatNormal(float3 posWS, float3 nWS, half amount)
{
    float3 f = normalize(cross(ddy(posWS), ddx(posWS)));
    f = dot(f, nWS) < 0 ? -f : f;
    return normalize(lerp(nWS, f, amount));
}

// Атмосферный туман: дистанция + высота + свечение к солнцу
half3 FWFog(half3 col, float3 posWS)
{
    if (_FWFogParams.y <= 0.0) return col;

    float3 toP = posWS - _WorldSpaceCameraPos;
    float dist = length(toP);
    float3 v = toP / max(dist, 0.001);

    float f = saturate((dist - _FWFogParams.x) / max(_FWFogParams.y - _FWFogParams.x, 0.001));
    f = f * f * (3.0 - 2.0 * f);

    float h = saturate((posWS.y - _FWFogParams.z) * _FWFogParams.w);
    f *= lerp(1.0, 0.35, h);

    half3 fogCol = lerp(_FWFogNear.rgb, _FWFogFar.rgb, f);
    half sun = pow(saturate(dot(v, _FWSunDir.xyz)), 4.0);
    fogCol += _FWSunGlow.rgb * sun * f;

    return lerp(col, fogCol, f);
}

half3 FWShade(half3 albedo, float3 nWS, float3 posWS, float4 posCS, FWParams P,
              out half lit, out Light mainLight)
{
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(posWS));
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(posWS);
    #endif
    mainLight = GetMainLight(shadowCoord);

    half ndl = saturate((dot(nWS, mainLight.direction) + 0.12h) / 1.12h);
    lit = smoothstep(P.shadowStep - P.shadowSoft, P.shadowStep + P.shadowSoft, ndl)
        * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

    half3 amb = SampleSH(nWS);
    half3 shadowCol = albedo * lerp(half3(1, 1, 1), P.shadowTint * 1.05h, P.shadowAmount);
    shadowCol *= lerp(1.0h, saturate(amb * 2.0h), P.skyAmbient);

    half3 lightTint = lerp(half3(1, 1, 1), mainLight.color, P.lightMix);
    half3 col = shadowCol + (albedo * lightTint - shadowCol) * lit;

    half3 add = 0;
    #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
        InputData inputData = (InputData)0;
        inputData.positionWS = posWS;
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(posCS);
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
        [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
        {
            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
            Light dl = GetAdditionalLight(lightIndex, posWS, half4(1, 1, 1, 1));
            add += FWAdditional(dl, nWS, P);
        }
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light al = GetAdditionalLight(lightIndex, posWS, half4(1, 1, 1, 1));
            add += FWAdditional(al, nWS, P);
        LIGHT_LOOP_END
    #endif
    col += albedo * add * P.lampBoost;

    half g = saturate((posWS.y - P.floorY) / max(0.01h, P.floorHeight));
    col *= lerp(P.floorDark, 1.0h, g);

    return col;
}

#endif


