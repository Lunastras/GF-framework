using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public struct ColorHsv
{
    public float Hue, Saturation, Value, Opacity;
    public bool Smoothing;

    public ColorHsv(Color aColor, bool aSmoothing = true)
    {
        float3 hsv = rgb2hsv(new(aColor.r, aColor.g, aColor.b));
        Hue = hsv.x;
        Saturation = hsv.y;
        Value = hsv.z;
        Opacity = aColor.a;
        Smoothing = aSmoothing;
    }

    public ColorHsv(Color32 aColor, bool aSmoothing = true)
    {
        float3 hsv = rgb2hsv(new(aColor.r / 255, aColor.g / 255, aColor.b / 255));
        Hue = hsv.x;
        Saturation = hsv.y;
        Value = hsv.z;
        Opacity = aColor.a / 255;
        Smoothing = aSmoothing;
    }

    public readonly Color GetColor() { return Hsv2RgbCol(this, Opacity, Smoothing); }

    public static implicit operator Color(ColorHsv d) => d.GetColor();
    public static implicit operator Color32(ColorHsv d) => d.GetColor();
    public static implicit operator float3(ColorHsv d) => new(d.Hue, d.Saturation, d.Value);
    public static implicit operator float4(ColorHsv d) => new(d.Hue, d.Saturation, d.Value, d.Opacity);

    //HSV functions from iq (https://www.shadertoy.com/view/MsS3Wc)
    public static float3 Hsv2rgbSmooth(in float3 c)
    {
        float3 rgb = clamp(abs(fmod(c.x * 6.0f + new float3(0.0f, 4.0f, 2.0f), 6.0f) - 3.0f) - 1.0f, 0.0f, 1.0f);
        rgb = rgb * rgb * (3.0f - 2.0f * rgb); // cubic smoothing	
        return c.z * lerp(new float3(1.0f), rgb, c.y);
    }

    public static float3 Hsv2rgb(float3 c)
    {
        float3 rgb = clamp(abs(fmod(c.x * 6.0f + new float3(0.0f, 4.0f, 2.0f), 6.0f) - 3.0f) - 1.0f, 0.0f, 1.0f);
        return c.z * lerp(float3(1.0f), rgb, c.y);
    }

    //From Sam Hocevar: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
    public static float3 rgb2hsv(float3 c)
    {
        float4 K = new(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
        float4 p = lerp(new float4(c.zy, K.wz), new float4(c.yz, K.xy), step(c.z, c.y));
        float4 q = lerp(new float4(p.xyw, c.x), new float4(c.x, p.yzx), step(p.x, c.x));

        float d = q.x - min(q.w, q.y);
        float e = 1.0e-10f;
        return new float3(abs(q.z + (q.w - q.y) / (6.0f * d + e)), d / (q.x + e), q.x);
    }

    public static Color Hsv2RgbCol(float3 anHsv, float anOpacity = 1, bool aSmooth = true)
    {
        float3 col = aSmooth ? Hsv2rgbSmooth(anHsv) : Hsv2rgb(anHsv);
        return new(col.x, col.y, col.z, anOpacity);
    }
}