// Рисует один мазок в маску. Используется через Graphics.Blit(src, dst, material, pass).
// Pass 0: мазок. Pass 1: высыхание (убавляет влажность).
// Маска износа:   R = пыль, G = грязь, B = старая краска, A = ржавчина (слои сверху вниз).
// Маска покрытия: R = новая краска, G = лак, B = влажность.
Shader "Hidden/Restoration/Stamp"
{
    Properties
    {
        _MainTex ("Mask being painted", 2D) = "black" {}
        _GateTex ("Wear mask for gating", 2D) = "black" {}
        _WetTex ("Wetness (paint mask, channel B)", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _GateTex;
            sampler2D _WetTex;
            float2 _BrushUV;
            float _Radius;      // в единицах UV
            float _Hardness;    // 0..0.99, доля радиуса с полной силой
            float _Strength;    // сила за этот мазок (уже умножена на dt)
            float _GateByWear;  // 1 = не наносить краску и лак там, где виден износ
            float _Stack;       // 1 = нижний слой нельзя тереть, пока над ним лежит верхний
            float _Peel;        // 1 = пинцет: берёт только то, что уже отходит (края), толщу не берёт
            float4 _NeedWet;    // по каналам: 1 = слой отмывается только по мокрому
            float4 _Add;        // какие каналы увеличиваем (для маски покрытия: краска, лак, влажность)
            float4 _Sub;        // насколько берём каждый канал

            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 prev = tex2D(_MainTex, i.uv);

                float d = distance(i.uv, _BrushUV) / max(_Radius, 1e-5);
                float a = 1.0 - smoothstep(_Hardness, 1.0, d);
                a *= _Strength;

                // Износ, который виден глазами: пыль, грязь, ржавчина (ржавчина скрыта старой краской)
                float4 w = tex2D(_GateTex, i.uv);
                // Continuous layers remain visible below 0.5 coverage. Do not let
                // paint/varnish pass through a thin dirty film left by cleaning.
                float residue = max(saturate(w.r), max(saturate(w.g), saturate(w.a) * (1.0 - saturate(w.b))));
                float visible = step(0.015, residue);
                float gateV = lerp(1.0, 1.0 - visible, _GateByWear);
                float4 gate = float4(gateV, gateV, 1.0, 1.0);   // ограничение только для краски и лака

                float wet = tex2D(_WetTex, i.uv).b;

                float4 sub = _Sub * a;

                // Слои лежат стопкой: что сверху, то мешает тереть то, что под ним
                if (_Stack > 0.5)
                {
                    float4 above = float4(0.0,
                                          prev.r,
                                          max(prev.r, prev.g),
                                          max(max(prev.r, prev.g), prev.b));
                    sub *= 1.0 - smoothstep(0.45, 0.7, above);
                }

                // Пинцет: плотный слой держится, поддаются только тонкие края
                if (_Peel > 0.5)
                {
                    float2 texel = _MainTex_TexelSize.xy;
                    float4 neighbour = min(min(tex2D(_MainTex, i.uv + float2(texel.x, 0)),
                                               tex2D(_MainTex, i.uv - float2(texel.x, 0))),
                                           min(tex2D(_MainTex, i.uv + float2(0, texel.y)),
                                               tex2D(_MainTex, i.uv - float2(0, texel.y))));
                    float4 exposed = 1.0 - smoothstep(0.45, 0.85, neighbour);
                    sub *= max(1.0 - smoothstep(0.80, 1.0, prev), exposed);
                }

                // Грязь не оттирается всухую
                sub *= lerp(float4(1, 1, 1, 1), float4(wet, wet, wet, wet), _NeedWet);

                float4 r = prev + _Add * a * gate - sub;
                return saturate(r);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragDry
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Decay;       // не меньше 0.004, иначе 8-битная маска не изменится

            fixed4 fragDry(v2f_img i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                c.b = max(0.0, c.b - _Decay);
                return c;
            }
            ENDCG
        }
    }
}
