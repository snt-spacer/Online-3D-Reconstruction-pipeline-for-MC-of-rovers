Shader "Unlit/PointCloudCutout_SPI"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

        SubShader
    {
        Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
        LOD 100
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Enable instancing and stereo rendering
            #pragma multi_compile_instancing
            #pragma multi_compile _ SINGLE_PASS_STEREO

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uvr : TEXCOORD0;
                float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 worldPos = float4(v.vertex.xyz, 1.0);
                float4 clipPos = UnityObjectToClipPos(worldPos); // Handles stereo transform

                float2 uv = v.uvr.xy;
                float radius = v.uvr.z;

                clipPos.x += (uv.x - 0.5) * 2 * radius * _ScreenParams.y / _ScreenParams.x;

#if UNITY_UV_STARTS_AT_TOP
                clipPos.y -= (uv.y - 0.5) * 2 * radius;
#else
                clipPos.y += (uv.y - 0.5) * 2 * radius;
#endif

                o.vertex = clipPos;
                o.uv = uv;
                o.color = v.color;
                o.color.a = 1;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                clip(color.a - 0.1);
                color.a = 1;
                return color * i.color;
            }
            ENDCG
        }
    }
}
