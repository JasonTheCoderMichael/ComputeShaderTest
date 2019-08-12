Shader "MJ/SimpleBoid"
{
    Properties
    {
        _MainColor("Main Color(RGB)", Color) = (1,1,1,1)
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
            
            struct BoidData
            {
                float3 pos;
                float3 rot;
                float3 scale;
            };

        #if SHADER_TARGET >= 45
            StructuredBuffer<BoidData> _BoidDataBuffer;
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
            float4 _MainColor;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                
                #if SHADER_TARGET >= 45
                {
                    BoidData data = _BoidDataBuffer[instanceID];
                    float3 worldPos = v.vertex.xyz * data.scale + data.pos;

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

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * _MainColor;
                return col;
            }
            ENDCG
        }
    }
}