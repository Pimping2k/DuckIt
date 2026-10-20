#ifndef SURFACE_WIPE_INCLUDED
#define SURFACE_WIPE_INCLUDED
float PHHash(float3 p) { p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z); }
float PHNoise(float3 p)
{
    float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(lerp(PHHash(a),PHHash(a+float3(1,0,0)),f.x),lerp(PHHash(a+float3(0,1,0)),PHHash(a+float3(1,1,0)),f.x),f.y),
                lerp(lerp(PHHash(a+float3(0,0,1)),PHHash(a+float3(1,0,1)),f.x),lerp(PHHash(a+float3(0,1,1)),PHHash(a+1),f.x),f.y),f.z);
}
half3 PHBrush(half3 color,float3 p)
{
    // Optional stand-in for a clean wood BaseMap in this isolated preview.
    float bend=PHNoise(p*float3(.55,1,7));
    float grain=sin(p.z*95+bend*9+p.x*.25)*.5+.5;
    float thin=pow(saturate(grain),8);
    float fiber=PHNoise(p*float3(1.5,1,160));
    float tone=1-thin*.10+(fiber-.5)*.10;
    return color*lerp(1,tone,_WoodGrain);
}
struct WearColors { half3 dust,dirt,oldPaint,rust,substrate; half dustOpacity,detailStrength,texturedCoating,dustRagged,cleanGlow; };
struct WearNoise { half broad,mid,fine; };
WearNoise WearNoiseAt(float3 p)
{
    WearNoise n;p/=30;n.broad=PHNoise(p*3);n.mid=PHNoise(p*9);n.fine=.5;return n;
}
half3 WearBelow(half3 base,half4 w,WearNoise n,WearColors c,out half rustA,out half paintA)
{
    // This study intentionally has no damaged/peeling surface.
    rustA=0;paintA=0;return base;
}
half3 WearAbove(half3 base,half4 w,WearNoise n,WearColors c,out half dirtA,out half dustA)
{
    // The mask is continuous coverage, not an erosion threshold. The tool's
    // exact soft footprint reveals the unchanged clean material beneath.
    dustA=saturate(w.r);dirtA=saturate(w.g);
    half density=max(dustA,dirtA);
    half luminance=dot(base,half3(.2126,.7152,.0722));
    half3 muted=lerp(base,luminance.xxx,_GrimeDesaturation);
    half3 dirt=_GrimeColor.rgb*(.92h+n.broad*.12h);
    half3 covered=lerp(muted*.60h,dirt,_GrimeOpacity);
    return lerp(base,covered,density);
}
#endif
