// Additive radial glow for particles: like SecretOfHegra/Glow, but tinted and faded by the particle's vertex colour
// (colour over lifetime). Used for the treasure sparkles and the drifting dust motes.
Shader "SecretOfHegra/GlowParticle"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.82, 0.45, 1)
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

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; half fog : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half r = saturate(1.0 - length(i.uv * 2.0 - 1.0));
                half a = pow(r, _Falloff) * _Intensity * i.color.a;
                half3 col = _Color.rgb * i.color.rgb * a;
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                col *= ComputeFogIntensity(i.fog);
                #endif
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
