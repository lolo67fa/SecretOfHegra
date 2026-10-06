// Procedural sandstone for the rock masses: darker at the foot, lighter toward the crown, faint horizontal
// strata, cracks/erosion darkening from vertex colour, moonlit rim on silhouettes, one extra point light
// (the Quest asset allows one per object) and fog. No textures; opaque; stereo-instancing safe.
// Vertex colour: rgb = cavity/crack occlusion (1 = open face), a = normalized height within the rock (0 foot, 1 crown).
Shader "SecretOfHegra/Sandstone"
{
    Properties
    {
        _BottomColor ("Foot colour", Color) = (0.42, 0.22, 0.17, 1)
        _TopColor ("Crown colour", Color) = (0.85, 0.59, 0.45, 1)
        _StrataColor ("Strata tint", Color) = (0.62, 0.36, 0.27, 1)
        _StrataFrequency ("Strata per unit height", Float) = 9
        _StrataStrength ("Strata strength", Range(0, 1)) = 0.35
        _RimColor ("Moon rim", Color) = (0.62, 0.72, 0.95, 1)
        _RimPower ("Rim power", Range(1, 8)) = 3
        _RimStrength ("Rim strength", Range(0, 2)) = 0.7
        _Ambient ("Ambient boost", Range(0, 2)) = 1
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor, _TopColor, _StrataColor, _RimColor;
                half _StrataFrequency, _StrataStrength, _RimPower, _RimStrength, _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                float heightOS : TEXCOORD3;
                half fog : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.heightOS = v.positionOS.y;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.normalWS);
                half3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));

                // base: foot-to-crown gradient + soft strata bands (wobbled so they are not ruler-straight)
                half h = saturate(i.color.a);
                half3 albedo = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(0.0, 1.0, h));
                half band = sin((i.heightOS + sin(i.positionWS.x * 0.35 + i.positionWS.z * 0.27) * 0.06) * _StrataFrequency * 6.2831) * 0.5 + 0.5;
                band = smoothstep(0.55, 1.0, band);
                albedo = lerp(albedo, albedo * _StrataColor.rgb * 1.6, band * _StrataStrength);
                albedo *= i.color.rgb; // cavities and cracks

                // moonlight (main light) + ambient + one additional light
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(n, mainLight.direction));
                half3 lit = albedo * (mainLight.color * ndl + SampleSH(n) * _Ambient);
                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    lit += albedo * l.color * (l.distanceAttenuation * saturate(dot(n, l.direction)));
                }
                #endif

                // rim on edges facing the moon picks out silhouettes and ridges at night
                half rim = pow(1.0 - saturate(dot(n, viewDir)), _RimPower);
                rim *= saturate(dot(n, mainLight.direction) * 0.6 + 0.5);
                lit += _RimColor.rgb * rim * _RimStrength * (0.35 + 0.65 * h);

                lit = MixFog(lit, i.fog);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
