// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/UnlitTestSpinning"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                uint instanceID : SV_InstanceID;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            //Get angle axis quaternion on given axisNormalised. sinRadiansHalf and cosRadiansHalf
            // are sin(radians * 0.5) and cos(radians * 0.5) respectively. This is done for optimisations reasons
            void angleAxis(in float cosRadiansHalf, in float sinRadiansHalf, float3 axisNormalised, out float4 quat) 
            {  
                axisNormalised = axisNormalised * sinRadiansHalf;
                quat = float4(axisNormalised.x, axisNormalised.y, axisNormalised.z, cosRadiansHalf);

                //normalise
                float scaleInv = 1.0f / sqrt(quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w);
                quat *= scaleInv;
            }

            //Multiplies a quaternion with a vector3 
            void quatVec3Mult(in float4 quat, in float3 inVec3, out float3 outVec3) 
            { 
                float num = quat.x * 2.0f;
		        float num2 = quat.y * 2.0f;
		        float num3 = quat.z * 2.0f;
		        float num4 = quat.x * num;
		        float num5 = quat.y * num2;
		        float num6 = quat.z * num3;
		        float num7 = quat.x * num2;
		        float num8 = quat.x * num3;
		        float num9 = quat.y * num3;
		        float num10 = quat.w * num;
		        float num11 = quat.w * num2;
		        float num12 = quat.w * num3;
		        outVec3.x = (1.0f - (num5 + num6)) * inVec3.x + (num7 - num12) * inVec3.y + (num8 + num11) * inVec3.z;
		        outVec3.y = (num7 + num12) * inVec3.x + (1.0f - (num4 + num6)) * inVec3.y + (num9 - num10) * inVec3.z;
		        outVec3.z = (num8 - num11) * inVec3.x + (num9 + num10) * inVec3.y + (1.0f - (num4 + num5)) * inVec3.z;
            }

            v2f vert (appdata v)
            {
                v2f o;
                float3 upvec = float3(0,1,0); 
                float time = _Time.y * 3.0f + 100.0f * v.uv.z;
                float timeSin = sin(time);

                v.vertex.y += 0.3f * timeSin; 
                float3 center = float3(v.uv.w, v.uv1.x, v.uv1.y);
                float3 objectPos = v.vertex.xyz - center;

                timeSin = sin(time * 0.2);
                float timeCos = cos(time * 0.2);

                float4 quat;
                float3 currentPos;
                angleAxis(timeCos, timeSin, upvec, quat);
                quatVec3Mult(quat, objectPos, objectPos);

                objectPos += center;
        
                o.vertex = UnityObjectToClipPos(objectPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * float4(abs(_SinTime.w),1,0,1);
            }
            ENDCG
        }
    }
}
