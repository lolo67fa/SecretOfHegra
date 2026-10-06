// Lightweight animated water for the stone channels: an unlit strip with scrolling ripples along UV.y.
// No fluid simulation, no transparency sorting cost (opaque), stereo-instancing safe.
Shader "SecretOfHegra/WaterFlow"
{
    Properties
    {
        _Color ("Water", Color) = (0.18, 0.45, 0.62, 1)
        _Highlight ("Ripple highlight", Color) = (0.55, 0.85, 1, 1)
        _Speed ("Flow speed", Float) = 0.8
        _Frequency ("Ripple frequency", Float) = 6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Highlight;
                half _Speed;
                half _Frequency;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y * _Speed;
                half r = sin((i.uv.y - t) * _Frequency * 6.2831) * 0.5 + 0.5;
                half r2 = sin((i.uv.y * 1.7 - t * 1.3 + i.uv.x * 2.0) * _Frequency * 6.2831) * 0.5 + 0.5;
                half k = smoothstep(0.75, 1.0, r * r2);
                half3 col = lerp(_Color.rgb, _Highlight.rgb, k);
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
