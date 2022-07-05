using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WeaponBasic : MonoBehaviour
{
    [SerializeField]
    private WeaponValues weaponValues;

    [SerializeField]
    private ParticleSystem[] particleSystems;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private Transform levelsTransform;

    protected ParticleSystem currentParticleSystem;

    public float currentExp { get; private set; } = 0;

    public int currentLevel { get; private set; } = 0;

    public float nextLevelProgress { get; private set; } = 0;

    public StatsCharacter statsCharacter { get; set; }

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

        currentParticleSystem = particleSystems[0];
        
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

        currentParticleSystem.transform.rotation = Quaternion.LookRotation(dirBullet);

        currentParticleSystem.Play();
    }

    protected virtual void InternalReleasedFire(RaycastHit hit, int currentLevel, bool hitAnObject)
    {
        currentParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

    public ParticleSystem GetParticleSystem(int index)
    {
        return particleSystems[index];
    }

    public ParticleSystem SetParticleSystem(ParticleSystem systemToCopy, int index)
    {
        CopyParticleSystem.CopyFrom(systemToCopy, particleSystems[index]);
        return particleSystems[index];
    }

    public int GetNumParticleSystems()
    {
        return particleSystems.Length;
    }

    public void IncreaseNumPartSystemsTo(int newSize)
    {
        if(particleSystems.Length < newSize)
        {
            Array.Resize(ref particleSystems, newSize);

            for (int i = particleSystems.Length; i < newSize; ++i)
            {
                GameObject newParticleSystem = new("level" + (i + 1));
                particleSystems[i] = newParticleSystem.AddComponent<ParticleSystem>();
                newParticleSystem.transform.SetParent(levelsTransform);
                newParticleSystem.transform.localPosition = Vector3.zero;
            }
        }     
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

    protected void UpdateFireLevel()
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

        auxCurrentLevel = Mathf.Min(auxCurrentLevel, particleSystems.Length - 1);

        if(auxCurrentLevel >= 0 && currentLevel != auxCurrentLevel)
        {
            particleSystems[currentLevel].gameObject.SetActive(false);
            particleSystems[auxCurrentLevel].gameObject.SetActive(true);
            currentLevel = auxCurrentLevel;

            currentParticleSystem = particleSystems[currentLevel];
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
public struct WeaponValues
{
    [SerializeField]
    public Sound[] levelFireSounds;

    [SerializeField]
    public Sound[] levelReleaseFireSounds;

    [SerializeField]
    public float[] expRequiredForLevels;

    [SerializeField]
    public WeanponType weaponType;
}
