Shader "Unlit/VertexColor_SPI"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            // Required for stereo rendering and GPU instancing
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ SINGLE_PASS_STEREO

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID // For instanced rendering
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 vertexColor : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO // For stereo eye indexing
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);                // Setup instance data
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);  // Initialize stereo output
                o.vertex = UnityObjectToClipPos(v.vertex); // Correct stereo MVP transform
                o.vertexColor = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.vertexColor;
            }
            ENDCG
        }
    }
}
