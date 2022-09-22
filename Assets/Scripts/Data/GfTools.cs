using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public class GfTools
{
    //godbless stack overflow
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AngleDifference(float deg1, float deg2)
    {
        float diff = (deg1 - deg2 + 180) % 360 - 180;
        return ((diff < -180 ? diff + 360 : diff));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Degree2Vector2(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
    }

    /*
    Implementation from quat.rotationTo function from toji/gl-matrix on github, found in quat.js
    */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion RotationTo(Vector3 initial, Vector3 final)
    {
        float dot = Vector3.Dot(initial, final);
        if (dot < -0.999999)
        {
            Vector3 cross = Vector3.Cross(Vector3.right, final);
            if (cross.magnitude < 0.000001)
                cross = Vector3.Cross(Vector3.up, initial);
            return Quaternion.AngleAxis(180, cross.normalized);
        }
        else if (dot < 0.999999)
        {
            Vector3 cross = Vector3.Cross(initial, final);
            float w = 1 + dot;
            return new Quaternion(cross.x, cross.y, cross.z, w).normalized;
        }
        else //vectors are identical, dot = 1
        {
            return Quaternion.identity;
        }
    }



}
