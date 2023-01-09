using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public class GfTools
{
    private const float PHI = 1.618033988749895f; // min squared length of a displacement vector required for a Move() to proceed.

    //public const float kEpsilon = 0.00000001F;
    public const float kEpsilonNormalSqrt = 1e-15F;

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

    //Thank you andrew-lukasik on the unity forum for this
    //https://answers.unity.com/questions/1872216/how-to-transformrotate-a-vector-onto-the-same-plan.html
    public static void StraightProjectOnPlane(ref Vector3 vector, Vector3 plane, Vector3 upVec)
    {
        GfTools.RemoveAxis(ref vector, upVec);
        new Plane { normal = plane }.Raycast(new Ray { origin = vector, direction = upVec }, out float Y);
        GfTools.Add3(ref vector, upVec * Y);
    }

    /*
    Implementation from quat.rotationTo function from toji/gl-matrix on github, found in quat.js
    */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion RotationToOld(Vector3 initial, Vector3 final)
    {
        float dot = Vector3.Dot(initial, final);
        if (dot < -0.9999999)
        {
            Vector3 cross = Vector3.Cross(Vector3.right, initial);
            if (cross.magnitude < 0.0000001)
                cross = Vector3.Cross(Vector3.up, initial);
            return Quaternion.AngleAxis(180, cross.normalized);
        }
        else if (dot < 0.9999999)
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

    /*
    Implementation from quat.rotationTo function from toji/gl-matrix on github, found in quat.js
    */
    public static Quaternion RotationTo(Vector3 initial, Vector3 final, Vector3 horizontalVector)
    {
        float dot = Vector3.Dot(initial, final);
        if (dot < -0.9999999) //opposite vectors
        {
            Debug.Log("OPPOSITE VECTOR");
            Vector3 cross = Vector3.Cross(horizontalVector, initial);
            if (cross.magnitude < 0.0000001)
                cross = Vector3.Cross(Vector3.up, initial);
            return Quaternion.AngleAxis(180, cross.normalized);
        }
        else if (dot < 0.9999999) // normal case
        {
            Vector3 cross = Vector3.Cross(initial, final);
            return new Quaternion(cross.x, cross.y, cross.z, 1 + dot).normalized;
        }
        else //vectors are identical, dot = 1
        {
            return Quaternion.identity;
        }
    }

    public static Quaternion RotationTo(Vector3 initial, Vector3 final)
    {
        return RotationTo(initial, final, Vector3.forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Minus3(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x -= rightHand.x; leftHand.y -= rightHand.y; leftHand.z -= rightHand.z; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add3(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x += rightHand.x; leftHand.y += rightHand.y; leftHand.z += rightHand.z; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mult3(ref Vector3 leftHand, float rightHand) { leftHand.x *= rightHand; leftHand.y *= rightHand; leftHand.z *= rightHand; }

    public static void Div3(ref Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; }
    public static Vector3 Div3(Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; return leftHand; }

    public static void RemoveAxis(ref Vector3 leftHand, Vector3 rightHand)
    {
        Mult3(ref rightHand, Vector3.Dot(leftHand, rightHand));
        Minus3(ref leftHand, rightHand);
    }

    public static Vector3 RemoveAxis(Vector3 leftHand, Vector3 rightHand) { RemoveAxis(ref leftHand, rightHand); return leftHand; }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Minus2(ref Vector2 leftHand, Vector2 rightHand) { leftHand.x -= rightHand.x; leftHand.y -= rightHand.y; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add2(ref Vector2 leftHand, Vector2 rightHand) { leftHand.x += rightHand.x; leftHand.y += rightHand.y; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mult2(ref Vector2 leftHand, float rightHand) { leftHand.x *= rightHand; leftHand.y *= rightHand; }

    public static void Div2(ref Vector2 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; }

    /*Props to allista from the kerbal space program forum for this incredible function*/
    public static float Angle(Vector3 a, Vector3 b)
    {
        Vector3 abm = a * b.magnitude;
        Vector3 bam = b * a.magnitude;
        return 2 * Mathf.Atan2((abm - bam).magnitude, (abm + bam).magnitude) * Mathf.Rad2Deg;
    }

    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        float unsignedAngle = Angle(from, to);

        float cross_x = from.y * to.z - from.z * to.y;
        float cross_y = from.z * to.x - from.x * to.z;
        float cross_z = from.x * to.y - from.y * to.x;
        float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
        return unsignedAngle * sign;
    }

    public static Quaternion Lerp4(Quaternion a, Quaternion b, float t)
    {
        Quaternion r;
        float t_ = 1 - t;
        r.x = t_ * a.x + t * b.x;
        r.y = t_ * a.y + t * b.y;
        r.z = t_ * a.z + t * b.z;
        r.w = t_ * a.w + t * b.w;
        r.Normalize();
        return r;
    }

    public static Quaternion QuaternionFraction(Quaternion a, float coef)
    {
        Quaternion r;
        float t_ = 1 - coef;
        r.x = coef * a.x;
        r.y = coef * a.y;
        r.z = coef * a.z;
        r.w = t_ + coef * a.w;
        r.Normalize();
        return r;
    }

    //https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
    public static Quaternion QuatSmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
    {
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
        ).normalized;

        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;

        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }

    // public static bool Equals(Quaternion lefHhand, Quaternion rightHand)
    // {
    // return Quaternion.Dot(lefHhand, rightHand) > 1.0f - kEpsilon;
    //}




}
