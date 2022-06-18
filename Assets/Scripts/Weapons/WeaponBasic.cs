using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBasic : MonoBehaviour
{
    [SerializeField]
    private WeaponValues weaponValues;

    [SerializeField]
    private AudioSource audioSource;

    public float currentExp { get; private set; } = 0;

    public int currentLevel { get; private set; } = 0;

    public float nextLevelProgress { get; private set; } = 0;

    public StatsCharacter statsCharacter { get; set; }

    private bool releasedFire;

    private float timeOfLastFiring = 0;

    private void Start()
    {
        weaponValues.Initialize();

        if (null == audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (null == audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    public bool canFire()
    {
        float currentTime = Time.time;
        float timeSinceLastFire = currentTime - timeOfLastFiring;
        float coolDownTime = GetFireRate();

        return timeSinceLastFire >= coolDownTime && (releasedFire || GetAutomatic());
    }

    protected virtual Sound GetFireSound()
    {
        return weaponValues.levelFireSounds.Length > 0 ? weaponValues.levelFireSounds[Mathf.Clamp(currentLevel, 0, weaponValues.levelFireSounds.Length - 1)] : null;
    }

    protected virtual Sound GetReleaseFireSound()
    {
        return weaponValues.levelReleaseFireSounds.Length > 0 ? weaponValues.levelReleaseFireSounds[Mathf.Clamp(currentLevel, 0, weaponValues.levelReleaseFireSounds.Length - 1)] : null;
    }

    protected virtual float GetFireRate()
    {
        return weaponValues.levelFireRates[Mathf.Clamp(currentLevel, 0, weaponValues.levelFireRates.Length - 1)];
    }

    protected virtual float GetMultiplier()
    {
        return weaponValues.levelSpeedMultiplier[Mathf.Clamp(currentLevel, 0, weaponValues.levelSpeedMultiplier.Length - 1)];
    }

    protected virtual bool GetAutomatic()
    {
        return weaponValues.levelAutomatic[Mathf.Clamp(currentLevel, 0, weaponValues.levelAutomatic.Length - 1)];
    }

    protected virtual GameObject GetBullet()
    {
        return weaponValues.levelBullets[Mathf.Clamp(currentLevel, 0, weaponValues.levelBullets.Length - 1)];
    }

    protected virtual float GetSpread()
    {
        return weaponValues.levelSpreadDegrees[Mathf.Clamp(currentLevel, 0, weaponValues.levelSpreadDegrees.Length - 1)];
    }

    protected virtual int GetNumBullets()
    {
        return weaponValues.levelNumberBullets[Mathf.Clamp(currentLevel, 0, weaponValues.levelNumberBullets.Length - 1)];
    }


    protected virtual void InternalFire(RaycastHit hit, bool hitAnObject)
    {
        Vector3 dirBullet = (hit.point - transform.position).normalized;

        float yOffset, xOffset;
        float angleOffset = GetSpread();
        int numBullets = GetNumBullets();

        for (int i = 0; i < numBullets; ++i)
        {
            Transform bullet = GfPolymorphism.Instantiate(GetBullet()).transform;

            bullet.position = transform.position + dirBullet * 0.4f;

            yOffset = GfRandom.Range(-1, 1) * angleOffset;
            xOffset = GfRandom.Range(-1, 1) * angleOffset;
            bullet.rotation = Quaternion.LookRotation(dirBullet);
            bullet.Rotate(xOffset, yOffset, 0);

            bullet.GetComponent<HitBoxGeneric>().characterStats = statsCharacter;
            bullet.GetComponent<BulletMovement>().multiplier = GetMultiplier();
        }
    }

    protected virtual void InternalReleasedFire(RaycastHit hit, int currentLevel, bool hitAnObject)
    {
        releasedFire = true;
        // Debug.Log("Released fire");
    }

    public virtual void Fire(RaycastHit hit, bool hitAnObject, bool forceFire = false)
    {
        if (forceFire || canFire())
        {
            if (weaponValues.levelFireSounds.Length > 0)
            {
                GetFireSound().Play(audioSource);
            }

            releasedFire = false;
            timeOfLastFiring = Time.time;

            InternalFire(hit, hitAnObject);
        }
    }

    public virtual void ReleasedFire(RaycastHit hit, bool hitAnObject)
    {
        releasedFire = true;
        if (weaponValues.levelReleaseFireSounds.Length > 0)
            GetReleaseFireSound().Play(audioSource);
        
        InternalReleasedFire(hit, currentLevel, hitAnObject);
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
        currentLevel = 0;

        if (weaponValues.expRequiredForLevels.Length > 0)
        {
            // float effectiveExp = currentExp / (float)weapons.Count;
            float effectiveExp = currentExp;

            currentLevel = -1;
            while (weaponValues.expRequiredForLevels.Length > ++currentLevel && weaponValues.expRequiredForLevels[currentLevel] < effectiveExp) ;

            //Debug.Log("The current level is " + loadOut.currentLevel);
            float upperExp = weaponValues.expRequiredForLevels[currentLevel];
            float lowerExp = effectiveExp;

            if (currentLevel != 0)
            {
                upperExp -= weaponValues.expRequiredForLevels[currentLevel - 1];
                lowerExp -= weaponValues.expRequiredForLevels[currentLevel - 1];
            }

            if (effectiveExp == weaponValues.expRequiredForLevels[weaponValues.expRequiredForLevels.Length - 1])
            {
                currentLevel = weaponValues.expRequiredForLevels.Length;
            }

            nextLevelProgress = lowerExp / upperExp;
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
    public float[] levelFireRates;
    [SerializeField]
    public float[] levelSpeedMultiplier;
    [SerializeField]
    public GameObject[] levelBullets;

    [SerializeField]
    public float[] levelSpreadDegrees;

    //the number of bullets fired per round
    [SerializeField]
    public int[] levelNumberBullets;

    [SerializeField]
    public bool[] levelAutomatic;

    [SerializeField]
    public Sound[] levelFireSounds;

    [SerializeField]
    public Sound[] levelReleaseFireSounds;

    [SerializeField]
    public float[] expRequiredForLevels;

    [SerializeField]
    public WeanponType weaponType;

    public void Initialize()
    {
        if (levelSpeedMultiplier.Length == 0)
        {
            levelSpeedMultiplier = new float[1];
            levelSpeedMultiplier[0] = 1;
        }

        if (levelAutomatic.Length == 0)
        {
            levelAutomatic = new bool[1];
            levelAutomatic[0] = true;
        }

        if (levelNumberBullets.Length == 0)
        {
            levelNumberBullets = new int[1];
            levelNumberBullets[0] = 1;
        }

        if (levelSpreadDegrees.Length == 0)
        {
            levelSpreadDegrees = new float[1];
            levelSpreadDegrees[0] = 0;
        }
    }
}
