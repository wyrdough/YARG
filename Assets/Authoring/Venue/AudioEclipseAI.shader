Shader "VisualizerVenue/AudioEclipseAI"
{
    // Converted from https://www.shadertoy.com/view/MdsXWM
    Properties
    {
        [NoScaleOffset] _Yarg_SoundTex ("SoundTexture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            ColorMask RGB

            // We don't want this to be culled
            Cull Off

            ZWrite On
            ZTest On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define iResolution _ScreenParams
            #define gl_FragCoord ((_iParam.scrPos.xy/_iParam.scrPos.w) * _ScreenParams.xy)
            #define iTime _Time

            #include "UnityCG.cginc"

            texture2D _Yarg_SoundTex;
            SamplerState sampler_Yarg_SoundTex;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 scrPos : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
				v2f OUT;
                // Expects you're using the default Unity quad
                // this makes it cover whole screen/camera
                float4 pos = float4(v.vertex.xy * 2.0, 0.0, 1.0);
                #if UNITY_REVERSED_Z
                pos.z = 0.000001;
                #else
                pos.z = 0.999999;
                #endif

                OUT.pos = pos;
                OUT.scrPos = ComputeScreenPos(pos);

                return OUT;
            }

            // Define necessary inputs via constant buffer, texture, and sampler registers
            // cbuffer Globals : register(b0)
            // {
            //     float2 iResolution; // Viewport resolution (e.g., float2(width, height))
            //     float  iTime;       // Shader playback time in seconds
            // };

            Texture2D iChannel0 : register(t0); // Audio texture input
            SamplerState Sampler0 : register(s0); // Sampler for the audio texture

            // Define the input structure from the vertex shader
            struct PSInput
            {
                float4 position : SV_Position; // Clip space position
                float2 uv : TEXCOORD0;         // Texture coordinates (typically 0-1 range)
            };

            // --- Shader Constants ---
            static const float dots = 40.0f; // number of lights
            static const float radius = 0.25f; // radius of light ring
            static const float brightness = 0.02f;
            static const float PI = 3.14159265f;
            static const float TWO_PI = 2.0f * PI;

            // --- Helper Function ---

            // Convert HSV to RGB (HLSL version)
            float3 hsv2rgb(float3 c)
            {
                // Use float4 and 'f' suffix for constants
                float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
                // Use frac() instead of fract()
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0f - K.www);
                // Use lerp() instead of mix(), add 'f' suffix
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0f, 1.0f), c.y);
            }

            // --- Pixel Shader Entry Point ---

            // Returns float4 color for the SV_Target semantic
            // float4 PSMain(PSInput input) : SV_Target
            float4 mainImage(float2 fragCoord)
            {
                // Calculate 'p': normalized coordinates centered at (0,0) and aspect-corrected
                // This replicates the common Shadertoy coordinate system setup
                // float2 fragCoord = input.uv * iResolution;
                // fragCoord = fragCoord * iResolution;
                float2 p = (fragCoord - 0.5f * iResolution) / min(iResolution.x, iResolution.y);

                // Background color (use 'f' suffix)
                float3 c = float3(0.0f, 0.0f, 0.1f);

                // Loop through the dots (use 'f' suffix for float loop)
                for (float i = 0.0f; i < dots; i++)
                {
                    // Read frequency for this dot from audio input channel
                    // Use Texture.Sample(Sampler, UV)

                    // rn YARG's sound texture only fills half the FFT bins, so adjust accordingly
                    float bin = (i / dots) * 0.5f;
                    float vol = _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(bin, 0.0f)).x;
                    float b = vol * brightness;

                    // Get location of dot (use defined TWO_PI)
                    float angle = TWO_PI * i / dots;
                    float x = radius * cos(angle);
                    float y = radius * sin(angle);
                    float2 o = float2(x, y);

                    // Get color of dot based on its index + time
                    // Use 'f' suffix
                    float foo = (i + iTime * 10.0f) / dots;
                    float3 dotCol = hsv2rgb(float3(foo, 1.0f, 1.0f));

                    // Get brightness contribution based on distance
                    // Add small epsilon to length() to avoid potential division by zero
                    c += b / (length(p - o) + 1e-6f) * dotCol;
                }

                // Black circle overlay
                // Use length() instead of distance to origin (0,0)
                // First bin should be the average volume
                float avgvol = _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(0.0f, 0.0f));
                float dist = length(p);
                float inner = 0.26f + avgvol * 0.1f;
                float outer = 0.28f + avgvol * 0.1f;
                // Use 'f' suffix for smoothstep edges
                c = c * smoothstep(inner, outer, dist);

                // Return final color (use 'f' suffix for alpha)
                return float4(c, 1.0f);
            }

            fixed4 frag(v2f _iParam) : SV_Target {
                return mainImage(gl_FragCoord);
            }

            ENDCG
        }
    }
}
