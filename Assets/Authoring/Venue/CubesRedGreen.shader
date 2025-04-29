// Adapted from https://www.shadertoy.com/view/4fdSzB

// TODO: Needs more tuning..and there are some visual bugs in the reflections

Shader "CubesRedGreen"
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

            #define mod(x, y) (x - y * floor(x / y))
            #define FAR 30.0

            float map(float3 p)
            {
                float n = sin(dot(floor(p), float3(7, 157, 113)));
                float3 rnd = frac(float3(2097152, 262144, 32768) * n) * 0.16 - 0.08;

                p = frac(p + rnd) - 0.5;

                float eq = _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(frac(n), 0.25)).r;
                float boxsize = 0.2 + eq * 0.4;

                p = abs(p);
                return max(p.x, max(p.y, p.z)) - boxsize + dot(p, p) * 0.5;
            }

            float trace(float3 ro, float3 rd)
            {
                float t = 0.0;
                float d;

                for (int i = 0; i < 96; i++)
                {
                    d = map(ro + rd * t);
                    if (abs(d) < 0.002 || t > FAR) break;
                    t += d * 0.75;
                }

                return t;
            }

            float traceRef(float3 ro, float3 rd)
            {
                float t = 0.0;
                float d;

                for (int i = 0; i < 48; i++)
                {
                    d = map(ro + rd * t);
                    if (abs(d) < 0.0025 || t > FAR) break;
                    t += d;
                }

                return t;
            }

            float softShadow(float3 ro, float3 lp, float k)
            {
                const int maxIterationsShad = 24;

                float3 rd = (lp - ro);
                float shade = 1.0;
                float dist = 0.005;
                float end = max(length(rd), 0.001);
                // float stepDist = end / float(maxIterationsShad);

                rd /= end;

                for (int i = 0; i < maxIterationsShad; i++)
                {
                    float h = map(ro + rd * dist);
                    shade = min(shade, smoothstep(0.0, 1.0, k * h / dist));
                    dist += clamp(h, 0.02, 0.2);

                    if (h < 0.0 || dist > end) break;
                }

                return min(max(shade, 0.0) + 0.25, 1.0);
            }

            float3 getNormal(float3 p)
            {
                float2 e = float2(0.0035, -0.0035);
                return normalize(
                    e.xyy * map(p + e.xyy) +
                    e.yyx * map(p + e.yyx) +
                    e.yxy * map(p + e.yxy) +
                    e.xxx * map(p + e.xxx));
            }

            float3 getObjectColor(float3 p)
            {
                float3 col;
                float modX = fmod(p.x, 4.0);
                // float modY = fmod(p.y, 4.0);
                // float modZ = fmod(p.z, 4.0);

                if (modX < 1.0)
                    col = float3(1.0, 0.0, 0.0);
                else if (modX < 2.0)
                    col = float3(0.0, 1.0, 0.0);
                else if (modX < 3.0)
                    col = float3(0.0, 0.0, 1.0);
                else
                    col = float3(1.0, 1.0, 1.0);

                return col;
            }

            float3 doColor(float3 sp, float3 rd, float3 sn, float3 lp)
            {
                float3 ld = lp - sp;
                float lDist = max(length(ld), 0.001);
                ld /= lDist;

                float atten = 1.0 / (1.0 + lDist * 0.2 + lDist * lDist * 0.1);

                float diff = max(dot(sn, ld), 0.0);
                float spec = pow(max(dot(reflect(-ld, sn), -rd), 0.0), 8.0);

                float3 objCol = getObjectColor(sp);

                float3 sceneCol = (objCol * (diff + 0.15) + float3(1.0, 0.6, 0.2) * spec * 2.0) * atten;

                return sceneCol;
            }

            float4 mainImage(float2 fragCoord)
            {
                float2 uv = (fragCoord.xy - iResolution.xy * 0.5) / iResolution.y;

                float3 rd = normalize(float3(uv, 1.0));

                float cs = cos(iTime * 0.25);
                float si = sin(iTime * 0.25);
                rd.xy = mul(float2x2(cs, si, -si, cs), rd.xy);
                rd.xz = mul(float2x2(cs, si, -si, cs), rd.xz);

                float3 ro = float3(0.0, 0.0, iTime * 1.5);
                float3 lp = ro + float3(0.0, 1.0, -0.5);

                float t = trace(ro, rd);

                float fog = smoothstep(0.0, 0.95, t / FAR);

                ro += rd * t;
                float3 sn = getNormal(ro);
                float3 sceneColor = doColor(ro, rd, sn, lp);

                float sh = softShadow(ro, lp, 16.0);

                rd = reflect(rd, sn);

                t = traceRef(ro + rd * 0.01, rd);

                ro += rd * t;
                sn = getNormal(ro);
                sceneColor += doColor(ro, rd, sn, lp) * 0.35;

                sceneColor *= sh;
                sceneColor = lerp(sceneColor, float3(0, 0, 0), fog);

                return float4(sqrt(clamp(sceneColor, 0.0, 1.0)), 1.0);
            }
            fixed4 frag(v2f _iParam) : SV_Target
            {
                return mainImage(gl_FragCoord);
            }
            ENDCG
        }
    }
}