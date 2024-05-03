using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public struct GfcCoroutineHandle
{
    public CoroutineHandle CoroutineHandle { get; private set; }

    public bool CoroutineIsRunning { get; private set; }

    public GfcCoroutineHandle(CoroutineHandle aCoroutineHandle = default)
    {
        CoroutineHandle = aCoroutineHandle;
        CoroutineIsRunning = aCoroutineHandle.IsValid;
    }

    public GfcCoroutineHandle(IEnumerator<float> aEnumerator, string aTag)
    {
        CoroutineHandle = Timing.RunCoroutine(aEnumerator, aTag);
        CoroutineIsRunning = CoroutineHandle.IsValid;
    }

    public GfcCoroutineHandle(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        CoroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);
        CoroutineIsRunning = CoroutineHandle.IsValid;
    }

    public int KillCoroutine()
    {
        int ret = 0;
        CoroutineIsRunning = false;
        if (CoroutineHandle.IsValid)
            ret = Timing.KillCoroutines(CoroutineHandle);
        return ret;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, string aTag)
    {
        if (!CoroutineIsRunning)
        {
            CoroutineHandle = Timing.RunCoroutine(aEnumerator, aTag);
            CoroutineIsRunning = CoroutineHandle.IsValid;
        }

        return CoroutineHandle;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        if (!CoroutineIsRunning)
        {
            CoroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);
            CoroutineIsRunning = CoroutineHandle.IsValid;
        }

        return CoroutineHandle;
    }

    public static GfcCoroutineHandle RunCoroutine(IEnumerator<float> aEnumerator, string aTag) { return new GfcCoroutineHandle(aEnumerator, aTag); }

    public static GfcCoroutineHandle RunCoroutine(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null) { return new GfcCoroutineHandle(aEnumerator, aSegment, aTag); }

    public void Finished()
    {
        CoroutineHandle = default;
        CoroutineIsRunning = false;
    }

    public static implicit operator CoroutineHandle(GfcCoroutineHandle d) => d.CoroutineHandle;
}
