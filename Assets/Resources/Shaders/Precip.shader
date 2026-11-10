Shader "Fitz/Precip"
{
    Properties
    {
        _Shape ("Shape (0 streak, 1 soft)", Float) = 0
        _Brightness ("Brightness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "FitzCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Shape;
                float _Brightness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = v.color;
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Never inside the wheelhouse.
                float3 ls = mul(_FitzShipW2L, float4(i.positionWS, 1)).xyz;
                if (all(ls > _FitzCabinMin.xyz) && all(ls < _FitzCabinMax.xyz)) discard;

                float2 c = i.uv * 2.0 - 1.0;
                float streak = saturate(1.0 - abs(c.x)) * saturate(1.0 - abs(c.y) * 0.8);
                streak *= streak;
                float soft = saturate(1.0 - dot(c, c));
                soft *= soft;
                float a = lerp(streak, soft, _Shape) * i.color.a;

                float3 ambient = SampleSH(float3(0, 1, 0));
                float3 lit = i.color.rgb * (ambient * 1.5 + GetMainLight().color * 0.3) * _Brightness;
                lit += _FitzFlash * 0.4;
                lit = MixFog(lit, i.fog * 0.5 + 0.5 * saturate(i.fog));
                return half4(lit, a);
            }
            ENDHLSL
        }
    }
}
