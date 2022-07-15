using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WeaponBasic : MonoBehaviour
{
    [SerializeField]
    private WeaponValues weaponValues;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    protected ParticleSingleDamage currentParticleSystem;

    public float currentExp { get; private set; } = 0;

    public int currentLevel { get; private set; } = 0;

    public float nextLevelProgress { get; private set; } = 0;

    private StatsCharacter statsCharacter;

    private bool fireReleased;

    private void OnEnable()
    {
        fireReleased = true;
        if(null == currentParticleSystem)
        {
            currentParticleSystem = ParticleSingleDamage.GetNewFiringSource();
            currentParticleSystem.transform.SetParent(transform);
            if(weaponValues.particleSystems.Length > 0)
                ParticleSingleDamage.SetNewParticleSingleDamage(weaponValues.particleSystems[currentLevel], ref currentParticleSystem);
        }
    }

    public void FreeFiringSource()
    {
        if (null != currentParticleSystem)
        {
            currentParticleSystem.Stop();
            ParticleSingleDamage.DestroyFiringSource(ref currentParticleSystem);
        }
    }

    private void Start()
    {
        if (null == audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (null == audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if(weaponValues.particleSystems.Length > 0 && null != weaponValues.particleSystems[currentLevel])
            ParticleSingleDamage.SetNewParticleSingleDamage(weaponValues.particleSystems[currentLevel], ref currentParticleSystem);

    }

    public void SetStatsCharacter(StatsCharacter value)
    {
        statsCharacter = value;
        if(currentParticleSystem)
            currentParticleSystem.SetStatsCharacter(statsCharacter);
    }

    public StatsCharacter GetStatsCharacter()
    {
        return statsCharacter;
    }

    protected virtual Sound GetFireSound()
    {
        return weaponValues.levelFireSounds.Length > 0 ? weaponValues.levelFireSounds[Mathf.Clamp(currentLevel, 0, weaponValues.levelFireSounds.Length - 1)] : null;
    }

    protected virtual Sound GetReleaseFireSound()
    {
        return weaponValues.levelReleaseFireSounds.Length > 0 ? weaponValues.levelReleaseFireSounds[Mathf.Clamp(currentLevel, 0, weaponValues.levelReleaseFireSounds.Length - 1)] : null;
    }

    protected virtual void InternalFire(RaycastHit hit, bool hitAnObject)
    {
        Vector3 dirBullet = (hit.point - transform.position).normalized;

       // Debug.Log("I GOT HERE ehehe");
        currentParticleSystem.transform.rotation = Quaternion.LookRotation(dirBullet);

        if(fireReleased)
        {
            currentParticleSystem.particleSystem.Play();
            fireReleased = false;
        }

        
    }

    protected virtual void InternalReleasedFire(RaycastHit hit, int currentLevel, bool hitAnObject)
    {
        fireReleased = true;
        currentParticleSystem.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        // Debug.Log("Released fire");
    }

    public virtual void Fire(RaycastHit hit, bool hitAnObject, bool forceFire = false)
    {
        if (weaponValues.levelFireSounds.Length > 0)
        {
            GetFireSound().Play(audioSource);
        }

        InternalFire(hit, hitAnObject);
    }

    public virtual void ReleasedFire(RaycastHit hit, bool hitAnObject)
    {
        if (weaponValues.levelReleaseFireSounds.Length > 0)
            GetReleaseFireSound().Play(audioSource);
        
        InternalReleasedFire(hit, currentLevel, hitAnObject);
    }

    public ParticleSingleDamage GetParticleSystem()
    {
        return currentParticleSystem;
    }

    /**
    *   Adds exp points to the weapon and sets the level accordingly
    *   @param points The ammount of points to be added
    *   @return The current number of points the weapon has
    */
    public float AddExpPoint(float points)
    {
        //Debug.Log("Added exp points to weapon " + points);

        if (weaponValues.expRequiredForLevels.Length > 0)
        {
            currentExp += points;
            currentExp = Mathf.Clamp(currentExp, 0, weaponValues.expRequiredForLevels[weaponValues.expRequiredForLevels.Length - 1]);

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
        if (currentLevel <= weaponValues.expRequiredForLevels.Length && weaponValues.expRequiredForLevels.Length > 0)
        {
            int effectiveLevel = currentLevel;
            if (currentLevel == weaponValues.expRequiredForLevels.Length)
                effectiveLevel--;

            float upperExp = weaponValues.expRequiredForLevels[effectiveLevel];
            float lowerExp = 0;

            if (currentLevel > 1)
            {
                lowerExp = weaponValues.expRequiredForLevels[effectiveLevel - 1];
            }

            AddExpPoint(percent * (upperExp - lowerExp));
        }

        return currentExp;
    }

    protected void UpdateFireLevel(bool forceUpdate = false)
    {
        int auxCurrentLevel = 0;

        if (weaponValues.expRequiredForLevels.Length > 0)
        {
            // float effectiveExp = currentExp / (float)weapons.Count;
            float effectiveExp = currentExp;

            auxCurrentLevel = -1;
            while (weaponValues.expRequiredForLevels.Length > ++auxCurrentLevel && weaponValues.expRequiredForLevels[auxCurrentLevel] < effectiveExp) ;

            //Debug.Log("The current level is " + loadOut.currentLevel);
            float upperExp = weaponValues.expRequiredForLevels[auxCurrentLevel];
            float lowerExp = effectiveExp;

            if (auxCurrentLevel != 0)
            {
                upperExp -= weaponValues.expRequiredForLevels[auxCurrentLevel - 1];
                lowerExp -= weaponValues.expRequiredForLevels[auxCurrentLevel - 1];
            }

            if (effectiveExp == weaponValues.expRequiredForLevels[weaponValues.expRequiredForLevels.Length - 1])
            {
                auxCurrentLevel = weaponValues.expRequiredForLevels.Length;
            }

            nextLevelProgress = lowerExp / upperExp;
        }

        auxCurrentLevel = Mathf.Min(auxCurrentLevel, weaponValues.particleSystems.Length - 1);

        if((forceUpdate || (auxCurrentLevel >= 0 && currentLevel != auxCurrentLevel)) && (null != currentParticleSystem && weaponValues.particleSystems.Length > 0 && null != weaponValues.particleSystems[currentLevel]))
        {
            //Debug.Log("Name of the object on rn is " + gameObject.name);
           // Debug.Log("The current level is " + currentLevel + " and the current weapon count level is " + weaponValues.particleSystems.Length);
            currentLevel = auxCurrentLevel;
            ParticleSingleDamage.SetNewParticleSingleDamage(weaponValues.particleSystems[currentLevel], ref currentParticleSystem);
            fireReleased = true;
        }
    }

    public bool IsMaxLevel()
    {
        return currentLevel > weaponValues.expRequiredForLevels.Length;
    }

    public WeanponType GetWeaponType()
    {
        return weaponValues.weaponType;
    }

    public WeaponValues GetWeaponValues()
    {
        return weaponValues;
    }

    public void SetWeaponValues(WeaponValues values)
    {
        weaponValues = values;
        UpdateFireLevel(true);
    }
}

public enum WeanponType
{
    EXPERIENCE,
    CHARGE,
    AMMO,
    PASSIVE
}

[System.Serializable]
public class WeaponValues
{
    [SerializeField]
    public Sound[] levelFireSounds;

    [SerializeField]
    public Sound[] levelReleaseFireSounds;

    [SerializeField]
    public float[] expRequiredForLevels;

    [SerializeField]
    public WeanponType weaponType;

    [SerializeField]
    public ParticleSingleDamage[] particleSystems;
}
