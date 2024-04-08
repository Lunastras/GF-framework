using MEC;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Collections.Generic;
using static Unity.Mathematics.math;

public static class GfcToolsStatic
{
    public static bool IsEmpty(this string aString) { return aString == null || aString.Length == 0; }
}

public class GfcTools
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
        GfcTools.RemoveAxis(ref vector, upVec);
        new Plane { normal = plane }.Raycast(new Ray { origin = vector, direction = upVec }, out float Y);
        GfcTools.Add3(ref vector, upVec * Y);
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

    /*

    public static int Compare(string aLeft, string aRight, StringComparison aStringComparison)
    {
        bool leftEmpty = aLeft == null || aLeft.Length == 0;
        bool rightEmpty = aRight == null || aRight.Length == 0;

        //if (leftEmpty == rightEmpty && leftEmpty)
        //return 0;
    }*/

    //inlining these functions makes it slower by a considerable ammount from the local tests performed
    public static void Minus3(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x -= rightHand.x; leftHand.y -= rightHand.y; leftHand.z -= rightHand.z; }


    public static void Add3(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x += rightHand.x; leftHand.y += rightHand.y; leftHand.z += rightHand.z; }


    public static void Mult3(ref Vector3 leftHand, float rightHand) { leftHand.x *= rightHand; leftHand.y *= rightHand; leftHand.z *= rightHand; }

    public static void Div3(ref Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; }

    public static void Div3Safe(ref Vector3 leftHand, float rightHand)
    {
        if (rightHand > 0.000001f)
        {
            float inv = 1.0F / rightHand;
            leftHand.x *= inv;
            leftHand.y *= inv;
            leftHand.z *= inv;
        }
        else
        {
            leftHand.x = 0F;
            leftHand.y = 0F;
            leftHand.z = 0F;
        }
    }

    public static Vector3 Div3(Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; return leftHand; }

    public static int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    public static void RemoveAxis(ref Vector3 vector, Vector3 axisNormalised)
    {
        Mult3(ref axisNormalised, Vector3.Dot(vector, axisNormalised));
        Minus3(ref vector, axisNormalised);
    }

    //removes any rotation from the given quaternion that is not on the 'mainAxis' axis.
    public static void RemoveOtherAxisFromRotation(ref Quaternion rotation, Vector3 mainAxisNormalised)
    {
        Vector3 rotationAxis = new(rotation.x, rotation.y, rotation.z);
        // same as 'float rotationAxisMagnitude = rotationAxis.magnitude', but faster
        float rotationAxisMagnitude = MathF.Sqrt(rotationAxis.x * rotationAxis.x + rotationAxis.y * rotationAxis.y + rotationAxis.z * rotationAxis.z);
        float rotationAngleRad = 2.0f * MathF.Atan(rotationAxisMagnitude / rotation.w);
        GfcTools.Div3(ref rotationAxis, rotationAxisMagnitude); //normalize rotation axis

#pragma warning disable 0618
        //marked as deprecated, but system.math returns radians, and Quaternion.AngleAxis converts the euler angle in degrees to radians anyway, so we don't care about the warning
        rotation.SetAxisAngle(mainAxisNormalised, rotationAngleRad * Vector3.Dot(rotationAxis, mainAxisNormalised));
#pragma warning restore 0618
    }

    public static void RemoveAxisKeepMagnitude(ref Vector3 leftHand, Vector3 rightHand)
    {
        float mag = leftHand.magnitude;
        Mult3(ref rightHand, Vector3.Dot(leftHand, rightHand));
        Minus3(ref leftHand, rightHand);
        leftHand.Normalize();
        Mult3(ref leftHand, mag);
    }

    public static void QuatMultLocal(ref Quaternion lhs, Quaternion rhs)
    {
        float lhsx = lhs.x;
        float lhsy = lhs.y;
        float lhsz = lhs.z;
        float lhsw = lhs.w;

        lhs.x = lhsw * rhs.x + lhsx * rhs.w + lhsy * rhs.z - lhsz * rhs.y;
        lhs.y = lhsw * rhs.y + lhsy * rhs.w + lhsz * rhs.x - lhsx * rhs.z;
        lhs.z = lhsw * rhs.z + lhsz * rhs.w + lhsx * rhs.y - lhsy * rhs.x;
        lhs.w = lhsw * rhs.w - lhsx * rhs.x - lhsy * rhs.y - lhsz * rhs.z;
    }

    public static void QuatMultWorld(ref Quaternion rhs, Quaternion lhs)
    {
        float lhsx = lhs.x;
        float lhsy = lhs.y;
        float lhsz = lhs.z;
        float lhsw = lhs.w;

        lhs.x = lhsw * rhs.x + lhsx * rhs.w + lhsy * rhs.z - lhsz * rhs.y;
        lhs.y = lhsw * rhs.y + lhsy * rhs.w + lhsz * rhs.x - lhsx * rhs.z;
        lhs.z = lhsw * rhs.z + lhsz * rhs.w + lhsx * rhs.y - lhsy * rhs.x;
        lhs.w = lhsw * rhs.w - lhsx * rhs.x - lhsy * rhs.y - lhsz * rhs.z;
    }

    public static void Blend(ref Color a, Color b, float coef)
    {
        float aCoef = 1f - coef;
        a.r = a.r * aCoef + b.r * coef;
        a.g = a.g * aCoef + b.g * coef;
        a.b = a.b * aCoef + b.b * coef;
        a.a = a.a * aCoef + b.a * coef;
    }

    public static Vector3 RemoveAxis(Vector3 aLeftHand, Vector3 aRightHand) { RemoveAxis(ref aLeftHand, aRightHand); return aLeftHand; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Minus2(ref Vector2 aLeftHand, Vector2 aRightHand) { aLeftHand.x -= aRightHand.x; aLeftHand.y -= aRightHand.y; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add2(ref Vector2 aLeftHand, Vector2 aRightHand) { aLeftHand.x += aRightHand.x; aLeftHand.y += aRightHand.y; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mult2(ref Vector2 aLeftHand, float aRightHand) { aLeftHand.x *= aRightHand; aLeftHand.y *= aRightHand; }

    public static void Mult2(ref Vector2 aLeftHand, Vector2 aRightHand) { aLeftHand.x *= aRightHand.x; aLeftHand.y *= aRightHand.y; }

    public static void Div2(ref Vector2 aLeftHand, float rightHand) { float inv = 1.0f / rightHand; aLeftHand.x *= inv; aLeftHand.y *= inv; }

    public static void Normalize(ref Vector3 aVector)
    {
        float sqrMag = aVector.sqrMagnitude;
        if (sqrMag > 0.000001f)
        {
            float inv = 1.0f / MathF.Sqrt(sqrMag);

            aVector.x *= inv;
            aVector.y *= inv;
            aVector.z *= inv;
        }
        else
        {
            aVector.x = 0F;
            aVector.y = 0F;
            aVector.z = 0F;
        }
    }

    public static void Project(ref Vector3 vector, Vector3 onNormal)
    {
        var dot = Vector3.Dot(vector, onNormal);
        vector.x = onNormal.x * dot;
        vector.y = onNormal.y * dot;
        vector.z = onNormal.z * dot;
    }

    /*Props to allista from the kerbal space program forum for this incredible function*/
    /*
    public static float AngleDeg(Vector3 a, Vector3 b)
    {
        GfTools.Mult3(ref a, sqrt(b.sqrMagnitude));
        GfTools.Mult3(ref b, sqrt(a.sqrMagnitude));
        return 2 * atan2(sqrt((a - b).sqrMagnitude), sqrt((a + b).sqrMagnitude)) * Mathf.Rad2Deg;
    }*/

    public static bool RayIntersectsAABB(Vector3 org, Vector3 dirNorm, Bounds bound)
    {
        float dirFracX = 1.0f / dirNorm.x;
        float dirFracY = 1.0f / dirNorm.y;
        float dirFracZ = 1.0f / dirNorm.z;

        Vector3 lb = bound.min;
        Vector3 rt = bound.max;
        float t1 = (lb.x - org.x) * dirFracX;
        float t2 = (rt.x - org.x) * dirFracX;

        float t3 = (lb.y - org.y) * dirFracY;
        float t4 = (rt.y - org.y) * dirFracY;

        float t5 = (lb.z - org.z) * dirFracZ;
        float t6 = (rt.z - org.z) * dirFracZ;

        float tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
        float tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));


        return !(tmax < 0 || tmin > tmax);
    }

    public static float AngleDegNorm(Vector3 aNorm, Vector3 bNorm)
    {
        float dot = aNorm.x * bNorm.x + aNorm.y * bNorm.y + aNorm.z * bNorm.z;
        //dot = clamp(dot, -1F, 1F);
        dot *= 0.9999999f;
        return acos(dot) * Mathf.Rad2Deg;
    }

    public static float SignedAngleDegNorm(Vector3 from, Vector3 to, Vector3 axis)
    {
        float unsignedAngle = AngleDegNorm(from, to);

        float cross_x = from.y * to.z - from.z * to.y;
        float cross_y = from.z * to.x - from.x * to.z;
        float cross_z = from.x * to.y - from.y * to.x;
        float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
        return unsignedAngle * sign;
    }

    public static float SignedAngleDeg(Vector3 from, Vector3 to, Vector3 axis)
    {
        float unsignedAngle = Vector3.Angle(from, to);

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

    public static int Count<T>(List<T> aList)
    {
        if (aList != null)
            return aList.Count;
        else
            return 0;
    }

    public static bool Equals(string aLeft, string aRight)
    {
        bool valuesEqual = aLeft == aRight;
        bool leftEmpty = aLeft.IsEmpty();
        bool rightEmpty = aRight.IsEmpty();

        return valuesEqual || (leftEmpty && rightEmpty);
    }

    //https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
    public static Quaternion QuatSmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time, float deltaTime)
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
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time, int.MaxValue, deltaTime),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time, int.MaxValue, deltaTime),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time, int.MaxValue, deltaTime),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time, int.MaxValue, deltaTime)
        ).normalized;

        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;

        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }

    public static void PoolAndDestroyWhenParticlesDead(GameObject aGameObject, ParticleSystem[] someParticleSystems, float aCheckInterval, bool aStopParticleSystems = true)
    {
        bool particlesAreLive = false;

        for (int i = 0; i < someParticleSystems.Length && aStopParticleSystems; i++)
        {
            someParticleSystems[i].Stop(true);
            particlesAreLive |= someParticleSystems[i].IsAlive(true);
        }

        if (particlesAreLive)
        {
            GfcPooling.DestroyInsert(aGameObject, 0, true);
            Timing.RunCoroutine(_PoolAndDestroyWhenParticlesDead(aGameObject, someParticleSystems, aCheckInterval, aStopParticleSystems));
        }
        else
        {
            GfcPooling.Destroy(aGameObject);
        }
    }

    protected static IEnumerator<float> _PoolAndDestroyWhenParticlesDead(GameObject aGameObject, ParticleSystem[] someParticleSystems, float aCheckInterval, bool aStopParticleSystems = true)
    {
        bool particlesAreLive;

        do
        {
            particlesAreLive = false;
            yield return Timing.WaitForSeconds(aCheckInterval);

            //we do nullchecks in case the objects were unloaded in the mean time
            for (int i = 0; someParticleSystems != null && i < someParticleSystems.Length && !particlesAreLive; i++)
                particlesAreLive = someParticleSystems[i] != null && someParticleSystems[i].IsAlive(true);

        } while (particlesAreLive);

        aGameObject?.SetActive(false);
    }



    #region BOILERPLATE SERIALIZATION CODE

    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref string aString) where T : IReaderWriter
    {
        if (aSerializer.IsWriter && aString == null) aString = "";
    }
    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T, M>(BufferSerializer<T> aSerializer, ref List<M> aList) where T : IReaderWriter where M : struct, INetworkSerializable
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<int> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<uint> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<long> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<ulong> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<float> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<double> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref List<string> aList) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter && aList != null ? aList.Count : -1;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (length == -1)
                aList = null;
            else if (aList == null)
                aList = new(length);

            aList?.Clear();
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aSerializer.IsWriter ? aList[i] : default;
            aSerializer.SerializeValue(ref value);
            if (aSerializer.IsReader) aList.Add(value);
        }
    }

    public static void SerializeValue<T, M>(BufferSerializer<T> aSerializer, ref M[] aArray) where T : IReaderWriter where M : struct, INetworkSerializable
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new M[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref int[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new int[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref uint[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new uint[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref long[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new long[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref ulong[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new ulong[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref float[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new float[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref double[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new double[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref string[] aArray) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader) aArray = new string[length];

        for (int i = 0; i < length; ++i)
            aSerializer.SerializeValue(ref aArray[i]);
    }

    public static void SerializeValue<T, M>(BufferSerializer<T> aSerializer, ref NativeArray<M> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter where M : struct, INetworkSerializable
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref NativeArray<int> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref NativeArray<uint> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref NativeArray<ulong> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref NativeArray<long> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref NativeArray<float> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    //boilerplate because the unity api sucks and the original function only supports INetworkSerializable structs, not primitives 
    public static void SerializeValue<T>(BufferSerializer<T> aSerializer, ref NativeArray<double> aNativeArray, Allocator aReadAllocator = Allocator.Persistent) where T : IReaderWriter
    {
        int length = aSerializer.IsWriter ? aNativeArray.Length : 0;
        aSerializer.SerializeValue(ref length);

        if (aSerializer.IsReader)
        {
            if (aNativeArray.IsCreated) aNativeArray.Dispose();
            aNativeArray = new(length, aReadAllocator, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0; i < length; ++i)
        {
            var value = aNativeArray[i];
            aSerializer.SerializeValue(ref value);
            aNativeArray[i] = value;
        }
    }

    #endregion BOILERPLATE SERIALIZATION CODE
}