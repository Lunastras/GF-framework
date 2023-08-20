using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponChargeLevels : WeaponGeneric
{
    [SerializeField]
    protected AudioSource m_audioSource = null;

    [SerializeField]
    protected TurretWeapons m_turret = null;

    [SerializeField]
    protected float m_chargePointsSpeed = 50;

    [SerializeField]
    protected float m_dischargePointsSpeed = 50;

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

    public float CurrentExp(int index = 0)
    {
        return m_currentExps[index];
    }

    public int CurrentLevel(int index = 0)
    {
        return m_currentLevels[index];
    }

    public float NextLevelProgress(int index = 0)
    {
        return m_nextLevelProgress[index];
    }

    public bool IsCharging { get { return m_isCharging; } }

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
        if (m_isCharging || m_isDischarging)
        {
            int chargeIndex = 1;
            float pointsToAdd = Time.deltaTime;

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
                    HostilityManager.EraseAllEnemyBullets(GetStatsCharacter());
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
        m_isFiring = false;
        m_turret.Stop(killBullets);
    }

    public override void SetMovementParent(GfMovementGeneric parent)
    {
        m_turret.SetMovementParent(parent);
    }

    public override bool IsFiring() { return m_turret.IsFiring(); }

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

    protected void OnMainFire(FireHit hit, FireType fireType, bool forceFire)
    {
        m_isFiring = true;

        if (m_levelFireSounds.Length > 0)
        {
            GetFireSound().Play(m_audioSource);
        }

        if (m_currentLevels[0] != m_previousExpLevelPlayed)
            m_turret.Stop(m_previousExpLevelPlayed, false);

        m_turret.Play(forceFire, m_currentLevels[0], false);
        m_previousExpLevelPlayed = m_currentLevels[0];
    }

    protected virtual void OnChargeStart()
    {
        m_isCharging = !m_isDischarging;
    }

    protected virtual void OnChargeEnd()
    {
        m_isCharging = false;
    }


    protected virtual void OnDischargeStart(bool canBomb)
    {
        OnChargeEnd();
        m_dischargeLevel = 1 + m_currentLevels[(int)WeaponPointsTypes.CHARGE] + m_expRequiredForLevels[(int)WeaponPointsTypes.EXPERIENCE].array.Length;
        if (canBomb && IsMaxLevel(WeaponPointsTypes.EXPERIENCE) && IsMaxLevel(WeaponPointsTypes.CHARGE))
        {
            //play the bomb, the special phase
            m_dischargeLevel++;
            SetPoints(WeaponPointsTypes.EXPERIENCE, 0);
            Debug.Log("BOMBING THESE GUYS");
            m_eraseEnemyBullets = true;
            StatsCharacter selfStats = GetStatsCharacter();

            HostilityManager.EraseAllEnemyBullets(GetStatsCharacter());
        }

        m_turret.Play(false, m_dischargeLevel);
        m_isDischarging = true;
    }

    protected virtual void OnDischargeOver()
    {
        m_eraseEnemyBullets = false;
        m_turret.Stop(m_dischargeLevel, false);
        m_isDischarging = false;
    }

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN)
    {
        switch (fireType)
        {
            case (FireType.MAIN):
                if (m_levelReleaseFireSounds.Length > 0)
                    GetReleaseFireSound().Play(m_audioSource);
                m_isFiring = false;
                m_turret.Stop(false);
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

    public override void SetSpeedMultiplier(float multiplier)
    {
        base.SetSpeedMultiplier(multiplier);
        m_turret.SetSpeedMultiplier(multiplier, 0, true);
    }

    public override void SetDamageMultiplier(float multiplier)
    {
        base.SetSpeedMultiplier(multiplier);
        m_turret.SetDamageMultiplier(multiplier, 0, true);
    }

    public override void EraseAllBullets(StatsCharacter characterResponsible)
    {
        m_turret.EraseAllBullets(characterResponsible);
    }

    public override void SetFireRateMultiplier(float multiplier)
    {
        base.SetSpeedMultiplier(multiplier);
        m_turret.SetFireRateMultiplier(multiplier, 0, true);
    }


    public bool IsMaxLevel(WeaponPointsTypes type)
    {
        return m_currentLevels[(int)type] >= m_expRequiredForLevels[(int)type].array.Length;
    }

}
