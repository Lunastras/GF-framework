using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerMovGenericModifier : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = false;
    [SerializeField]
    protected PriorityValueSetter<float> m_speedMultiplier = new(1);
    [SerializeField]
    protected PriorityValueSetter<float> m_massMultiplier = new(1);

    public override void MgOnTrigger(ref MgCollisionStruct collision, GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GameManager.GetPlayer() == movement.transform)
        {
            if (!m_massMultiplier.m_ignore)
                movement.SetMassMultiplier(m_massMultiplier, m_massMultiplier.m_priority, m_massMultiplier.m_overridePriority);

            if (!m_speedMultiplier.m_ignore)
                movement.SetSpeedMultiplier(m_speedMultiplier, m_speedMultiplier.m_priority, m_speedMultiplier.m_overridePriority);
        }

    }
}
