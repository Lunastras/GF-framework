using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponParticle : WeaponBasic
{
    private int m_particleTriggerDamageListIndex = -1;
    protected ParticleSystem m_particleSystem;

    public virtual ParticleSystem GetParticleSystem()
    {
        return m_particleSystem;
    }

    public override void StopFiring()
    {
        m_particleSystem.Stop(true);
    }

    public override void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false)
    {
        m_particleSystem.Play();
    }

    public void SetParticleTriggerDamageIndex(int index) { m_particleTriggerDamageListIndex = index; }

    public int GetParticleTriggerDamageIndex() { return m_particleTriggerDamageListIndex; }

    public override void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false) { }
}