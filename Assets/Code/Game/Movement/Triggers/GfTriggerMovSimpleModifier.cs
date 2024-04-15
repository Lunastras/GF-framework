using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerMovSimpleModifier : GfTriggerMovGenericModifier
{
    [SerializeField]
    protected PriorityValueSetter<float> m_accelerationMultiplier = new(1);
    [SerializeField]
    protected PriorityValueSetter<float> m_deaccelerationMultiplier = new(1);

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GfgManagerLevel.GetPlayer() == movement.transform)
        {
            if (!m_massMultiplier.m_ignore)
                movement.GetRunnerTemplate().SetMassMultiplier(m_massMultiplier, m_massMultiplier.m_priority, m_massMultiplier.m_overridePriority);

            if (!m_speedMultiplier.m_ignore)
                movement.GetRunnerTemplate().SetSpeedMultiplier(m_speedMultiplier, m_speedMultiplier.m_priority, m_speedMultiplier.m_overridePriority);

            GfRunnerSimple simpleMovement = movement.GetRunnerTemplate() as GfRunnerSimple;
            if (simpleMovement)
            {
                if (!m_accelerationMultiplier.m_ignore)
                    simpleMovement.SetAccelerationMultiplier(m_accelerationMultiplier, m_accelerationMultiplier.m_priority, m_accelerationMultiplier.m_overridePriority);

                if (!m_deaccelerationMultiplier.m_ignore)
                    simpleMovement.SetDeaccelerationMultiplier(m_deaccelerationMultiplier, m_deaccelerationMultiplier.m_priority, m_deaccelerationMultiplier.m_overridePriority);
            }
        }
    }
}
