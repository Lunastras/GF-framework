using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLoadoutSetter : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = true;

    [SerializeField]
    protected int m_loadoutIndex = 0; //the index of the loadout

    [SerializeField]
    protected int m_weaponIndex = 0; //the index of the weapon in the loadout

    [SerializeField]
    protected WeaponData m_weapon;

    [SerializeField]
    protected bool m_fillToCapacity = false; //Fill the loadout with the given weapon. Ignores m_weaponIndex

    [SerializeField]
    protected bool m_switchToLoadout = true; //the index of the weapon in the loadout

    [SerializeField]
    protected bool m_destroyOnWeaponSet = true;


    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        LoadoutManager loadoutManager;
        if ((!m_onlyForPlayer || GfManagerLevel.GetPlayer() == movement.transform)
        && null != (loadoutManager = movement.GetComponent<LoadoutManager>()))
        {
            if (m_fillToCapacity)
                loadoutManager.SetLoadoutAllWeapons(m_loadoutIndex, m_weapon, true, m_switchToLoadout);
            else
                loadoutManager.SetLoadoutWeapon(m_loadoutIndex, m_weaponIndex, m_weapon, m_switchToLoadout);

            if (m_destroyOnWeaponSet)
                GfcPooling.Destroy(gameObject);

        }
    }
}
