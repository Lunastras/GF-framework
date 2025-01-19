using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class GfcTransitionParent : MonoBehaviour
{
#if UNITY_EDITOR
    public bool PrintLogs = false;
#endif
    public bool IgnoreTimeScale = false;
    [SerializeField] private float m_duration = 0.3f;

    public float ParentIndexDelaySeconds = 0;
    GfcCoroutineHandle m_transitionHandle;

    protected bool m_fadeIn;
    private bool m_ignoreTimeScale;
    private float m_progressRaw = 0;
    private float m_durationInv;

    public float Duration
    {
        get { return m_duration; }
        set
        {
            m_duration = value;
            m_durationInv = m_duration.SafeInverse();
        }
    }

    List<GfcTransitionChild> m_children = new(1);

    public void AddChild(GfcTransitionChild aChild) { m_children.Add(aChild); }

    public CoroutineHandle TransitionCoroutineHandle { get { return m_transitionHandle; } }

    public float GetProgressRaw() { return m_progressRaw; }

    public CoroutineHandle StartFadeIn(bool anIgnoreTimeScale = false, bool aForceChangeDuration = false) { return StartTransition(true, anIgnoreTimeScale, aForceChangeDuration); }
    public CoroutineHandle StartFadeOut(bool anIgnoreTimeScale = false, bool aForceChangeDuration = false) { return StartTransition(false, anIgnoreTimeScale, aForceChangeDuration); }

    public CoroutineHandle StartTransition(bool aFadeIn, bool anIgnoreTimeScale = false, bool aForceChangeDuration = false)
    {
#if UNITY_EDITOR
        if (PrintLogs) Debug.Log("Received request for " + (aFadeIn ? " FADE IN " : "FADE OUT"));
#endif

        m_ignoreTimeScale = anIgnoreTimeScale;

        //simply ignore if the fade is already ongoing, even if the requested duration is different
        if (!m_transitionHandle.CoroutineIsRunning || aFadeIn != m_fadeIn || aForceChangeDuration)
        {
            m_fadeIn = aFadeIn;
            m_durationInv = m_duration.SafeInverse();
            m_transitionHandle.RunCoroutineIfNotRunning(_TransitionRoutine().CancelWith(gameObject));
        }

        return m_transitionHandle;
    }

    public void FinishTransition()
    {
        m_progressRaw = m_fadeIn ? 1 : 0;
        m_transitionHandle.KillCoroutine();
        SetProgressInternal(m_progressRaw);
    }

    public bool Transitioning() { return m_transitionHandle.CoroutineIsRunning; }

    public bool FadingIn() { return m_fadeIn; }

    public void SetProgress(float aProgress, bool aFadeIn, bool aKillCoroutine = true)
    {
        m_fadeIn = aFadeIn;
        m_progressRaw = aProgress;
        if (aKillCoroutine) m_transitionHandle.KillCoroutine();
        SetProgressInternal(aProgress);
    }

    protected void SetProgressInternal(float aProgress)
    {
        foreach (GfcTransitionChild child in m_children) child.SetProgress(aProgress);
    }

    private bool IgnoreTimeScaleEffective { get { return m_ignoreTimeScale || IgnoreTimeScale; } }

    private IEnumerator<float> _TransitionRoutine()
    {
        yield return Timing.WaitForOneFrame;

        if (ParentIndexDelaySeconds > 0)
            yield return GfcCoroutineHandle.WaitForSeconds(ParentIndexDelaySeconds * transform.GetSiblingIndex(), IgnoreTimeScaleEffective);

        float targetProgress = m_fadeIn ? 1 : 0;
        while (m_progressRaw != targetProgress)
        {
            SetProgressInternal(m_progressRaw);

            yield return Timing.WaitForOneFrame;
            m_progressRaw += m_durationInv * (m_fadeIn ? 1 : -1) * (IgnoreTimeScaleEffective ? Time.unscaledDeltaTime : Time.deltaTime);
            m_progressRaw.ClampSelf(0, 1);
            targetProgress = m_fadeIn ? 1 : 0;
        }

        FinishTransition();
    }
}