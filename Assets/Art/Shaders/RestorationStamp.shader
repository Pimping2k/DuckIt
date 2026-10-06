// Рисует один мазок в маску состояния. Используется через Graphics.Blit(src, dst, material).
// Каналы: R = грязь, G = старая краска, B = новая краска, A = лак.
Shader "Hidden/Restoration/Stamp"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "black" {}
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
            float2 _BrushUV;
            float _Radius;      // в единицах UV
            float _Hardness;    // 0..0.99, доля радиуса с полной силой
            float _Strength;    // сила за этот мазок (уже умножена на dt)
            float _GateByDirt;  // 1 = не рисовать там, где есть грязь (краска, лак)
            float4 _Add;        // какие каналы увеличиваем
            float4 _Sub;        // какие каналы уменьшаем

            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 prev = tex2D(_MainTex, i.uv);

                float d = distance(i.uv, _BrushUV) / max(_Radius, 1e-5);
                float a = 1.0 - smoothstep(_Hardness, 1.0, d);
                a *= _Strength;

                // по грязи краску и лак не наносим
                float gate = lerp(1.0, 1.0 - step(0.5, prev.r), _GateByDirt);

                float4 r = prev + _Add * a * gate - _Sub * a;
                return saturate(r);
            }
            ENDCG
        }
    }
}
