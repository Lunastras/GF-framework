#ifndef DITHERINGFUNCTIONS_INCLUDED
#define DITHERINGFUNCTIONS_INCLUDED

float ReferenceHeight_half(out half Height) { Height = 1080; return Height; }

void NormalizedResolution_half(out half2 NormalizedResolution)
{
    half referenceHeight;
    ReferenceHeight_half(referenceHeight);
    NormalizedResolution = half2(round(referenceHeight * (_ScreenParams.x / _ScreenParams.y)), referenceHeight);
}

void NormalizedScreenParams_half(in half2 ScreenPosition, out half2 NormalizedResolution, out half2 NormalizedScreenPosition)
{
    NormalizedResolution_half(NormalizedResolution);
    NormalizedScreenPosition = NormalizedResolution * (ScreenPosition / _ScreenParams.xy);
}

float ReferenceHeight_float(out half Height) { return ReferenceHeight_half(Height); }
void NormalizedResolution_float(out float2 NormalizedResolution) { NormalizedResolution_half(NormalizedResolution); }
void NormalizedScreenParams_float(in float2 ScreenPosition, out float2 NormalizedResolution, out float2 NormalizedScreenPosition)
{
    NormalizedScreenParams_half(ScreenPosition, NormalizedResolution, NormalizedScreenPosition);
}

void DitherRaw_float(float4 In, float Scale, float2 uv, out float4 Out)
{
    uv *= Scale;
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
}

void Dither_float(float In, float Scale, float2 ScreenPosition, out float Out)
{
    float2 normalizedResolution;
    NormalizedScreenParams_half(ScreenPosition, normalizedResolution, ScreenPosition);
    float2 uv = ScreenPosition * normalizedResolution * Scale;

    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
}

void Dither_float(float4 In, float Scale, float2 ScreenPosition, out float4 Out)
{
    float2 res;
    NormalizedResolution_half(res);
    float2 uv = ScreenPosition * res;
    DitherRaw_float(In, Scale, uv, Out);
}

void GetDither(float Intensity, float Scale, float2 ScreenPosition, out float Out) {
    Out = 1.0f;
    
    #ifndef UNITY_PASS_SHADOWCASTER
        Dither_float(Intensity, Scale, ScreenPosition, Out);
    #endif //UNITY_PASS_SHADOWCASTER
}

void GetDitherAlphaThreshold(float Distance, float FadeStartDistance, out float Threshold) {
    Threshold = 1.0f;
    #ifndef UNITY_PASS_SHADOWCASTER
        Distance += FadeStartDistance * unity_OrthoParams.w; //disable distance dithering when projection is orthographic
        Threshold = Distance / FadeStartDistance;
        Threshold = 1.0f - Threshold;
        Threshold = max(0, Threshold);
    #endif //UNITY_PASS_SHADOWCASTER
}

void ApplyDither_float(float Intensity, float Scale, float2 ScreenPosition, inout float AlphaClippingThreshold, inout float4 Color) {
    AlphaClippingThreshold += 0.0001;
    float discardPixel = step(Color.a, AlphaClippingThreshold);
    float dither;
    Dither_float(Intensity, Scale, ScreenPosition, dither);
    float ditherThreshold = 1.0f - Color.a;
    AlphaClippingThreshold = discardPixel * AlphaClippingThreshold + (1.0f - discardPixel) * ditherThreshold;
    Color.a = discardPixel * Color.a + (1.0f - discardPixel) * dither;
}


void ApplyDistanceDither_float(float Intensity, float Scale, float2 ScreenPosition, float Distance, float FadeStartDistance, inout float AlphaClippingThreshold, inout float4 Color) {
    #ifndef UNITY_PASS_SHADOWCASTER
        AlphaClippingThreshold += 0.0001;
        float discardPixel = step(Color.a, AlphaClippingThreshold);
        float dither;
        GetDither(Intensity, Scale, ScreenPosition, dither);
        float ditherThreshold;
        GetDitherAlphaThreshold(Distance, FadeStartDistance, ditherThreshold);
        ditherThreshold = max(1.0f - Color.a, ditherThreshold);

        AlphaClippingThreshold = discardPixel * AlphaClippingThreshold + (1.0f - discardPixel) * ditherThreshold;
        Color.a = discardPixel * Color.a + (1.0f - discardPixel) * dither;
    #endif //UNITY_PASS_SHADOWCASTER
}

void GetDither_half(float Intensity, float Scale, float2 ScreenPosition, out float Out) {
    GetDither(Intensity, Scale, ScreenPosition, Out);
}

void GetDither_float(float Intensity, float Scale, float2 ScreenPosition, out float Out) {
    GetDither(Intensity, Scale, ScreenPosition, Out);
}



void GetDitherAlphaThreshold_float(float Distance, float FadeStartDistance, out float Threshold) { //copied because of a unity shadergraph bug
    GetDitherAlphaThreshold(Distance, FadeStartDistance, Threshold);
}

void GetDitherAlphaThreshold_half(float Distance, float FadeStartDistance, out float Threshold) { //same thing
    GetDitherAlphaThreshold(Distance, FadeStartDistance, Threshold);
}



#endif //DITHERINGFUNCTIONS_INCLUDED
