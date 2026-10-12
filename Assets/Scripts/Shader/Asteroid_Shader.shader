Shader "Custom/Asteroid_Shader"
{
 Properties
    {
        [Header(Layers)]
        [Toggle] _ShowOutline ("Outline", Float) = 1
        [Toggle] _ShowSolid ("Solid", Float) = 0
        [Toggle] _ShowPattern ("Pattern", Float) = 0

        [Header(Colors)]
        [HDR] _LineColor ("Line Color", Color) = (1,1,1,1)
        [HDR] _FillColor ("Solid Color", Color) = (0.15,0.15,0.15,1)
        [HDR] _PatternColor ("Pattern Color", Color) = (0.6,0.6,0.6,1)

        [Header(Lines)]
        _LineWidth ("Line Width (pixels)", Range(0.5, 10)) = 1.5
        [Enum(Vertex Normals,0, Radial,1)] _ExtrudeMode ("Extrude Direction", Float) = 1
        _OutlineDepthBias ("Outline Depth Bias (world units)", Range(0, 0.5)) = 0.05

        [Header(Pattern)]
        [Enum(Stripes,0, Crosshatch,1)] _PatternType ("Pattern Type", Float) = 0
        [Enum(Object Space,0, Screen Space,1)] _PatternSpace ("Pattern Space", Float) = 0
        _PatternAngle ("Angle", Range(0,180)) = 0
        _PatternDensity ("Density", Range(0.5, 100)) = 12
        _PatternThickness ("Thickness", Range(0.05, 0.95)) = 0.35
        _PatternScroll ("Scroll Speed", Range(-5, 5)) = 0

        [Header(Retro Flicker)]
        _Flicker ("Flicker", Range(0, 1)) = 0.3
        _FlickerSpeed ("Flicker Speed", Range(1, 60)) = 25
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _ShowOutline;
            float _ShowSolid;
            float _ShowPattern;
            float4 _LineColor;
            float4 _FillColor;
            float4 _PatternColor;
            float _LineWidth;
            float _ExtrudeMode;
            float _OutlineDepthBias;
            float _PatternType;
            float _PatternSpace;
            float _PatternAngle;
            float _PatternDensity;
            float _PatternThickness;
            float _PatternScroll;
            float _Flicker;
            float _FlickerSpeed;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionOS : TEXCOORD0;
        };

        float FlickerMul()
        {
            float3 w = TransformObjectToWorld(float3(0, 0, 0));
            float t = floor(_Time.y * _FlickerSpeed);
            float n = frac(sin(dot(float2(t, t) + w.xy, float2(12.9898, 78.233))) * 43758.5453);
            return 1.0 - _Flicker * n;
        }

        float StripeLine(float2 p, float angleDeg, float density, float thickness, float scroll)
        {
            float a = radians(angleDeg);
            float2 dir = float2(cos(a), sin(a));
            float c = dot(p, dir) * density + _Time.y * scroll;
            return step(frac(c), thickness);
        }

        float PatternMask(Varyings i)
        {
            float2 p;
            float density = _PatternDensity;
            if (_PatternSpace < 0.5)
            {
                p = i.positionOS.xy + i.positionOS.z * 0.37;
            }
            else
            {
                p = i.positionCS.xy * 0.1;
                density = _PatternDensity * 0.1;
            }

            float m = StripeLine(p, _PatternAngle, density, _PatternThickness, _PatternScroll);
            if (_PatternType > 0.5)
                m = max(m, StripeLine(p, _PatternAngle + 90.0, density, _PatternThickness, _PatternScroll));
            return m;
        }

        Varyings VertFill(Attributes v)
        {
            Varyings o;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.positionOS = v.positionOS.xyz;
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "DepthFill"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertFill
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "FillPattern"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertFill
            #pragma fragment frag

            half4 frag(Varyings i) : SV_Target
            {
                bool solid = _ShowSolid > 0.5;
                bool pattern = _ShowPattern > 0.5;

                if (!solid && !pattern)
                    clip(-1);

                float m = pattern ? PatternMask(i) : 0.0;

                if (!solid)
                    clip(m - 0.5);

                float3 col = (pattern && m > 0.5) ? _PatternColor.rgb : _FillColor.rgb;
                return half4(col * FlickerMul(), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="AsteroidOutline" }
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 dirOS = (_ExtrudeMode < 0.5) ? v.normalOS : normalize(v.positionOS.xyz + 1e-5);
                float3 dirWS = normalize(TransformObjectToWorldDir(dirOS));

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                posWS += UNITY_MATRIX_V[2].xyz * _OutlineDepthBias;
                float4 posCS = TransformWorldToHClip(posWS);

                float2 dirCS = mul((float3x3)UNITY_MATRIX_VP, dirWS).xy;
                float2 dirPx = normalize(dirCS * _ScreenParams.xy + 1e-5);

                posCS.xy += dirPx * _LineWidth * (2.0 / _ScreenParams.xy) * posCS.w;

                o.positionCS = posCS;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                if (_ShowOutline < 0.5)
                    clip(-1);

                return half4(_LineColor.rgb * FlickerMul(), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertFill
            #pragma fragment fragDepth
            half4 fragDepth(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}