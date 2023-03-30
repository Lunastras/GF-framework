#ifndef PARTICLE_DITHER_INPUT
#define PARTICLE_DITHER_INPUT

struct DitheredV2f
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

void InitParticleParamsDither(DitheredV2f input, out ParticleParams output)
{
    output = (ParticleParams)0;
    output.uv = input.texcoord;
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

half4 SampleAlbedoDither(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition
, TEXTURE2D_PARAM(albedoMap, sampler_albedoMap), float2 screencoords, float distance)
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), uv, blendUv) * color;

    // No distortion Support
    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined(_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    albedo = MixParticleColor(albedo, half4(particleColor), colorAddSubDiff);

    ApplyDistanceDither_float(1, screencoords, distance, _StartFadeDistance, _Cutoff, albedo);
    AlphaDiscard(albedo.a, _Cutoff);

    albedo.rgb = AlphaModulate(albedo.rgb, albedo.a);

#if defined(_SOFTPARTICLES_ON)
    albedo = SOFT_PARTICLE_MUL_ALBEDO(albedo, half(SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, projectedPosition)));
#endif

#if defined(_FADING_ON)
    ALBEDO_MUL *= CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, projectedPosition);
#endif

    return albedo;
}

half4 SampleAlbedoDither(TEXTURE2D_PARAM(albedoMap, sampler_albedoMap), ParticleParams params, float2 screencoords, float distance)
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), params.uv, params.blendUv) * params.baseColor;

    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined(_COLORADDSUBDIFF_ON)
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

#endif // PARTICLE_DITHER_INPUT
