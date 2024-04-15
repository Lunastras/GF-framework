/* Original code[1] Copyright (c) 2022 Shane Celis[2]
   Licensed under the MIT License[3]
   [1]: https://gist.github.com/shanecelis/f0e295b12ec1ab09f67ad5980ac9b324
   [2]: https://twitter.com/shanecelis
   [3]: https://opensource.org/licenses/MIT
*/

using System;
using System.Collections.Generic;
using Unity.Collections;

/** Make one long linear NativeArray look like a two-dimensional jagged array. */
public struct NativeArrayOfArrays<T> : IDisposable where T : struct
{
    [ReadOnly] public NativeArray<int> starts;
    [ReadOnly] public NativeArray<int> counts;
    public NativeArray<T> linear;

    /** Provide the counts for a jagged two-dimensional array. */
    public NativeArrayOfArrays(IList<int> _counts,
                               Allocator allocator,
                               NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
        starts = new NativeArray<int>(_counts.Count, allocator, NativeArrayOptions.UninitializedMemory);
        counts = new NativeArray<int>(_counts.Count, allocator, NativeArrayOptions.UninitializedMemory);
        int start = 0;
        for (int i = 0; i < _counts.Count; i++)
        {
            starts[i] = start;
            start += (counts[i] = _counts[i]);
        }
        linear = new NativeArray<T>(start /* total */, allocator, options);
    }

    /** Initialize a non-jagged two-dimensional array. */
    public NativeArrayOfArrays(int m,
                               int n,
                               Allocator allocator,
                               NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
        starts = new NativeArray<int>(m, allocator, NativeArrayOptions.UninitializedMemory);
        counts = new NativeArray<int>(m, allocator, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < m; i++)
        {
            starts[i] = n * i;
            counts[i] = n;
        }
        linear = new NativeArray<T>(m * n, allocator, options);
    }

    /** Provide a list of arrays to initialize. */
    public NativeArrayOfArrays(IList<NativeArray<T>> lists,
                               Allocator allocator,
                               NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
        starts = new NativeArray<int>(lists.Count, allocator, NativeArrayOptions.UninitializedMemory);
        counts = new NativeArray<int>(lists.Count, allocator, NativeArrayOptions.UninitializedMemory);
        int start = 0;
        for (int i = 0; i < lists.Count; i++)
        {
            starts[i] = start;
            start += (counts[i] = lists[i].Length);
        }
        linear = new NativeArray<T>(start /* total */, allocator, options);

        for (int i = 0; i < lists.Count; i++)
        {
            this[i].CopyFrom(lists[i]);
        }
    }

    /** Provide a list of arrays to initialize. */
    public NativeArrayOfArrays(IList<T[]> lists,
                               Allocator allocator,
                               NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
        starts = new NativeArray<int>(lists.Count, allocator, NativeArrayOptions.UninitializedMemory);
        counts = new NativeArray<int>(lists.Count, allocator, NativeArrayOptions.UninitializedMemory);
        int start = 0;
        for (int i = 0; i < lists.Count; i++)
        {
            starts[i] = start;
            start += (counts[i] = lists[i].Length);
        }
        linear = new NativeArray<T>(start /* total */, allocator, options);

        for (int i = 0; i < lists.Count; i++)
        {
            this[i].CopyFrom(lists[i]);
        }
    }

    /** The subarray does not need to be disposed. */
    public NativeArray<T> this[int index]
    {
        get => linear.GetSubArray(starts[index], counts[index]);
    }

    /** Return the number of arrays. */
    public int Length => starts.Length;

    /** Return true if the linear array has been created. */
    public bool IsCreated => linear.IsCreated;

    /** Dispose of all the native arrays held by this struct. */
    public void Dispose()
    {
        linear.Dispose();
        starts.Dispose();
        counts.Dispose();
    }
}