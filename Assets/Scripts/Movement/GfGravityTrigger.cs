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

    [SerializeField]
    private float m_gravityCoef = 1.0f;

    [SerializeField]
    private bool m_reverseSphericalUpVec = false;

    private static readonly Vector3 UPDIR = Vector3.up;

    // Start is called before the first frame update
    void Start()
    {
        SetUpvec(m_upVec);
    }

    public override void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement)
    {
        if (m_sphericalParent)
            movement.SetParentSpherical(m_sphericalParent, m_smoothTime, m_priority, m_reverseSphericalUpVec);
        else
            movement.SetUpVec(m_upVec, m_smoothTime, m_priority);

        movement.SetGravityCoef(m_gravityCoef);
    }

    public Vector3 GetUpVec() { return m_upVec; }
    public void SetUpvec(Vector3 upVec)
    {
        m_upVec = upVec.normalized;
        if (m_upVec.sqrMagnitude < 0.000001f)
            m_upVec = UPDIR;
    }
}