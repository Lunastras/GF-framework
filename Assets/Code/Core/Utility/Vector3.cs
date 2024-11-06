using System;
using System.Runtime.CompilerServices;

public struct Vector3<T>
{
    public T x, y, z;

    public Vector3(T aX = default, T aY = default, T aZ = default) { x = aX; y = aY; z = aZ; }

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
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index! " + anIndex);
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
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3 index!" + anIndex);
            }
        }
    }

    public readonly int NumComponents() { return 3; }
}