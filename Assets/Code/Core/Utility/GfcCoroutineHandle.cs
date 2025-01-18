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

    private bool m_coroutineKilled; //made for the edge case when Finished() is called while RunCoroutine is executed
    private CoroutineHandle m_coroutineHandle;

    public bool CoroutineIsRunning { get { return m_coroutineHandle.IsValid; } }

    public GfcCoroutineHandle(CoroutineHandle aCoroutineHandle = default)
    {
        m_coroutineHandle = aCoroutineHandle;
        m_coroutineKilled = !m_coroutineHandle.IsValid;
    }

    //done like this to avoid issues when the coroutine ends up calling this function
    public int KillCoroutine()
    {
        int ret = 0;
        if (CoroutineIsRunning)
            ret = Timing.KillCoroutines(m_coroutineHandle);

        m_coroutineHandle = default;
        m_coroutineKilled = true;
        return ret;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, string aTag)
    {
        m_coroutineKilled = false;
        if (!CoroutineIsRunning)
            m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aTag);

        if (m_coroutineKilled)
            m_coroutineHandle = default;

        return m_coroutineHandle;
    }

    public CoroutineHandle RunCoroutineIfNotRunning(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        m_coroutineKilled = false;
        if (!CoroutineIsRunning)
            m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);

        if (m_coroutineKilled)
            m_coroutineHandle = default;

        return m_coroutineHandle;
    }

    public CoroutineHandle RunCoroutine(IEnumerator<float> aEnumerator, Segment aSegment = Segment.Update, string aTag = null)
    {
        m_coroutineKilled = false;
        m_coroutineHandle = Timing.RunCoroutine(aEnumerator, aSegment, aTag);

        if (m_coroutineKilled)
            m_coroutineHandle = default;

        return m_coroutineHandle;
    }

    public int Finished() { return KillCoroutine(); }
    public static implicit operator CoroutineHandle(GfcCoroutineHandle d) => d.m_coroutineHandle;

    public static float WaitForSeconds(float aSeconds, bool anIgnoreTimeScale = false)
    {
        if (aSeconds <= 0) return Timing.WaitForOneFrame;
        return Timing.WaitUntilDone(Timing.RunCoroutine(_WaitForSeconds(aSeconds, anIgnoreTimeScale)));
    }

    static IEnumerator<float> _WaitForSeconds(float aSeconds, bool anIgnoreTimeScale = false)
    {
        while (aSeconds > 0)
        {
            yield return Timing.WaitForOneFrame;
            aSeconds -= anIgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }
}