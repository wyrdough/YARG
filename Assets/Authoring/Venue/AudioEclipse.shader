Shader "AudioEclipse"
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
            ZTest Off

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

            const float dots = 40.; //number of lights
            const float radius = .25; //radius of light ring
            const float brightness = 0.2;

            //convert HSV to RGB
            float3 hsv2rgb(float3 c){
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float4 mainImage(float2 fragCoord) {

	            float2 p=(fragCoord.xy-.5*iResolution)/min(iResolution.x,iResolution.y);
                float3 c=float3(0,0,0.1); //background color

                for(float i=0.;i<dots; i++){

		            //read frequency for this dot from audio input channel
		            //based on its index in the circle
		            // float vol =  texture(iChannel0, vec2(i/dots, 0.0)).x;
                    float u = floor((256 / dots) * i);
                    // float vol = _Yarg_SoundTex.Sample(sampler_Yarg_SoundTex, float2(u, 0.0f)).x;
                    float vol = _Yarg_SoundTex.Load(i/dots, 0).x + 0.06f;
		            float b = vol * brightness;

		            //get location of dot
                    float x = radius*cos(2.*3.14*float(i)/dots);
                    float y = radius*sin(2.*3.14*float(i)/dots);
                    float2 o = float2(x,y);

		            //get color of dot based on its index in the
		            //circle + time to rotate colors
                    float foo = (i + iTime * 10) / dots;
		            float3 dotCol = hsv2rgb(float3(foo,1.,1.0));

                    //get brightness of this pixel based on distance to dot
		            c += b/(length(p-o))*dotCol;
                }

                //black circle overlay
	            float dist = distance(p , float2(0,0));
	            c = c * smoothstep(0.26, 0.28, dist);

	            return float4(c,1);
            }


            fixed4 frag(v2f _iParam) : SV_Target {
                return mainImage(gl_FragCoord);
            }

            ENDCG
        }
    }
}
