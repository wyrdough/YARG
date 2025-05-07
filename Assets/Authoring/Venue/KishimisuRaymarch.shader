// Adapted from https://www.shadertoy.com/view/cslSRr

Shader "VisualizerVenue/KishimisuRaymarch"
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

  // Set to 1 if you have a really good PC
            static const bool HIGH_PERF = false;

            static const float iterations = HIGH_PERF ? 50.0 : 30.0;
            static const float max_dist = HIGH_PERF ? 500.0 : 100.0;
            static const float light_neighbors_check = HIGH_PERF ? 1.0 : 0.0;

            static const float lightRep = 12.0;
            static const float attenuation = 20.0;

            float2x2 rot(float a) {
                float s = sin(a);
                float c = cos(a);
                return float2x2(c, -s, s, c);
            }

            // GLSL-style mod implementation
            float mod(float x, float y) {
                return x - y * floor(x / y);
            }
            float2 mod(float2 x, float y) { // Assuming y is scalar as in original rep
                return x - y * floor(x / y);
            }
            float3 mod(float3 x, float y) { // Assuming y is scalar as in original rep
                return x - y * floor(x / y);
            }

            float3 rep(float3 p, float r) {
                return (mod(p + r * 0.5, r) - r * 0.5);
            }

            float2 rep(float2 p, float r) {
                return (mod(p + r * 0.5, r) - r * 0.5);
            }

            float2 rid(float2 p, float r) {
                return floor((p + r * 0.5) / r);
            }

            float3 rid(float3 p, float r) { // Overload for float3 if needed, original uses vec2 for id
                return floor((p + r * 0.5) / r);
            }

            float3x3 mat3_hash_const = float3x3(
                float3(127.1, 311.7, 74.7),
                float3(269.5, 183.3, 246.1),
                float3(113.5, 271.9, 124.6)
            );

            static const float3x3 mat3_hash_const_colmajor = float3x3(
                127.1, 311.7,  74.7,  // Column 0
                269.5, 183.3, 246.1,  // Column 1
                113.5, 271.9, 124.6   // Column 2
            );

            float3 hash33(float3 p) {
                return frac(sin(mul(p, mat3_hash_const_colmajor)) * 43758.5453123);
            }

            float hash11(float p) {
                p = frac(p * .1031);
                p *= p + 33.33;
                return frac(2.*p*p);
            }

            float3 getLight(float d, float3 color_in) { // Renamed color to color_in to avoid conflict
                return max(float3(0.,0.,0.), color_in / (1. + pow(abs(d * attenuation), 1.3)) - .001*0.);
            }

            float getLevel(float x) {
                return _Yarg_SoundTex.Load(int3(int(x*512.), 0, 0)).r;
            }

            float getPitch(float freq, int octave){
               return getLevel(pow(2.0, float(octave)) * freq / 12000.0);
            }

            float logX(float x, float a, float c){
               return 1.0 / (exp(-a*(x-c)) + 1.0);
            }

            float logisticAmp(float amp){
               float c = 1.0 - (0.25);
               float a = 20.0; // Mouse input removed, using fixed value from original
               return (logX(amp, a, c) - logX(0.0, a, c)) / (logX(1.0, a, c) - logX(0.0, a, c));
            }

            float getAudioIntensityAt(float x) {
                x = abs(frac(x));
                float freq = pow(2., x*3.) * 261.;
                // iChannelTime[0] equivalent: check if iTime is very small (e.g., first frame)
                return iTime <= 0.01 ? hash11(x) : logisticAmp(getPitch(freq, 1));
            }

            float map(float3 p_map, inout float3 col_map) { // Renamed p and col to avoid conflict
                p_map.y = abs(p_map.y) - 13. - getAudioIntensityAt(0.)*1.2;

                float2 id = rid(p_map.xz, 2.);
                p_map.y += sin( length(sin(id/5.23 - iTime) * cos(id/10.45 + iTime))  ) * 8.;

                float3 fp = rep(p_map, lightRep);
                fp.y = p_map.y;

                const float r_loop = light_neighbors_check; // Use the preprocessor-defined value
                for (float j = -r_loop; j <= r_loop; j++) {
                    for (float i_loop = -r_loop; i_loop <= r_loop; i_loop++) { // Renamed i to i_loop
                        float3 off = float3(i_loop, 0., j) * lightRep;
                        float3 nid = rid(p_map - off, lightRep); // Use p_map here
                        float d = length( fp + off )-1.;

                        float3 c_hash = hash33(nid);
                        float3 light_val = float3(getAudioIntensityAt(c_hash.r*.33), getAudioIntensityAt(c_hash.g*.33+.33), 4.*getAudioIntensityAt(c_hash.b*.33+.67));
                        light_val *= getAudioIntensityAt(c_hash.r+c_hash.b+c_hash.g)+(c_hash.r+c_hash.b+c_hash.g);
                        col_map += getLight(d, light_val);
                    }
                }

                p_map.xz = rep(p_map.xz, 2.);
                return length(p_map) - 1.;
            }

            void initRayOriginAndDirection(float2 uv, out float3 ro, out float3 rd) {
                // vec2 m = iMouse.xy/iResolution.xy*2.-1.; // Mouse not used in ro/rd calculation
                ro = float3(iTime*8. -6., 0., 0.);

                float t_init = -iTime*.15*0.; // This is always 0
                float3 f = normalize(float3(cos(t_init),0,sin(t_init)));
                float3 r_vec = normalize(cross(float3(0,1,0), f)); // Renamed r to r_vec
                rd = normalize(f + uv.x*r_vec + uv.y*cross(f, r_vec));
            }

            float4 mainImage(float2 F) { // O is return, F is fragCoord
                float2 uv = (2.*F - iResolution.xy)/iResolution.y;
                float3 p_ray, ro_ray, rd_ray; // Renamed to avoid conflict
                float3 col_out = float3(0,0,0); // Initialize color output

                initRayOriginAndDirection(uv, ro_ray, rd_ray);

                float t_march = 0.; // Renamed t

                for (float i_march = 0.; i_march < iterations; i_march++) { // Renamed i
                    p_ray = ro_ray + t_march*rd_ray;
                    //p_ray.yz = mul(p_ray.yz, rot(-t_march*lerp(-.01, .01, sin(iTime*.1)*.5+.5))); // HLSL mul for matrix
                    t_march += map(p_ray, col_out);
                    if (t_march > max_dist) break;
                }

                col_out = pow(col_out, float3(.45,.45,.45));
                col_out = pow(col_out, 2.2); // Gamma correction
                return float4(col_out, 1.0);
            }

            fixed4 frag(v2f _iParam) : SV_Target
            {
                return mainImage(gl_FragCoord);
            }
            ENDCG
        }
    }
}