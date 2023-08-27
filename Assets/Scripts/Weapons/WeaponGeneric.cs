using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using System.Linq;

public abstract class WeaponGeneric : DamageSource
{
    [SerializeField]
    protected bool m_automatic = true;

    [SerializeField]
    protected DamageType m_damageType = DamageType.NORMAL;

    [SerializeField]
    protected float m_damage;

    [SerializeField]
    protected GfMovementGeneric m_movementParent;

    [SerializeField]
    protected Transform m_target;

    protected bool m_isFiring = false;

    protected float[] m_weaponMultipliers = Enumerable.Repeat(1f, (int)WeaponMultiplierTypes.COUNT_TYPES).ToArray();

    //the index of the weapon in the loadout it is part of
    protected int m_loadoutWeaponIndex = 0;

    protected int m_loadoutIndex = 0;

    //number of weapons in the loadout it is part of
    protected int m_loadoutCount = 1;

    public bool DestroyWhenDone { get; set; } = false;

    public bool DisableWhenDone { get; set; } = false;

    public abstract void StopFiring(bool killBullets);

    public abstract void Fire(FireHit hit = default, FireType fireType = FireType.MAIN, bool forceFire = false);

    public abstract void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN);

    public virtual bool IsAlive() { return false; }

    public virtual void SetTarget(Transform target) { m_target = target; }
    public virtual Transform GetTarget() { return m_target; }

    public virtual float GetDamage() { return m_damage * m_weaponMultipliers[(int)WeaponMultiplierTypes.DAMAGE]; }

    public virtual float GetRawDamage() { return m_damage; }
    public virtual void SetRawDamage(float damage) { m_damage = damage; }

    public virtual bool SetMultiplier(WeaponMultiplierTypes multiplierType, float multiplier)
    {
        bool changedValue = multiplier != m_weaponMultipliers[(int)multiplierType];
        if (changedValue)
        {
            foreach (Transform item in transform)
            {
                WeaponGeneric subSystem = item.GetComponent<WeaponGeneric>();
                subSystem?.SetMultiplier(multiplierType, multiplier);
            }

            m_weaponMultipliers[(int)multiplierType] = multiplier;
        }

        return changedValue;
    }

    public virtual float GetMultiplier(WeaponMultiplierTypes multiplierType) { return m_weaponMultipliers[(int)multiplierType]; }

    public virtual void EraseAllBullets(StatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius) { }

    public virtual int GetLoadoutIndex() { return m_loadoutIndex; }
    public virtual void SetLoadoutIndex(int index) { m_loadoutIndex = index; }

    public virtual int GetLoadoutWeaponIndex() { return m_loadoutWeaponIndex; }
    public virtual void SetLoadoutWeaponIndex(int index) { m_loadoutWeaponIndex = index; }

    public virtual int GetLoadoutCount() { return m_loadoutCount; }
    public virtual void SetLoadoutCount(int index) { m_loadoutCount = index; }

    public virtual GfMovementGeneric GetMovementParent() { return m_movementParent; }
    public virtual void SetMovementParent(GfMovementGeneric parent) { m_movementParent = parent; }

    public virtual void SetSeed(uint seed) { }

    public virtual uint GetSeed() { return 0; }

    //Called when the loadout changes this weapon to something else
    public virtual void WasSwitchedOff() { }

    //called when the loadout changes a weapon to this one
    public virtual void WasSwitchedOn() { }

    public virtual void Initialize() { }

    public virtual bool GetIsAutomatic() { return m_automatic; }

    public virtual void SetIsAutomatic(bool isAutomatic) { m_automatic = isAutomatic; }

    public virtual bool IsFiring()
    {
        return m_isFiring;
    }

    /**
    *   Adds exp points to the weapon and sets the level accordingly
    *   @param points The ammount of points to be added
    *   @return The current number of points the weapon has
    */
    public virtual float AddPoints(WeaponPointsTypes type, float points) { return 0; }

    public virtual float SetPoints(WeaponPointsTypes type, float points) { return 0; }

    public virtual float GetPoints(WeaponPointsTypes type) { return 0; }
}

public enum WeaponPointsTypes
{
    EXPERIENCE,
    CHARGE,
    NUMBER_OF_TYPES
}


