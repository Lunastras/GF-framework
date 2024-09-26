using System;
using System.Runtime.CompilerServices;

public struct Vector4<T>
{
    public T x, y, z, w;

    public Vector4(T aX = default, T aY = default, T aZ = default, T aW = default) { x = aX; y = aY; z = aZ; w = aW; }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            switch (index)
            {
                case 0: return x;
                case 1: return y;
                case 2: return z;
                case 3: return w;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector4 index!");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            switch (index)
            {
                case 0: x = value; break;
                case 1: y = value; break;
                case 2: z = value; break;
                case 3: w = value; break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector4 index!");
            }
        }
    }
}