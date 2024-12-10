using UnityEngine;

[System.Serializable]
public struct StructArray<T>
{
    [SerializeField]
    public T[] array;
}