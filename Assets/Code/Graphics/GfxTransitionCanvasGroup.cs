using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class GfxTransitionCanvasGroup : GfcTransitionChild
{
    public AnimationCurve AnimationCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));

    [SerializeField] private CanvasGroup m_canvasGroup;

    // Start is called before the first frame update
    protected new void Awake()
    {
        this.GetComponentIfNull(ref m_canvasGroup);
        base.Awake();
    }

    public override void SetProgress(float aProgress)
    {
        aProgress = AnimationCurve.Evaluate(aProgress);
        m_canvasGroup.alpha = aProgress;
        m_canvasGroup.blocksRaycasts = aProgress != 0;
    }
}