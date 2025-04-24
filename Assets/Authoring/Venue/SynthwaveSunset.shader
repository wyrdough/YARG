// Based on https://www.shadertoy.com/view/XctBW7

// Still needs work to deal with the brightness difference between Shadertoy WebGL and Unity

Shader "VisualizerVenue/SynthwaveSunset"
{
    Properties
    {
        [NoScaleOffset] _Yarg_SoundTex ("SoundTexture", 2D) = "white" {}
        _FFTSmoothing ("FFT Smoothing Factor", Float) = 0.8
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
            sampler2D _Yarg_SoundTex;

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

            #define VAPORWAVE
            #define speed 10.0
            #define city
            #define audio_vibration_amplitude 0.200

            float4 _ChannelResolution;
            float _TimeDelta;
            float jTime;

            float mod(float x, float y)
            {
                return x - y * floor(x/y);
            }

            // Previous helper functions remain the same...

            float4 textureMirror(sampler2D tex, float2 c)
            {
                float2 cf = frac(c);
                return tex2Dlod(tex, float4(lerp(cf, 1.0 - cf, mod(floor(c), 2.0)), 0, 0));
            }

            float amp(float2 p)
            {
                return smoothstep(1.0, 8.0, abs(p.x));
            }

            float pow512(float a)
            {
                a *= a;//^2
                a *= a;//^4
                a *= a;//^8
                a *= a;//^16
                a *= a;//^32
                a *= a;//^64
                a *= a;//^128
                a *= a;//^256
                return a * a;
            }

            float pow1d5(float a)
            {
                return a * sqrt(a);
            }

            float hash21(float2 co)
            {
                return frac(sin(dot(co.xy, float2(1.9898, 7.233))) * 45758.5433);
            }

            float hash(float2 uv)
            {
                float a = amp(uv);
                #ifdef wave_thing
                    float w = a > 0.0 ? (1.0 - 0.4 * pow512(0.51 + 0.49 * sin((0.02 * (uv.y + 0.5 * uv.x) - jTime) * 2.0))) : 0.0;
                #else
                    float w = 1.0;
                #endif
                return (a > 0.0 ?
                    a * pow1d5(tex2Dlod(_Yarg_SoundTex, float2(uv.x / iResolution.x, uv.y /iResolution.y).r)) * w
                    : 0.0) - (textureMirror(_Yarg_SoundTex, float2((uv.x * 29.0 + uv.y) * 0.03125, 1.0)).x) * audio_vibration_amplitude;
            }

            float edgeMin(float dx, float2 da, float2 db, float2 uv)
            {
                uv.x += 5.0;
                float3 c = frac((round(float3(uv, uv.x + uv.y))) * (float3(0,1,2) + 0.61803398875));
                float a1 = textureMirror(_Yarg_SoundTex, float2(c.y, 0.0)).x > 0.6 ? 0.15 : 1.0;
                float a2 = textureMirror(_Yarg_SoundTex, float2(c.x, 0.0)).x > 0.6 ? 0.15 : 1.0;
                float a3 = textureMirror(_Yarg_SoundTex, float2(c.z, 0.0)).x > 0.6 ? 0.15 : 1.0;

                return min(min((1.0 - dx) * db.y * a3, da.x * a2), da.y * a1);
            }

            float2 trinoise(float2 uv)
            {
                const float sq = sqrt(3.0/2.0);
                uv.x *= sq;
                uv.y -= 0.5 * uv.x;
                float2 d = frac(uv);
                uv -= d;

                bool c = dot(d, float2(1,1)) > 1.0;

                float2 dd = 1.0 - d;
                float2 da = c ? dd : d;
                float2 db = c ? d : dd;

                float nn = hash(uv + (c ? 1.0 : 0.0));
                float n2 = hash(uv + float2(1,0));
                float n3 = hash(uv + float2(0,1));

                float nmid = lerp(n2, n3, d.y);
                float ns = lerp(nn, c ? n2 : n3, da.y);
                float dx = da.x/db.y;
                return float2(lerp(ns, nmid, dx), edgeMin(dx, da, db, uv + d));
            }

            float2 map(float3 p)
            {
                float2 n = trinoise(p.xz);
                return float2(p.y - 2.0 * n.x, n.y);
            }


            float3 grad(float3 p)
            {
                const float2 e = float2(0.005, 0);
                float a = map(p).x;
                return float3(map(p + float3(e.x,e.y,e.y)).x - a,
                            map(p + float3(e.y,e.x,e.y)).x - a,
                            map(p + float3(e.y,e.y,e.x)).x - a) / e.x;
            }

            float2 intersect(float3 ro, float3 rd)
            {
                float d = 0.0;
                float h = 0.0;
                for(int i = 0; i < 200; i++)
                {
                    float3 p = ro + d * rd;
                    float2 s = map(p);
                    h = s.x;
                    d += h * 0.5;
                    if(abs(h) < 0.003 * d)
                        return float2(d, s.y);
                    if(d > 150.0 || p.y > 2.0) break;
                }
                return float2(-1, -1);
            }

            void addsun(float3 rd, float3 ld, inout float3 col)
            {
                float sun = smoothstep(0.21, 0.2, distance(rd, ld));

                if(sun > 0.0)
                {
                    float yd = (rd.y - ld.y);
                    float a = sin(3.1 * exp(-(yd) * 14.0));
                    sun *= smoothstep(-0.8, 0.0, a);
                    col = lerp(col, float3(1.0, 0.8, 0.4) * 0.75, sun);
                }
            }

            float starnoise(float3 rd)
            {
                float c = 0.0;
                float3 p = normalize(rd) * 300.0;
                for(float i = 0.0; i < 4.0; i++)
                {
                    float3 q = frac(p) - 0.5;
                    float3 id = floor(p);
                    float c2 = smoothstep(0.5, 0.0, length(q));
                    c2 *= step(hash21(id.xz/id.y), 0.06-i*i*0.005);
                    c += c2;
                    p = 0.6 * p + 0.5 * mul(float3x3(3.0/5.0, 0, 4.0/5.0,
                                                     0, 1, 0,
                                                     -4.0/5.0, 0, 3.0/5.0), p);
                }
                c *= c;
                float g = dot(sin(rd * 10.512), cos(rd.yzx * 10.512));
                c *= smoothstep(-3.14, -0.9, g) * 0.5 + 0.5 * smoothstep(-0.3, 1.0, g);
                return c * c;
            }

            float3 gsky(float3 rd, float3 ld, bool mask)
            {
                float haze = exp2(-5.0 * (abs(rd.y) - 0.2 * dot(rd, ld)));

                float st = mask ? pow512(tex2D(_Yarg_SoundTex, (rd.xy + float2(300.1, 100) * rd.z) * 10.0).r) * (1.0 - min(haze, 1.0)) : 0.0;
                float3 back = float3(0.4, 0.1, 0.7) * (1.0 - 0.5 * textureMirror(_Yarg_SoundTex, float2(0.5 + 0.05 * rd.x/rd.y, 0.0)).x
                    * exp2(-0.1 * abs(length(rd.xz)/rd.y))
                    * max(sign(rd.y), 0.0));

                #ifdef city
                float x = round(rd.x * 30.0);
                float h = hash21(float2(x-166.0, 0));
                bool building = (h*h*0.125*exp2(-x*x*x*x*0.0025) > rd.y);
                if(mask && building)
                {
                    back *= 0.0;
                    haze = 0.8;
                    mask = mask && !building;
                }
                #endif

                float3 col = clamp(lerp(back, float3(0.7, 0.1, 0.4), haze) + st, 0.0, 1.0);
                if(mask) addsun(rd, ld, col);
                return col;
            }

            float4 mainImage(float2 fragCoord)
            {
                float4 fragColor = float4(0, 0, 0, 0);

                #ifdef AA
                    for(float x = 0.0; x < 1.0; x += 1.0/float(AA))
                    {
                        for(float y = 0.0; y < 1.0; y += 1.0/float(AA))
                        {
                #else
                    const float AA = 1.0, x = 0.0, y = 0.0;
                #endif

                float2 uv = (2.0 * (fragCoord + float2(x,y)) - _ScreenParams.xy) / _ScreenParams.y;

                float dt = frac(tex2D(_Yarg_SoundTex, float(AA) * (fragCoord + float2(x,y))/_ChannelResolution.xy).r + _Time.y);
                jTime = mod(iTime.y-dt*_TimeDelta*.25,4000);
                float3 ro = float3(0.0, 1.0, (-20000.0 + iTime.y * speed));

                #ifdef stereo
                    ro += float3(0.2 * (float(uv.x > 0.0) - 0.5), 0.0, 0.0);
                    const float de = 0.9;
                    uv.x = uv.x + 0.5 * (uv.x > 0.0 ? -de : de);
                    uv *= 2.0;
                #endif

                float3 rd = normalize(float3(uv, 4.0/3.0));

                float2 i = intersect(ro, rd);
                float d = i.x;

                float3 ld = normalize(float3(0, 0.125 + 0.05 * sin(0.1 * jTime), 1));
                float3 fog = d > 0.0 ? exp2(-d * float3(0.14, 0.1, 0.28)) : float3(0, 0, 0);
                float3 sky = gsky(rd, ld, d < 0.0);

                float3 p = ro + d * rd;
                float3 n = normalize(grad(p));

                float diff = dot(n, ld) + 0.1 * n.y;
                float3 col = float3(0.1, 0.11, 0.18) * diff;

                float3 rfd = reflect(rd, n);
                float3 rfcol = gsky(rfd, ld, true);

                col = lerp(col, rfcol, 0.05 + 0.95 * pow(max(1.0 + dot(rd,n), 0.0), 5.0));

                #ifdef VAPORWAVE
                    col = lerp(col, float3(0.4, 0.5, 1.0), smoothstep(0.05, 0.0, i.y));
                    col = lerp(sky, col, fog);
                    col = sqrt(col);
                #else
                    col = lerp(col, float3(0.8, 0.1, 0.92), smoothstep(0.05, 0.0, i.y));
                    col = lerp(sky, col, fog);
                #endif

                if(d < 0.0)
                    d = 1e6;
                d = min(d, 10.0);
                fragColor += float4(clamp(col, 0.0, 1.0), d < 0.0 ? 0.0 : 0.1 + exp2(-d));

                #ifdef AA
                        }
                    }
                    fragColor /= float(AA * AA);
                #endif

                return pow(fragColor, 2.2);
            }

            fixed4 frag(v2f _iParam) : SV_Target
            {
                return mainImage(gl_FragCoord);
            }
            ENDCG
        }
    }
}