using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class GfxTransitionCanvasGroup : GfxTransitionGeneric
{
    private CanvasGroup m_canvasGroup;

    // Start is called before the first frame update
    protected new void Awake()
    {
        this.GetComponent(ref m_canvasGroup);
        base.Awake();
    }

    protected override void SetProgressInternal(float aProgress)
    {
        m_canvasGroup.alpha = aProgress;
        m_canvasGroup.blocksRaycasts = aProgress != 0;
    }
}