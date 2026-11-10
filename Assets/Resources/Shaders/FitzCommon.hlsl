#ifndef FITZ_COMMON_INCLUDED
#define FITZ_COMMON_INCLUDED

// Globals pushed every frame by Ocean.cs / Weather.cs. The C# side mirrors the
// wave maths in Ocean.SampleHeight so the ship rides the surface you see.
#define FITZ_MAX_WAVES 10
float4 _FitzWaveA[FITZ_MAX_WAVES]; // dir.x, dir.y, k (wavenumber), amplitude
float4 _FitzWaveB[FITZ_MAX_WAVES]; // omega, phase, steepness Q, unused
float _FitzWaveCount;
float _FitzTime;
float4 _FitzOceanOffset;  // xy = navigation offset in metres (floating origin)
float4 _FitzWind;         // xy = direction waves travel, z = storm 0..1, w = foam amount
float4 _FitzSkyTop;
float4 _FitzSkyHorizon;
float4 _FitzSunDir;       // xyz towards sun, w = daylight 0..1
float _FitzFlash;
float _FitzRain;          // 0..1 wetness of glass
float _FitzSpray;         // 0..1 recent spray on glass
float4x4 _FitzShipW2L;
float4 _FitzCabinMin;
float4 _FitzCabinMax;

float FitzHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float FitzNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = FitzHash(i);
    float b = FitzHash(i + float2(1, 0));
    float c = FitzHash(i + float2(0, 1));
    float d = FitzHash(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float FitzFbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    [unroll] for (int i = 0; i < 5; i++)
    {
        v += a * FitzNoise(p);
        p = p * 2.03 + float2(17.1, 9.7);
        a *= 0.5;
    }
    return v;
}

float3 FitzSkyColor(float3 d)
{
    float h = d.y;
    float3 col = lerp(_FitzSkyHorizon.rgb, _FitzSkyTop.rgb, saturate(h * 2.2));
    // faint glow where the sun is hiding behind the overcast
    float sun = saturate(dot(normalize(d), _FitzSunDir.xyz));
    col += pow(sun, 6.0) * 0.18 * _FitzSunDir.w * float3(1.0, 0.85, 0.7);
    col += _FitzFlash * float3(0.6, 0.65, 0.7);
    return col;
}

#endif
