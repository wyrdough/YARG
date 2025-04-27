/* "3D Audio Visualizer" by @kishimisu - 2022 (https://www.shadertoy.com/view/dtl3Dr)
   Wait for the drop!

   The lights of this scene react live to the audio input.
   I'm trying to find interesting ways to extract audio
   features from the audio's FFT to animate my scenes.

   Each light is associated to a random frequency range,
   ranging from bass (distant lights) to high (close lights)

   Really happy with this result!
*/

// Adapted from the version at https://www.shadertoy.com/view/dt3XDl

Shader "3DAudioVisualizer"
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

            #define st(t1, t2, v1, v2) lerp(v1, v2, smoothstep(t1, t2, iTime))
            #define light(d, att) 1.0 / (1.0 + pow(abs(d*att), 1.3))

            /* Audio-related functions */
            float getLevel(float x) {
                return _Yarg_SoundTex.Load(int3(int(x * 512.0), 0, 0)).r;
            }

            float logX(float x, float a, float c) {
                return 1.0 / (exp(-a*(x-c)) + 1.0);
            }

            float logisticAmp(float amp) {
                float c = st(0.0, 10.0, 0.8, 1.0), a = 20.0;
                return (logX(amp, a, c) - logX(0.0, a, c)) / (logX(1.0, a, c) - logX(0.0, a, c));
            }

            float getPitch(float freq, float octave) {
                freq = pow(2.0, freq) * 261.0;
                freq = pow(2.0, octave) * freq / 12000.0;
                return logisticAmp(getLevel(freq));
            }

            float getVol(float samples) {
                float avg = 0.0;
                for (float i = 0.0; i < samples; i++)
                    avg += getLevel(i/samples);
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

            float4 mainImage(float2 fragCoord) {
                float2 uv = (2.0 * fragCoord - iResolution.xy) / iResolution.y;
                float3 col = float3(0.0, 0.0, 0.0);
                float vol = getVol(8.0);

                float t = 0.0;
                for (float i = 0.0; i < 30.0; i++) {
                    float3 p = t * normalize(float3(uv, 1.0));

                    float3 id = floor(abs(p));
                    float3 q = frac(p) - 0.5;

                    float boxRep = sdBox(q, float3(0.3, 0.3, 0.3));
                    float boxCtn = sdBox(p, float3(7.5, 6.5, 16.5));

                    float dst = max(boxRep, abs(boxCtn) - vol * 0.2);
                    float freq = smoothstep(16.0, 0.0, id.z) * 3.0 + hash13(id) * 1.5;

                    col += float3(0.8, 0.6, 1.0) * (cos(id * 0.4 + float3(0,1,2) + iTime) + 2.0)
                        * light(dst, 10.0 - vol)
                        * getPitch(freq, 1.0);

                    t += dst;
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