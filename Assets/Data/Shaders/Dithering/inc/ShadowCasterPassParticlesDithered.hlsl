#ifndef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "DitheringFunctions.hlsl"

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;


struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    float4 color    : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float3 uv           : TEXCOORD0; //uv.z = color alpha
    float4 positionCS   : SV_POSITION;
    float4 positionSS   : TEXCOORD1;
}; 

float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
   
    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);

    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionCS = GetShadowPositionHClip(input);
    output.positionSS = ComputeScreenPos(output.positionCS);
    output.uv.z = input.color.a;

    return output;
}


float _DitherIntensity;
float _DitherScale;

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    input.positionSS.xy /= input.positionSS.w;
    half4 alpha = SampleAlbedoAlpha(input.uv.xy, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    alpha.a *= _BaseColor.a * input.uv.z;
    ApplyDither_float(_DitherIntensity, _DitherScale, input.positionSS.xy, _Cutoff, alpha);
    Alpha(alpha.a, half4(0,0,0,1), _Cutoff);
    return 0;
}

#endif