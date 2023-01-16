using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.CompilerServices;

public class ParticleTrigerSingleDamage : ParticleTrigger
{
    [SerializeField]
    protected StatsCharacter m_statsCharacter;
    [SerializeField]
    protected float m_damage;
    [SerializeField]
    protected bool m_canDamageSelf;

    protected override void InternalAwake()
    {
        m_statsCharacter = null == m_statsCharacter ? GetComponent<StatsCharacter>() : m_statsCharacter;
    }

    override protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject)
    {
        StatsCharacter collisionStats = hitObject.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = m_statsCharacter == collisionStats;
            float damageMultiplier = HostilityManager.DamageMultiplier(m_statsCharacter, collisionStats);

            //check if it can damage target
            if (!hitSelf || (hitSelf && m_canDamageSelf))
                HitTarget(ref particle, collisionStats);
        }
        else
        {
            HitCollision(ref particle, hitObject);
        }
    }

    protected virtual void HitTarget(ref ParticleSystem.Particle particle, StatsCharacter target)
    {
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(m_damage, m_statsCharacter);
        particle.remainingLifetime = 0;
    }

    protected virtual void HitCollision(ref ParticleSystem.Particle particle, GameObject hitObject)
    {

    }
}
