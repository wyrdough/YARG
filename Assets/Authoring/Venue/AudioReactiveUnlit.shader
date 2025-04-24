Shader "Custom/AudioReactiveUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Yarg_SoundTex ("Audio Data", 2D) = "black" {}
        _BounceAmount ("Bounce Amount", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Yarg_SoundTex;
            float4 _MainTex_ST;
            float _BounceAmount;

            v2f vert (appdata v)
            {
                v2f o;

                // Get audio intensity at (0,0)
                float audioIntensity = tex2Dlod(_Yarg_SoundTex, float4(0, 0, 0, 0)).r;

                // Apply vertical offset in object space
                v.vertex.y += audioIntensity * _BounceAmount;
                // v.vertex.y += 5;

                // Transform to clip space
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}