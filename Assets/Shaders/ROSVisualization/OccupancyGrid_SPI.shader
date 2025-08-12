Shader "Unlit/OccupancyGrid_SPI"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ColorUnknown("Unknown Color", Color) = (0,0,0,0)
        _Color0("Color 0", Color) = (1,1,1,1)
        _Color100("Color 100", Color) = (0,0,0,1)
    }
        SubShader
        {
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            LOD 100
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                Offset -1, -1

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma multi_compile _ SINGLE_PASS_STEREO

                #include "UnityCG.cginc"

                fixed4 _ColorUnknown;
                fixed4 _Color0;
                fixed4 _Color100;

                sampler2D _MainTex;
                float4 _MainTex_ST;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;

                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(appdata v)
                {
                    UNITY_SETUP_INSTANCE_ID(v);

                    v2f o;
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.vertex = UnityObjectToClipPos(v.vertex); // stereo-aware MVP transform
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // byte range 0–255 scaled to 0–1, then remapped so 100 = 1
                    float frac = tex2D(_MainTex, i.uv).r * 255.0 / 100.0;
                    if (frac > 1)
                        return _ColorUnknown;
                    else
                        return lerp(_Color0, _Color100, frac);
                }
                ENDCG
            }
        }
}
