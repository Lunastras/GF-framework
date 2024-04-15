#ifndef UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED

float _StartFadeDistance;
float _FadeDistanceOffset;
float _DitherIntensity;
float _DitherScale;
float _LambertDotOverride;
float _xAxisFollowFactor;

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"
#include "../../../Quaternion.hlsl"
#include "../../inc/DitheringFunctions.hlsl"
#include "../../../MatrixMath.hlsl"
#include "../../inc/ParticlesDitheredInput.hlsl"
#include "../../inc/CustomLighting.hlsl"


struct CustomVertInput 
{
    float4 positionOS               : POSITION;
    half4 color                     : COLOR;
    
    float4 texcoords            : TEXCOORD0; //texcoords.zw = center.xy
    float4 texcoords1           : TEXCOORD1; //texcoords1.x = center.z, texcoords1.yzw = custom1.xyz 
    float texcoords2           : TEXCOORD2; //texcoords2.x = custom1.w,


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

void FaceCamera(float3 objCenter, float3 vertPos, float4 gravity4, float xFollowFactor, out float3 finalPos, out float3x3 rotMat, out float4 rotQuat) {
    float3 upDir = gravity4.xyz; 
    upDir -= gravity4.w * objCenter; //if the gravity vector is a position (aka v.uv2.y = 1), then substract, if it is a direction (aka v.uv2.y = 0), don't do anything
    upDir = -normalize(upDir);

    vertPos -= objCenter;
    float3 dirToCamera = normalize(_WorldSpaceCameraPos - objCenter);//v.vertex.xyz;
    float3 rightDir = normalize(cross(upDir, dirToCamera));
    rotMat = LookRotation3x3(dirToCamera, upDir, rightDir);
    vertPos = mul(rotMat, vertPos);

    float angle = angleDeg(upDir, dirToCamera); 
    float auxAngle = 90 + xFollowFactor * (angle - 90);
    rotQuat =  angleDegAxis(auxAngle - angle, rightDir);

    finalPos = quatVec3Mult(rotQuat, vertPos) + objCenter;
}

DitheredV2f ParticlesLitVertex(CustomVertInput input)
{
    float3 objCenter = float3(input.texcoords.zw, input.texcoords1.x); //particle center
    float4 gravity4 = float4(input.texcoords1.yzw, input.texcoords2.x);
    float3 vertPos, normal;
    float4 tangent;
    float4 rotationQuat;
    float3x3 rotMat;
    FaceCamera(objCenter, input.positionOS.xyz, gravity4, _xAxisFollowFactor, vertPos, rotMat, rotationQuat);

    DitheredV2f output;

    normal = quatVec3Mult(rotationQuat, mul(rotMat, input.normalOS));
    tangent.xyz = quatVec3Mult(rotationQuat, mul(rotMat, input.tangentOS.xyz));
    tangent.w = input.tangentOS.w;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(vertPos);
    VertexNormalInputs normalInput = GetVertexNormalInputs(normal, tangent);
   // VertexNormalInputs normalInput = GetVertexNormalInputs(normal, input.tangentOS);

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

    float2 screenpos = float2(input.positionSS.xy / input.positionSS.w);
    float dst = max(0.0, length(input.positionVS) - _FadeDistanceOffset);

    half3 normalTS = SampleNormalTS(particleParams.uv, particleParams.blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    half4 albedo = SampleAlbedoDither(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), particleParams, screenpos, dst, _DitherIntensity, _DitherScale);  
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

    half4 color = UniversalFragmentBlinnPhongCustom(inputData, diffuse, specularGloss, specularGloss.a, emission, alpha, normalTS, _LambertDotOverride);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);

    return color;
}

#endif // UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED












