using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;

public struct Vector4<T> : IList<T>
{
    public T x, y, z, w;

    public readonly int Count => 4;

    public readonly bool IsReadOnly => false;

    public Vector4(T aX = default, T aY = default, T aZ = default, T aW = default) { x = aX; y = aY; z = aZ; w = aW; }

    public T this[int anIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            switch (anIndex)
            {
                case 0: return x;
                case 1: return y;
                case 2: return z;
                case 3: return w;
                default:
                    throw new IndexOutOfRangeException("Invalid " + GetType() + " index! " + anIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            switch (anIndex)
            {
                case 0: x = value; break;
                case 1: y = value; break;
                case 2: z = value; break;
                case 3: w = value; break;
                default:
                    throw new IndexOutOfRangeException("Invalid " + GetType() + " index!" + anIndex);
            }
        }
    }

    public void Insert(int anIndex, T anItem)
    {
        if (anIndex < 0 || anIndex > 3)
            throw new IndexOutOfRangeException("Invalid " + GetType() + " index!" + anIndex);

        for (int i = Count - 1; i > anIndex; --i)
            this[i] = this[i - 1];

        this[anIndex] = anItem;
    }

    public void RemoveAt(int anIndex)
    {
        if (anIndex < 0 || anIndex > 3)
            throw new IndexOutOfRangeException("Invalid " + GetType() + " index!" + anIndex);

        for (; anIndex < 3; anIndex++)
            this[anIndex] = this[anIndex + 1];

        w = default;
    }

    public void Clear()
    {
        for (int i = Count; --i >= 0;)
            this[i] = default;
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
            RemoveAt(index);
        return index >= 0;
    }

    public readonly bool Contains(T item) { return IndexOf(item) > 0; }

    public readonly int IndexOf(T item)
    {
        int index = -1;
        int numComponents = Count;

        for (int i = 0; i < numComponents && index == -1; ++i)
            if (this[i].Equals(item)) index = i;

        return index;
    }

    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        for (int i = Count - 1; i >= 0; --i)
            array[i + arrayIndex] = this[i];
    }

    public void Add(T item)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}