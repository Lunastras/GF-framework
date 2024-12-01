using MEC;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Collections.Generic;
using static Unity.Mathematics.math;
using System.Reflection;
using System.Linq;
public static class GfcToolsStatic
{
    //the list must be a class because this doesn't work with structs
    public static int AddSorted<T>(this IList<T> aList, T aValue, bool anAscending = true) where T : IComparable<T>
    {
        Debug.Assert(aList.GetType().IsClass, "This function does not work for value types. Please use 'GetSortedIndex' instead and insert it at the respective index");
        int sortedIndex = aList.GetSortedIndex(aValue, anAscending);
        aList.Insert(sortedIndex, aValue);
        return sortedIndex;
    }

    public static int GetSortedIndex<T>(this IList<T> aList, T aValue, bool anAscending = true) where T : IComparable<T>
    {
        int low = 0, high = aList.Count, mid;
        int sortSign = anAscending ? -1 : 1;

        while (low < high)
        {
            mid = (low + high) >> 1;
            if (aList[mid].CompareTo(aValue) == sortSign)
                low = mid + 1;
            else
                high = mid;
        }

        return low;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag(this uint aFlagMask, uint aFlag) { return GfcTools.HasFlag(aFlagMask, aFlag); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndUnsetBit(this ref uint aFlagMask, int aBitIndex) { return GfcTools.GetAndUnsetBit(ref aFlagMask, aBitIndex); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(this ref uint aFlagMask, int aBitIndex) { return GfcTools.GetAndSetBit(ref aFlagMask, aBitIndex); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(this ref uint aFlagMask, int aBitIndex, bool aState = true) { return GfcTools.GetAndSetBit(ref aFlagMask, aBitIndex, aState); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this uint aFlagMask, int aBitIndex) { return GfcTools.GetBit(aFlagMask, aBitIndex); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(this ref uint aFlagMask, int aBitIndex) { GfcTools.SetBit(ref aFlagMask, aBitIndex); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(this ref uint aFlagMask, int aBitIndex, bool aState) { GfcTools.SetBit(ref aFlagMask, aBitIndex, aState); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnsetBit(this ref uint aFlagMask, int aBitIndex) { GfcTools.UnsetBit(ref aFlagMask, aBitIndex); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag(this ulong aFlagMask, ulong aFlag) { return GfcTools.HasFlag(aFlagMask, aFlag); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndUnsetBit(this ref ulong aFlagMask, int aBitIndex) { return GfcTools.GetAndUnsetBit(ref aFlagMask, aBitIndex); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(this ref ulong aFlagMask, int aBitIndex) { return GfcTools.GetAndSetBit(ref aFlagMask, aBitIndex); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(this ref ulong aFlagMask, int aBitIndex, bool aState = true) { return GfcTools.GetAndSetBit(ref aFlagMask, aBitIndex, aState); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(this ulong aFlagMask, int aBitIndex) { return GfcTools.GetBit(aFlagMask, aBitIndex); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(this ref ulong aFlagMask, int aBitIndex) { GfcTools.SetBit(ref aFlagMask, aBitIndex); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(this ref ulong aFlagMask, int aBitIndex, bool aState) { GfcTools.SetBit(ref aFlagMask, aBitIndex, aState); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnsetBit(this ref ulong aFlagMask, int aBitIndex) { GfcTools.UnsetBit(ref aFlagMask, aBitIndex); }

    public static void SetSingleton<T>(this T aGameObject, ref T anInstance) where T : MonoBehaviour
    {
        if (anInstance != aGameObject)
            MonoBehaviour.Destroy(anInstance);
        anInstance = aGameObject;
    }

    public static bool ActiveInHierarchyGf(this GameObject aGameObject)
    {
        bool activeInHierarchy = aGameObject.activeInHierarchy;

        if (activeInHierarchy)
        {
            Transform currentObj = aGameObject.transform;

            do
            {
                currentObj.TryGetComponent(out GfcTransitionActive customActive);
                activeInHierarchy = customActive == null || !customActive.FadingOut();
                currentObj = currentObj.parent;
            } while (activeInHierarchy && currentObj);
        }

        return activeInHierarchy;
    }

    public static void SetActiveGf(this GameObject aGameObject, bool anActive)
    {
        if (aGameObject.TryGetComponent(out GfcTransitionActive customActive))
            customActive.SetActive(anActive);
        else
            aGameObject.SetActive(anActive);
    }

    public static void SetAlpha(this Image anImage, float anAlpha)
    {
        Color col = anImage.color;
        col.a = anAlpha;
        anImage.color = col;
    }

    private static IEnumerator<float> _SetActiveInFrames(GameObject aGameObject, bool anActive, int aCountFrames)
    {
        while (aCountFrames-- != 0) yield return Timing.WaitForOneFrame;
        aGameObject.SetActive(anActive);
    }

    public static CoroutineHandle SetActiveInFrames(this GameObject aGameObject, bool anActive, int aCountFrames)
    {
        CoroutineHandle handle = default;

        if (aCountFrames == 0)
            aGameObject.SetActive(anActive);
        else
            handle = Timing.RunCoroutine(_SetActiveInFrames(aGameObject, anActive, aCountFrames));

        return handle;
    }

    public static bool HasValueAt<T>(this T[] anArray, int anIndex) { return anArray != null && anArray.Length > anIndex; }

    public static bool IsEmpty<T>(this T[] anArray) { return anArray == null || anArray.Length == 0; }

    public static bool IsEmpty<T>(this List<T> aList) { return aList == null || aList.Count == 0; }

    public static TransformData GetTransformData(this Transform aTransform, bool aLocalTransform = false, bool aCopyParent = true, Transform aNewParent = null) { return new(aTransform, aLocalTransform, aCopyParent, aNewParent); }

    public static void SetTransformData(this Transform aTransform, TransformData aTransformData)
    {
        aTransform.SetParent(aTransformData.Parent);

        if (aTransformData.IsLocal)
        {
            aTransform.localPosition = aTransformData.Position;
            aTransform.localRotation = aTransformData.Rotation;
        }
        else
        {
            aTransform.position = aTransformData.Position;
            aTransform.rotation = aTransformData.Rotation;
        }

        aTransform.localScale = aTransformData.Scale;
    }

    public static void SetRectTransformdData(this RectTransform aTransform, RectTransformData aData, bool aLocalTransform = false)
    {
        aTransform.SetTransformData(aData.TransformData);
        aTransform.SetParent(null);

        aTransform.anchoredPosition3D = aData.AnchoredPosition3D;
        aTransform.anchorMax = aData.AnchorMax;
        aTransform.anchorMin = aData.AnchorMin;
        aTransform.sizeDelta = aData.SizeDelta;
        aTransform.pivot = aData.Pivot;

        aTransform.SetParent(aData.TransformData.Parent, !aLocalTransform);
    }

    public static void CopyRectWorldData(this RectTransform aTransform, RectTransform aRefTransform, bool aLocalTransform = false, bool aCopyParent = true, Transform aNewParent = null) { aTransform.SetRectTransformdData(aRefTransform.GetRectWorldData(aCopyParent, aNewParent), aLocalTransform); }

    public static void CopyTransformData(this Transform aTransform, Transform aRefTransform, bool aLocalTransform = false, bool aCopyParent = true, Transform aNewParent = null)
    {
        RectTransform rectTransform = aTransform as RectTransform;
        RectTransform refRectTransform = aRefTransform as RectTransform;

        if (rectTransform && refRectTransform)
            rectTransform.CopyRectWorldData(refRectTransform, aLocalTransform, aCopyParent, aNewParent);
        else
            aTransform.SetTransformData(aRefTransform.GetTransformData(aLocalTransform, aCopyParent, aNewParent));
    }

    public static RectTransformData GetRectWorldData(this RectTransform aTransform, bool aCopyParent = true, Transform aNewParent = null) { return new(aTransform, aCopyParent, aNewParent); }

    //code suggests compound assignment instead of if statement, but it does not work
    public static Component GetComponentIfNull<T>(this Component aComponent, ref T aContainer) where T : Component { if (aContainer == null) aContainer = aComponent.GetComponent<T>(); return aContainer; }

    public static Component GetComponent<T>(this Component aComponent, ref T aContainer) where T : Component { aContainer = aComponent.GetComponent<T>(); return aContainer; }

    public static Component GetComponent<T>(this GameObject aGameObject, ref T aContainer) where T : Component { aContainer = aGameObject.GetComponent<T>(); return aContainer; }

    public static bool IsEmpty(this string aString) { return aString == null || aString.Length == 0; }

    public static unsafe T IndexToEnum<T>(this int anEnumIndex) where T : unmanaged, Enum { return Index64ToEnum<T>((ulong)anEnumIndex); }

    public static unsafe T Index64ToEnum<T>(this ulong anEnumIndex) where T : unmanaged, Enum
    {
        switch (sizeof(T))
        {
            case 1:
                Debug.Assert(anEnumIndex <= byte.MaxValue);
                return *(T*)(byte*)&anEnumIndex;

            case 2:
                Debug.Assert(anEnumIndex <= ushort.MaxValue);
                return *(T*)(ushort*)&anEnumIndex;

            case 4:
                Debug.Assert(anEnumIndex <= uint.MaxValue);
                return *(T*)(uint*)&anEnumIndex;

            case 8: return *(T*)&anEnumIndex;

            default:
                throw new ArgumentException("The enum " + typeof(T) + " is outside the range of 8 bytes and cannot be evaluated.");
        }
    }

    public static unsafe ulong Index64<T>(this T anEnum) where T : unmanaged, Enum
    {
        return sizeof(T) switch
        {
            1 => *(byte*)&anEnum,
            2 => *(ushort*)&anEnum,
            4 => *(uint*)&anEnum,
            8 => *(ulong*)&anEnum,
            _ => throw new ArgumentException("The enum " + typeof(T) + " is outside the range of 8 bytes and cannot be evaluated."),
        };
    }

    public static unsafe int Index<T>(this T anEnum) where T : unmanaged, Enum
    {
        ulong index = Index64(anEnum);
        Debug.Assert(index <= int.MaxValue);
        return (int)index;
    }

    public static unsafe bool EqualsNoBox<T>(this T anEnum, T aSecondEnum) where T : unmanaged, Enum { return anEnum.Index() == aSecondEnum.Index(); }

    public static Vector2 xy(this Vector3 aSelf) { return new(aSelf.x, aSelf.y); }
    public static Vector2 yx(this Vector3 aSelf) { return new(aSelf.y, aSelf.x); }

    public static Vector2 xz(this Vector3 aSelf) { return new(aSelf.x, aSelf.z); }
    public static Vector2 zx(this Vector3 aSelf) { return new(aSelf.z, aSelf.x); }

    public static Vector2 yz(this Vector3 aSelf) { return new(aSelf.y, aSelf.z); }
    public static Vector2 zy(this Vector3 aSelf) { return new(aSelf.z, aSelf.y); }

    public static Vector3 xzy(this Vector3 aSelf) { return new(aSelf.x, aSelf.z, aSelf.y); }

    public static Vector3 zxy(this Vector3 aSelf) { return new(aSelf.z, aSelf.x, aSelf.y); }
    public static Vector3 zyx(this Vector3 aSelf) { return new(aSelf.z, aSelf.y, aSelf.x); }

    public static Vector3 yxz(this Vector3 aSelf) { return new(aSelf.y, aSelf.x, aSelf.z); }
    public static Vector3 yzx(this Vector3 aSelf) { return new(aSelf.y, aSelf.z, aSelf.x); }

    public static Vector2 yx(this Vector2 aSelf) { return new(aSelf.y, aSelf.x); }

    public static int Clamp(this int aNum, int aMin, int aMax) { return Math.Clamp(aNum, aMin, aMax); }
    public static long Clamp(this long aNum, long aMin, long aMax) { return Math.Clamp(aNum, aMin, aMax); }
    public static float Clamp(this float aNum, float aMin, float aMax) { return Math.Clamp(aNum, aMin, aMax); }
    public static double Clamp(this double aNum, double aMin, double aMax) { return Math.Clamp(aNum, aMin, aMax); }

    public static void ClampSelf(this ref int aNum, int aMin, int aMax) { aNum = Math.Clamp(aNum, aMin, aMax); }
    public static void ClampSelf(this ref long aNum, long aMin, long aMax) { aNum = Math.Clamp(aNum, aMin, aMax); }
    public static void ClampSelf(this ref float aNum, float aMin, float aMax) { aNum = Math.Clamp(aNum, aMin, aMax); }
    public static void ClampSelf(this ref double aNum, double aMin, double aMax) { aNum = Math.Clamp(aNum, aMin, aMax); }

    public static int Max(this int aSelf, int aNum) { return Math.Max(aSelf, aNum); }
    public static float Max(this float aSelf, float aNum) { return Math.Max(aSelf, aNum); }
    public static double Max(this double aSelf, double aNum) { return Math.Max(aSelf, aNum); }

    public static void MaxSelf(this ref int aSelf, int aNum) { aSelf = Math.Max(aSelf, aNum); }
    public static void MaxSelf(this ref float aSelf, float aNum) { aSelf = Math.Max(aSelf, aNum); }
    public static void MaxSelf(this ref double aSelf, double aNum) { aSelf = Math.Max(aSelf, aNum); }

    public static int Min(this int aSelf, int aNum) { return Math.Min(aSelf, aNum); }
    public static float Min(this float aSelf, float aNum) { return Math.Min(aSelf, aNum); }
    public static double Min(this double aSelf, double aNum) { return Math.Min(aSelf, aNum); }

    public static void MinSelf(this ref int aSelf, int aNum) { aSelf = Math.Min(aSelf, aNum); }
    public static void MinSelf(this ref float aSelf, float aNum) { aSelf = Math.Min(aSelf, aNum); }
    public static void MinSelf(this ref double aSelf, double aNum) { aSelf = Math.Min(aSelf, aNum); }

    public static float Round(this float aSelf) { return Mathf.Round(aSelf); }
    public static double Round(this double aSelf) { return Math.Round(aSelf); }

    public static void RoundSelf(this ref float aSelf) { aSelf = Mathf.Round(aSelf); }
    public static void RoundSelf(this ref double aSelf) { aSelf = Math.Round(aSelf); }

    public static int Abs(this int aSelf) { return Math.Abs(aSelf); }
    public static float Abs(this float aSelf) { return Math.Abs(aSelf); }
    public static double Abs(this double aSelf) { return Math.Abs(aSelf); }

    public static float SafeInverse(this float aSelf) { return aSelf.Abs() < 0.00001f ? float.MaxValue : 1.0f / aSelf; }
    public static double SafeInverse(this double aSelf) { return aSelf.Abs() < 0.000000001 ? double.MaxValue : 1.0 / aSelf; }

    public static void AbsSelf(this ref int aSelf) { aSelf = Math.Abs(aSelf); }
    public static void AbsSelf(this ref float aSelf) { aSelf = Math.Abs(aSelf); }
    public static void AbsSelf(this ref double aSelf) { aSelf = Math.Abs(aSelf); }

    public static int Sign(this int aSelf) { return Math.Sign(aSelf); }
    public static int Sign(this float aSelf) { return Math.Sign(aSelf); }
    public static int Sign(this double aSelf) { return Math.Sign(aSelf); }

    public static void SignSelf(this ref int aSelf) { aSelf = Math.Sign(aSelf); }
    public static void SignSelf(this ref float aSelf) { aSelf = Math.Sign(aSelf); }
    public static void SignSelf(this ref double aSelf) { aSelf = Math.Sign(aSelf); }

    public static float SqrtF(this int aSelf) { return MathF.Sqrt(aSelf); }
    public static double Sqrt(this int aSelf) { return Math.Sqrt(aSelf); }

    public static float Sqrt(this float aSelf) { return MathF.Sqrt(aSelf); }
    public static double Sqrt(this double aSelf) { return Math.Sqrt(aSelf); }

    public static void SqrtSelf(this ref float aSelf) { aSelf = MathF.Sqrt(aSelf); }
    public static void SqrtSelf(this ref double aSelf) { aSelf = Math.Sqrt(aSelf); }

    public static float Sin(this float aSelf) { return MathF.Sin(aSelf); }
    public static double Sin(this double aSelf) { return Math.Sin(aSelf); }

    public static void SinSelf(this ref float aSelf) { aSelf = MathF.Sin(aSelf); }
    public static void SinSelf(this ref double aSelf) { aSelf = Math.Sin(aSelf); }

    public static float Cos(this float aSelf) { return MathF.Cos(aSelf); }
    public static double Cos(this double aSelf) { return Math.Cos(aSelf); }

    public static void CosSelf(this ref float aSelf) { aSelf = MathF.Cos(aSelf); }
    public static void CosSelf(this ref double aSelf) { aSelf = Math.Cos(aSelf); }

    public static float Tan(this float aSelf) { return MathF.Tan(aSelf); }
    public static double Tan(this double aSelf) { return Math.Tan(aSelf); }

    public static void TanSelf(this ref float aSelf) { aSelf = MathF.Tan(aSelf); }
    public static void TanSelf(this ref double aSelf) { aSelf = Math.Tan(aSelf); }

    public static float Atan(this float aSelf) { return MathF.Atan(aSelf); }
    public static double Atan(this double aSelf) { return Math.Atan(aSelf); }

    public static void AtanSelf(this ref float aSelf) { aSelf = MathF.Atan(aSelf); }
    public static void AtanSelf(this ref double aSelf) { aSelf = Math.Atan(aSelf); }

    public static float Atanh(this float aSelf) { return MathF.Atanh(aSelf); }
    public static double Atanh(this double aSelf) { return Math.Atanh(aSelf); }

    public static void AtanhSelf(this ref float aSelf) { aSelf = MathF.Atanh(aSelf); }
    public static void AtanhSelf(this ref double aSelf) { aSelf = Math.Atanh(aSelf); }

    public static float Atan2(this float aSelf, float aNum) { return MathF.Atan2(aSelf, aNum); }
    public static double Atan2(this double aSelf, double aNum) { return Math.Atan2(aSelf, aNum); }

    public static void Atan2Self(this ref float aSelf, float aNum) { aSelf = MathF.Atan2(aSelf, aNum); }
    public static void Atan2Self(this ref double aSelf, double aNum) { aSelf = Math.Atan2(aSelf, aNum); }

    public static float Lerp(this float aSelf, float aTarget, float aCoef) { return Mathf.Lerp(aSelf, aTarget, aCoef); }
    public static Vector2 Lerp(this Vector2 aSelf, Vector2 aTarget, float aCoef) { return Vector2.Lerp(aSelf, aTarget, aCoef); }
    public static Vector3 Lerp(this Vector3 aSelf, Vector3 aTarget, float aCoef) { return Vector3.Lerp(aSelf, aTarget, aCoef); }

    public static void LerpSelf(this ref float aSelf, float aTarget, float aCoef) { aSelf = Mathf.Lerp(aSelf, aTarget, aCoef); }
    public static void LerpSelf(this ref Vector2 aSelf, Vector2 aTarget, float aCoef) { aSelf = Vector2.Lerp(aSelf, aTarget, aCoef); }
    public static void LerpSelf(this ref Vector3 aSelf, Vector3 aTarget, float aCoef) { aSelf = Vector3.Lerp(aSelf, aTarget, aCoef); }

    public static int Pow(this int aSelf, int aNum) { return (int)MathF.Pow(aSelf, aNum); }
    public static float Pow(this float aSelf, float aNum) { return MathF.Pow(aSelf, aNum); }
    public static double Pow(this double aSelf, double aNum) { return Math.Pow(aSelf, aNum); }

    public static void PowSelf(this ref int aSelf, int aNum) { aSelf = (int)Math.Pow(aSelf, aNum); }
    public static void PowSelf(this ref float aSelf, float aNum) { aSelf = MathF.Pow(aSelf, aNum); }
    public static void PowSelf(this ref double aSelf, double aNum) { aSelf = Math.Pow(aSelf, aNum); }

    public static float Log(this float aSelf, float aNum) { return MathF.Log(aSelf, aNum); }
    public static double Log(this double aSelf, double aNum) { return Math.Log(aSelf, aNum); }

    public static void LogSelf(this ref float aSelf, float aNum) { aSelf = MathF.Log(aSelf, aNum); }
    public static void LogSelf(this ref double aSelf, double aNum) { aSelf = Math.Log(aSelf, aNum); }

    public static float Log10(this ref float aSelf) { return MathF.Log10(aSelf); }
    public static double Log10(this ref double aSelf) { return Math.Log10(aSelf); }

    public static void Log10Self(this ref float aSelf) { aSelf = MathF.Log10(aSelf); }
    public static void Log10Self(this ref double aSelf) { aSelf = Math.Log10(aSelf); }

    public static float Ln(this float aSelf) { return aSelf.Log((float)GfcTools.EULER); }
    public static double Ln(this double aSelf) { return aSelf.Log(GfcTools.EULER); }

    public static void LnSelf(this ref float aSelf) { aSelf = aSelf.Log((float)GfcTools.EULER); }
    public static void LnSelf(this ref double aSelf) { aSelf = aSelf.Log(GfcTools.EULER); }

    public static void Add<T>(this List<T> aList, IEnumerable<T> someItems) { foreach (T item in someItems) aList.Add(item); }

    public static void MinusSelf(this ref Vector3 leftHand, Vector3 rightHand) { leftHand.x -= rightHand.x; leftHand.y -= rightHand.y; leftHand.z -= rightHand.z; }

    public static void AddSelf(this ref Vector3 leftHand, Vector3 rightHand) { leftHand.x += rightHand.x; leftHand.y += rightHand.y; leftHand.z += rightHand.z; }

    public static void MinusSelf(this ref Vector3 leftHand, float rightHand) { leftHand.x -= rightHand; leftHand.y -= rightHand; leftHand.z -= rightHand; }

    public static void AddSelf(this ref Vector3 leftHand, float rightHand) { leftHand.x += rightHand; leftHand.y += rightHand; leftHand.z += rightHand; }

    public static Vector3 Mult(this Vector3 leftHand, Vector3 rightHand) { leftHand.x *= rightHand.x; leftHand.y *= rightHand.y; leftHand.z *= rightHand.z; return leftHand; }

    public static void MultSelf(this ref Vector3 leftHand, float rightHand) { leftHand.x *= rightHand; leftHand.y *= rightHand; leftHand.z *= rightHand; }

    public static void MultSelf(this ref Vector3 leftHand, Vector3 rightHand) { leftHand.x *= rightHand.x; leftHand.y *= rightHand.y; leftHand.z *= rightHand.z; }

    public static void DivSelf(this ref Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; }
    public static void DivSafeSelf(this ref Vector3 leftHand, float rightHand) { GfcTools.DivSafe(ref leftHand, rightHand); }
}

public class GfcTools
{
    public const double EULER = 2.7182818284590452;
    public const float EPSILON = 0.99999f;

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
        GfcTools.Add(ref vector, upVec * Y);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag(uint aFlagMask, uint aFlag) { return 0 < (aFlagMask & aFlag); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndUnsetBit(ref uint aFlagMask, int aBitIndex)
    {
        bool state = GetBit(aFlagMask, aBitIndex);
        UnsetBit(ref aFlagMask, aBitIndex);
        return state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(ref uint aFlagMask, int aBitIndex)
    {
        bool state = GetBit(aFlagMask, aBitIndex);
        SetBit(ref aFlagMask, aBitIndex);
        return state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(ref uint aFlagMask, int aBitIndex, bool aState = true)
    {
        bool state = GetBit(aFlagMask, aBitIndex);
        SetBit(ref aFlagMask, aBitIndex, aState);
        return state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(uint aFlagMask, int aBitIndex) { return HasFlag(aFlagMask, (uint)(1 << aBitIndex)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(ref uint aFlagMask, int aBitIndex) { aFlagMask |= (uint)1 << aBitIndex; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnsetBit(ref uint aFlagMask, int aBitIndex) { aFlagMask &= ~((uint)1 << aBitIndex); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(ref uint aFlagMask, int aBitIndex, bool aState)
    {
        if (aState)
            SetBit(ref aFlagMask, aBitIndex);
        else
            UnsetBit(ref aFlagMask, aBitIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag(ulong aFlagMask, ulong aFlag) { return 0 < (aFlagMask & aFlag); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndUnsetBit(ref ulong aFlagMask, int aBitIndex)
    {
        bool state = GetBit(aFlagMask, aBitIndex);
        UnsetBit(ref aFlagMask, aBitIndex);
        return state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(ref ulong aFlagMask, int aBitIndex)
    {
        bool state = GetBit(aFlagMask, aBitIndex);
        SetBit(ref aFlagMask, aBitIndex);
        return state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAndSetBit(ref ulong aFlagMask, int aBitIndex, bool aState = true)
    {
        bool state = GetBit(aFlagMask, aBitIndex);
        SetBit(ref aFlagMask, aBitIndex, aState);
        return state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ulong aFlagMask, int aBitIndex) { return HasFlag(aFlagMask, (ulong)(1 << aBitIndex)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(ref ulong aFlagMask, int aBitIndex) { aFlagMask |= (ulong)1 << aBitIndex; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnsetBit(ref ulong aFlagMask, int aBitIndex) { aFlagMask &= ~((ulong)1 << aBitIndex); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(ref ulong aFlagMask, int aBitIndex, bool aState)
    {
        if (aState)
            SetBit(ref aFlagMask, aBitIndex);
        else
            UnsetBit(ref aFlagMask, aBitIndex);
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
    public static void Minus(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x -= rightHand.x; leftHand.y -= rightHand.y; leftHand.z -= rightHand.z; }


    public static void Add(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x += rightHand.x; leftHand.y += rightHand.y; leftHand.z += rightHand.z; }


    public static void Mult(ref Vector3 leftHand, float rightHand) { leftHand.x *= rightHand; leftHand.y *= rightHand; leftHand.z *= rightHand; }

    public static void Mult(ref Vector3 leftHand, Vector3 rightHand) { leftHand.x *= rightHand.x; leftHand.y *= rightHand.y; leftHand.z *= rightHand.z; }

    public static void Div(ref Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; }

    public static void DivSafe(ref Vector3 leftHand, float rightHand)
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

    public static Vector3 Div(Vector3 leftHand, float rightHand) { float inv = 1.0f / rightHand; leftHand.x *= inv; leftHand.y *= inv; leftHand.z *= inv; return leftHand; }

    public static int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    public static void RemoveAxis(ref Vector3 vector, Vector3 axisNormalised)
    {
        Mult(ref axisNormalised, Vector3.Dot(vector, axisNormalised));
        Minus(ref vector, axisNormalised);
    }

    //removes any rotation from the given quaternion that is not on the 'mainAxis' axis.
    public static void RemoveOtherAxisFromRotation(ref Quaternion rotation, Vector3 mainAxisNormalised)
    {
        Vector3 rotationAxis = new(rotation.x, rotation.y, rotation.z);
        // same as 'float rotationAxisMagnitude = rotationAxis.magnitude', but faster
        float rotationAxisMagnitude = MathF.Sqrt(rotationAxis.x * rotationAxis.x + rotationAxis.y * rotationAxis.y + rotationAxis.z * rotationAxis.z);
        float rotationAngleRad = 2.0f * MathF.Atan(rotationAxisMagnitude / rotation.w);
        GfcTools.Div(ref rotationAxis, rotationAxisMagnitude); //normalize rotation axis

#pragma warning disable 0618
        //marked as deprecated, but system.math returns radians, and Quaternion.AngleAxis converts the euler angle in degrees to radians anyway, so we don't care about the warning
        rotation.SetAxisAngle(mainAxisNormalised, rotationAngleRad * Vector3.Dot(rotationAxis, mainAxisNormalised));
#pragma warning restore 0618
    }

    public static void RemoveAxisKeepMagnitude(ref Vector3 leftHand, Vector3 rightHand)
    {
        float mag = leftHand.magnitude;
        Mult(ref rightHand, Vector3.Dot(leftHand, rightHand));
        Minus(ref leftHand, rightHand);
        leftHand.Normalize();
        Mult(ref leftHand, mag);
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
    public static void Minus(ref Vector2 aLeftHand, Vector2 aRightHand) { aLeftHand.x -= aRightHand.x; aLeftHand.y -= aRightHand.y; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(ref Vector2 aLeftHand, Vector2 aRightHand) { aLeftHand.x += aRightHand.x; aLeftHand.y += aRightHand.y; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mult(ref Vector2 aLeftHand, float aRightHand) { aLeftHand.x *= aRightHand; aLeftHand.y *= aRightHand; }

    public static void Mult(ref Vector2 aLeftHand, Vector2 aRightHand) { aLeftHand.x *= aRightHand.x; aLeftHand.y *= aRightHand.y; }

    public static void Div(ref Vector2 aLeftHand, float rightHand) { float inv = 1.0f / rightHand; aLeftHand.x *= inv; aLeftHand.y *= inv; }

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

    public static IEnumerable<Type> GetSubclasses<T>() where T : class { return GetSubclasses<T>(typeof(T)); }

    public static IEnumerable<Type> GetSubclasses<T>(Type aTypeInAssembly) where T : class { return Assembly.GetAssembly(aTypeInAssembly).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))); }

    public static List<T> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class, IComparable<T>
    {
        var subclasses = GetSubclasses<T>();
        List<T> objects = new(subclasses.Count());
        foreach (Type type in subclasses)
        {
            objects.Add((T)Activator.CreateInstance(type, constructorArgs));
        }
        objects.Sort();
        return objects;
    }

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

    /*
    public static CoroutineHandle TweenFloat(PropertyInfo aProperty, object anObject, float aStartValue, float aTargetValue)
    {
        Type type = anObject.GetType();
        CoroutineHandle handle = default;
        aProperty.in

        if (typeof(float) == aProperty.PropertyType)
        {
            handle = Timing.RunCoroutine(_TweenFloat(aProperty, anObject, aValue));
        }
        else
        {
            Debug.LogError("Type mismatch! The property passed is of type " + aProperty.PropertyType.Name + ", not float.");
        }

        return handle;
        Image a;
        a.CrossFadeAlpha
    }

    public static IEnumerator<float> _TweenFloat(PropertyInfo aProperty, object anObject, float aStartValue, float aTargetValue)
    {
        //prop.SetValue(anObject, aValue, null);
        float refSmooth = 0;
        float value = aProperty.GetValue(anObject, null);
        float currentAlpha = color.a;
        float deltaTime;

        while (anImage && MathF.Abs(currentAlpha - aTargetAlpha) > 0.001f && group.alpha == currentAlpha) //stop coroutine if alpha is modified by somebody else
        {
            deltaTime = Time.deltaTime;
            if (ignoreTimeScale)
                deltaTime = Time.unscaledDeltaTime;

            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref refSmooth, smoothTime, int.MaxValue, deltaTime);
            group.alpha = currentAlpha;
            yield return Timing.WaitForOneFrame;
        }

        if (anImage && anImage.color.a == currentAlpha)
            anImage.SetAlpha(aTargetAlpha);

    }*/

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

public struct TransformData
{
    public TransformData(bool aLocalTransform = true)
    {
        Position = Vector3.zero;
        Rotation = Quaternion.identity;
        Scale = new Vector3(1, 1, 1);
        Parent = null;
        IsLocal = aLocalTransform;
    }

    public TransformData(Transform aTransform, bool aLocalTransform = true, bool aCopyParent = true, Transform aNewParent = null)
    {
        Parent = aCopyParent ? aTransform.parent : aNewParent;

        if (aLocalTransform)
        {
            Position = aTransform.localPosition;
            Rotation = aTransform.localRotation;
            Scale = aTransform.localScale;
        }
        else
        {
            Position = aTransform.position;
            Rotation = aTransform.rotation;
            Scale = aTransform.lossyScale;
        }

        IsLocal = aLocalTransform;
    }

    public Transform Parent;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public bool IsLocal;
}

public struct RectTransformData
{
    public RectTransformData(bool valueToIgnoreOldCSharpLimitations = true)
    {
        TransformData = new();
        AnchorMin = default;
        AnchorMax = default;
        AnchoredPosition3D = default;
        SizeDelta = default;
        Pivot = default;
    }

    public RectTransformData(RectTransform aTransform, bool aCopyParent = true, Transform aNewParent = null)
    {
        AnchorMin = aTransform.anchorMin;
        AnchorMax = aTransform.anchorMax;
        AnchoredPosition3D = aTransform.anchoredPosition3D;
        SizeDelta = aTransform.sizeDelta;
        Pivot = aTransform.pivot;
        TransformData = new(aTransform, true, aCopyParent, aNewParent);
    }

    public TransformData TransformData;
    public Vector2 AnchorMin;
    public Vector2 AnchorMax;
    public Vector3 AnchoredPosition3D;
    public Vector2 SizeDelta;
    public Vector2 Pivot;
}