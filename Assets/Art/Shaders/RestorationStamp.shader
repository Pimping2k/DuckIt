// Рисует один мазок в маску. Используется через Graphics.Blit(src, dst, material).
// Маска износа:   R = пыль, G = грязь, B = старая краска, A = ржавчина (слои сверху вниз).
// Маска покрытия: R = новая краска, G = лак.
Shader "Hidden/Restoration/Stamp"
{
    Properties
    {
        _MainTex ("Mask being painted", 2D) = "black" {}
        _GateTex ("Wear mask for gating", 2D) = "black" {}
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
            sampler2D _GateTex;
            float2 _BrushUV;
            float _Radius;      // в единицах UV
            float _Hardness;    // 0..0.99, доля радиуса с полной силой
            float _Strength;    // сила за этот мазок (уже умножена на dt)
            float _GateByWear;  // 1 = не наносить там, где виден износ (краска, лак)
            float _Stack;       // 1 = нижний слой нельзя тереть, пока над ним лежит верхний
            float4 _Add;        // какие каналы увеличиваем
            float4 _Sub;        // насколько берём каждый канал

            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 prev = tex2D(_MainTex, i.uv);

                float d = distance(i.uv, _BrushUV) / max(_Radius, 1e-5);
                float a = 1.0 - smoothstep(_Hardness, 1.0, d);
                a *= _Strength;

                // Износ, который виден глазами: пыль, грязь, ржавчина (ржавчина скрыта старой краской)
                float4 w = tex2D(_GateTex, i.uv);
                float visible = max(step(0.5, w.r),
                                max(step(0.5, w.g), step(0.5, w.a) * (1.0 - step(0.5, w.b))));
                float gate = lerp(1.0, 1.0 - visible, _GateByWear);

                // Слои лежат стопкой: что сверху, то мешает тереть то, что под ним
                float4 sub = _Sub * a;
                if (_Stack > 0.5)
                {
                    float4 above = float4(0.0,
                                          prev.r,
                                          max(prev.r, prev.g),
                                          max(max(prev.r, prev.g), prev.b));
                    sub *= 1.0 - smoothstep(0.45, 0.7, above);
                }

                float4 r = prev + _Add * a * gate - sub;
                return saturate(r);
            }
            ENDCG
        }
    }
}
