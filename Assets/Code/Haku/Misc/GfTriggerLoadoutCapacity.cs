using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLoadoutCapacity : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = false;

    [SerializeField]
    protected int m_newCapacity = 1;

    [SerializeField]
    protected bool m_repeatWeapons = true; //whether or not the weapons should repeat in the loadouts until the new capacity is met

    [SerializeField]
    protected bool m_keepSameExp = true; //keep exp of the inherited weapons after filing. Ignored if m_repeatWeapons is false

    [SerializeField]
    protected bool m_ignoreIfCapacityIsHigher = true;

    [SerializeField]
    protected bool m_fillToCapacity = false; //Fill the loadout with the given weapon. Ignores m_weaponIndex

    [SerializeField]
    protected bool m_destroyOnWeaponSet = true;


    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        GfgLoadoutManager GfgLoadoutManager;
        if ((!m_onlyForPlayer || GfgManagerLevel.GetPlayer() == movement.transform)
        && null != (GfgLoadoutManager = movement.GetComponent<GfgLoadoutManager>()))
        {
            bool ignore = m_ignoreIfCapacityIsHigher && GfgLoadoutManager.GetWeaponCapacity() >= m_newCapacity;

            if (!ignore)
                GfgLoadoutManager.SetWeaponCapacity(m_newCapacity, m_repeatWeapons, m_keepSameExp);

            if (m_destroyOnWeaponSet)
                GfcPooling.Destroy(gameObject);

        }
    }
}
