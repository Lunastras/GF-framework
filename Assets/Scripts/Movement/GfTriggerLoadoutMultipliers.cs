using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLoadoutMultipliers : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = false;
    [SerializeField]
    protected PriorityValueSetter<float> m_speedMultiplier = new(1);
    [SerializeField]
    protected PriorityValueSetter<float> m_damageMultiplier = new(1);

    [SerializeField]
    protected PriorityValueSetter<float> m_fireRateMultiplier = new(1);

    [SerializeField]
    protected PriorityValueSetter<float> m_weaponCapacityMultiplier = new(1);

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        LoadoutManager loadoutManager;
        if ((!m_onlyForPlayer || GameManager.GetPlayer() == movement.transform)
        && null != (loadoutManager = movement.GetComponent<LoadoutManager>()))
        {
            if (!m_damageMultiplier.m_ignore)
                loadoutManager.SetDamageMultiplier(m_damageMultiplier, m_damageMultiplier.m_priority, m_damageMultiplier.m_overridePriority);

            if (!m_speedMultiplier.m_ignore)
                loadoutManager.SetSpeedMultiplier(m_speedMultiplier, m_speedMultiplier.m_priority, m_speedMultiplier.m_overridePriority);

            if (!m_fireRateMultiplier.m_ignore)
                loadoutManager.SetFireRateMultiplier(m_fireRateMultiplier, m_fireRateMultiplier.m_priority, m_fireRateMultiplier.m_overridePriority);

            if (!m_weaponCapacityMultiplier.m_ignore)
                loadoutManager.SetWeaponCapacityMultiplier(m_weaponCapacityMultiplier, m_weaponCapacityMultiplier.m_priority, m_weaponCapacityMultiplier.m_overridePriority);
        }
    }
}
