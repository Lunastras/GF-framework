#ifndef UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"
#include "../Dithering/DitheringFunctions.hlsl"

struct CustomV2f
{
    float4 clipPos                  : SV_POSITION;
    float2 texcoord                 : TEXCOORD0;
    half4 color                     : COLOR;
    float4 positionSS                  : TEXCOORD9;   //screen space
    float3 positionVS                  : TEXCOORD10;

    #if defined(_FLIPBOOKBLENDING_ON)
        float3 texcoord2AndBlend    : TEXCOORD5;
    #endif

    #if !defined(PARTICLES_EDITOR_META_PASS)
        float4 positionWS           : TEXCOORD1;

        #ifdef _NORMALMAP
            half4 normalWS         : TEXCOORD2;    // xyz: normal, w: viewDir.x
            half4 tangentWS        : TEXCOORD3;    // xyz: tangent, w: viewDir.y
            half4 bitangentWS      : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
        #else
            half3 normalWS         : TEXCOORD2;
            half3 viewDirWS        : TEXCOORD3;
        #endif

        #if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
            float4 projectedPosition: TEXCOORD6;
        #endif

        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            float4 shadowCoord      : TEXCOORD7;
        #endif

        half3 vertexSH             : TEXCOORD8; // SH
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct CustomVertInput 
{
    float4 positionOS               : POSITION;
    half4 color                     : COLOR;
    
    float2 texcoords            : TEXCOORD0; 

    #if !defined(PARTICLES_EDITOR_META_PASS)
        float3 normalOS             : NORMAL;
        float4 tangentOS            : TANGENT;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void InitParticleParamsCustom(CustomV2f input, out ParticleParams output)
{
    output = (ParticleParams) 0;
    output.uv = input.texcoord;
    output.vertexColor = input.color;

    #if defined(_FLIPBOOKBLENDING_ON)
        output.blendUv = input.texcoord2AndBlend;
    #else
        output.blendUv = float3(0,0,0);
    #endif

    #if !defined(PARTICLES_EDITOR_META_PASS)
        output.positionWS = input.positionWS;
        output.baseColor = _BaseColor;

        #if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
            output.projectedPosition = input.projectedPosition;
        #else
            output.projectedPosition = float4(0,0,0,0);
        #endif
    #endif
}

void InitializeInputData(CustomV2f input, half3 normalTS, out InputData inputData)
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

CustomV2f ParticlesLitVertex(CustomVertInput input)
{
    CustomV2f output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

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

float _StartFadeDistance;
float _FadeDistanceOffset;

half4 SampleAlbedoDither(TEXTURE2D_PARAM(albedoMap, sampler_albedoMap), ParticleParams params, float4 screencoords, float distance)
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), params.uv, params.blendUv) * params.baseColor;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
    #if defined (_COLORADDSUBDIFF_ON)
        colorAddSubDiff = _BaseColorAddSubDiff;
    #endif
    albedo = MixParticleColor(albedo, half4(params.vertexColor), colorAddSubDiff);

    ApplyDistanceDither_float(1, screencoords, distance, _StartFadeDistance, _Cutoff, albedo);
    AlphaDiscard(albedo.a, _Cutoff);

     #if defined(_SOFTPARTICLES_ON)
         ALBEDO_MUL *= SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, params);
     #endif

     #if defined(_FADING_ON)
         ALBEDO_MUL *= CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, params.projectedPosition);
     #endif

    return albedo;
}
 
half4 ParticlesLitFragment(CustomV2f input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    ParticleParams particleParams;
    InitParticleParamsCustom(input, particleParams);

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
