Shader "CozyDelivery/SoftGlass"
{
    Properties
    {
        _BaseColor("Glass Tint / Center Opacity", Color) = (0.62,0.84,0.78,0.06)
        _EdgeColor("Soft Reflection Color", Color) = (0.91,0.98,0.92,1)
        _EdgeOpacity("Edge Opacity", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half _EdgeOpacity;
            CBUFFER_END
            struct Input { float4 p:POSITION; float3 n:NORMAL; };
            struct Output { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            Output vert(Input i) { Output o; o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);return o; }
            half4 frag(Output i):SV_Target
            {
                float3 n=normalize(i.normal),v=normalize(GetWorldSpaceViewDir(i.world));
                half edge=pow(1-abs(dot(n,v)),2.7);
                half sheen=pow(abs(dot(n,normalize(float3(-0.35,0.75,-0.55)))),28)*0.12;
                half alpha=saturate(_BaseColor.a+edge*_EdgeOpacity+sheen);
                return half4(lerp(_BaseColor.rgb,_EdgeColor.rgb,saturate(edge+sheen*3)),alpha);
            }
            ENDHLSL
        }
    }
}
