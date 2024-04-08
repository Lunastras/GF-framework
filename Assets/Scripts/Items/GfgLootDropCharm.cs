using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgLootDropCharm : GfMovementTriggerable
{
    public GameObject MainGameObject = null;

    public ParticleSystem[] ParticleSystems;

    public EquipCharmData m_equipCharmData;

    private bool m_wasPicked = false;

    const float PARTICLES_ALIVE_CHECK_INTERVAL = 1.0f;

    public void SetEquipCharmData(EquipCharmData aEquipCharmData)
    {
        m_equipCharmData = aEquipCharmData;
    }

    public EquipCharmData GetEquipCharmData()
    {
        return m_equipCharmData;
    }

    protected void OnEnable()
    {
        m_wasPicked = false;
    }

    public override void MgOnTrigger(GfMovementGeneric aMovement)
    {
        StatsPlayer statsPlayer = aMovement.GetComponent<StatsPlayer>();

        if (!m_wasPicked && GfManagerLevel.GetPlayerStats() == statsPlayer && statsPlayer)
        {
            m_wasPicked = true;
            statsPlayer.AddCharm(m_equipCharmData);
            GameObject mainObject = MainGameObject ? MainGameObject : gameObject;
            GfcTools.PoolAndDestroyWhenParticlesDead(mainObject, ParticleSystems, PARTICLES_ALIVE_CHECK_INTERVAL);
        }
    }
}
