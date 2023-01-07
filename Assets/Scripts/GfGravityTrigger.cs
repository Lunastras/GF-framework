using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfGravityTrigger : GfMovementTriggerable
{
    [SerializeField]
    private Transform m_sphericalParent;

    [SerializeField]
    private float m_smoothTime = 2.0f;

    [SerializeField]
    private uint m_priority = 0;

    [SerializeField]
    private Vector3 m_upVec = Vector3.up;

    private static readonly Vector3 UPDIR = Vector3.up;

    // Start is called before the first frame update
    void Start()
    {
        Init();
        SetUpvec(m_upVec);
    }

    protected override void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement)
    {
        Debug.Log("Called");
        if (m_sphericalParent)
            movement.SetParentSpherical(m_sphericalParent, m_smoothTime, m_priority);
        else
            movement.SetUpVec(m_upVec, m_smoothTime, m_priority);
    }

    public Vector3 GetUpVec() { return m_upVec; }
    public void SetUpvec(Vector3 upVec)
    {
        m_upVec = upVec.normalized;
        if (m_upVec.sqrMagnitude < 0.000001f)
            m_upVec = UPDIR;
    }
}
