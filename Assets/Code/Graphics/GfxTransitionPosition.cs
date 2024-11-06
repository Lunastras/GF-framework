using UnityEngine;

public class GfxTransitionPosition : GfcTransitionChild
{
    public AnimationCurve AnimationCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));

    [SerializeField] private Transform m_movementTransform;
    [SerializeField] private Vector3 m_positionOffset;

    // Start is called before the first frame update
    protected new void Awake()
    {
        Debug.Assert(m_movementTransform);
        base.Awake();
    }

    public override void SetProgress(float aProgress)
    {
        aProgress = AnimationCurve.Evaluate(aProgress);
        m_movementTransform.localPosition = m_positionOffset * (1.0f - aProgress);
    }
}