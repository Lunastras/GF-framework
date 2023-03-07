using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponLevels : WeaponBasic
{
   [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    protected WeaponTurret turret;

    [SerializeField]
    public Sound[] levelFireSounds;

    [SerializeField]
    public Sound[] levelReleaseFireSounds;

    [SerializeField]
    public float[] expRequiredForLevels;



    public float currentExp { get; private set; } = 0;

    public int currentLevel { get; private set; } = 0;

    public float nextLevelProgress { get; private set; } = 0;

    public bool destroyWhenDone { get; set; } = false;

    public bool disableWhenDone { get; set; } = false;

    public override void StopFiring()
    {
        turret.Stop();
    }

    private void OnEnable()
    {
        destroyWhenDone = disableWhenDone = false;
    }

    private void Awake()
    {
        if (null == turret)
        {
            turret = GetComponent<WeaponTurret>();
        }

        if (null == audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (null == audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }


    public override bool IsAlive(bool currentPhase = false)
    {
        return turret.IsAlive(currentPhase);
    }

    private void FixedUpdate()
    {
        if ((disableWhenDone || destroyWhenDone) && !turret.IsAlive())
        {
            if (destroyWhenDone)
                GfPooling.Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    public override void SetStatsCharacter(StatsCharacter value)
    {
        m_statsCharacter = value;
        turret.SetStatsCharacter(value);
    }

    protected Sound GetFireSound()
    {
        return levelFireSounds.Length > 0 ? levelFireSounds[Mathf.Clamp(currentLevel, 0, levelFireSounds.Length - 1)] : null;
    }

    protected Sound GetReleaseFireSound()
    {
        return levelReleaseFireSounds.Length > 0 ? levelReleaseFireSounds[Mathf.Clamp(currentLevel, 0, levelReleaseFireSounds.Length - 1)] : null;
    }

    public override void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false)
    {
        if (levelFireSounds.Length > 0)
        {
            GetFireSound().Play(audioSource);
        }

        Vector3 dirBullet = (hit.point - transform.position).normalized;

        // Debug.Log("I GOT HERE ehehe");
        turret.SetRotation(Quaternion.LookRotation(dirBullet));

        turret.Play(false, currentLevel);
    }

    public override void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false)
    {
        if (levelReleaseFireSounds.Length > 0)
            GetReleaseFireSound().Play(audioSource);
        turret.Stop();
    }

    /**
    *   Adds exp points to the weapon and sets the level accordingly
    *   @param points The ammount of points to be added
    *   @return The current number of points the weapon has
    */
    public float AddExpPoint(float points)
    {
        if (expRequiredForLevels.Length > 0)
        {
            currentExp += points;
            currentExp = Mathf.Clamp(currentExp, 0, expRequiredForLevels[expRequiredForLevels.Length - 1]);

            UpdateFireLevel();
        }

        return currentExp;
    }

    /** Adds a fixed percentage of progress relative to the 
    * exp required for the current and next level
    * @param percent The percentage of progress to add
    * @return The current number of points the weapon has
    */
    public float AddExpPercent(float percent)
    {
        if (currentLevel <= expRequiredForLevels.Length && expRequiredForLevels.Length > 0)
        {
            int effectiveLevel = currentLevel;
            if (currentLevel == expRequiredForLevels.Length)
                effectiveLevel--;

            float upperExp = expRequiredForLevels[effectiveLevel];
            float lowerExp = 0;

            if (currentLevel > 1)
            {
                lowerExp = expRequiredForLevels[effectiveLevel - 1];
            }

            AddExpPoint(percent * (upperExp - lowerExp));
        }

        return currentExp;
    }

    protected void UpdateFireLevel(bool forceUpdate = false)
    {
        int auxCurrentLevel = 0;

        if (expRequiredForLevels.Length > 0)
        {
            // float effectiveExp = currentExp / (float)weapons.Count;
            float effectiveExp = currentExp;

            auxCurrentLevel = -1;
            while (expRequiredForLevels.Length > ++auxCurrentLevel && expRequiredForLevels[auxCurrentLevel] < effectiveExp) ;

            //Debug.Log("The current level is " + loadOut.currentLevel);
            float upperExp = expRequiredForLevels[auxCurrentLevel];
            float lowerExp = effectiveExp;

            if (auxCurrentLevel != 0)
            {
                upperExp -= expRequiredForLevels[auxCurrentLevel - 1];
                lowerExp -= expRequiredForLevels[auxCurrentLevel - 1];
            }

            if (effectiveExp == expRequiredForLevels[expRequiredForLevels.Length - 1])
            {
                auxCurrentLevel = expRequiredForLevels.Length;
            }

            nextLevelProgress = lowerExp / upperExp;
        }

        currentLevel = auxCurrentLevel;
    }

    public bool IsMaxLevel()
    {
        return currentLevel > expRequiredForLevels.Length;
    }

}
