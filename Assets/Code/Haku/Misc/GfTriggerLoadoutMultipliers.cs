using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLoadoutMultipliers : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = true;
    [SerializeField]
    protected float m_multiplier = 1;

    [SerializeField]
    protected WeaponMultiplierTypes m_multiplierType = WeaponMultiplierTypes.DAMAGE;

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        GfgLoadoutManager GfgLoadoutManager;
        if ((!m_onlyForPlayer || GfgManagerLevel.GetPlayer() == movement.transform)
        && null != (GfgLoadoutManager = movement.GetComponent<GfgLoadoutManager>()))
        {
            GfgLoadoutManager.SetMultiplier(m_multiplierType, m_multiplier);
        }
    }
}
