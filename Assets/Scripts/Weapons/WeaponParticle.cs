using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponParticle : WeaponGeneric
{
    [SerializeField]
    protected float m_fireRateInterval = 0.1f;

    [SerializeField]
    protected bool m_eraseParticlesAfterOwnerKilled = true;

    [SerializeField]
    protected float m_particlesEraseFromCenterSpeed = 10f;

    private int m_particleTriggerDamageListIndex = -1;
    protected ParticleSystem m_particleSystem;

    protected Transform m_transform;

    [SerializeField]
    protected bool m_aimsAtTarget = true;

    protected float m_timeUntilCanFire = 0;

    protected bool m_fireReleased = true;

    protected void Awake()
    {
        m_transform = transform;
        m_particleSystem = GetComponent<ParticleSystem>();
        if (GetStatsCharacter() == null)
            SetStatsCharacter(GetComponent<StatsCharacter>());
    }

    private void FixedUpdate()
    {
        m_timeUntilCanFire -= Time.deltaTime * m_fireRateMultiplier;
        LookAtTarget();

        if ((DisableWhenDone || DestroyWhenDone) && !IsAlive())
        {
            if (DestroyWhenDone)
                GfPooling.Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }


    protected void LookAtTarget()
    {
        if (m_aimsAtTarget && m_target && m_isFiring)
        {
            if (m_movementParent)
                m_transform.LookAt(m_target, m_movementParent.GetUpvecRotation());
            else
                m_transform.LookAt(m_target);
        }
    }

    public virtual ParticleSystem GetParticleSystem()
    {
        return m_particleSystem;
    }

    protected virtual ParticleSystem SetParticleSystem(ParticleSystem ps)
    {
        return ps;
    }

    public override void EraseAllBullets(StatsCharacter characterResponsible)
    {
        base.EraseAllBullets(characterResponsible);
        ParticleEraser.EraseParticlesFromCenter(m_particleSystem, characterResponsible.transform.position, m_particlesEraseFromCenterSpeed);
    }

    protected void OnOwnerKilled(StatsCharacter character, ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        if (m_eraseParticlesAfterOwnerKilled)
            ParticleEraser.EraseParticlesFromCenter(m_particleSystem, character.transform.position, m_particlesEraseFromCenterSpeed);
    }

    public override void StopFiring(bool killBullets)
    {
        m_fireReleased = true;
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
                if (m_timeUntilCanFire <= 0 && (m_automatic || m_fireReleased))
                {
                    m_fireReleased = false;
                    m_timeUntilCanFire = m_fireRateInterval;

                    if (!m_isFiring)
                    {
                        m_isFiring = true;
                        LookAtTarget();
                    }

                    m_isFiring = true;
                    m_particleSystem.Play();
                }
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

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN)
    {
        switch (fireType)
        {
            case (FireType.MAIN):
                m_fireReleased = false;
                StopFiring(false);
                break;
            case (FireType.SECONDARY):
                break;
            case (FireType.SPECIAL):
                break;

        }
    }

    public override bool IsAlive() { return m_particleSystem.IsAlive(true); }

    public override void SetSpeedMultiplier(float multiplier)
    {
        base.SetSpeedMultiplier(multiplier);
        var main = m_particleSystem.main;
        main.simulationSpeed = multiplier;
    }

    public override void SetFireRateMultiplier(float multiplier)
    {
        float previousMultiplier = m_fireRateMultiplier;
        base.SetFireRateMultiplier(multiplier);

        var emission = m_particleSystem.emission;
        float initialRateOverTimeMultiplier = emission.rateOverTimeMultiplier / previousMultiplier;
        emission.rateOverTimeMultiplier = initialRateOverTimeMultiplier * multiplier;

        int burstCount = emission.burstCount;
        for (int i = 0; i < burstCount; ++i)
        {
            var burstModule = m_particleSystem.emission.GetBurst(i);

            initialRateOverTimeMultiplier = burstModule.repeatInterval / previousMultiplier;
            emission.rateOverTimeMultiplier = initialRateOverTimeMultiplier * multiplier;

            m_particleSystem.emission.SetBurst(i, burstModule);
        }
    }

    public override void SetMovementParent(GfMovementGeneric parent)
    {
        var particleHoming = GetComponent<ParticleHoming>();
        if (particleHoming)
            particleHoming.MovementGravityReference = parent;
        m_movementParent = parent;
    }
}
