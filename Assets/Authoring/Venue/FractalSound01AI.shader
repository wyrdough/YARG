Shader "FractalSound01AI"
{
    Properties
    {
        [NoScaleOffset] _Yarg_SoundTex ("SoundTexture", 2D) = "white" {}
    }
    SubShader
    {
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
            #define iTime _Time

            #include "UnityCG.cginc"
            texture2D _Yarg_SoundTex;

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

            static const int iters = 150;

            int fractal(float2 p, float2 coords)
            {
                float2 so = (-1.0 + 2.0 * coords) * 0.4;
                float2 seed = float2(0.098386255 + so.x, 0.6387662 + so.y);

                for (int i = 0; i < iters; i++)
                {
                    if (length(p) > 2.0)
                    {
                        return i;
                    }
                    float2 r = p;
                    p = float2(p.x * p.x - p.y * p.y, 2.0 * p.x * p.y);
                    p = float2(p.x * r.x - p.y * r.y + seed.x, r.x * p.y + p.x * r.y + seed.y);
                }

                return 0;
            }

            float3 color(int i)
            {
                float f = (float)i / (float)iters * 2.0;
                f = f * f * 2.0;
                return float3(sin(f * 2.0), sin(f * 3.0), abs(sin(f * 7.0)));
            }

            float sampleMusicA(Texture2D _Yarg_SoundTex, SamplerState sampler_Yarg_SoundTex)
            {
                return _Yarg_SoundTex.Load(float3(0, 0, 0));
                return 0.5 * (
                    _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(0.15, 0.25)).x +
                    _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(0.30, 0.25)).x);
            }

            float4 mainImage(float2 fragCoord)
            {
                float2 uv = fragCoord.xy / iResolution;

                float2 position = 3.0 * (-0.5 + fragCoord.xy / iResolution);
                position.x *= iResolution.x / iResolution.y;

                float2 iFC = float2(iResolution.x - fragCoord.x, iResolution.y - fragCoord.y);
                float2 pos2 = 2.0 * (-0.5 + iFC.xy / iResolution);
                pos2.x *= iResolution.x / iResolution.y;

                float4 t3 = _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(length(position) / 2.0, 0.1));
                float pulse = 0.5 + sampleMusicA(_Yarg_SoundTex, sampler_Yarg_SoundTex) * 1.8;

                float xCoord = 0.55 + sin(iTime / 3.0 + 0.5) / 2.0;
                float2 params = float2(xCoord, pulse * 0.9);
                float3 invFract = color(fractal(pos2, params));

                xCoord = 0.6 + cos(iTime / 2.0 + 0.5) / 2.0;
                float3 fract4 = color(fractal(position / 1.6, float2(xCoord, pulse * 0.8)));

                xCoord = 0.5 + sin(iTime / 3.0) / 2.0;
                float3 c = color(fractal(position, float2(xCoord, pulse)));

                t3 = abs(float4(0.5, 0.1, 0.5, 1.0) - t3) * 2.0;

                float4 fract01 = float4(c, 1.0);
                float4 salida;
                salida = fract01 / t3 + fract01 * t3 + float4(invFract, 0.6) + float4(fract4, 0.3);
                return salida;
            }

            // float4 mainImage(float2 fragCoord)
            // {
            //     // Replace with shadertoy mainImage
            // }
            //
            fixed4 frag(v2f _iParam) : SV_Target
            {
                return mainImage(gl_FragCoord);
            }
            ENDCG
        }
    }
}