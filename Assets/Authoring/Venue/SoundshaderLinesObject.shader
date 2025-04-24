
// Based on https://www.shadertoy.com/view/WdfSDr

Shader "SoundShaders/SoundshaderLinesObject"
{
    Properties
    {
        [NoScaleOffset] _Yarg_SoundTex ("SoundTexture", 2D) = "white" {}
    }
    SubShader
    {
        Pass {
            ColorMask RGB

            Cull Off
            ZWrite On
            ZTest On

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #define iResolution _ScreenParams
            #define gl_FragCoord ((_iParam.scrPos.xy/_iParam.scrPos.w) * _ScreenParams.xy)
            #define iTime _Time
            // Define for potential future use, same as GLSL
            #define BEATMOVE 1

            // HLSL Constant Buffer for uniforms
            // cbuffer GlobalUniforms : register(b0)
            // {
            //     float2 iResolution; // Viewport resolution (pixels)
            //     float  iTime;       // Shader playback time (seconds)
            //     // Add other uniforms like iMouse, iDate if needed
            // };

            #include "UnityCG.cginc"
            Texture2D _Yarg_SoundTex;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 scrPos : TEXCOORD0;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

    //         v2f vert(appdata_t v)
    //         {
				// v2f OUT;
    //             // Expects you're using the default Unity quad
    //             // this makes it cover whole screen/camera
    //             // float4 pos = float4(v.vertex.xy * 2.0, 0.0, 1.0);
    //             float4 pos = UnityObjectToClipPos(v.vertex);
    //             #if UNITY_REVERSED_Z
    //             pos.z = 0.000001;
    //             #else
    //             pos.z = 0.999999;
    //             #endif
    //
    //             OUT.pos = pos;
    //             OUT.scrPos = ComputeScreenPos(pos);
    //
    //             return OUT;
    //         }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.scrPos = v.vertex.xyz;
                o.scrPos = v.uv;
                // UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            // HLSL Texture and Sampler declaration
            // Texture2D iChannel0 : register(t0); // Assumes texture is in slot t0
            SamplerState sampler_Yarg_SoundTex; // : register(s0); // Assumes sampler is in slot s0

            // Constants
            static const float FREQ_RANGE = 384.0f;
            static const float PI = 3.1415f;
            static const float RADIUS = 0.5f;
            // static const float BRIGHTNESS = 0.15f;
            static const float BRIGHTNESS = 0.025f;
            static const float SPEED = 2.5f;

            // Convert HSV to RGB (HLSL version)
            float3 hsv2rgb(float3 color) {
                float4 konvert = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
                float3 calc = abs(frac(color.xxx + konvert.xyz) * 6.0f - konvert.www);
                return color.z * lerp(konvert.xxx, clamp(calc - konvert.xxx, 0.0f, 1.0f), color.y);
            }

            // Calculate Luminance (HLSL version)
            float luma(float3 color) {
              // return dot(color, float3(0.299f, 0.587f, 0.114f));
              return dot(color, float3(0.299f, 0.587f, 0.5f)); // Using the modified version from GLSL
            }

            // Get frequency data from texture (HLSL version)
            float getFrequency(float x) {
                // Sample the texture using the provided sampler state

                x = floor(x * FREQ_RANGE);
                // x = x * FREQ_RANGE;
                // return _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(x + 1, 0.2)).x + 0.06f;
                // return _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2((x + 1) / x, 0)).x + 0.06f;
                // return _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2((floor(x + 1.0f) / FREQ_RANGE), 0)).x + 0.06f;
                // return _Yarg_SoundTex.Load(float3(floor(x * FREQ_RANGE + 1) / FREQ_RANGE, 0, 0)).x + 0.06f;
                return _Yarg_SoundTex.Load(float3(x + 1, 0, 0)).x + 0.06f;
            }

            // Get smoothed frequency data (HLSL version)
            float getFrequency_smooth(float x) {
                float index = floor(x * FREQ_RANGE) / FREQ_RANGE;
                float next = floor(x * FREQ_RANGE + 1.0f) / FREQ_RANGE;
                // float index = x;
                // float next = x + 1;
                // Use lerp (linear interpolation) which is HLSL's equivalent of mix
                return lerp(getFrequency(index), getFrequency(next), smoothstep(0.0f, 1.0f, frac(x * FREQ_RANGE)));
            }

            // Get blended frequency data (HLSL version)
            float getFrequency_blend(float x) {
                return lerp(getFrequency(x), getFrequency_smooth(x), 0.5f);
            }

            // Calculate circle illumination (HLSL version)
            float3 circleIllumination(float2 fragment, float radius) {
                float distance = length(fragment);
                // Use the translated getFrequency_smooth
                float ring = 1.0f / abs(distance - radius - (getFrequency_smooth(0.0f) / 4.50f));

                // float brightness = distance < radius ? BRIGHTNESS * 0.3f : BRIGHTNESS; // Optional brightness adjustment

                float3 color = float3(0.0f, 0.0f, 0.0f);

                // Use atan2(y, x) for HLSL, equivalent to GLSL's atan(x, y)
                float angle = atan2(fragment.y, fragment.x);
                // Use the translated hsv2rgb
                float h = (angle + iTime * 2.5f) / (PI * 2.0f);
                color += hsv2rgb(float3(h, 1.0f, 1.0f)) * ring * BRIGHTNESS;

                // Use the translated getFrequency_blend
                // float x = abs(angle / PI);
                float frequency = max(getFrequency_blend(abs(angle / PI)) - 0.02f, 0.0f);
                color *= frequency;

                // Black halo (optional)
                // color *= smoothstep(radius * 0.5f, radius, distance);

                return color;
            }

            // Calculate line effect (HLSL version)
            float3 doLine(float2 fragment, float radius, float x) {
                // Use the translated hsv2rgb
                float h = x * 0.23f + iTime * 0.12f;
                float3 col = hsv2rgb(float3(h, 1.0f, 1.0f));

                float freq = abs(fragment.x * 0.5f);

                // Use the translated getFrequency
                col *= (1.0f / abs(fragment.y)) * BRIGHTNESS * getFrequency(freq);
                col = col * smoothstep(radius, radius * 1.8f, abs(fragment.x));

                return col;
            }

            // HLSL Pixel Shader Entry Point
            // SV_Position provides screen coordinates (like gl_FragCoord)
            // SV_Target is the output render target
            float4 mainImage(v2f _iParam)
            {
                float2 fragCoord = _iParam.scrPos;
                // Normalize coordinates and center, adjust aspect ratio
                // float2 fragPos = fragCoord.xy / iResolution.xy;
                float2 fragPos = fragCoord;
                fragPos = (fragPos - 0.5f) * 4.0f;
                // fragPos.x *= iResolution.x / iResolution.y;
                // fragPos.x = _iParam.pos.x / _iParam.pos.y;
                // fragPos.x = fragPos.x / fragPos.y;

                float3 color = float3(0.0f, 0.0f, 0.0f);

                // Calculate effects using translated functions
                color += circleIllumination(fragPos, RADIUS);

                // Rotation 1
                float c = cos(iTime * SPEED);
                float s = sin(iTime * SPEED);
                // Construct float2x2 matrix and multiply using mul()
                // mul(matrix, vector) is used here to match GLSL column-vector style
                float2 rot = mul(float2x2(c, -s, s, c), fragPos);
                color += doLine(rot, RADIUS, rot.x);

                // Rotation 2
                float c1 = sin(iTime * SPEED);
                float s1 = cos(iTime * SPEED);
                // Construct float2x2 matrix and multiply using mul()
                float2 rot1 = mul(float2x2(c1, -s1, s1, c1), fragPos);
                color += doLine(rot1, RADIUS, rot1.y); // Note: using rot1.y here as in the original GLSL

                // Additive brightness based on luminance
                color += max(luma(color) - 1.0f, 0.0f);
                // color += max(Luminance(color) - 1.0f, 0.0f);
                // Return final color with alpha = 1.0
                return float4(color, 1.0f);
            }

            fixed4 frag(v2f _iParam) : SV_Target {
                return mainImage(_iParam);
            }

            ENDCG
        }
    }
}