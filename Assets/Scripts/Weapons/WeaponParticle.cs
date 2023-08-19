using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponParticle : WeaponGeneric
{
    [SerializeField]
    protected bool m_eraseParticlesAfterOwnerKilled = true;

    [SerializeField]
    protected float m_particlesEraseFromCenterSpeed = 10f;

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

    protected void OnOwnerKilled(StatsCharacter character, ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        if (m_eraseParticlesAfterOwnerKilled)
            ParticleEraser.EraseParticlesFromCenter(m_particleSystem, character.transform.position, m_particlesEraseFromCenterSpeed);
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
        switch (fireType)
        {
            case (FireType.MAIN):
                m_isFiring = true;
                m_particleSystem.Play();
                break;
            case (FireType.SECONDARY):
                break;
            case (FireType.SPECIAL):
                break;

        }
    }

    public override void SetSeed(uint seed)
    {
        if (seed != m_particleSystem.randomSeed)
        {
            m_particleSystem.randomSeed = seed;

            foreach (Transform item in m_particleSystem.transform)
            {
                ParticleSystem subSystem = item.GetComponent<ParticleSystem>();
                if (subSystem)
                    subSystem.randomSeed = seed;
            }
        }
    }

    public override void WasSwitchedOn()
    {
        base.WasSwitchedOn();
        GetStatsCharacter().OnKilled += OnOwnerKilled;
    }

    public override void WasSwitchedOff()
    {
        base.WasSwitchedOff();
        GetStatsCharacter().OnKilled -= OnOwnerKilled;
    }

    public override uint GetSeed() { return 0; }

    public override bool IsFiring() { return m_particleSystem.isEmitting; }

    public void SetParticleTriggerDamageIndex(int index) { m_particleTriggerDamageListIndex = index; }

    public int GetParticleTriggerDamageIndex() { return m_particleTriggerDamageListIndex; }

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN) { }

    public override bool IsAlive() { return m_particleSystem.IsAlive(true); }

    public override void SetSpeedMultiplier(float multiplier)
    {
        base.SetSpeedMultiplier(multiplier);
        var main = m_particleSystem.main;
        main.simulationSpeed = multiplier;
    }

    public override void SetFireRateMultiplier(float multiplier)
    {
        base.SetFireRateMultiplier(multiplier);
        var emission = m_particleSystem.emission;
        emission.rateOverTimeMultiplier = m_initialRateOverTimeMultiplier * multiplier;
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
