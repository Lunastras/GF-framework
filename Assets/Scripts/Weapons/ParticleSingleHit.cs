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

    // public ParticleSingleHitSystem masterSystem { get; set; } 

    public Transform Target { get; set; } = null;

    // public bool destroyWhenFinished { get; set; } = false;

    // private static Dictionary<ParticleSingleDamageData, HashSet<ParticleSingleHit>> releasedFireSources;
    public void Play()
    {
        m_particleSystem.Play(true);
    }

    private void OnEnable()
    {
        m_statsCharacter = null;
        Target = null;
    }

    private void OnDisable()
    {
        m_statsCharacter = null;
    }

    private void OnDestroy()
    {
        // if (releasedFireSources.ContainsKey(damageData))
        // releasedFireSources[damageData].Remove(this);
    }

    protected override void InternalAwake()
    {
        m_statsCharacter = null == m_statsCharacter ? GetComponent<StatsCharacter>() : m_statsCharacter;
        //releasedFireSources = null == releasedFireSources ? new Dictionary<ParticleSingleDamageData, HashSet<ParticleSingleHit>>(23) : releasedFireSources;
    }

    private void FixedUpdate()
    {
        /*
        if (destroyWhenFinished && !particleSystem.IsAlive(true))
        {
            ParticleSingleHit psd = this;
            DestroyFiringSource(ref psd);
        }
        */

        if (null == Target)
            return;

        transform.LookAt(Target);
    }

    public void Stop()
    {
        m_particleSystem.Stop(true);
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
        DustParticles.PlaySystem(collisionEvent.intersection, collisionEvent.normal);
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
