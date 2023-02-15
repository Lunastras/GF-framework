using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleHit : ParticleDamageSource
{
    [SerializeField]
    private float m_damage = 10;
    [SerializeField]
    private bool m_canDamageSelf = false;

   
    public Transform Target { get; set; } = null;

    private void OnEnable()
    {
        m_statsCharacter = null;
        Target = null;
    }

    private void OnDisable()
    {
        m_statsCharacter = null;
    }

    protected override void InternalAwake()
    {
        m_statsCharacter = null == m_statsCharacter ? GetComponent<StatsCharacter>() : m_statsCharacter;
    }

    private void FixedUpdate()
    {
        if (Target)
        {
            transform.LookAt(Target);
            var mainModule = m_particleSystem.main;
        }
    }

    protected virtual bool HitTarget(StatsCharacter target, float damageMultiplier, ParticleCollisionEvent collisionEvent)
    {
        //  Debug.Log("GONNA DAMAJE IT " + target.name);
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(m_damage, damageMultiplier, m_statsCharacter, this);

        return true;
    }

    protected virtual bool HitNonDamageTarget(StatsCharacter target, ParticleCollisionEvent collisionEvent)
    {
        // target.Damage(damage, characterStats);

        return true;
    }

    protected virtual void HitCollision(GameObject other, ParticleCollisionEvent collisionEvent)
    {
        GameParticles.PlayParticleDust(collisionEvent.intersection, collisionEvent.normal);
    }

    protected override void CollisionBehaviour(GameObject other, ParticleCollisionEvent collisionEvent)
    {
        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = m_statsCharacter == collisionStats;
            float damageMultiplier = HostilityManager.DamageMultiplier(m_statsCharacter, collisionStats);

            //check if it can damage target
            if (!hitSelf || (hitSelf && m_canDamageSelf))
                HitTarget(collisionStats, damageMultiplier, collisionEvent);
            else
                HitNonDamageTarget(collisionStats, collisionEvent);
        }
        else
        {
            HitCollision(other, collisionEvent);
        }
    }

    public override void OnDamageDealt(float damage, StatsCharacter damagedCharacter) { }
    
    public override void OnCharacterKilled(StatsCharacter damagedCharacter) { }
}
