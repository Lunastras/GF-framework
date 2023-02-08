//Get angle axis quaternion on given axisNormalised. sinRadiansHalf and cosRadiansHalf
            // are sin(radians * 0.5) and cos(radians * 0.5) respectively. This is done for optimisations reasons
            float4 angleAxis(in float cosRadiansHalf, in float sinRadiansHalf, float3 axisNormalised) 
            {  
                axisNormalised = axisNormalised * sinRadiansHalf;
                float4 quat = float4(axisNormalised.x, axisNormalised.y, axisNormalised.z, cosRadiansHalf);

                //normalise
                float scaleInv = 1.0f / sqrt(quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w);
                quat *= scaleInv;

                return quat;
            }

            //Multiplies a quaternion with a vector3 
            float3 quatVec3Mult(in float4 quat, in float3 inVec3) 
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

                return float3(
		                    (1.0f - (num5 + num6)) * inVec3.x + (num7 - num12) * inVec3.y + (num8 + num11) * inVec3.z
		                    ,(num7 + num12) * inVec3.x + (1.0f - (num4 + num6)) * inVec3.y + (num9 - num10) * inVec3.z
		                    ,(num8 - num11) * inVec3.x + (num9 + num10) * inVec3.y + (1.0f - (num4 + num5)) * inVec3.z);
            }

            float4 quatMult(in float4 lhs, in float4 rhs) 
            {  
                return float4(
                lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x,
                lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);;
            }

            float4 quatFromTo(in float3 initial, in float3 final) 
            { 
                float4 outQuat;
                float dotf = dot(initial, final); 
                if (dotf < -0.9999999f) //opposite vectors
                {
                    float3 crossf = cross(float3(1,0,0), initial);
                    if (length(crossf) < 0.0000001f)
                        crossf = cross(float3(0,1,0), initial);
                    return angleAxis(0, 1, normalize(crossf)); //(in float cosRadiansHalf, in float sinRadiansHalf, float3 axisNormalised, out float4 quat) 
                } 
                else if (dotf < 0.9999999f) // normal case
                {
                    float3 crossf = cross(initial, final);
                    outQuat = normalize(float4(crossf.x, crossf.y, crossf.z, 1 + dotf));
                }
                else //vectors are identical, dot = 1
                {
                    outQuat = float4(0,0,0,1);
                }

                return outQuat;
            }

    