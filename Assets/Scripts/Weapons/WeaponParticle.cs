using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponParticle : WeaponGeneric
{
    private int m_particleTriggerDamageListIndex = -1;
    protected ParticleSystem m_particleSystem;

    protected float m_initialRateOverTimeMultiplier;

    protected void InitWeaponParticle()
    {
        m_particleSystem = GetComponent<ParticleSystem>();
        var emission = m_particleSystem.emission;
        m_initialRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
    }

    public virtual ParticleSystem GetParticleSystem()
    {
        return m_particleSystem;
    }

    protected virtual ParticleSystem SetParticleSystem(ParticleSystem ps)
    {
        return ps;
    }

    public override void StopFiring(bool killBullets)
    {
        m_isFiring = false;

        ParticleSystemStopBehavior stopBehaviour = ParticleSystemStopBehavior.StopEmitting;
        if (killBullets) stopBehaviour = ParticleSystemStopBehavior.StopEmittingAndClear;

        m_particleSystem.Stop(true, stopBehaviour);
    }

    public override void Fire(FireHit hit = default, FireType fireType = FireType.MAIN, bool forceFire = false)
    {
        m_isFiring = true;
        m_particleSystem.Play();
    }

    public override bool IsFiring() { return m_particleSystem.isEmitting; }

    public void SetParticleTriggerDamageIndex(int index) { m_particleTriggerDamageListIndex = index; }

    public int GetParticleTriggerDamageIndex() { return m_particleTriggerDamageListIndex; }

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN) { }

    public override bool IsAlive() { return m_particleSystem.IsAlive(true); }

    public override bool SetSpeedMultiplier(float multiplier, uint priority, bool overridePriority)
    {
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
        {
            var main = m_particleSystem.main;
            main.simulationSpeed = multiplier;
        }

        return changedValue;
    }

    public override bool SetFireRateMultiplier(float multiplier, uint priority, bool overridePriority)
    {
        bool changedValue = m_fireRateMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
        {
            var emission = m_particleSystem.emission;
            emission.rateOverTimeMultiplier = m_initialRateOverTimeMultiplier * multiplier;
        }

        return changedValue;
    }

    public override void SetMovementParent(GfMovementGeneric parent)
    {
        var particleHoming = GetComponent<ParticleHoming>();
        if (particleHoming)
            particleHoming.MovementGravityReference = parent;
        m_movementParent = parent;
    }

    public virtual float GetInitialRateOverTimeMultiplier() { return m_initialRateOverTimeMultiplier; }
    public virtual void SetInitialRateOverTimeMultiplier(float initialRateOverTime) { initialRateOverTime = m_initialRateOverTimeMultiplier; }

}
