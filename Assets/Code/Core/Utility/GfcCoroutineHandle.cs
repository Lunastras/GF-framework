using System.Collections.Generic;
using MEC;
using UnityEngine;

public struct GfcCoroutineHandle
{
    public CoroutineHandle CoroutineHandle
    {
        readonly get { return m_coroutineHandle; }
        set { m_coroutineHandle = value; }
    }

    private CoroutineHandle m_coroutineHandle;

    public bool CoroutineIsRunning { get { return m_coroutineHandle.IsValid; } }

    public GfcCoroutineHandle(CoroutineHandle aCoroutineHandle = default)
    {
        m_coroutineHandle = aCoroutineHandle;
    }

    public GfcCoroutineHandle(IEnumerator<float> aEnumerator, string aTag)
    {
        m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aTag);
    }

    public GfcCoroutineHandle(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);
    }

    //done like this to avoid issues when the coroutine ends up calling this function
    public int KillCoroutine()
    {
        int ret = 0;
        if (CoroutineIsRunning)
            ret = Timing.KillCoroutines(CoroutineHandle);

        CoroutineHandle = default;
        return ret;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, string aTag)
    {
        if (!CoroutineIsRunning)
            CoroutineHandle = Timing.RunCoroutine(aEnumerator, aTag);

        Debug.Assert(CoroutineHandle.IsValid);
        return CoroutineHandle;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        if (!CoroutineIsRunning)
            CoroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);

        Debug.Assert(CoroutineHandle.IsValid);
        return CoroutineHandle;
    }

    public CoroutineHandle RunCoroutine(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        CoroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);
        Debug.Assert(CoroutineHandle.IsValid);
        return CoroutineHandle;
    }

    public int Finished()
    {
        return KillCoroutine();
    }

    public static implicit operator CoroutineHandle(GfcCoroutineHandle d) => d.CoroutineHandle;
}
