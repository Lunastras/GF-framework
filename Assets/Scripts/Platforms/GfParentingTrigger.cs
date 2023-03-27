using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfParentingTrigger : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = false;

    [SerializeField]
    protected PriorityValueSetter<Transform> m_parent = new();


    public override void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement)
    {
        if (!m_parent.m_ignore && (!m_onlyForPlayer || GameManager.gameManager.GetPlayer() == movement.transform))
        {
            movement.SetParentTransform(m_parent, m_parent.m_priority, m_parent.m_overridePriority);
        }
    }
}
