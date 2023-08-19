using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfParentingTrigger : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = false;

    [SerializeField]
    protected PriorityValueSetter<Transform> m_parent = new();


    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (!m_parent.m_ignore && (!m_onlyForPlayer || GfLevelManager.GetPlayer() == movement.transform))
        {
            movement.SetParentTransform(m_parent, m_parent.m_priority, m_parent.m_overridePriority);
        }
    }
}
