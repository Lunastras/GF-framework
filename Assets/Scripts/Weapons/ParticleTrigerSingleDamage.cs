using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.CompilerServices;

public class ParticleTrigerSingleDamage : ParticleTrigger
{
    [SerializeField]
    protected StatsCharacter statsCharacter;

    [SerializeField]
    ParticleSingleDamageStruct damageData;

    protected override void InternalAwake()
    {
        statsCharacter = null == statsCharacter ? GetComponent<StatsCharacter>() : statsCharacter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    override protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject)
    {
        StatsCharacter collisionStats = hitObject.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = statsCharacter == collisionStats;
            bool canDamageEnemy = HostilityManager.CanDamage(statsCharacter, collisionStats);

            //check if it can damage target
            if ((!hitSelf && canDamageEnemy) || (hitSelf && damageData.canDamageSelf))
            {
                HitTarget(ref particle, collisionStats);
            }
            else
            {
                HitNonDamageTarget(ref particle, collisionStats);
            }
        }
        else
        {
            HitCollision(ref particle, hitObject);
        }
    }

    protected virtual void HitTarget(ref ParticleSystem.Particle particle, StatsCharacter target)
    {
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(damageData.damage, statsCharacter);
        particle.remainingLifetime = 0;
    }

    protected virtual void HitNonDamageTarget(ref ParticleSystem.Particle particle, StatsCharacter target)
    {
        // target.Damage(damage, characterStats);
    }

    protected virtual void HitCollision(ref ParticleSystem.Particle particle, GameObject hitObject)
    {

    }
}

[System.Serializable]
public class ParticleSingleDamageStruct
{
    public ParticleSingleDamageStruct(float damage = 1, bool canDamageSelf = false)
    {
        this.damage = damage;
        this.canDamageSelf = canDamageSelf;
    }

    public float damage;
    public bool canDamageSelf;
}
