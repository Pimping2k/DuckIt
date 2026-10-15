#ifndef FIREWATCH_LIGHTING_INCLUDED
#define FIREWATCH_LIGHTING_INCLUDED

// Общее освещение для всех шейдеров игры. Подключать после Core.hlsl и Lighting.hlsl.
// Мягкие тени цветом, окружение из градиента сцены (Environment Lighting), лампы, затемнение у пола.
// Написано под URP 17 (Unity 6000.0) с Forward+.

struct FWParams
{
    half3 shadowTint;     // цвет теней (лавандовый)
    half shadowAmount;    // насколько тень тянется к shadowTint
    half shadowStep;      // где проходит граница света и тени
    half shadowSoft;      // мягкость границы (чем больше, тем размытее, "нарисованнее")
    half shadingBlend;    // 0 = плавный объём, 1 = плоские toon-зоны
    half lightMix;        // насколько цвет солнца красит материал
    half skyAmbient;      // сила окраски теней цветом неба и земли
    half lampBoost;       // сила ламп и других источников света
    half floorY;          // высота пола в мире
    half floorHeight;     // на какой высоте затемнение пропадает
    half floorDark;       // яркость у пола (1 = без затемнения)
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

half3 FWShade(half3 albedo, float3 nWS, float3 posWS, float4 posCS, FWParams P,
              out half lit, out Light mainLight)
{
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(posWS));
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(posWS);
    #endif
    mainLight = GetMainLight(shadowCoord);

    half ndl = dot(nWS, mainLight.direction) * 0.5h + 0.5h;
    lit = FWBand(ndl, P.shadowStep, P.shadowSoft, P.shadingBlend)
        * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

    // Тень: тянется к фиолетовому и окрашивается градиентом окружения (небо сверху, земля снизу)
    half3 amb = SampleSH(nWS);
    half3 shadowCol = albedo * lerp(half3(1, 1, 1), P.shadowTint * 1.4h, P.shadowAmount);
    shadowCol *= lerp(1.0h, saturate(amb * 2.0h), P.skyAmbient);

    half3 lightTint = lerp(half3(1, 1, 1), mainLight.color, P.lightMix);
    half3 col = lerp(shadowCol, albedo, lit) * lightTint;

    // Лампы и прочие источники света
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

    // Затемнение у пола: комната "прижимается" к земле
    half g = saturate((posWS.y - P.floorY) / max(0.01h, P.floorHeight));
    col *= lerp(P.floorDark, 1.0h, g);

    return col;
}

#endif
