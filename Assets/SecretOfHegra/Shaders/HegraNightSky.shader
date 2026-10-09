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
        _BandColor ("Milky Way colour", Color) = (0.55, 0.6, 0.78, 1)
        _BandIntensity ("Milky Way intensity", Range(0, 1)) = 0.4
        _BandNormal ("Milky Way plane normal", Vector) = (0.78, 0.2, -0.59, 0)
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
                half4 _BandColor;
                half _BandIntensity;
                float4 _BandNormal;
            CBUFFER_END

            // cheap value noise for the Milky Way's clouds and dark dust lanes
            float hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash3(i), hash3(i + float3(1, 0, 0)), f.x),
                                 lerp(hash3(i + float3(0, 1, 0)), hash3(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(hash3(i + float3(0, 0, 1)), hash3(i + float3(1, 0, 1)), f.x),
                                 lerp(hash3(i + float3(0, 1, 1)), hash3(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float fbm3(float3 p)
            {
                return 0.5 * vnoise(p) + 0.25 * vnoise(p * 2.07) + 0.125 * vnoise(p * 4.13);
            }

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

                // the Milky Way: a soft band along a tilted great circle, broken into clouds, split by dust lanes,
                // brightest high in the sky and fading into the horizon haze
                float across = dot(d, normalize(_BandNormal.xyz));
                float band = exp(-across * across * 22.0);
                float clouds = saturate(fbm3(d * 5.5) * 1.6 - 0.35);
                float lane = smoothstep(0.42, 0.68, fbm3(d * 13.0 + 3.1)) * exp(-across * across * 90.0);
                float core = exp(-across * across * 80.0) * 0.6;
                float mw = band * (clouds + core * clouds) * (1.0 - 0.75 * lane);
                sky += _BandColor.rgb * mw * _BandIntensity * smoothstep(0.02, 0.35, up);
                half3 col = up >= 0 ? sky : lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-up * 6.0));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
