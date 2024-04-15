using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgLootDropWeapon : GfMovementTriggerable
{
    public GameObject MainGameObject = null;

    public ParticleSystem[] ParticleSystems;

    public WeaponData m_weaponData;

    public bool WasPicked = false;

    const float PARTICLES_ALIVE_CHECK_INTERVAL = 1.0f;

    public void SetWeaponData(WeaponData aWeaponData)
    {
        m_weaponData = aWeaponData;
    }

    public WeaponData GetWeaponData()
    {
        return m_weaponData;
    }

    protected void OnEnable()
    {
        WasPicked = false;
    }

    public override void MgOnTrigger(GfMovementGeneric aMovement)
    {
        StatsPlayer statsPlayer = aMovement.GetComponent<StatsPlayer>();
        if (!WasPicked && InvManagerLevel.GetPlayerStats() == statsPlayer && statsPlayer)
        {
            WasPicked = true;
            statsPlayer.AddWeapon(m_weaponData);
            GameObject mainObject = MainGameObject ? MainGameObject : gameObject;
            GfcTools.PoolAndDestroyWhenParticlesDead(mainObject, ParticleSystems, PARTICLES_ALIVE_CHECK_INTERVAL);
        }
    }
}
