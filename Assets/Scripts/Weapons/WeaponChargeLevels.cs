using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class WeaponChargeLevels : WeaponGeneric
{
    [SerializeField]
    protected float m_fireRateInterval = 0.1f;

    [SerializeField]
    protected AudioSource m_audioSource = null;

    [SerializeField]
    protected TurretWeapons m_turret = null;

    [SerializeField]
    protected float m_chargePointsSpeed = 50;

    [SerializeField]
    protected float m_dischargePointsSpeed = 50;

    [SerializeField]
    protected int m_bombPhase = 6;

    [SerializeField]
    protected int m_bombAltMainFirePhase = -1;

    [SerializeField]
    protected bool m_eraseEnemyBulletsOnBomb = true;

    [SerializeField]
    protected int m_expPointsRequiredForBomb = 100;

    [SerializeField]
    protected int m_expPointsAfterBomb = 0;

    [SerializeField]
    protected int m_bombParticleEraseSpeed = 30;

    [SerializeField]
    protected float m_bombParticleEraseRadius = float.MaxValue;

    [SerializeField]
    public StructArray<float>[] m_expRequiredForLevels = new StructArray<float>[2];

    [SerializeField]
    public GfSound[] m_chargeBeginSound = null;

    [SerializeField]
    public GfSound[] m_chargeEndSound = null;

    [SerializeField]
    public GfSound[] m_levelFireSounds = null;

    [SerializeField]
    public GfSound[] m_levelReleaseFireSounds = null;

    protected bool m_fireReleased = false;

    protected float m_timeUntilCanFire = 0;

    public float GetCurrentExp(int index = 0)
    {
        return m_currentExps[index];
    }

    public int GetCurrentLevel(int index = 0)
    {
        return m_currentLevels[index];
    }

    public float GetNextLevelProgress(int index = 0)
    {
        return m_nextLevelProgress[index];
    }

    public bool GetIsCharging { get { return m_isCharging; } }

    protected float[] m_currentExps = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];

    protected int[] m_currentLevels = new int[(int)WeaponPointsTypes.NUMBER_OF_TYPES];

    protected float[] m_nextLevelProgress = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];

    protected bool m_initialised = false;

    protected bool m_isCharging = false;

    protected bool m_isDischarging = false;

    protected int m_dischargeLevel = 0;

    protected int m_previousExpLevelPlayed = 0;

    protected bool m_eraseEnemyBullets = false;

    protected float m_timeUntilNextBulletsErase = 0;

    protected void Awake()
    {
        Initialize();
    }

    protected void Update()
    {
        float deltaTime = Time.deltaTime * GetStatsCharacter().GetDeltaTimeCoef();
        m_timeUntilCanFire -= Time.deltaTime * m_weaponMultipliers[(int)WeaponMultiplierTypes.FIRE_RATE];

        if (m_isCharging || m_isDischarging)
        {
            int chargeIndex = 1;
            float pointsToAdd = deltaTime;

            if (m_isCharging)
                pointsToAdd *= m_chargePointsSpeed;
            else // m_isDischarging is true
                pointsToAdd *= -m_dischargePointsSpeed;

            if (m_isDischarging && m_eraseEnemyBullets)
            {
                m_timeUntilNextBulletsErase -= Time.deltaTime;
                if (m_timeUntilNextBulletsErase <= 0)
                {
                    m_timeUntilNextBulletsErase = 0.5f;
                    var statsCharacter = GetStatsCharacter();
                    HostilityManager.EraseAllEnemyBullets(statsCharacter, statsCharacter.transform.position, m_bombParticleEraseSpeed, m_bombParticleEraseRadius);
                }
            }

            AddPoints(WeaponPointsTypes.CHARGE, pointsToAdd);
            if (m_isDischarging && 0 == m_currentExps[chargeIndex])
                OnDischargeOver();
        }
    }

    protected void FixedUpdate()
    {
        if ((DisableWhenDone || DestroyWhenDone) && !m_turret.IsAlive())
        {
            if (DestroyWhenDone)
                GfPooling.Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    protected void OnDisable()
    {
        DestroyWhenDone = DisableWhenDone = false;
    }

    public override void Initialize()
    {
        if (!m_initialised)
        {
            if (null == m_turret)
            {
                m_turret = GetComponent<TurretWeapons>();
            }

            if (null == m_audioSource)
            {
                m_audioSource = GetComponent<AudioSource>();
                if (null == m_audioSource)
                {
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            for (int i = 0; i < m_levelFireSounds.Length; ++i)
            {
                m_levelFireSounds[i].LoadAudioClip();
            }

            for (int i = 0; i < m_levelReleaseFireSounds.Length; ++i)
            {
                m_levelReleaseFireSounds[i].LoadAudioClip();
            }

            m_initialised = true;
        }
    }

    public override void StopFiring(bool killBullets)
    {
        m_fireReleased = true;
        m_isFiring = false;
        m_turret.Stop(killBullets);
    }

    public override void SetMovementParent(GfMovementGeneric parent)
    {
        m_turret.SetMovementParent(parent);
    }

    public override bool IsFiring() { return m_turret.IsPlaying(); }

    public override bool IsAlive()
    {
        return m_turret.IsAlive();
    }

    protected GfSound GetFireSound()
    {
        return m_levelFireSounds.Length > 0 ? m_levelFireSounds[Mathf.Clamp(m_currentLevels[m_currentLevels[0]], 0, m_levelFireSounds.Length - 1)] : null;
    }

    protected GfSound GetReleaseFireSound()
    {
        return m_levelReleaseFireSounds.Length > 0 ? m_levelReleaseFireSounds[Mathf.Clamp(m_currentLevels[0], 0, m_levelReleaseFireSounds.Length - 1)] : null;
    }

    public override void Fire(FireHit hit = default, FireType fireType = FireType.MAIN, bool forceFire = false)
    {
        switch (fireType)
        {
            case (FireType.MAIN):
                OnMainFire(hit, fireType, forceFire);
                if (m_isCharging)//used to give players a way to not activate the bomb
                    OnDischargeStart(false);
                break;
            case (FireType.SECONDARY):
                OnChargeStart();
                break;
            case (FireType.SPECIAL):
                break;

        }
    }

    protected virtual void OnMainFire(FireHit hit, FireType fireType, bool forceFire)
    {
        if (m_timeUntilCanFire <= 0 && (m_automatic || m_fireReleased))
        {
            m_fireReleased = false;
            m_timeUntilCanFire = m_fireRateInterval;

            m_isFiring = true;

            int phaseToPlay = m_currentLevels[0];
            if (m_isDischarging && m_dischargeLevel == m_bombPhase && 0 <= m_bombAltMainFirePhase)
            {
                phaseToPlay = m_bombAltMainFirePhase;
            }

            if (m_levelFireSounds.Length > 0)
            {
                GetFireSound().Play(m_audioSource);
            }

            if (phaseToPlay != m_previousExpLevelPlayed)
                m_turret.Stop(m_previousExpLevelPlayed, false);

            m_turret.Play(forceFire, phaseToPlay, false);
            m_previousExpLevelPlayed = phaseToPlay;
        }
    }

    protected virtual void OnChargeStart()
    {
        m_isCharging = !m_isDischarging;
    }

    protected virtual void OnChargeEnd()
    {
        m_isCharging = false;
    }

    public virtual void FireBomb()
    {
        SetPoints(WeaponPointsTypes.EXPERIENCE, m_expPointsRequiredForBomb);
        m_isDischarging = true;
        m_dischargeLevel = m_bombPhase;
        if (m_bombPhase >= 0)
            m_turret.Play(false, m_bombPhase);
        SetPoints(WeaponPointsTypes.EXPERIENCE, 0);
        if (m_eraseEnemyBulletsOnBomb)
        {
            m_eraseEnemyBullets = true;
            var statsCharacter = GetStatsCharacter();
            HostilityManager.EraseAllEnemyBullets(statsCharacter, statsCharacter.transform.position, m_bombParticleEraseSpeed, m_bombParticleEraseRadius);
        }
    }

    protected virtual void OnDischargeStart(bool canBomb)
    {
        OnChargeEnd();
        m_dischargeLevel = 1 + m_currentLevels[(int)WeaponPointsTypes.CHARGE] + m_expRequiredForLevels[(int)WeaponPointsTypes.EXPERIENCE].array.Length;
        if (canBomb && GetCurrentExp(0) == m_expPointsRequiredForBomb && IsMaxLevel(WeaponPointsTypes.CHARGE))
        { //play the bomb, the special phase
            FireBomb();
            SetPoints(WeaponPointsTypes.EXPERIENCE, m_expPointsAfterBomb);
        }
        else
        {
            m_turret.Play(false, m_dischargeLevel);
        }

        m_isDischarging = true;
    }

    protected virtual void OnDischargeOver()
    {
        if (m_dischargeLevel >= 0)
            m_turret.Stop(m_dischargeLevel, false);

        if (m_bombAltMainFirePhase >= 0)
            m_turret.Stop(m_bombAltMainFirePhase, false);

        m_eraseEnemyBullets = false;
        m_isDischarging = false;
    }

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN)
    {
        switch (fireType)
        {
            case (FireType.MAIN):
                m_fireReleased = true;
                if (m_levelReleaseFireSounds.Length > 0)
                    GetReleaseFireSound().Play(m_audioSource);
                m_isFiring = false;
                m_turret.Stop(m_previousExpLevelPlayed, false);
                break;
            case (FireType.SECONDARY):
                if (m_isCharging)
                    OnDischargeStart(true);
                break;
            case (FireType.SPECIAL):
                break;

        }
    }

    /**
    *   Adds exp points to the weapon and sets the level accordingly
    *   @param points The ammount of points to be added
    *   @return The current number of points the weapon has
    */
    public override float AddPoints(WeaponPointsTypes type, float points)
    {
        int index = (int)type;
        m_currentExps[index] += points;
        GetExpLevels(m_expRequiredForLevels[index].array, ref m_currentExps[index], ref m_currentLevels[index], ref m_nextLevelProgress[index]);

        return m_currentExps[index];
    }

    public override float GetPoints(WeaponPointsTypes type)
    {
        return m_currentExps[(int)type];
    }

    /**
    *   Adds exp points to the weapon and sets the level accordingly
    *   @param points The ammount of points to be added
    *   @return The current number of points the weapon has
    */
    public override float SetPoints(WeaponPointsTypes type, float points)
    {
        int index = (int)type;
        m_currentExps[index] = points;
        GetExpLevels(m_expRequiredForLevels[index].array, ref m_currentExps[index], ref m_currentLevels[index], ref m_nextLevelProgress[index]);

        return m_currentExps[index];
    }

    public override void SetSeed(uint seed)
    {
        base.SetSeed(seed);
        m_turret.SetSeed(seed);
    }

    protected static void GetExpLevels(float[] expArray, ref float currentExp, ref int currentLevel, ref float nextLevelProgress)
    {
        int auxCurrentLevel = 0;

        if (expArray.Length > 0)
        {
            currentExp = Mathf.Clamp(currentExp, 0, expArray[expArray.Length - 1]);

            // float effectiveExp = currentExp / (float)weapons.Count;
            auxCurrentLevel = -1;
            while (expArray.Length > ++auxCurrentLevel && expArray[auxCurrentLevel] < currentExp) ;

            //Debug.Log("The current level is " + loadOut.currentLevel);
            float upperExp = expArray[auxCurrentLevel];
            float lowerExp = currentExp;

            if (auxCurrentLevel != 0)
            {
                upperExp -= expArray[auxCurrentLevel - 1];
                lowerExp -= expArray[auxCurrentLevel - 1];
            }

            if (currentExp == expArray[expArray.Length - 1])
            {
                auxCurrentLevel = expArray.Length;
            }

            nextLevelProgress = lowerExp / upperExp;
        }

        currentLevel = auxCurrentLevel;
    }

    public override bool SetMultiplier(WeaponMultiplierTypes weaponMultiplier, float multiplier)
    {
        bool changedValue = base.SetMultiplier(weaponMultiplier, multiplier);
        m_turret.SetMultiplier(weaponMultiplier, multiplier);

        return changedValue;
    }

    public override void EraseAllBullets(StatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius)
    {
        m_turret.EraseAllBullets(characterResponsible, centerOfErase, speedFromCenter, eraseRadius);
    }


    public bool IsMaxLevel(WeaponPointsTypes type)
    {
        return m_currentLevels[(int)type] >= m_expRequiredForLevels[(int)type].array.Length;
    }
}
