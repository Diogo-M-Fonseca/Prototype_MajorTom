Shader "Custom/Asteroid_Shader"
{
       Properties
    {
        [Enum(Outline,0, Solid,1, Outline And Pattern,2, Solid And Outline,3)] _Mode ("Mode", Float) = 0
        [Enum(All,0, Fill Only,1, Outline Only,2)] _PassFilter ("Pass Filter (use Fill Only + Outline Only em 2 materiais)", Float) = 0

        [Header(Colors)]
        [HDR] _LineColor ("Line Color", Color) = (1,1,1,1)
        [HDR] _FillColor ("Solid Color", Color) = (0.15,0.15,0.15,1)
        [HDR] _PatternColor ("Pattern Color", Color) = (0.6,0.6,0.6,1)

        [Header(Lines)]
        _LineWidth ("Line Width (pixels)", Range(0.5, 10)) = 1.5
        [Enum(Normal,0, Radial,1)] _ExtrudeMode ("Extrude Direction (Radial = no gaps on hard edges)", Float) = 1
        _OutlineDepthBias ("Outline Depth Bias (world units)", Range(0, 0.5)) = 0.02
        [Enum(UnityEngine.Rendering.CompareFunction)] _OutlineZTest ("Outline ZTest (Always = vê-se através de tudo)", Float) = 4

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
            float _Mode;
            float _PassFilter;
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
                int mode = (int)(_Mode + 0.5);
                bool fill = (mode == 1 || mode == 3);
                bool pattern = (mode == 2);

                if ((!fill && !pattern) || _PassFilter > 1.5)
                    clip(-1);

                float flick = FlickerMul();

                if (fill)
                    return half4(_FillColor.rgb * flick, 1);

                clip(PatternMask(i) - 0.5);
                return half4(_PatternColor.rgb * flick, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite Off
            ZTest [_OutlineZTest]
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
                int mode = (int)(_Mode + 0.5);

                if (mode == 1 || (_PassFilter > 0.5 && _PassFilter < 1.5))
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