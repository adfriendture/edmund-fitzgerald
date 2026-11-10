Shader "Fitz/Sky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FitzCommon.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 col = FitzSkyColor(d);
                if (d.y > 0.0)
                {
                    // Low scudding cloud deck racing downwind.
                    float2 p = d.xz / (d.y + 0.08) * 1.4;
                    float2 wind = normalize(_FitzWind.xy);
                    p -= wind * _FitzTime * (0.02 + 0.05 * _FitzWind.z);
                    float c = FitzFbm(p);
                    float c2 = FitzFbm(p * 2.7 + 3.3);
                    float shade = saturate((c * 0.7 + c2 * 0.3 - 0.35) * 1.8);
                    float horizonBlend = saturate(d.y * 4.0);
                    float3 dark = _FitzSkyTop.rgb * 0.55;
                    float3 light = lerp(_FitzSkyHorizon.rgb, _FitzSkyTop.rgb, 0.4) * 1.25;
                    float3 clouds = lerp(light, dark, shade);
                    col = lerp(col, clouds, horizonBlend * 0.85);
                    col += _FitzFlash * 0.5;
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
