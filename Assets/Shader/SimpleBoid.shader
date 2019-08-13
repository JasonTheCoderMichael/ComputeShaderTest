Shader "MJ/SimpleBoid"
{
    Properties
    {
        _MainColor("Main Color(RGB)", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    struct BoidData
    {
        float3 pos;
        float3 rot;
        float3 scale;
    };
    // #if SHADER_TARGET >= 45
        StructuredBuffer<BoidData> _BoidDataBuffer;
    // #endif
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            static float PI = 3.141593;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;

                // key //
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;

            // 对顶点进行欧拉角旋转, ZXY顺序 //
            float3 RotateEulerAngle(float3 vertex, float3 angleDeg)
            {
                angleDeg = angleDeg*PI/180.0;

                float sinX,cosX;
                sincos(angleDeg.x, sinX, cosX);
                float3x3 rotateMatrix_X = float3x3(
                    1, 0, 0,
                    0, cosX, sinX,
                    0, -sinX, cosX
                );

                float sinY,cosY;
                sincos(angleDeg.y, sinY, cosY);
                float3x3 rotateMatrix_Y = float3x3(
                    cosY, 0, -sinY,
                    0, 1, 0,
                    sinY, 0, cosY
                );

                float sinZ,cosZ;
                sincos(angleDeg.z, sinZ, cosZ);
                float3x3 rotateMatrix_Z = float3x3(
                    cosZ, sinZ, 0,
                    -sinZ, cosZ, 0,
                    0, 0, 1
                );

                // unity中欧拉角的旋转顺序是 Z -> X -> Y //
                // https://docs.unity3d.com/ScriptReference/Transform-eulerAngles.html //
                vertex = mul(rotateMatrix_Z, vertex);
                vertex = mul(rotateMatrix_X, vertex);
                vertex = mul(rotateMatrix_Y, vertex);

                return vertex;
            }

            // 左手坐标系 //
            float3 Rotate(float3 vertex, float3 forward, float3 up)
            {
                forward = normalize(forward);
                up = normalize(up);

                float3 right = cross(up, forward);
                right = normalize(right);

                up = cross(forward, right);
                up = normalize(up);

                float3x3 rotateMatrix = float3x3(
                    right.x, up.x, forward.x,
                    right.y, up.y, forward.y,
                    right.z, up.z, forward.z
                );

                // 不应该是用下面这个矩阵吗? //
                // float3x3 rotateMatrix = float3x3(
                //     right.x, right.y, right.z,
                //     up.x, up.y, up.z,
                //     forward.x, forward.y, forward.z
                // );

                return mul(rotateMatrix, vertex);
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                #if SHADER_TARGET >= 45
                {
                    BoidData data = _BoidDataBuffer[v.instanceID];

                    // scale //
                    float3 vertex = v.vertex.xyz * data.scale;

                    // rotate //
                    // vertex = RotateEulerAngle(vertex, data.rot);
                    vertex = Rotate(vertex, data.rot, float3(0,1,0));

                    // translate //
                    vertex += data.pos;

                    float3 worldPos = vertex;

                    // float3 worldPos = v.vertex.xyz * data.scale + data.pos;

                    o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));

                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
                    o.worldPos = worldPos;
                }
                #else
                {
                    o.pos = UnityObjectToClipPos(v.pos);
                }
                #endif
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                TRANSFER_SHADOW(o)
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 texColor = tex2D(_MainTex, i.uv) * _MainColor;

                float3 lightDir = normalize(_WorldSpaceLightPos0);
                float3 worldNormal = normalize(i.worldNormal);

                // ndotl //
                float NDotL = dot(lightDir, worldNormal);
                // half lambert //
                NDotL = NDotL * 0.5 + 0.5;

                float3 lightColor = _LightColor0.rgb;
                float3 diffuse = texColor * lightColor * NDotL;

                // ambient //
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT;

                // shadow //
                float shadow = SHADOW_ATTENUATION(i);

                float3 finalColor = diffuse * shadow + ambient;

                return float4(finalColor, 1);
            }
            ENDCG
        }

        // 方式1 //
        Pass
		{
			Tags{ "LightMode"="ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_shadowcaster

			#include "UnityCG.cginc"

            struct VertexData
            {
                float3 vertex : POSITION;
            };

			struct v2f
			{
                float4 vertex : SV_POSITION;
			};
			
			v2f vert(VertexData v, uint instanceID : SV_INSTANCEID)
			{
                v2f o;
                BoidData data = _BoidDataBuffer[instanceID];
                float3 worldPos = v.vertex.xyz * data.scale + data.pos;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));
                return o;
			}

			float4 frag(v2f i) : SV_TARGET
			{
                return 0;
			}
			ENDCG
		}

        // // 方式2, 阴影是错误的 //
        // Pass
		// {
		// 	Tags{ "LightMode"="ShadowCaster" }

		// 	CGPROGRAM
		// 	#pragma vertex vert
		// 	#pragma fragment frag

		// 	#pragma multi_compile_shadowcaster
        //     #pragma multi_compile_instancing

		// 	#include "UnityCG.cginc"

        //     struct VertexData
        //     {
        //         float3 vertex : POSITION;

        //         float3 normal : NORMAL;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
        //     };

		// 	struct v2f
		// 	{
		// 		V2F_SHADOW_CASTER;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
		// 	};
			
		// 	v2f vert(VertexData v, uint instanceID : SV_INSTANCEID)
		// 	{
		// 		v2f o;
        //         UNITY_SETUP_INSTANCE_ID(v);
        //         UNITY_TRANSFER_INSTANCE_ID(v, o);
		// 		TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
		// 		return o;
		// 	}

		// 	float4 frag(v2f i) : SV_TARGET
		// 	{
        //         UNITY_SETUP_INSTANCE_ID(i);
		// 		SHADOW_CASTER_FRAGMENT(i);
		// 	}
		// 	ENDCG
		// }
    }
}