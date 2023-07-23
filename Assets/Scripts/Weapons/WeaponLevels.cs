using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponLevels : WeaponGeneric
{
    [SerializeField]
    private AudioSource m_audioSource = null;

    [SerializeField]
    protected TurretWeapons m_turret = null;

    [SerializeField]
    public Sound[] m_levelFireSounds = null;

    [SerializeField]
    public Sound[] m_levelReleaseFireSounds = null;

    [SerializeField]
    public float[] m_expRequiredForLevels = null;

    public float CurrentExp { get; private set; } = 0;

    public int CurrentLevel { get; private set; } = 0;

    public float NextLevelProgress { get; private set; } = 0;

    protected bool m_initialised = false;

    private void Awake()
    {
        Initialize();
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

    private void OnDisable()
    {
        DestroyWhenDone = DisableWhenDone = false;
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

    private void FixedUpdate()
    {
        if ((DisableWhenDone || DestroyWhenDone) && !m_turret.IsAlive())
        {
            if (DestroyWhenDone)
                GfPooling.Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    protected Sound GetFireSound()
    {
        return m_levelFireSounds.Length > 0 ? m_levelFireSounds[Mathf.Clamp(CurrentLevel, 0, m_levelFireSounds.Length - 1)] : null;
    }

    protected Sound GetReleaseFireSound()
    {
        return m_levelReleaseFireSounds.Length > 0 ? m_levelReleaseFireSounds[Mathf.Clamp(CurrentLevel, 0, m_levelReleaseFireSounds.Length - 1)] : null;
    }

    public override void Fire(FireHit hit = default, FireType fireType = FireType.MAIN, bool forceFire = false)
    {
        m_isFiring = true;

        if (m_levelFireSounds.Length > 0)
        {
            GetFireSound().Play(m_audioSource);
        }
        Vector3 dirBullet = (hit.point - transform.position).normalized;

        // Debug.Log("I GOT HERE ehehe");
        m_turret.SetRotation(Quaternion.LookRotation(dirBullet));

        m_turret.Play(false, CurrentLevel);
    }

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN)
    {
        if (m_levelReleaseFireSounds.Length > 0)
            GetReleaseFireSound().Play(m_audioSource);
        m_isFiring = false;
        m_turret.Stop(false);
    }

    /**
    *   Adds exp points to the weapon and sets the level accordingly
    *   @param points The ammount of points to be added
    *   @return The current number of points the weapon has
    */
    public override float AddPoints(WeaponPointsTypes type, float points)
    {
        float retPoints = 0;
        switch (type)
        {
            case (WeaponPointsTypes.EXPERIENCE):
                if (m_expRequiredForLevels.Length > 0)
                {
                    CurrentExp += points;
                    CurrentExp = Mathf.Clamp(CurrentExp, 0, m_expRequiredForLevels[m_expRequiredForLevels.Length - 1]);

                    UpdateFireLevel();

                    retPoints = CurrentExp;
                }
                break;
        }


        return retPoints;
    }

    protected void UpdateFireLevel(bool forceUpdate = false)
    {
        int auxCurrentLevel = 0;

        if (m_expRequiredForLevels.Length > 0)
        {
            // float effectiveExp = currentExp / (float)weapons.Count;
            float effectiveExp = CurrentExp;

            auxCurrentLevel = -1;
            while (m_expRequiredForLevels.Length > ++auxCurrentLevel && m_expRequiredForLevels[auxCurrentLevel] < effectiveExp) ;

            //Debug.Log("The current level is " + loadOut.currentLevel);
            float upperExp = m_expRequiredForLevels[auxCurrentLevel];
            float lowerExp = effectiveExp;

            if (auxCurrentLevel != 0)
            {
                upperExp -= m_expRequiredForLevels[auxCurrentLevel - 1];
                lowerExp -= m_expRequiredForLevels[auxCurrentLevel - 1];
            }

            if (effectiveExp == m_expRequiredForLevels[m_expRequiredForLevels.Length - 1])
            {
                auxCurrentLevel = m_expRequiredForLevels.Length;
            }

            NextLevelProgress = lowerExp / upperExp;
        }

        CurrentLevel = auxCurrentLevel;
    }

    public override bool SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
            m_turret.SetSpeedMultiplier(multiplier, priority, overridePriority);

        return changedValue;
    }

    public override bool SetDamageMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
            m_turret.SetDamageMultiplier(multiplier, priority, overridePriority);

        return changedValue;
    }

    public override bool SetFireRateMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
            m_turret.SetFireRateMultiplier(multiplier, priority, overridePriority);

        return changedValue;
    }


    public bool IsMaxLevel()
    {
        return CurrentLevel > m_expRequiredForLevels.Length;
    }

}
