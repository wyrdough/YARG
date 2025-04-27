// Adapted from https://www.shadertoy.com/view/XcBcR3

Shader "Fractal77Visualizer"
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

            float3 R(float3 p, float3 a, float r) {
                float cr = cos(r);
                float sr = sin(r);
                return lerp(a * dot(p, a), p, cr) + sr * cross(p, a);
            }

            // #define H(h)(cos((h)*6.3+vec3(0,23,21))*.5+.5)
            float3 H(float h) {
                // Use UNITY_TWO_PI if desired, 6.3 is close enough
                return cos(h * 6.3 + float3(0, 23, 21)) * 0.5 + 0.5;
            }

            // pow3 function replaced by direct multiplication later

            // float fft(float x) {
            //    return texelFetch( iChannel0, ivec2(128.*x,0), 0 ).x;
            // }
            float fft(float x) {
                // Use Texture2D.Load for texelFetch equivalent
                // Input coordinates must be int3(x, y, lod)
                return _Yarg_SoundTex.Load(int3(int(128.0 * x), 0, 0)).x;
            }

            float4 mainImage(float2 C) // Changed signature to return float4
            {
                float3 O = float3(0, 0, 0); // Initialize O

                // float bass  = texelFetch( iChannel0, ivec2(1,0), 0 ).x;
                float bass = _Yarg_SoundTex.Load(int3(1, 0, 0)).x;
                bass = clamp((bass - 0.5) * 2.0, 0.0, 1.0);

                float3 p = float3(0, 0, 0);
                float3 r = float3(iResolution.xy, 0); // iResolution is float4 in Unity (_ScreenParams)
                // Use iResolution.y for aspect correction as in original
                float3 d = normalize(float3((bass * 2.0 + C - 0.5 * r.xy) / iResolution.y, 1.0));

                float g = 0.0; // Initialize g
                float e = 0.0; // Initialize e
                float s = 0.0; // Initialize s

                // for(float i=0.,g=0.,e,s; ++i<99.; O.rgb+=mix(vec3(1) ,H(log(s)),.7)*.08*exp(-i*i*e))
                for (float i = 0.0; ++i < 99.0; ) // Standard for loop
                {
                    p = g * d;
                    p.z -= 0.6;
                    p = R(p, normalize(float3(1, 2, 3)), iTime * 0.3);
                    s = 4.0;

                    for (int j = 0; j++ < 8; ) // Standard for loop
                    {
                        p = abs(p);
                        // p=p.x<p.y?p.zxy:p.zyx;
                        p = (p.x < p.y) ? p.zxy : p.zyx; // Use ternary operator

                        // s*=e=1.8/min(dot(p,p),1.3);
                        e = 1.8 / min(dot(p, p), 1.3);
                        s *= e;

                        // p=p*e-vec3(12,3,3)+2.*fft((e)/99.);
                        p = p * e - float3(12, 3, 3) + 2.0 * fft(e / 99.0);
                    }
                    // g+=e=length(p.xz)/s;
                    e = length(p.xz) / s;
                    g += e;

                    // Accumulate color inside the loop body, after calculations using 'i', 's', 'e'
                    O.rgb += lerp(float3(1, 1, 1), H(log(s)), 0.7) * 0.08 * exp(-i * i * e);
                }

                // O=vec4(pow3(O.rgb,2),1.);
                O.rgb = O.rgb * O.rgb; // Replace pow3(v, 2) with v*v
                O = pow(O, 2.8);

                return float4(O, 1.0); // Return the final color
            }

            fixed4 frag(v2f _iParam) : SV_Target
            {
                return mainImage(gl_FragCoord);
            }
            ENDCG
        }
    }
}