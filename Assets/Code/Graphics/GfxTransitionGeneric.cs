using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public abstract class GfxTransitionGeneric : MonoBehaviour
{
    const float DEFAULT_TRANSITION_TIME = 0.4f;

    GfcCoroutineHandle m_transitionHandle;
    protected bool m_fadeIn;
    private bool m_ignoreTimeScale;
    private float m_progress = 0;
    private float m_durationInv;

    protected void Awake()
    {
        SetProgress(0, false);
    }

    public CoroutineHandle StartFadeIn(float aDuration = DEFAULT_TRANSITION_TIME, bool anIgnoreTimeScale = false, bool aForceChangeDuration = false) { return StartTransition(true, aDuration, anIgnoreTimeScale, aForceChangeDuration); }
    public CoroutineHandle StartFadeOut(float aDuration = DEFAULT_TRANSITION_TIME, bool anIgnoreTimeScale = false, bool aForceChangeDuration = false) { return StartTransition(false, aDuration, anIgnoreTimeScale, aForceChangeDuration); }

    public CoroutineHandle StartTransition(bool aFadeIn, float aDuration = DEFAULT_TRANSITION_TIME, bool anIgnoreTimeScale = false, bool aForceChangeDuration = false)
    {
        m_ignoreTimeScale = anIgnoreTimeScale;

        //simply ignore if the fade is already ongoing, even if the requested duration is different
        if (!m_transitionHandle.CoroutineIsRunning || aFadeIn != m_fadeIn || aForceChangeDuration)
        {
            m_fadeIn = aFadeIn;
            m_durationInv = aDuration.SafeInverse();
            m_transitionHandle.RunCoroutineIfNotRunning(_TransitionRoutine());
        }

        return m_transitionHandle;
    }

    public void FinishTransition()
    {
        m_progress = m_fadeIn ? 1 : 0;
        m_transitionHandle.KillCoroutine();
        SetProgressInternal(m_progress);
    }

    public bool Transitioning() { return m_transitionHandle.CoroutineIsRunning; }

    public void SetProgress(float aProgress, bool aFadeIn, bool aKillCoroutine = true)
    {
        m_fadeIn = aFadeIn;
        m_progress = aProgress;
        if (aKillCoroutine) m_transitionHandle.KillCoroutine();
        SetProgressInternal(aProgress);
    }

    protected abstract void SetProgressInternal(float aProgress);

    private IEnumerator<float> _TransitionRoutine()
    {
        float targetProgress = m_fadeIn ? 1 : 0;
        while (m_progress != targetProgress)
        {
            SetProgressInternal(m_progress);

            yield return Timing.WaitForOneFrame;
            m_progress += m_durationInv * (m_fadeIn ? 1 : -1) * (m_ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
            m_progress.ClampSelf(0, 1);
            targetProgress = m_fadeIn ? 1 : 0;
        }

        FinishTransition();
    }
}