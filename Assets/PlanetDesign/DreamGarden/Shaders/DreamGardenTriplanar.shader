Shader "Dream Garden/Triplanar Ground"
{
    Properties
    {
        _BaseMap ("Legacy grass texture", 2D) = "white" {}
        _GrassMap ("Grass and moss", 2D) = "white" {}
        _DirtMap ("Earth and paths", 2D) = "white" {}
        _RockMap ("Stone and cliffs", 2D) = "white" {}
        _BaseColor ("Atmosphere tint", Color) = (0.86, 0.87, 0.80, 1)
        _GrassTint ("Grass tint", Color) = (0.70, 0.82, 0.72, 1)
        _DirtTint ("Earth tint", Color) = (0.79, 0.73, 0.69, 1)
        _RockTint ("Stone tint", Color) = (0.78, 0.81, 0.85, 1)
        _Tiling ("Texture repeats per metre", Range(0.04, 1)) = 0.20
        _BlendSharpness ("Triplanar blend sharpness", Range(1, 12)) = 4
        _RockSlope ("Rock starts on slopes", Range(0.02, 0.6)) = 0.17
        _PlanetRadius ("Planet radius", Float) = 70
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "DreamGardenGround"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_GrassMap); SAMPLER(sampler_GrassMap);
            TEXTURE2D(_DirtMap); SAMPLER(sampler_DirtMap);
            TEXTURE2D(_RockMap); SAMPLER(sampler_RockMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GrassTint;
                half4 _DirtTint;
                half4 _RockTint;
                float _Tiling;
                float _BlendSharpness;
                float _RockSlope;
                float _PlanetRadius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half3 normalOS : TEXCOORD3;
                half fog : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalOS = v.normalOS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half3 SampleThreeAxes(TEXTURE2D_PARAM(tex, texSampler), float3 p, float3 blend)
            {
                half3 x = SAMPLE_TEXTURE2D(tex, texSampler, p.yz).rgb;
                half3 y = SAMPLE_TEXTURE2D(tex, texSampler, p.xz).rgb;
                half3 z = SAMPLE_TEXTURE2D(tex, texSampler, p.xy).rgb;
                return x * blend.x + y * blend.y + z * blend.z;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 nOS = normalize(i.normalOS);
                float3 radial = normalize(i.positionOS);
                float3 blend = pow(abs(nOS), _BlendSharpness);
                blend /= max(dot(blend, float3(1, 1, 1)), 0.001);
                float3 p = i.positionOS * _Tiling;

                half3 grass = SampleThreeAxes(TEXTURE2D_ARGS(_GrassMap, sampler_GrassMap), p, blend) * _GrassTint.rgb;
                half3 earth = SampleThreeAxes(TEXTURE2D_ARGS(_DirtMap, sampler_DirtMap), p * .92, blend) * _DirtTint.rgb;
                half3 stone = SampleThreeAxes(TEXTURE2D_ARGS(_RockMap, sampler_RockMap), p * .87, blend) * _RockTint.rgb;

                // slope is measured against the spherical surface, not world up
                float slope = 1.0 - saturate(dot(nOS, radial));
                float altitude = length(i.positionOS) - _PlanetRadius;
                float variation = sin(dot(radial, float3(24.37, 11.13, 16.87))) *
                    sin(dot(radial, float3(13.91, 19.41, 31.17)));
                float earthWeight = max(smoothstep(.035, .22, slope) * .7,
                    smoothstep(.6, 3.5, abs(altitude)) * .35);
                earthWeight = saturate(earthWeight + variation * .09);
                float stoneWeight = max(smoothstep(_RockSlope, _RockSlope + .19, slope),
                    smoothstep(3.5, 8.0, altitude) * .74);
                stoneWeight = saturate(stoneWeight + variation * .035);

                half3 base = lerp(grass, earth, earthWeight);
                base = lerp(base, stone, stoneWeight);
                base *= _BaseColor.rgb;

                half3 normalWS = normalize(i.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 ambient = max(SampleSH(normalWS), half3(.14, .14, .17));
                half direct = saturate(dot(normalWS, mainLight.direction));
                half3 lightColor = ambient + mainLight.color * direct * mainLight.shadowAttenuation;
                return half4(MixFog(base * lightColor, i.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Diffuse"
}
