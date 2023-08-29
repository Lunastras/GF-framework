using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(ParticleEraser))]
public abstract class WeaponParticle : WeaponGeneric
{
    [SerializeField]
    protected float m_fireRateInterval = 0.1f;

    [SerializeField]
    protected bool m_eraseParticlesAfterOwnerKilled = true;

    [SerializeField]
    protected float m_particlesEraseFromCenterSpeed = 10f;

    [SerializeField]
    protected ParticleEraser m_particleEraser = null;

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

        if (m_particleEraser == null)
            m_particleEraser = GetComponent<ParticleEraser>();
    }

    protected void FixedUpdate()
    {
        m_timeUntilCanFire -= Time.deltaTime * m_weaponMultipliers[(int)WeaponMultiplierTypes.FIRE_RATE];
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

    public override void EraseAllBullets(StatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius)
    {
        base.EraseAllBullets(characterResponsible, centerOfErase, speedFromCenter, eraseRadius);
        m_particleEraser.EraseParticles(centerOfErase, speedFromCenter, eraseRadius);
    }

    protected void OnOwnerKilled(StatsCharacter character, ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        if (m_eraseParticlesAfterOwnerKilled)
            m_particleEraser.EraseParticles(character.transform.position, m_particlesEraseFromCenterSpeed, 100000);
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
        var statsCharacter = GetStatsCharacter();
        if (statsCharacter)
            statsCharacter.OnKilled += OnOwnerKilled;
    }

    public override void WasSwitchedOff()
    {
        base.WasSwitchedOff();

        var statsCharacter = GetStatsCharacter();
        if (statsCharacter)
            statsCharacter.OnKilled -= OnOwnerKilled;
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

    public override bool SetMultiplier(WeaponMultiplierTypes multiplierType, float multiplier)
    {
        float previousMultiplier = m_weaponMultipliers[(int)multiplierType];

        bool changedValue = base.SetMultiplier(multiplierType, multiplier);
        if (changedValue)
        {
            switch (multiplierType)
            {
                case (WeaponMultiplierTypes.FIRE_RATE):
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
                    break;

                case (WeaponMultiplierTypes.SPEED):
                    var main = m_particleSystem.main;
                    main.simulationSpeed = multiplier;
                    break;
                case (WeaponMultiplierTypes.SPREAD):
                    break;
            }
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
}
