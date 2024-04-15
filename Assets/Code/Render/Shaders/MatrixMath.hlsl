            float4x4 LookRotation4x4(float3 eye, float3 zaxis, float3 up, float3 xaxis) {
                zaxis *= -1;
                xaxis *= -1;
                float3 yaxis = cross(zaxis, xaxis);

                float4x4 viewMatrix = float4x4(
                    xaxis.x, yaxis.x, zaxis.x, 0,
                    xaxis.y, yaxis.y, zaxis.y, 0,
                    xaxis.z, yaxis.z, zaxis.z, 0,
                    -dot(xaxis, eye), -dot(yaxis, eye), -dot(zaxis, eye), 1 );

                return viewMatrix;
            }

            float3x3 LookRotation3x3(float3 zaxis, float3 up, float3 xaxis) {
                zaxis *= -1;
                xaxis *= -1;
                float3 yaxis = cross(zaxis, xaxis);

                float3x3 viewMatrix = float3x3(
                    xaxis.x, yaxis.x, zaxis.x,
                    xaxis.y, yaxis.y, zaxis.y,
                    xaxis.z, yaxis.z, zaxis.z);

                return viewMatrix;
            }

            float3x3 LookRotation3x3(float3 zaxis, float3 up) { 
                float3 xaxis = normalize(cross(up, zaxis));
                return LookRotation3x3(zaxis, up, xaxis);
            } 

            float4x4 LookRotation4x4(float3 eye, float3 zaxis, float3 up) { 
                float3 xaxis = normalize(cross(up, zaxis));
                return LookRotation4x4(eye, zaxis, up, xaxis);
            }

            float4x4 LookAt4x4(float3 eye, float3 at, float3 up)
            {
                float3 zaxis = -normalize(at - eye);   
                float3 xaxis = normalize(cross(up, zaxis)); 
                float3 yaxis = cross(zaxis, xaxis);
                
                float4x4 viewMatrix = float4x4(
                    xaxis.x, yaxis.x, zaxis.x, 0,
                    xaxis.y, yaxis.y, zaxis.y, 0,
                    xaxis.z, yaxis.z, zaxis.z, 0,
                    -dot(xaxis, eye), -dot(yaxis, eye), -dot(zaxis, eye), 1 );

                return viewMatrix;
            }

            float3x3 LookAt3x3(float3 eye, float3 at, float3 up)
            {
                float3 zaxis = -normalize(at - eye);   
                float3 xaxis = normalize(cross(up, zaxis)); 
                float3 yaxis = cross(zaxis, xaxis);

                float3x3 viewMatrix = float3x3(
                    xaxis.x, yaxis.x, zaxis.x,
                    xaxis.y, yaxis.y, zaxis.y,
                    xaxis.z, yaxis.z, zaxis.z);

                return viewMatrix;
            }