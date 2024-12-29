using System;
using UnityEngine;

[Serializable]
public class EnumSingletons<T, ENUM> where ENUM : unmanaged, Enum
{
    public bool FirstElementIsFallback;
    public EnumSingletonInstance<T, ENUM>[] Elements;

    public int Length { get { return Elements != null ? Elements.Length : 0; } }

    private int EnumCount = 0;

    public bool Validate(ENUM aCountEnum) { return Validate(aCountEnum.Index()); }
    public bool Validate(int anEnumCount)
    {
        bool valid = Elements != null;

        if (anEnumCount <= 0)
            Debug.LogError("The enum count passes must be higher than 0, value passed is " + anEnumCount);

        if (EnumCount == 0)
        {
            EnumCount = anEnumCount;

            if (valid)
            {
                for (int i = 0; i < Elements.Length; i++)
                {
                    bool validOrder = Elements[i].Type.Index() == i;
                    if (!validOrder) Debug.LogError("The element " + Elements[i].Type + " is in the wrong spot at index " + i + ", it should be at index " + Elements[i].Type.Index());
                    valid &= validOrder;
                }

                if (FirstElementIsFallback && (Elements.Length == 0 || Elements[0].Singleton == null))
                {
                    Debug.LogError("FirstElementIsFallback is set to true, but the first element of type " + 0.IndexToEnum<ENUM>() + " is null. This will cause issues when accessing elements. Please assign an element to the first item in the array.");
                    valid = false;
                }
            }
            else
            {
                Debug.LogError("The array is null");
            }

            Debug.Assert(valid, "Validation failed for enum singleton array");
        }
        else Debug.LogError("The struct was already validated");

        return valid;
    }

    public T GetValue(ENUM aType) { return GetValue(aType.Index()); }

    [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
    private void ValidateIndex(int aType) { if (aType < 0 || aType >= EnumCount) throw new IndexOutOfRangeException("Invalid index! " + aType + " the max count is: " + EnumCount); }

    public T GetValue(int aType)
    {
        Debug.Assert(Elements != null);
        ValidateIndex(aType);
        return Elements != null && Elements.Length > aType ? Elements[aType] : (Elements != null && FirstElementIsFallback && Elements.Length > 0 ? Elements[0] : default);
    }

    public T this[int anIndex]
    {
        get { return GetValue(anIndex); }
        set
        {
            ValidateIndex(anIndex);

            if (Length <= anIndex)
            {
                Debug.Assert(EnumCount > 0, "Please call Validate() before assigning any values");
                Array.Resize(ref Elements, EnumCount);
            }

            Elements[anIndex] = new()
            {
                Singleton = value,
                Type = anIndex.IndexToEnum<ENUM>()
            };
        }
    }

    public T this[ENUM aType]
    {
        get { return GetValue(aType); }
        set { this[aType.Index()] = value; }
    }
}

[Serializable]
public class EnumSingletonInstance<T, ENUM> where ENUM : unmanaged, Enum
{
    public T Singleton;
    public ENUM Type;

    public static implicit operator T(EnumSingletonInstance<T, ENUM> d) => d.Singleton;
}