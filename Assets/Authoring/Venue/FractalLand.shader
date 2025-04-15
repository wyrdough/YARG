Shader "FractalLand"
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
            ZTest Off

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
            texture2D _Yarg_SoundTex;

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

            // HLSL Texture and Sampler declaration
            // Texture2D iChannel0 : register(t0); // Assumes texture is in slot t0
            SamplerState sampler_Yarg_SoundTex; // : register(s0); // Assumes sampler is in slot s0

            //  --- Ported Defines ---
            //#define SHOWONLYEDGES
            #undef NYAN
            #undef WAVES
            #undef BORDER

            #define RAY_STEPS 150

            #define BRIGHTNESS 1.2f
            #define GAMMA 1.4f
            #define SATURATION 0.65f

            #define detail 0.001f
            #define t (_Time.y * 0.5f) // Use Unity's time

            // --- Ported Constants ---
            static const float3 origin = float3(-1.0f, 0.7f, 0.0f);
            // Note: 'det' is calculated dynamically in raymarch

            // --- Uniforms / Properties ---
            sampler2D _Channel0Tex;
            sampler2D _Channel1Tex;
            float4 _MouseParams; // xy: position (pixels), z: button state (0 or 1)

            // --- Helper Functions (HLSL) ---

            // 2D rotation function
            float2x2 rot(float a) {
                float c = cos(a);
                float s = sin(a);
                // HLSL matrices are row-major by default, but construction order matters.
                // This matches GLSL's column-major construction visually.
                return float2x2(c, -s, s, c);
            }

            // "Amazing Surface" fractal
            float4 formula(float4 p) {
                p.xz = abs(p.xz + 1.0f) - abs(p.xz - 1.0f) - p.xz;
                p.y -= 0.25f;
                // Use mul() for matrix * vector
                p.xy = mul(rot(radians(35.0f)), p.xy);
                p = p * 2.0f / clamp(dot(p.xyz, p.xyz), 0.2f, 1.0f);
                return p;
            }

            // Distance function
            // Forward declare normal function because de uses it indirectly via raymarch->normal
            float3 normal(float3 p, float det, out float edge);

            float mod(float x, float y)
            {
              return x - y * floor(x/y);
            }

            float de(float3 pos) {
                #ifdef WAVES
                pos.y += sin(pos.z - t * 6.0f) * 0.15f; //waves!
                #endif
                float hid = 0.0f;
                float3 tpos = pos;
                tpos.z = abs(3.0f - mod(tpos.z, 6.0f)); // Use fmod for HLSL
                float4 p = float4(tpos, 1.0f);
                for (int i = 0; i < 4; i++) { p = formula(p); }
                float fr = (length(max(float2(0.0f, 0.0f), p.yz - 1.5f)) - 1.0f) / p.w;
                float ro = max(abs(pos.x + 1.0f) - 0.3f, pos.y - 0.35f);
                ro = max(ro, -max(abs(pos.x + 1.0f) - 0.1f, pos.y - 0.5f));
                pos.z = abs(0.25f - mod(pos.z, 0.5f));
                ro = max(ro, -max(abs(pos.z) - 0.2f, pos.y - 0.3f));
                ro = max(ro, -max(abs(pos.z) - 0.01f, -pos.y + 0.32f));
                float d = min(fr, ro);
                return d;
            }

            // Camera path
            float3 path(float ti) {
                ti *= 1.5f;
                float3 p = float3(sin(ti), (1.0f - sin(ti * 2.0f)) * 0.5f, -ti * 5.0f) * 0.5f;
                return p;
            }

            // Calc normals, and edge detection
            // 'edge' is now an out parameter
            float3 normal(float3 p, float det, out float edge) {
                // Use a small epsilon based on dynamic detail 'det'
                float3 e = float3(0.0f, det * 5.0f, 0.0f);

                float d1 = de(p - e.yxx); float d2 = de(p + e.yxx);
                float d3 = de(p - e.xyx); float d4 = de(p + e.xyx);
                float d5 = de(p - e.xxy); float d6 = de(p + e.xxy);
                float d = de(p);

                // Calculate edge value
                edge = abs(d - 0.5f * (d2 + d1)) + abs(d - 0.5f * (d4 + d3)) + abs(d - 0.5f * (d6 + d5));
                edge = min(1.0f, pow(edge, 0.55f) * 15.0f);

                return normalize(float3(d1 - d2, d3 - d4, d5 - d6));
            }


            // Used Nyan Cat code by mu6k, with some mods
            float4 rainbow(float2 p)
            {
                float q = max(p.x, -0.1f);
                float s = sin(p.x * 7.0f + t * 70.0f) * 0.08f;
                p.y += s;
                p.y *= 1.1f;

                float4 c = float4(0,0,0,0); // Initialize to transparent black
                // Use explicit float comparisons
                if (p.x > 0.0f) c = float4(0, 0, 0, 0); else
                if ((0.0f / 6.0f < p.y) && (p.y < 1.0f / 6.0f)) c = float4(255, 43, 14, 255) / 255.0f; else
                if ((1.0f / 6.0f < p.y) && (p.y < 2.0f / 6.0f)) c = float4(255, 168, 6, 255) / 255.0f; else
                if ((2.0f / 6.0f < p.y) && (p.y < 3.0f / 6.0f)) c = float4(255, 244, 0, 255) / 255.0f; else
                if ((3.0f / 6.0f < p.y) && (p.y < 4.0f / 6.0f)) c = float4(51, 234, 5, 255) / 255.0f; else
                if ((4.0f / 6.0f < p.y) && (p.y < 5.0f / 6.0f)) c = float4(8, 163, 255, 255) / 255.0f; else
                if ((5.0f / 6.0f < p.y) && (p.y < 6.0f / 6.0f)) c = float4(122, 85, 255, 255) / 255.0f; else
                if (abs(p.y) - 0.05f < 0.0001f) c = float4(0.0f, 0.0f, 0.0f, 1.0f); else // Use float literals
                if (abs(p.y - 1.0f) - 0.05f < 0.0001f) c = float4(0.0f, 0.0f, 0.0f, 1.0f); else
                    c = float4(0, 0, 0, 0);

                c.a *= 0.8f - min(0.8f, abs(p.x * 0.08f));
                c.xyz = lerp(c.xyz, float3(length(c.xyz), length(c.xyz), length(c.xyz)), 0.15f); // Use lerp for mix
                return c;
            }

            float4 nyan(float2 p)
            {
                float2 uv = p * float2(0.4f, 1.0f);
                float ns = 3.0f;
                float nt = _Time.y * ns; // Use Unity time directly
                nt -= mod(nt, 240.0f / 256.0f / 6.0f);
                nt = mod(nt, 240.0f / 256.0f);
                float ny = mod(_Time.y * ns, 1.0f);
                ny -= mod(ny, 0.75f); ny *= -0.05f;

                // Use tex2D for sampling. Adjust UVs if texture origin differs.
                // Shadertoy's 0.5 - uv.y implies bottom-left origin. Unity is top-left.
                // If _Channel1Tex is imported normally, use 1.0 - (0.5 - uv.y - ny) = 0.5 + uv.y + ny
                // However, let's try matching the Shadertoy coord style first:
                float2 texCoords = float2(uv.x / 3.0f + 210.0f / 256.0f - nt + 0.05f, 0.5f - uv.y - ny);
                float4 color = tex2D(_Channel1Tex, texCoords);

                if (uv.x < -0.3f) color.a = 0.0f;
                if (uv.x > 0.2f) color.a = 0.0f;
                return color;
            }


            // Raymarching and 2D graphics
            float3 raymarch(in float3 from, in float3 dir)
            {
                float3 p = from; // Initialize p
                float3 norm = float3(0,0,1); // Default normal
                float edge = 0.0f; // Local edge value
                float d = 100.0f;
                float totdist = 0.0f;
                float det = 0.0f; // Dynamic detail

                for (int i = 0; i < RAY_STEPS; i++) {
                    if (d > det && totdist < 25.0f) {
                        p = from + totdist * dir;
                        d = de(p);
                        det = detail * exp(0.13f * totdist); // Use HLSL exp
                        totdist += d;
                    } else {
                        break; // Exit loop early if possible
                    }
                }

                float3 col = float3(0.0f, 0.0f, 0.0f);

                if (totdist < 25.0f) // Hit something
                {
                     p -= (det - d) * dir; // Refine position
                     norm = normal(p, det, edge); // Calculate normal and edge here
                     #ifdef SHOWONLYEDGES
                     col = 1.0f - float3(edge, edge, edge); // show wireframe version
                     #else
                     col = (1.0f - abs(norm)) * max(0.0f, 1.0f - edge * 0.8f); // set normal as color with dark edges
                     #endif
                }

                totdist = clamp(totdist, 0.0f, 26.0f);

                // Background calculation
                float3 bgDir = dir; // Use a copy for background calcs
                bgDir.y -= 0.02f;

                // Use tex2D for sampling _Channel0Tex
                float sunsize = 7.0f - max(0.0f, tex2D(sampler_Yarg_SoundTex, float2(0.6f, 0.2f)).x) * 5.0f; // responsive sun size
                float an = atan2(bgDir.y, bgDir.x) + _Time.y * 1.5f; // angle for drawing and rotating sun (use atan2(y,x))
                float s = pow(clamp(1.0f - length(bgDir.xy) * sunsize - abs(0.2f - mod(an, 0.4f)), 0.0f, 1.0f), 0.1f); // sun
                float sb = pow(clamp(1.0f - length(bgDir.xy) * (sunsize - 0.2f) - abs(0.2f - mod(an, 0.4f)), 0.0f, 1.0f), 0.1f); // sun border
                float sg = pow(clamp(1.0f - length(bgDir.xy) * (sunsize - 4.5f) - 0.5f * abs(0.2f - mod(an, 0.4f)), 0.0f, 1.0f), 3.0f); // sun rays
                float y = lerp(0.45f, 1.2f, pow(smoothstep(0.0f, 1.0f, 0.75f - bgDir.y), 2.0f)) * (1.0f - sb * 0.5f); // gradient sky

                // set up background with sky and sun
                float3 backg = float3(0.5f, 0.0f, 1.0f) * ((1.0f - s) * (1.0f - sg) * y + (1.0f - sb) * sg * float3(1.0f, 0.8f, 0.15f) * 3.0f);
                backg += float3(1.0f, 0.9f, 0.1f) * s;
                backg = max(backg, sg * float3(1.0f, 0.9f, 0.5f));

                // Mix object color with background
                if (totdist > 25.0f) {
                    col = backg; // Hit background
                } else {
                    // distant fading to sun color
                    col = lerp(float3(1.0f, 0.9f, 0.3f), col, exp(-0.004f * totdist * totdist));
                }

                col = pow(col, float3(GAMMA, GAMMA, GAMMA)) * BRIGHTNESS;
                col = lerp(float3(length(col), length(col), length(col)), col, SATURATION);

                #ifdef SHOWONLYEDGES
                col = 1.0f - float3(length(col), length(col), length(col));
                #else
                col *= float3(1.0f, 0.9f, 0.85f);
                #ifdef NYAN
                float3 nyanDir = dir; // Use a copy
                nyanDir.yx = mul(rot(nyanDir.x), nyanDir.yx); // Apply rotation
                float2 ncatpos = (nyanDir.xy + float2(-3.0f + mod(-t, 6.0f), -0.27f));
                float4 ncat = nyan(ncatpos * 5.0f);
                float4 rain = rainbow(ncatpos * 10.0f + float2(0.8f, 0.5f));
                // Mix based on distance (only if background wasn't hit directly)
                if (totdist <= 25.0f && totdist > 8.0f) {
                     col = lerp(col, max(float3(0.2f, 0.2f, 0.2f), rain.xyz), rain.a * 0.9f);
                     col = lerp(col, max(float3(0.2f, 0.2f, 0.2f), ncat.xyz), ncat.a * 0.9f);
                } else if (totdist > 25.0f) { // If we hit background, still apply nyan if far enough
                     // This logic might need adjustment depending on desired background interaction
                     if (length(from - p) > 8.0f) { // Check distance from camera instead of totdist
                        col = lerp(col, max(float3(0.2f, 0.2f, 0.2f), rain.xyz), rain.a * 0.9f);
                        col = lerp(col, max(float3(0.2f, 0.2f, 0.2f), ncat.xyz), ncat.a * 0.9f);
                     }
                }
                #endif // NYAN
                #endif // SHOWONLYEDGES

                return col;
            }

            // get camera position and modify direction
            float3 move(inout float3 dir) {
                float3 go = path(t);
                float3 adv = path(t + 0.7f);
                float hd = de(adv); // Original had this, but wasn't used?
                float3 advec = normalize(adv - go);
                float an = adv.x - go.x; an *= min(1.0f, abs(adv.z - go.z)) * sign(adv.z - go.z) * 0.7f;
                dir.xy = mul(rot(an), dir.xy); // Use mul()
                an = advec.y * 1.7f;
                dir.yz = mul(rot(an), dir.yz); // Use mul()
                an = atan2(advec.z, advec.x); // Use atan2(y,x)
                dir.xz = mul(rot(an), dir.xz); // Use mul()
                return go;
            }

            float4 mainImage(float2 fragCoord)
            {
                float2 uv = fragCoord.xy / iResolution.xy * 2. - 1;
                float2 oriuv = uv * 2.0f - 1.0f; // Save original [-1,1] uv for border calc
                uv = uv * 2.0f - 1.0f; // Convert to [-1, 1] range

                // Apply aspect ratio correction (match GLSL)
                uv.x *= _ScreenParams.x / _ScreenParams.y;

                // Mouse input processing
                // float2 mouse = (_MouseParams.xy / _ScreenParams.xy - 0.5f) * 3.0f;
                // // Flip mouse Y if needed, Unity's origin is bottom-left for mouse coords usually
                // // mouse.y *= -1.0f; // Uncomment if mouse Y seems inverted
                // if (_MouseParams.z < 1.0f) mouse = float2(0.0f, -0.05f); // Default if no click
                float2 mouse = float2(0.0f, -0.05f);

                float fov = 0.9f - max(0.0f, 0.7f - _Time.y * 0.3f); // Use Unity time
                float3 dir = normalize(float3(uv * fov, 1.0f));

                // Apply mouse rotation
                dir.yz = mul(rot(mouse.y), dir.yz);
                dir.xz = mul(rot(mouse.x), dir.xz);

                // Calculate camera origin and modify direction based on path
                float3 from = origin + move(dir); // 'move' modifies 'dir' in place

                // Perform raymarching
                float3 color = raymarch(from, dir);

                #ifdef BORDER
                // Apply border vignette using the original [-1,1] uv
                color = lerp(float3(0.0f, 0.0f, 0.0f), color, pow(max(0.0f, 0.95f - length(oriuv * oriuv * oriuv * float2(1.05f, 1.1f))), 0.3f));
                #endif

                return float4(color, 1.0f);
            }

            fixed4 frag(v2f _iParam) : SV_Target {
                return mainImage(gl_FragCoord);
            }

            ENDCG
        }
    }
}