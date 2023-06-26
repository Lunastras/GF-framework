using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerMovSimpleModifier : GfTriggerMovGenericModifier
{
    [SerializeField]
    protected PriorityValueSetter<float> m_accelerationMultiplier = new(1);
    [SerializeField]
    protected PriorityValueSetter<float> m_deaccelerationMultiplier = new(1);

    public override void MgOnTrigger(ref MgCollisionStruct collision, GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GameManager.GetPlayer() == movement.transform)
        {
            if (!m_massMultiplier.m_ignore)
                movement.SetMassMultiplier(m_massMultiplier, m_massMultiplier.m_priority, m_massMultiplier.m_overridePriority);

            if (!m_speedMultiplier.m_ignore)
                movement.SetSpeedMultiplier(m_speedMultiplier, m_speedMultiplier.m_priority, m_speedMultiplier.m_overridePriority);

            GfMovementSimple simpleMovement = movement as GfMovementSimple;
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
