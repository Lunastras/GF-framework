using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleHit : ParticleCollision
{
    [SerializeField]
    private float m_damage = 10;
    [SerializeField]
    private bool m_canDamageSelf = false;

    [SerializeField]
    private StatsCharacter m_statsCharacter;
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
        target.Damage(m_damage, m_statsCharacter);

        return true;
    }

    protected virtual bool HitNonDamageTarget(StatsCharacter target, float damageMultiplier, ParticleCollisionEvent collisionEvent)
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
            {
                HitTarget(collisionStats, damageMultiplier, collisionEvent);
            }
            else
            {
                HitNonDamageTarget(collisionStats, damageMultiplier, collisionEvent);
            }
        }
        else
        {
            HitCollision(other, collisionEvent);
        }
    }

    public void SetStatsCharacter(StatsCharacter value)
    {
        m_statsCharacter = value;

        foreach (Transform child in transform)
        {
            child.GetComponent<ParticleSingleHit>().SetStatsCharacter(value);
        }
    }

    public StatsCharacter GetStatsCharacter()
    {
        return m_statsCharacter;
    }
}
