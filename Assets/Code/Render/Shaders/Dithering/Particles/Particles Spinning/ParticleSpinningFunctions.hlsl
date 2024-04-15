#ifndef PARTICLE_SPINNING_FUNCTIONS
#define PARTICLE_SPINNING_FUNCTIONS

#include "../../../Quaternion.hlsl"

void CalculateParticleRotation(float3 objCenter, float3 vertPos, float4 gravity4, float spinSpeed, float offset, float bopRange
, out float3 finalPos, out float4 rotationQuat) 
{
    float time = _Time.y * spinSpeed + offset; 
    float timeSin = sin(time);   

    float3 upDir = gravity4.xyz;
    if(gravity4.w == 1) { //if it's a position, then get the upvec 
        upDir -= objCenter; 
        upDir = normalize(upDir);
    }
    
    upDir = normalize(upDir);


    vertPos -= objCenter;
    vertPos.y += bopRange * timeSin;  

    timeSin = sin(time * 0.2);
    float timeCos = cos(time * 0.2); 

    rotationQuat = float4(0,0,0,1);

    rotationQuat = angleRadAxis(timeCos, timeSin, upDir);
    float4 verticalCorrectionQuat = quatFromTo(float3(0,1,0), upDir);
    rotationQuat = quatMult(rotationQuat, verticalCorrectionQuat);
    vertPos = quatVec3Mult(rotationQuat, vertPos);

    finalPos = vertPos + objCenter;
}

#endif //PARTICLE_SPINNING_FUNCTIONS
