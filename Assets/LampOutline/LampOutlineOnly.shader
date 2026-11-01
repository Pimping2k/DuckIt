Shader "Restoration/LampOutlineOnly"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (pixels)", Range(0,6)) = 1.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;};
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                float3 n=TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
                float2 direction=float2(n.x*UNITY_MATRIX_P._m00,n.y*UNITY_MATRIX_P._m11);
                direction*=rsqrt(max(dot(direction,direction),1e-8));
                output.positionCS.xy+=direction*(2.0*_OutlineWidth/_ScaledScreenParams.xy)*output.positionCS.w;
                return output;
            }
            half4 Frag(Varyings input):SV_Target {return _OutlineColor;}
            ENDHLSL
        }
    }
}
