using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class WeaponBasic : MonoBehaviour, DamageSource
{

    [SerializeField]
    protected StatsCharacter m_statsCharacter;

    public virtual void SetStatsCharacter(StatsCharacter value) {
        m_statsCharacter = value;
    }

    public virtual  StatsCharacter GetStatsCharacter() {
        return m_statsCharacter;
    }

    public abstract void StopFiring();

    public abstract void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false);

    public abstract void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false);

    public virtual bool IsAlive(bool withChildren = true) { return false; }

    public virtual void SetTarget(Transform target) {}
    public virtual Transform GetTarget() { return null; }

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

