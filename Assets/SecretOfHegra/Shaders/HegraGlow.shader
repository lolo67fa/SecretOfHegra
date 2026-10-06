// Additive radial glow with no texture: lantern halos, light pools on the ground, fire, stars and markers.
// Used instead of extra realtime lights (the clay lamp is the only realtime light).
Shader "SecretOfHegra/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (0.92, 0.70, 0.31, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _Falloff ("Falloff", Range(0.5, 8)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Falloff;
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
                half r = saturate(1.0 - length(i.uv * 2.0 - 1.0));
                half a = pow(r, _Falloff) * _Intensity;
                half3 col = _Color.rgb * a;
                // fade additive light into fog (fog color would brighten an additive pass)
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                col *= ComputeFogIntensity(i.fog);
                #endif
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
