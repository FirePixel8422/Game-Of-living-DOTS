Shader "Custom/InstancedColorShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Required for instancing
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Pass instance ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Colors) // Declare per-instance color
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // Get instance-specific color and pass it to the fragment shader
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Colors);
                
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color; // Directly use the color set in the vertex shader
            }
            ENDCG
        }
    }
}
