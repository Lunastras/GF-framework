#ifndef UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED

float _StartFadeDistance;
float _FadeDistanceOffset;

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"
#include "../Quaternion.hlsl"
#include "../Dithering/DitheringFunctions.hlsl"
#include "../Dithering/inc/ParticlesDitheredInput.hlsl"


struct CustomVertInput 
{
    float4 positionOS               : POSITION;
    half4 color                     : COLOR;
    
    float4 texcoords            : TEXCOORD0; //texcoords.zw = center.xy
    float4 texcoords1           : TEXCOORD1; //texcoords1.x = center.z, texcoords1.yzw = custom1.xyz 
    float2 texcoords2           : TEXCOORD2; //texcoords2.x = custom1.w, texcoords2.y = random


    #if !defined(PARTICLES_EDITOR_META_PASS)
        float3 normalOS             : NORMAL;
        float4 tangentOS            : TANGENT;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void InitializeInputData(DitheredV2f input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS.xyz;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
#else
    half3 viewDirWS = input.viewDirWS;
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.positionWS.w);
    inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    inputData.shadowMask = half4(1, 1, 1, 1);

    #if defined(DEBUG_DISPLAY) && !defined(PARTICLES_EDITOR_META_PASS)
    inputData.vertexSH = input.vertexSH;
    #endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

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

float _BopRangeHalf;
float _SpinSpeed;

DitheredV2f ParticlesLitVertex(CustomVertInput input)
{
    float3 objCenter = float3(input.texcoords.zw, input.texcoords1.x); //particle center
    float4 gravity4 = float4(input.texcoords1.yzw, input.texcoords2.x);
    float3 vertPos, normal;
    float4 tangent;
    int offset = input.texcoords2.y * 100.0f;
    float4 rotationQuat;
    CalculateParticleRotation(objCenter, input.positionOS.xyz, gravity4
    , _SpinSpeed, offset, _BopRangeHalf, vertPos, rotationQuat);

    DitheredV2f output;

    normal = quatVec3Mult(rotationQuat, input.normalOS);
    tangent.xyz = quatVec3Mult(rotationQuat, input.tangentOS.xyz);
    tangent.w = input.tangentOS.w;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(vertPos);
    //VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(normal, tangent);

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

#ifdef _NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = half3(normalInput.normalWS);
    output.viewDirWS = viewDirWS;
#endif

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    half fogFactor = 0.0;
#if !defined(_FOG_FRAGMENT)
    fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    output.positionWS.xyz = vertexInput.positionWS.xyz;
    output.positionWS.w = fogFactor;
    output.clipPos = vertexInput.positionCS;
    output.color = GetParticleColor(input.color);
    
    output.positionSS = ComputeScreenPos(vertexInput.positionCS);
    output.positionVS = mul(UNITY_MATRIX_V, float4(vertexInput.positionWS, 1.0)).xyz;

    GetParticleTexcoords(output.texcoord, input.texcoords.xy);

#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    output.projectedPosition = vertexInput.positionNDC;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}
 
half4 ParticlesLitFragment(DitheredV2f input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    ParticleParams particleParams;
    InitParticleParamsDither(input, particleParams);

    float4 screenpos = float4(input.positionSS.xy / input.positionSS.w, 0, 0);
    float dst = max(0.0, length(input.positionVS) - _FadeDistanceOffset);

    half3 normalTS = SampleNormalTS(particleParams.uv, particleParams.blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    half4 albedo = SampleAlbedoDither(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), particleParams, screenpos, dst); 
    half3 diffuse = AlphaModulate(albedo.rgb, albedo.a);
    half alpha = albedo.a;

#if defined(_EMISSION)
    half3 emission = BlendTexture(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), particleParams.uv, particleParams.blendUv) * _EmissionColor.rgb;
#else
    half3 emission = half3(0, 0, 0);
#endif
    half4 specularGloss = SampleSpecularSmoothness(particleParams.uv, particleParams.blendUv, albedo.a, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));

#if defined(_DISTORTION_ON)
    diffuse = Distortion(half4(diffuse, alpha), normalTS, _DistortionStrengthScaled, _DistortionBlend, particleParams.projectedPosition);
#endif

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

    half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specularGloss, specularGloss.a, emission, alpha, normalTS);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);

    return color;
}

#endif // UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
