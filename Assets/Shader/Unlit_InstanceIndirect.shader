Shader "MJ/Unlit_InstanceIndirect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"
            
            struct Data
            {
                float3 translation;
                float3 eulerAngle;
                float3 scale;
            };

        #if SHADER_TARGET >= 45
            StructuredBuffer<Data> _DataBuffer;
        #endif

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
            float4 _MainTex_ST;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                
                #if SHADER_TARGET >= 45
                {
                    Data data = _DataBuffer[instanceID];

                    // key, 必须要加上 v.vertex.xyz //
                    float3 worldPos = v.vertex.xyz * data.scale + data.translation;

                    o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));
                }
                #else
                {
                    o.vertex = UnityObjectToClipPos(v.vertex);
                }
                #endif

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}