// Adapted from https://www.shadertoy.com/view/Dtj3zW

/* @kishimisu - 2023

   This work is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License (https://creativecommons.org/licenses/by-nc-sa/4.0/deed.en)

   An audio-reactive scene that maps the frequencies of the input music
   and the audio volume to different cells, colors, size and intensity!

   I've been struggling to complete this scene as I wanted to repeat
   the space with random variations for each cell. There's a wonderful
   tutorial by Blackle Mori explaining how to achieve this (https://www.youtube.com/watch?v=I8fmkLK1OKg) ,
   however I'm using an accumulation technique for the lighting with a
   fixed number of steps (30) which gets broken with this new technique.

   I decided to keep a reasonable random variation amount to prevent having
   raymarching artifacts that are too visible. I couldn't get totally rid of them,
   however with this kind of audio reactive scene it seems to be more acceptable
   as there's a lot of rapid movements!
*/

Shader "VisualizerVenue/3DAudioVisualizer2"
{
    Properties
    {
        [NoScaleOffset] _Yarg_SoundTex ("SoundTexture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "PreviewType" = "Plane"
        }

        Pass
        {
            ColorMask RGB

            Cull Off
            ZWrite On
            ZTest Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define iResolution _ScreenParams
            #define gl_FragCoord ((_iParam.scrPos.xy/_iParam.scrPos.w) * _ScreenParams.xy)
            #define iTime _Time.y

            #include "UnityCG.cginc"
            Texture2D _Yarg_SoundTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
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

            SamplerState sampler_Yarg_SoundTex;

            #define light(d, att) 1.0 / (1.0 + pow(abs(d * att), 1.3))
            #define rot(a) float2x2(cos(a + float4(0, 33, 11, 0)))

            float getLevel(float x) {
                return _Yarg_SoundTex.Load(float3(x * 512.0, 0, 0)).r;
            }

            float logX(float x, float a, float c) {
                return 1.0 / (exp(-a * (x - c)) + 1.0);
            }

            float logisticAmp(float amp) {
                float c = 0.88, a = 20.0;
                return (logX(amp, a, c) - logX(0.0, a, c)) / (logX(1.0, a, c) - logX(0.0, a, c));
            }

            float getPitch(float freq, float octave) {
                freq = pow(2.0, freq) * 261.0;
                freq = pow(2.0, octave) * freq / 12000.0;
                return logisticAmp(getLevel(freq));
            }

            float getVol(float samples) {
                float avg = 0.0;
                for (float i = 0.0; i < samples; i++) avg += getLevel(i / samples);
                return avg / samples;
            }

            float sdBox(float3 p, float3 b) {
                float3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }

            float hash13(float3 p3) {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            float4 mainImage(float2 fragCoord)
            {
                float2 uv = (2.0 * fragCoord - iResolution.xy) / iResolution.y;
                float3 col = float3(0.1, 0.0, 0.14);
                float vol = getVol(8.0);

                float3 ro = float3(0, 8, 12) * (1.0 + vol * 0.3);
                ro.zx = mul(ro.zx, rot(iTime * 0.4));
                float3 f = normalize(-ro);
                float3 r = normalize(cross(float3(0, 1, 0), f));
                float3 rd = normalize(f + uv.x * r + uv.y * cross(f, r));

                float hasSound = 1.0;
                if (iTime <= 0.0) hasSound = 0.4;

                for (float i = 0.0, t = 0.0; i < 30.0; i++)
                {
                    float3 p = ro + t * rd;

                    float2 cen = floor(p.xz) + 0.5;
                    float3 id = abs(float3(cen.x, 0, cen.y));
                    float d = length(id);

                    float freq = smoothstep(0.0, 20.0, d) * 3.0 * hasSound + hash13(id) * 2.0;
                    float pitch = getPitch(freq, 0.7);

                    float v = vol * smoothstep(2.0, 0.0, d);
                    float h = d * 0.2 * (1.0 + pitch * 1.5) + v * 2.0;
                    float me = sdBox(p - float3(cen.x, -50.0, cen.y), float3(0.3, 50.0 + h, 0.3) + pitch) - 0.05;

                    col += lerp(lerp(float3(0.8, 0.2, 0.4), float3(0, 1, 0), min(v * 2.0, 1.0)), float3(0.5, 0.3, 1.2), smoothstep(10.0, 30.0, d))
                           * (cos(id) + 1.5)
                           * (pitch * d * 0.08 + v)
                           * light(me, 20.0) * (1.0 + vol * 2.0);

                    t += me;
                }

                col = pow(col, 2.2);
                return float4(col, 1.0);
            }

            fixed4 frag(v2f _iParam) : SV_Target
            {
                return mainImage(gl_FragCoord);
            }

            ENDCG
        }
    }
}