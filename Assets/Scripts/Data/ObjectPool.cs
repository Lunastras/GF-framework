using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectPool<T> where T : class, new()
{
    private static List<T> pool;

    private static int capacity = 0;

    public static void PoolResize(int newSize)
    {
        newSize = System.Math.Max(newSize, 0);

        if(newSize > 0)
        {
            if (null == pool)
                pool = new(newSize);

            for (int i = pool.Count - 1; i >= newSize; --i)
            {
                pool.RemoveAt(i);
            }

            for (int i = pool.Count; i < newSize; ++i)
            {
                pool.Add(new());
            }
        }

        capacity = newSize;

        if (capacity == 0)
            pool = null;
    }

    public static void PoolExpand(int quantity)
    {
        PoolResize(capacity + quantity);
    }

    public static void PoolShrink(int quantity)
    {
        PoolResize(capacity - quantity);
    }

    public static T Get()
    { 
        return null != pool && pool.Count == 0 ? new() : pool[^1];
    }

    public static void Store(ref T obj, bool expandPool = true)
    {
        if (null == pool && expandPool)
        {
            pool = new();
            ++capacity;          
        } 
        else if (null != pool && pool.Count >= capacity && expandPool)
        {
            ++capacity;
        }

        if (null != pool && pool.Count < capacity)
            pool.Add(obj);

        obj = null;
    }

}
