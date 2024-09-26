using System.Collections.Generic;
using MEC;
using UnityEngine;

public struct GfcCoroutineHandle
{
    public CoroutineHandle CoroutineHandle
    {
        readonly get { return m_coroutineHandle; }
        set { m_coroutineHandle = value; CoroutineIsRunning = value.IsValid; }
    }

    private CoroutineHandle m_coroutineHandle;

    public bool CoroutineIsRunning { get; private set; }

    public GfcCoroutineHandle(CoroutineHandle aCoroutineHandle = default)
    {
        m_coroutineHandle = aCoroutineHandle;
        CoroutineIsRunning = aCoroutineHandle.IsValid;
    }

    public GfcCoroutineHandle(IEnumerator<float> aEnumerator, string aTag)
    {
        m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aTag);
        CoroutineIsRunning = m_coroutineHandle.IsValid;
    }

    public GfcCoroutineHandle(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);
        CoroutineIsRunning = m_coroutineHandle.IsValid;
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

        return CoroutineHandle;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        if (!CoroutineIsRunning)
            CoroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);

        return CoroutineHandle;
    }

    public CoroutineHandle RunCoroutine(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        CoroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);

        return CoroutineHandle;
    }

    public void Finished()
    {
        CoroutineHandle = default;
    }

    public static implicit operator CoroutineHandle(GfcCoroutineHandle d) => d.CoroutineHandle;
}
