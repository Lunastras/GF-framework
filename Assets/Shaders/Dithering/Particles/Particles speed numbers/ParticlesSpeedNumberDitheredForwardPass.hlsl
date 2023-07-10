#ifndef UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"
#include "../../inc/DitheringFunctions.hlsl"
#include "../../inc/CustomLighting.hlsl"

struct CustomV2f
{
    float4 clipPos : SV_POSITION;
    float4 texcoord : TEXCOORD0; // texcoord.z = sprite start number x coord,
    half4 color : COLOR;
    float4 positionSS : TEXCOORD9; // screen space
    float3 positionVS : TEXCOORD10;
    float4 digits : TEXCOORD11; // the digits of the value

#if defined(_FLIPBOOKBLENDING_ON)
    float3 texcoord2AndBlend : TEXCOORD5;
#endif

#if !defined(PARTICLES_EDITOR_META_PASS)
    float4 positionWS : TEXCOORD1;

#ifdef _NORMALMAP
    half4 normalWS : TEXCOORD2;    // xyz: normal, w: viewDir.x
    half4 tangentWS : TEXCOORD3;   // xyz: tangent, w: viewDir.y
    half4 bitangentWS : TEXCOORD4; // xyz: bitangent, w: viewDir.z
#else
    half3 normalWS : TEXCOORD2;
    half3 viewDirWS : TEXCOORD3;
#endif

#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    float4 projectedPosition : TEXCOORD6;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD7;
#endif

    half3 vertexSH : TEXCOORD8; // SH
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct CustomVertInput
{
    float4 positionOS : POSITION;
    half4 color : COLOR;

    float3 texcoords : TEXCOORD0; // texcoords.z = particle speed

#if !defined(PARTICLES_EDITOR_META_PASS)
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void InitParticleParamsCustom(CustomV2f input, out ParticleParams output)
{
    output = (ParticleParams)0;
    output.uv = input.texcoord.xy;
    output.vertexColor = input.color;

#if defined(_FLIPBOOKBLENDING_ON)
    output.blendUv = input.texcoord2AndBlend;
#else
    output.blendUv = float3(0, 0, 0);
#endif

#if !defined(PARTICLES_EDITOR_META_PASS)
    output.positionWS = input.positionWS;
    output.baseColor = _BaseColor;

#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    output.projectedPosition = input.projectedPosition;
#else
    output.projectedPosition = float4(0, 0, 0, 0);
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

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
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

    const float MAX_VALUE = 9999;
    const int MAX_DECIMALS = 4;
    const float DIGIT_OFFSET = 1.0 / 4.0;              // 1.0 / MAX_DECIMALS
    const float DIGIT_OFFSET_HALF = (1.0 / 4.0) / 2.0; // DIGIT_OFFSET / 2.0
    const uint TWO = 2;

    float value = round(input.texcoords.z);
    value = min(value, MAX_VALUE);

    int i = 0, numDigits = 0;

    while (i < MAX_DECIMALS)
    {
        output.digits[MAX_DECIMALS - i++ - 1] = value % 10;
        numDigits += (value > 0);
        value = floor(value / 10.0);
    }

    numDigits = max(numDigits, 1);

    float startUvXCoord = 0.5 - (DIGIT_OFFSET_HALF * (numDigits % TWO) + floor(numDigits * 0.5) * DIGIT_OFFSET);
    float2 finalTexCoords;
    GetParticleTexcoords(finalTexCoords, input.texcoords.xy);
    output.texcoord = float4(finalTexCoords, startUvXCoord, MAX_DECIMALS - numDigits);

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
float _DitherIntensity;
float _DitherScale;
float _LambertDotOverride;

half4 SampleAlbedoDither(TEXTURE2D_PARAM(albedoMap, sampler_albedoMap), ParticleParams params, float2 screencoords, float distance, float4 digits, float4 texCoords, float ditherIntensity, float ditherScale)
{
    float startUvXCoord = texCoords.z;

    float isInBounds = texCoords.x >= startUvXCoord && texCoords.x <= 1.0f - startUvXCoord;
    const float MAX_DECIMALS = 4.0f;
    const float DIGIT_OFFSET = 1.0f / MAX_DECIMALS;

    float startIndex = round(texCoords.w);

    float uvX = texCoords.x - startUvXCoord;
    float digitUvX = (uvX % DIGIT_OFFSET) / DIGIT_OFFSET;
    int digitIndex = max(0, uvX / DIGIT_OFFSET);
    float2 uv = float2(digits[startIndex + digitIndex] * 0.1f + digitUvX * 0.1f, texCoords.y);
    //  float4 albedo = UNITY_SAMPLE_TEX2DARRAY(_BaseMap, float3(digitUvX, texCoords.y, ));
    float4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), uv, params.blendUv) * params.baseColor;
    albedo.a *= isInBounds;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined(_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    albedo = MixParticleColor(albedo, half4(params.vertexColor), colorAddSubDiff);

    ApplyDistanceDither_float(ditherIntensity, ditherScale, screencoords, distance, _StartFadeDistance, _Cutoff, albedo);
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

    float2 screenpos = float2(input.positionSS.xy / input.positionSS.w);
    float dst = max(0.0, length(input.positionVS) - _FadeDistanceOffset);

    half3 normalTS = SampleNormalTS(particleParams.uv, particleParams.blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    half4 albedo = SampleAlbedoDither(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), particleParams, screenpos, dst, input.digits, input.texcoord, _DitherIntensity, _DitherScale); 
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
