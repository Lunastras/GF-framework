using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class WeaponGeneric : DamageSource
{
    [SerializeField]
    protected float m_damage;

    [SerializeField]
    protected GfMovementGeneric m_movementParent;

    [SerializeField]
    protected Transform m_target;

    protected bool m_isFiring = false;

    protected float m_damageMultiplier = 1;

    protected float m_speedMultiplier = 1;

    protected float m_fireRateMultiplier = 1;

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

    public virtual float GetDamage() { return m_damage * m_damageMultiplier; }

    public virtual float GetRawDamage() { return m_damage; }
    public virtual void SetRawDamage(float damage) { m_damage = damage; }
    public virtual float GetDamageMultiplier() { return m_damageMultiplier; }
    public virtual void SetDamageMultiplier(float multiplier)
    {
        foreach (Transform item in transform)
        {
            WeaponGeneric subSystem = item.GetComponent<WeaponGeneric>();
            subSystem?.SetDamageMultiplier(multiplier);
        }

        m_damageMultiplier = multiplier;
    }

    public virtual void EraseAllBullets(StatsCharacter characterResponsible) { }

    public virtual float GetSpeedMultiplier() { return m_speedMultiplier; }
    public virtual void SetSpeedMultiplier(float multiplier)
    {
        foreach (Transform item in transform)
        {
            WeaponGeneric subSystem = item.GetComponent<WeaponGeneric>();
            subSystem?.SetSpeedMultiplier(multiplier);
        }

        m_speedMultiplier = multiplier;
    }

    public virtual float GetFireRateMultiplier() { return m_fireRateMultiplier; }
    public virtual void SetFireRateMultiplier(float multiplier)
    {
        foreach (Transform item in transform)
        {
            WeaponGeneric subSystem = item.GetComponent<WeaponGeneric>();
            subSystem?.SetFireRateMultiplier(multiplier);
        }

        m_fireRateMultiplier = multiplier;
    }

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


