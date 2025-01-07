using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

[RequireComponent(typeof(GfcTransitionParent))]
public class GfcTransitionActive : MonoBehaviour
{
    [HideInInspector] public GfcTransitionParent Transition { get; protected set; }
    public GfcTransitionActiveType TransitionActiveType = GfcTransitionActiveType.BOTH;
    public GfcCoroutineHandle CoroutineHandle { get; protected set; }

    protected void Awake()
    {
        Transition = GetComponent<GfcTransitionParent>();
        Debug.Assert(Transition, "Please assign a GfcTransitionGeneric in the editor for the gameobject.");
    }

    public CoroutineHandle SetActive(bool anActive, bool aTransitionOnlyNoActiveSet = false)
    {
        Debug.Assert(TransitionActiveType == GfcTransitionActiveType.BOTH, "Selected transition mode is not supported. Only 'BOTH' is implemented"); //TODO
        if (anActive) gameObject.SetActive(true);

        Transition.SetProgress(anActive ? 0 : 1, true);
        Transition.StartTransition(anActive);

        if (aTransitionOnlyNoActiveSet)
        {
            CoroutineHandle.KillCoroutine();
            return Transition.TransitionCoroutineHandle;
        }
        else
            return CoroutineHandle.RunCoroutineIfNotRunning(_EnableRoutine().CancelWith(gameObject));
    }

    public CoroutineHandle SetTransitionNoSetActive(bool anActive)
    {
        return Transition.StartTransition(anActive);
    }

    protected virtual void OnEnable()
    {
        SetActive(true);
    }

    protected IEnumerator<float> _EnableRoutine()
    {
        yield return Timing.WaitUntilDone(Transition.TransitionCoroutineHandle);
        gameObject.SetActive(Transition.FadingIn());
    }

    public bool FadingOut() { return Transition && Transition.Transitioning() && !Transition.FadingIn(); }
}

public enum GfcTransitionActiveType
{
    BOTH,
    ENABLE,
    DISABLE,
}