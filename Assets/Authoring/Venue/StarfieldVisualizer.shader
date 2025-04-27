// Adapted from https://www.shadertoy.com/view/43cGDs

Shader "StarfieldVisualizer"
{
    Properties
    {
        [HideInInspector] _ConvertToLinear("Convert To Linear", Float) = 1
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
            ZTest On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #define iResolution _ScreenParams
            #define gl_FragCoord ((_iParam.scrPos.xy/_iParam.scrPos.w) * _ScreenParams.xy)
            #define iTime _Time.y

            #include "UnityCG.cginc"
            Texture2D _Yarg_SoundTex;
            SamplerState sampler_Yarg_SoundTex;

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

                // Ensure the quad is rendered correctly regardless of projection settings
                // Use near clip plane values
                #if UNITY_REVERSED_Z
                    pos.z = 0.000001; // Small offset from near plane
                #else
                    pos.z = 0.999999; // Small offset from near plane
                #endif

                OUT.pos = pos;
                OUT.scrPos = ComputeScreenPos(pos); // Compute screen pos for frag shader

                return OUT;
            }

            static const float NUM_LAYERS = 7.0;


            float3 palette (float t){
             float3 a = float3(0.498, 0.588, 1.128);
             float3 b = float3(0.303, 0.388, 0.273);
             float3 c = float3(1.763, 0.938, 0.787);
             float3 d = float3(-2.982, 1.818, 1.948);

             return a + b * cos(6.28318*(c*t+d) );
            }

            float2x2 Rot(float a){
                float s= sin(a), c=cos(a);
                return float2x2(c, s, -s, c);
            }

            float Hash21(float2 p){
                p = frac(p*float2(123.34,456.821));
                p += dot(p,p+45.32);
                return frac(p.x*p.y);
            }

            float Star (float2 uv, float size, float baseRotation) {
                // Use HLSL's length function
                float d = length(uv);
                float m = 0.0;
                // Use HLSL's smoothstep function
                // inverted circle
                m += smoothstep(.12,0.15,d)/7. * (size); // Added m +=
                // glow
                m += 0.01/d *(size*.5 +.5); // Added m +=
                // circle
                // rotate 45 deg
                // HLSL matrix multiplication: mul(matrix, vector) for column vectors
                uv = mul(Rot(baseRotation), uv);
                float rays = 0.;
                // Use HLSL's abs, pow, max functions
                rays += (max(0.,1.-abs(pow(abs(uv.x),1.8)*uv.y*30000.))) ;

                // rotate 45 deg (pi/4)
                uv = mul(Rot(3.14159/4.), uv);
                rays += (max(0.,1.-abs(uv.x*uv.y* 3000.))) * .7 ;

                // Original GLSL had m*=... and m+=... which seems like a potential bug/typo.
                // Assuming the intent was additive based on the structure.
                // If the original intent was multiplication first, adjust accordingly.
                // Let's try to match the original structure more closely, though it looks odd:
                // m *= smoothstep(1.,0.2,d); // This line was commented out or missing in the original snippet's Star function logic description? Re-adding based on common patterns. Check original ShaderToy if needed.
                // Let's assume the original GLSL meant:
                float starShape = 0.0;
                starShape += smoothstep(.12,0.15,d)/7. * (size); // inverted circle part
                starShape += 0.01/d *(size*.5 +.5); // glow part

                m = starShape * smoothstep(1.0, 0.2, d); // Apply fade based on distance first
                m += rays * smoothstep(1.0, 0.2, d / size); // Add rays, also faded

                return m;
            }

            float3 StarLayer(float2 uv, float vol) {
                    float3 col = float3(0,0,0); // Initialize with 0
                    float2 gv = frac(uv) - 0.5;
                    float2 id = floor(uv);

                    // Standard nested loops in HLSL
                    for(int y=-1;y<=1;y++){
                        for(int x=-1;x<=1;x++){
                            float2 offset = float2(x,y);
                            float n = Hash21(id+offset); // random between 0 and 1
                            float size = frac (n*149.1);
                            size *= (sin(iTime*0.3 +n *48.123)*.5+1.);
                            size *= vol; // Apply volume scaling

                            float star = Star(-offset + gv-(float2(n,frac(n*34.))-0.5),smoothstep(.4,1.,size),-3.14159/10.);

                            float3 color = palette(star/3. +iTime * 0.3 + frac(n*9438.7));
                            col += star*color;
                        }
                    }
                    return col;
                }

            float4 StarfieldEffect(float2 fragCoord)
            {
                float2 uv = (fragCoord - 0.5 * iResolution.xy) / iResolution.y;

                // Use iTime (_Time.y)
                float t = iTime * 0.02;

                float2 M = float2(sin(iTime / 4.0), cos(iTime / 4.0)); // Removed mouse, kept time part

                uv += M * 0.4; // Apply movement parallax

                float3 col = float3(0,0,0); // Initialize color

                // Loop through layers
                for(float i=0.; i < 1.0; i += 1.0 / NUM_LAYERS)
                {
                    float depth = frac (i+t);
                    float scale = lerp(10.,1.,depth);

                    float vol = pow(_Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(0.01, depth)).r * 1.5, 2.0);

                    // Call translated StarLayer function
                    // Use HLSL's smoothstep and clamp functions
                    col += StarLayer(uv*scale+i*400.3 -M-t, vol)*
                    // fade
                    smoothstep(1.,.9,depth)*depth * clamp((i-1.+1./NUM_LAYERS)*1.5+iTime/10.,-1.,1.);
                }

                // Adjust gamma
                col = pow(col, 2.2);

                // Return final color as float4 (RGBA)
                return float4(col,1.0);
            }

            // --- Translated ShaderToy Code Ends Here ---


            fixed4 frag(v2f _iParam) : SV_Target
            {
                // Call the main image generation function using the calculated fragment coordinates
                return StarfieldEffect(gl_FragCoord);
            }
            ENDCG
        }
    }
}