// Cheap gradient night sky for the skybox: zenith → horizon blend plus a faint warm glow band near the
// horizon. Stars are a separate static mesh, so the per-pixel cost stays tiny on Quest.
Shader "SecretOfHegra/NightSky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.03, 0.04, 0.10, 1)
        _HorizonColor ("Horizon", Color) = (0.16, 0.20, 0.36, 1)
        _GroundColor ("Below horizon", Color) = (0.05, 0.05, 0.09, 1)
        _GlowColor ("Horizon glow", Color) = (0.35, 0.22, 0.20, 1)
        _Exponent ("Gradient exponent", Range(0.2, 4)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _GlowColor;
                half _Exponent;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 d = normalize(i.dir);
                float up = d.y;
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(up), _Exponent));
                half glow = exp(-abs(up) * 18.0);
                sky += _GlowColor.rgb * glow * 0.35;
                half3 col = up >= 0 ? sky : lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-up * 6.0));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
