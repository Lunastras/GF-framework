using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class WeaponBasic : DamageSource
{
    [SerializeField]
    protected float m_damage;
    [SerializeField]
    protected Transform m_target;

    protected float m_damageMultiplier = 1;

    protected float m_speedMultiplier = 1;

    protected float m_fireRateMultiplier = 1;

    //the index of the weapon in the loadout it is part of
    protected int m_loadoutWeaponIndex = 0;

    //number of weapons in the loadout it is part of
    protected int m_loadoutCount = 1;

    public abstract void StopFiring();

    public abstract void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false);

    public abstract void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false);

    public virtual bool IsAlive(bool withChildren = true) { return false; }

    public virtual void SetTarget(Transform target) { m_target = target; }
    public virtual Transform GetTarget() { return m_target; }

    public virtual float GetDamage() { return m_damage * m_damageMultiplier; }

    public virtual float GetRawDamage() { return m_damage; }
    public virtual void SetRawDamage(float damage) { m_damage = damage; }
    public virtual float GetDamageMultiplier() { return m_damageMultiplier; }
    public virtual void SetDamageMultiplier(float multiplier) { m_damageMultiplier = multiplier; }

    public virtual float GetSpeedMultiplier() { return m_speedMultiplier; }
    public virtual void SetSpeedMultiplier(float multiplier) { m_speedMultiplier = multiplier; }

    public virtual float GetFireRateMultiplier() { return m_fireRateMultiplier; }
    public virtual void SetFireRateMultiplier(float multiplier) { m_fireRateMultiplier = multiplier; }

    public virtual int GetLoadoutWeaponIndex() { return m_loadoutWeaponIndex; }
    public virtual void SetLoadoutWeaponIndex(int index) { m_loadoutWeaponIndex = index; }

    public virtual int GetLoadoutCount() { return m_loadoutCount; }
    public virtual void SetLoadoutCount(int index) { m_loadoutCount = index; }



    // public WeanponType GetWeaponType();
}

/*
public enum WeanponType
{
    EXPERIENCE,
    CHARGE,
    AMMO,
    PASSIVE
}*/

