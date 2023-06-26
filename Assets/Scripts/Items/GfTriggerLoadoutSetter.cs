using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLoadoutSetter : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = false;

    [SerializeField]
    protected int m_loadoutIndex = 0; //the index of the loadout

    [SerializeField]
    protected int m_weaponIndex = 0; //the index of the weapon in the loadout

    [SerializeField]
    protected int m_weapon = 0; //the value of the weapon to be used

    [SerializeField]
    protected bool m_fillToCapacity = false; //Fill the loadout with the given weapon. Ignores m_weaponIndex

    [SerializeField]
    protected bool m_destroyOnWeaponSet = true;


    public override void MgOnTrigger(ref MgCollisionStruct collision, GfMovementGeneric movement)
    {
        LoadoutManager loadoutManager;
        if ((!m_onlyForPlayer || GameManager.GetPlayer() == movement.transform)
        && null != (loadoutManager = movement.GetComponent<LoadoutManager>()))
        {
            if (m_fillToCapacity)
                loadoutManager.SetLoadoutAllWeapons(m_loadoutIndex, m_weapon, true);
            else
                loadoutManager.SetLoadoutWeapon(m_loadoutIndex, m_weaponIndex, m_weapon);

            if (m_destroyOnWeaponSet)
                GfPooling.Destroy(gameObject);

        }
    }
}
