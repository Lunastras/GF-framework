using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class WeaponBasic : DamageSource
{
    [SerializeField]
    protected float m_damage;

    [SerializeField]
    protected GfMovementGeneric m_movementParent;

    [SerializeField]
    protected Transform m_target;

    protected bool m_isFiring = false;

    protected PriorityValue<float> m_damageMultiplier = new(1);

    protected PriorityValue<float> m_speedMultiplier = new(1);

    protected PriorityValue<float> m_fireRateMultiplier = new(1);

    //the index of the weapon in the loadout it is part of
    protected int m_loadoutWeaponIndex = 0;

    //number of weapons in the loadout it is part of
    protected int m_loadoutCount = 1;

    public bool DestroyWhenDone { get; set; } = false;

    public bool DisableWhenDone { get; set; } = false;

    public abstract void StopFiring();

    public abstract void Fire(FireHit hit = default, FireType fireType = FireType.MAIN, bool forceFire = false);

    public abstract void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN);

    public virtual bool IsAlive() { return false; }

    public virtual void SetTarget(Transform target) { m_target = target; }
    public virtual Transform GetTarget() { return m_target; }

    public virtual float GetDamage() { return m_damage * m_damageMultiplier; }

    public virtual float GetRawDamage() { return m_damage; }
    public virtual void SetRawDamage(float damage) { m_damage = damage; }
    public virtual float GetDamageMultiplier() { return m_damageMultiplier; }
    public virtual bool SetDamageMultiplier(float multiplier, uint priority = 0, bool overridePriority = false) { return m_damageMultiplier.SetValue(multiplier, priority, overridePriority); }

    public virtual float GetSpeedMultiplier() { return m_speedMultiplier; }
    public virtual bool SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false) { return m_speedMultiplier.SetValue(multiplier, priority, overridePriority); }

    public virtual float GetFireRateMultiplier() { return m_fireRateMultiplier; }
    public virtual bool SetFireRateMultiplier(float multiplier, uint priority = 0, bool overridePriority = false) { return m_fireRateMultiplier.SetValue(multiplier, priority, overridePriority); }

    public virtual int GetLoadoutWeaponIndex() { return m_loadoutWeaponIndex; }
    public virtual void SetLoadoutWeaponIndex(int index) { m_loadoutWeaponIndex = index; }

    public virtual int GetLoadoutCount() { return m_loadoutCount; }
    public virtual void SetLoadoutCount(int index) { m_loadoutCount = index; }

    public virtual GfMovementGeneric GetMovementParent() { return m_movementParent; }
    public virtual void SetMovementParent(GfMovementGeneric parent) { m_movementParent = parent; }

    //Called when the loadout changes this weapon to something else
    public virtual void WasSwitchedOff() { }

    //called when the loadout changes a weapon to this one
    public virtual void WasSwitchedOn() { }

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
}

/*
public enum WeanponType
{
    EXPERIENCE,
    CHARGE,
    AMMO,
    PASSIVE
}*/

