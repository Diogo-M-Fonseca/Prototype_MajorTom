Shader "Custom/Asteroid_Shader"
{
    Properties
    {
        [HDR] _LineColor ("Line Color", Color) = (1,1,1,1)
        _LineWidth ("Line Width (pixels)", Range(0.5, 10)) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM

            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float _LineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct V2g { float4 pos : SV_POSITION; };
            struct G2f { float4 pos : SV_POSITION; float3 bary : TEXCOORD0; };

            V2g vert(Attributes IN)
            {
                V2g o;
                o.pos = TransformObjectToHClip(IN.positionOS.xyz);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle V2g IN[3], inout TriangleStream<G2f> stream)
            {
                G2f o;
                o.pos = IN[0].pos; o.bary = float3(1,0,0); stream.Append(o);
                o.pos = IN[1].pos; o.bary = float3(0,1,0); stream.Append(o);
                o.pos = IN[2].pos; o.bary = float3(0,0,1); stream.Append(o); 
            }

            half4 frag(G2f i) : SV_Target
            {
                float3 d = fwidth(i.bary);
                float3 a = smoothstep(float3(0,0,0), d * _LineWidth, i.bary);
                float edge = 1.0 - min(a.x, min(a.y, a.z));
                clip(edge - 0.01);
                return half4(_LineColor.rgb, _LineColor.a * edge);
            }
            ENDHLSL
        }
    }
}
