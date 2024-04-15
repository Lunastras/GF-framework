using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerMovGravity : GfMovementTriggerable
{
    [SerializeField]
    private Transform m_sphericalParent;

    [SerializeField]
    private Vector3 m_defaultUpVec = Vector3.up;

    [SerializeField]
    private uint m_priority = 0;

    [SerializeField]
    private bool m_overridePriority = false;


    private static readonly Vector3 UPDIR = Vector3.up;

    // Start is called before the first frame update
    void Start()
    {
        SetUpvec(m_defaultUpVec);
    }

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (m_sphericalParent)
            movement.SetParentSpherical(m_sphericalParent, m_priority, m_overridePriority);
        else
            movement.SetUpVec(m_defaultUpVec, m_priority, m_overridePriority);
    }

    public Vector3 GetUpVec() { return m_defaultUpVec; }
    public void SetUpvec(Vector3 upVec)
    {
        m_defaultUpVec = upVec.normalized;
        if (m_defaultUpVec.sqrMagnitude < 0.000001f)
            m_defaultUpVec = UPDIR;
    }

    public Transform GetSphericalParent()
    {
        return m_sphericalParent;
    }

    public void SphericalParent(Transform parent)
    {
        m_sphericalParent = parent;
    }
}
