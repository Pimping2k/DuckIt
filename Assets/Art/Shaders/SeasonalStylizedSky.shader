
Shader "PawPaw/Sky/Seasonal Stylized Sky"
{
    Properties
    {
        _ZenithColor ("Zenith Color", Color) = (0.12, 0.35, 0.58, 1)
        _MidColor ("Mid Sky Color", Color) = (0.35, 0.62, 0.78, 1)
        _HorizonColor ("Horizon Color", Color) = (0.85, 0.88, 0.82, 1)
        _GroundColor ("Below Horizon Color", Color) = (0.2, 0.3, 0.28, 1)
        _GradientPower ("Gradient Power", Range(0.1, 3)) = 1.2

        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
        _SunColor ("Sun Color", Color) = (1, 0.92, 0.75, 1)
        _SunSize ("Sun Angular Radius", Range(0.005, 0.2)) = 0.035
        _SunGlow ("Sun Glow", Range(0, 10)) = 2.5
        _SunGlowTightness ("Sun Glow Tightness", Range(2, 128)) = 12.0

        _HorizonGlowColor ("Twilight Band Color", Color) = (0, 0, 0, 1)
        _HorizonGlowWidth ("Twilight Band Height", Range(0.05, 0.8)) = 0.22

        _MoonColor ("Moon Color", Color) = (0.9, 0.94, 1, 1)
        _MoonSize ("Moon Angular Radius", Range(0.005, 0.2)) = 0.04
        _MoonPhase ("Moon Phase", Range(0, 1)) = 0.2

        _StarsIntensity ("Stars Intensity", Range(0, 5)) = 2.0
        _StarsDensity ("Stars Density", Range(20, 400)) = 180

        // Procedural Clouds
        _CloudColor ("Cloud Light Color", Color) = (1.0, 0.78, 0.62, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.32, 0.25, 0.38, 1)
        _CloudCoverage ("Cloud Coverage", Range(0.25, 0.75)) = 0.43
        _CloudScale ("Cloud Scale", Range(1, 12)) = 4.0
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0.85
        _CloudSpeed ("Cloud Speed", Range(0, 0.1)) = 0.008
        _CloudDetail ("Cloud Detail", Range(0, 1)) = 0.25

        _SkyTime ("Sky Time (driven by script)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            float4 _ZenithColor;
            float4 _MidColor;
            float4 _HorizonColor;
            float4 _GroundColor;
            float _GradientPower;

            float4 _SunDir;
            float4 _SunColor;
            float _SunSize;
            float _SunGlow;
            float _SunGlowTightness;

            float4 _HorizonGlowColor;
            float _HorizonGlowWidth;

            float4 _MoonColor;
            float _MoonSize;
            float _MoonPhase;

            float _StarsIntensity;
            float _StarsDensity;

            float4 _CloudColor;
            float4 _CloudShadowColor;
            float _CloudCoverage;
            float _CloudScale;
            float _CloudOpacity;
            float _CloudSpeed;
            float _CloudDetail;

            float _SkyTime;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            // Hash functions
            float hash13(float3 p3)
            {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            float3 hash33(float3 p3)
            {
                p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yxx) * p3.zyx);
            }

            // Smooth 3D value noise
            float cloudNoise(float3 p)
            {
                float3 id = floor(p);
                float3 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(id + float3(0, 0, 0));
                float n100 = hash13(id + float3(1, 0, 0));
                float n010 = hash13(id + float3(0, 1, 0));
                float n110 = hash13(id + float3(1, 1, 0));

                float n001 = hash13(id + float3(0, 0, 1));
                float n101 = hash13(id + float3(1, 0, 1));
                float n011 = hash13(id + float3(0, 1, 1));
                float n111 = hash13(id + float3(1, 1, 1));

                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);

                float y0 = lerp(x00, x10, f.y);
                float y1 = lerp(x01, x11, f.y);

                return lerp(y0, y1, f.z);
            }

            // Fractal Brownian Motion
            float cloudFBM(float3 p)
            {
                float n = cloudNoise(p) * 0.5714;
                n += cloudNoise(p * 2.0) * 0.2857;
                n += cloudNoise(p * 4.0) * 0.1429;

                return n;
            }

            // Angular disc for the sun and moon
            float disc(float cosAngle, float radius)
            {
                float ang = acos(clamp(cosAngle, -1.0, 1.0));
                return 1.0 - smoothstep(radius * 0.85, radius, ang);
            }

            // Procedural stylized clouds
            float getClouds(float3 dir, float sunFacing, out float3 cloudCol)
            {
                float y = dir.y;

                cloudCol = _CloudColor.rgb;

                if (y <= -0.05 || _CloudOpacity <= 0.001)
                    return 0.0;

                // Spherical sky projection
                float3 cloudP = dir * _CloudScale;

                // Wind animation
                cloudP += float3(
                    _SkyTime * _CloudSpeed,
                    0.0,
                    _SkyTime * _CloudSpeed * 0.35
                );

                // Large cloud masses
                float baseNoise = cloudFBM(cloudP);

                // Smaller detail breaks up the cloud edges
                float detailNoise = cloudNoise(cloudP * 2.0 + 17.3);

                float cloudShape = lerp(
                    baseNoise,
                    baseNoise * 0.8 + detailNoise * 0.2,
                    _CloudDetail
                );

                // Soft cloud silhouettes
                float clouds = smoothstep(
                    _CloudCoverage,
                    _CloudCoverage + 0.14,
                    cloudShape
                );

                // Fade near and below the horizon
                float horizonFade = smoothstep(-0.05, 0.12, y);

                clouds *= horizonFade * _CloudOpacity;

                // Sunlit and shadowed cloud colors
                float lighting = saturate(0.25 + sunFacing * 0.85);

                cloudCol = lerp(
                    _CloudShadowColor.rgb,
                    _CloudColor.rgb,
                    lighting
                );

                // Warm sunset tint close to the horizon
                float sunset = exp(-abs(y) * 5.0);

                cloudCol = lerp(
                    cloudCol,
                    cloudCol * 0.65 + _SunColor.rgb * 0.55,
                    sunset * sunFacing * 0.65
                );

                return clouds;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float y = dir.y;

                float3 sunDir = normalize(_SunDir.xyz);
                float3 moonDir = -sunDir;

                float sd = dot(dir, sunDir);

                float2 hdir = dir.xz / max(length(dir.xz), 1e-4);
                float2 sh = sunDir.xz / max(length(sunDir.xz), 1e-4);

                // 1. Sky gradient
                float yy = pow(saturate(y), _GradientPower);

                float3 col = lerp(
                    _HorizonColor.rgb,
                    _MidColor.rgb,
                    smoothstep(0.0, 0.35, yy)
                );

                col = lerp(
                    col,
                    _ZenithColor.rgb,
                    smoothstep(0.25, 0.95, yy)
                );

                // 2. Ground color below horizon
                col = lerp(
                    col,
                    _GroundColor.rgb,
                    smoothstep(0.0, 0.3, -y)
                );

                // 3. Sun halo
                float halo = saturate(
                    pow(saturate(sd * 0.5 + 0.5), 4.0)
                    * 0.5
                    * _SunGlow
                );

                float haloFade = exp(-pow(abs(y) / 0.4, 2.0));

                col = lerp(
                    col,
                    _SunColor.rgb,
                    halo * haloFade
                );

                // 4. Twilight band
                float azimuth = pow(
                    saturate(dot(sh, hdir) * 0.5 + 0.5),
                    2.5
                );

                float band = exp(
                    -pow(abs(y) / _HorizonGlowWidth, 2.0)
                );

                col += _HorizonGlowColor.rgb * band * azimuth;

                // 5. Procedural clouds
                float sunFacing = saturate(sd * 0.5 + 0.5);

                float3 cloudCol;
                float cloudAmount = getClouds(
                    dir,
                    sunFacing,
                    cloudCol
                );

                col = lerp(col, cloudCol, cloudAmount);

                // 6. Night stars
                float nightFactor = smoothstep(
                    -0.02,
                    -0.2,
                    sunDir.y
                );

                if (nightFactor > 0.01 && y > 0.02)
                {
                    float3 sp = dir * _StarsDensity;
                    float3 cell = floor(sp);
                    float3 local = frac(sp) - 0.5;

                    float3 jitter = (hash33(cell) - 0.5) * 0.4;
                    float rnd = hash13(cell + 7.7);

                    float starPattern = step(0.965, rnd);

                    float starPoint =
                        (1.0 - smoothstep(
                            0.0,
                            0.18,
                            length(local - jitter)
                        )) * starPattern;

                    float twinkle = 0.6 + 0.4 * sin(
                        _SkyTime * (2.0 + rnd * 4.0)
                        + rnd * 50.0
                    );

                    col += _MoonColor.rgb
                        * starPoint
                        * twinkle
                        * _StarsIntensity
                        * nightFactor
                        * smoothstep(0.02, 0.25, y);
                }

                // 7. Moon
                float moonFade = smoothstep(
                    -0.05,
                    0.2,
                    -sunDir.y
                );

                float md = dot(dir, moonDir);
                float moonBody = disc(md, _MoonSize);

                float3 moonRight = normalize(
                    cross(moonDir, float3(0, 1, 0))
                    + float3(1e-4, 0, 0)
                );

                float3 moonCut = normalize(
                    moonDir
                    + moonRight * (_MoonPhase * _MoonSize * 1.5)
                );

                float cutAmount = smoothstep(
                    0.0,
                    0.1,
                    _MoonPhase
                );

                float moonShape = moonBody * (
                    1.0 - disc(dot(dir, moonCut), _MoonSize)
                    * cutAmount
                );

                float moonGlow = pow(saturate(md), 45.0) * 0.4;

                col += _MoonColor.rgb
                    * (moonShape * 1.6 + moonGlow)
                    * moonFade;

                // 8. Sun disc
                float sunBody = disc(sd, _SunSize);

                float sunGlow = pow(
                    saturate(sd),
                    _SunGlowTightness
                ) * _SunGlow * 0.25;

                col += _SunColor.rgb * (
                    sunBody * 2.5
                    + sunGlow * smoothstep(-0.1, 0.15, y)
                );

                return float4(col, 1.0);
            }

            ENDCG
        }
    }

    Fallback Off
}
