Shader "Fitz/Ocean"
{
    Properties
    {
        _DeepColor ("Deep", Color) = (0.015, 0.045, 0.05, 1)
        _ScatterColor ("Scatter", Color) = (0.05, 0.2, 0.17, 1)
        _FoamColor ("Foam", Color) = (0.86, 0.9, 0.92, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Name "OceanForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "FitzCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ScatterColor;
                float4 _FoamColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 nav        : TEXCOORD2;
                float2 crestFog   : TEXCOORD3;
                float height      : TEXCOORD4;
            };

            void Gerstner(float2 p, out float3 disp, out float3 nrm, out float crest)
            {
                disp = 0;
                float3 n = float3(0, 1, 0);
                crest = 0;
                int count = (int)_FitzWaveCount;
                [loop] for (int i = 0; i < FITZ_MAX_WAVES; i++)
                {
                    if (i >= count) break;
                    float4 a = _FitzWaveA[i];
                    float4 b = _FitzWaveB[i];
                    float f = a.z * dot(a.xy, p) - b.x * _FitzTime + b.y;
                    float s, c;
                    sincos(f, s, c);
                    float amp = a.w;
                    float q = b.z;
                    disp.x += q * amp * a.x * c;
                    disp.z += q * amp * a.y * c;
                    disp.y += amp * s;
                    n.x -= a.x * a.z * amp * c;
                    n.z -= a.y * a.z * amp * c;
                    n.y -= q * a.z * amp * s;
                    crest += q * a.z * amp * s;
                }
                nrm = normalize(n);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float2 nav = ws.xz + _FitzOceanOffset.xy;
                float3 disp, n;
                float crest;
                Gerstner(nav, disp, n, crest);
                ws += disp;
                o.positionWS = ws;
                o.normalWS = n;
                o.nav = nav + disp.xz;
                o.height = disp.y;
                o.positionCS = TransformWorldToHClip(ws);
                o.crestFog = float2(crest, ComputeFogFactor(o.positionCS.z));
                return o;
            }

            float2 DetailSlope(float2 p, float t, float storm)
            {
                // short chop the vertex grid can't carry
                float2 g = 0;
                float2 wind = normalize(_FitzWind.xy);
                float2 perp = float2(-wind.y, wind.x);
                [unroll] for (int i = 0; i < 7; i++)
                {
                    float fi = i;
                    float ang = (FitzHash(float2(fi, 3.1)) - 0.5) * 2.2;
                    float2 d = normalize(wind * cos(ang) + perp * sin(ang));
                    float lambda = 2.3 + fi * fi * 1.1 + fi * 1.7;
                    float k = 6.2831853 / lambda;
                    float w = sqrt(9.81 * k);
                    float amp = lambda * 0.022 * (0.4 + storm);
                    float f = k * dot(d, p) - w * t + FitzHash(float2(fi, 7.7)) * 6.28;
                    g += d * k * amp * cos(f);
                }
                return g;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                float storm = _FitzWind.z;
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float dist = length(_WorldSpaceCameraPos - i.positionWS);
                float detailFade = saturate(1.0 - dist / 160.0);
                float2 g = DetailSlope(i.nav, _FitzTime, storm) * detailFade;
                float3 N = normalize(i.normalWS + float3(-g.x, 0, -g.y));
                if (!front) N = -N;

                float daylight = _FitzSunDir.w;
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                float3 lightCol = mainLight.color;

                float ndv = saturate(dot(N, V));
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndv, 5.0);
                fresnel = min(fresnel, 0.75);

                float3 R = reflect(-V, N);
                R.y = abs(R.y);
                float3 sky = FitzSkyColor(R);

                float3 ambient = SampleSH(N);
                float waveLift = saturate(i.height * 0.15 + 0.4);
                float sss = pow(saturate(dot(V, -L) * 0.5 + 0.5), 3.0) * waveLift;
                float3 body = _DeepColor.rgb * (ambient * 2.0 + lightCol * 0.25)
                            + _ScatterColor.rgb * (sss * lightCol * 0.6 + ambient * waveLift * 0.8);

                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), 220.0) * 0.6;

                float3 col = lerp(body, sky, fresnel) + spec * lightCol;

                // Foam: whitecaps on the crests plus long streaks down the wind.
                float2 wind = normalize(_FitzWind.xy);
                float2 perp = float2(-wind.y, wind.x);
                float2 q = float2(dot(i.nav, wind), dot(i.nav, perp));
                float drift = _FitzTime * 1.5;
                float n1 = FitzFbm(q * float2(0.03, 0.09) + float2(-drift * 0.03, 0));
                float n2 = FitzFbm(q * float2(0.35, 0.35) + float2(-drift * 0.2, 0));
                float crest = i.crestFog.x;
                float foamAmt = _FitzWind.w;
                float caps = smoothstep(0.75 - foamAmt * 0.45, 1.05, crest + (n2 - 0.5) * 0.5);
                float streaks = smoothstep(0.58, 0.72, n1) * smoothstep(0.35, 0.65, n2) * foamAmt * 0.7;
                float foam = saturate(caps + streaks);
                float3 foamLit = _FoamColor.rgb * (ambient * 1.6 + lightCol * saturate(dot(N, L)) * 0.6);
                col = lerp(col, foamLit, foam);

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < lightCount; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    float atten = l.distanceAttenuation * l.shadowAttenuation;
                    float3 h2 = normalize(l.direction + V);
                    float diff = saturate(dot(N, l.direction));
                    float3 surf = lerp(_ScatterColor.rgb * 0.5, _FoamColor.rgb, foam);
                    col += l.color * atten * (diff * surf + pow(saturate(dot(N, h2)), 120.0) * 0.8);
                }
                #endif

                col += _FitzFlash * 0.25;
                col = MixFog(col, i.crestFog.y);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
