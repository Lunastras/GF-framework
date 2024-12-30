#ifndef HSV_INCLUDED
#define HSV_INCLUDED

//from https://www.shadertoy.com/view/MsS3Wc
// Official HSV to RGB conversion 
void hsv2rgb_float( in float3 c, out float3 col)
{
    float3 rgb = clamp( abs((c.x*6.0+float3(0.0,4.0,2.0)) % 6.0 -3.0)-1.0, 0.0, 1.0 );
	col = c.z * lerp(float3(1.0,1.0,1.0), rgb, c.y);
}

// Smooth HSV to RGB conversion 
void hsv2rgb_smooth_float( in float3 c, out float3 col)
{
    float3 rgb = clamp(abs((c.x*6.0+float3(0.0,4.0,2.0)) % 6.0 -3.0)-1.0, 0.0, 1.0 );

	rgb = rgb*rgb*(3.0-2.0*rgb); // cubic smoothing	

	col = c.z * lerp(float3(1.0,1.0,1.0), rgb, c.y);
}

//From Sam Hocevar: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
void rgb2hsv_float(float3 c, out float3 col)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    col = float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

#endif //DITHERINGFUNCTIONS_INCLUDED