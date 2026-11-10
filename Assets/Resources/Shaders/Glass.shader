Shader "Fitz/Glass"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.55, 0.62, 0.6, 0.1)
        _Facing ("Facing (1 = into wind)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "FitzCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Facing;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; // metres across the pane
                return o;
            }

            // Returns xy = refraction offset, z = drop coverage.
            float3 StaticDrops(float2 uv, float t, float amount)
            {
                float2 grid = uv * 28.0;
                float2 id = floor(grid);
                float2 f = frac(grid) - 0.5;
                float r = FitzHash(id);
                float life = frac(t * 0.15 + r * 7.0);
                float2 c = (float2(FitzHash(id + 3.1), FitzHash(id + 7.9)) - 0.5) * 0.6;
                float rad = 0.12 + 0.22 * FitzHash(id + 11.3);
                float2 dv = f - c;
                float d = length(dv);
                float m = smoothstep(rad, rad * 0.55, d) * step(r, amount) * smoothstep(1.0, 0.85, life);
                return float3(dv * m * 2.0, m);
            }

            float3 RunningDrops(float2 uv, float t, float amount)
            {
                float2 grid = uv * float2(9.0, 2.0);
                float colId = floor(grid.x);
                float h = FitzHash(float2(colId, 1.7));
                float speed = 0.25 + h * 0.6;
                float y = frac(-t * speed + h * 5.0);  // drop head position within the cell
                float2 f = frac(grid);
                f.x += sin(grid.y * 9.0 + h * 6.0) * 0.06;
                float2 dropPos = float2(0.5, y);
                float2 dv = (f - dropPos) * float2(1.0, 4.5);
                float d = length(dv);
                float head = smoothstep(0.22, 0.1, d);
                float trail = smoothstep(0.05, 0.0, abs(f.x - 0.5)) * step(y, f.y) * smoothstep(1.0, 0.0, f.y - y) * 0.6;
                float on = step(h, amount * 0.9);
                float m = (head + trail) * on;
                return float3(dv * head * on * 0.5, saturate(m));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float wet = saturate(_FitzRain * 0.85 + _FitzSpray * 0.6);
                float t = _FitzTime;
                float3 a = StaticDrops(i.uv, t, wet);
                float3 b = StaticDrops(i.uv * 1.7 + 4.2, t * 1.3, wet * 0.8);
                float3 c = RunningDrops(i.uv, t, wet);
                float2 offs = a.xy + b.xy * 0.7 + c.xy;
                float cover = saturate(a.z + b.z + c.z);

                // A spray hit leaves a sheet of water that blurs everything for a moment.
                float sheet = _FitzSpray * _Facing;
                float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                float wob = (FitzNoise(i.uv * 6.0 + t * 2.0) - 0.5) * sheet * 0.03;
                float2 ruv = suv + offs * 0.035 + wob;
                float3 scene = SampleSceneColor(ruv);
                float3 sceneB = SampleSceneColor(ruv + float2(0.004, 0.003) * sheet);
                scene = lerp(scene, (scene + sceneB) * 0.5, sheet);

                float3 col = scene * (1.0 + cover * 0.15) * lerp(float3(1, 1, 1), _Tint.rgb * 1.6, 0.25);
                col += cover * 0.015 + _FitzFlash * 0.05;
                float alpha = saturate(_Tint.a + cover * 0.9 + sheet * 0.7);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
