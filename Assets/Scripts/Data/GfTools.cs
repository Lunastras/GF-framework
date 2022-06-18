using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTools
{
    //godbless stack overflow
    public static float AngleDifference(float deg1, float deg2)
    {
        float diff = (deg1 - deg2 + 180) % 360 - 180;
        return ((diff < -180 ? diff + 360 : diff));
    }

    public static Vector2 Degree2Vector2(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
    }

    public static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.01f;
    }



}
