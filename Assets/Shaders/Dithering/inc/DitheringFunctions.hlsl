#ifndef DITHERINGFUNCTIONS_INCLUDED
#define DITHERINGFUNCTIONS_INCLUDED

void Dither_float(float In, float Scale, float2 ScreenPosition, out float Out)
{
    float2 uv = (ScreenPosition) * _ScreenParams.xy * Scale;

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

void GetDither(float Intensity, float Scale, float2 ScreenPosition, out float Out) {
    Out = 1.0f;
    
    #ifndef UNITY_PASS_SHADOWCASTER
        Dither_float(Intensity, Scale, ScreenPosition, Out);
    #endif //UNITY_PASS_SHADOWCASTER
}

void GetDitherAlphaThreshold(float Distance, float FadeStartDistance, out float Threshold) {
    Threshold = 1.0f;
    #ifndef UNITY_PASS_SHADOWCASTER
        Threshold = Distance / FadeStartDistance;
        Threshold = 1.0f - Threshold;
        Threshold = max(0, Threshold);
    #endif //UNITY_PASS_SHADOWCASTER
}



void ApplyDither_float(float Intensity, float Scale, float2 ScreenPosition, inout float AlphaClippingThreshold, inout float4 Color) {
    float discardPixel = step(Color.a, AlphaClippingThreshold);
    float dither;
    Dither_float(Intensity, Scale, ScreenPosition, dither);
    float ditherThreshold = 1.0f - Color.a;
    AlphaClippingThreshold = discardPixel * AlphaClippingThreshold + (1.0f - discardPixel) * ditherThreshold;
    Color.a = discardPixel * Color.a + (1.0f - discardPixel) * dither;
}


void ApplyDistanceDither_float(float Intensity, float Scale, float2 ScreenPosition, float Distance, float FadeStartDistance, inout float AlphaClippingThreshold, inout float4 Color) {
    #ifndef UNITY_PASS_SHADOWCASTER
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
