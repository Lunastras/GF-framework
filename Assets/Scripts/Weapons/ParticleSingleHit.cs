using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleHit : WeaponParticle
{
    [SerializeField]
    private bool m_canDamageSelf = false;
    [SerializeField]
    private Sound m_damageSound = null;

    [SerializeField]
    private Sound m_collisionSound = null;

    public static List<ParticleCollisionEvent> m_collisionEvents;

    protected void Awake()
    {
        if (GetStatsCharacter() == null)
            SetStatsCharacter(GetComponent<StatsCharacter>());

        m_collisionEvents = new(8);
        m_particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        //SetStatsCharacter(null);
        m_target = null;
    }

    private void OnParticleCollision(GameObject collision)
    {
        StatsCharacter selfStats = GetStatsCharacter();
        int collisionNum = m_particleSystem.GetCollisionEvents(collision, m_collisionEvents);
        StatsCharacter collisionStats = collision.GetComponent<StatsCharacter>();

        for (int i = 0; i < collisionNum; ++i)
        {
            ParticleCollisionEvent collisionEvent = m_collisionEvents[i];
            if (collisionStats != null)
            {
                bool hitSelf = selfStats == collisionStats;
                float damageMultiplier = HostilityManager.DamageMultiplier(selfStats, collisionStats);

                //check if it can damage target
                if (!hitSelf || (hitSelf && m_canDamageSelf))
                    HitTarget(collisionEvent, selfStats, collisionStats, damageMultiplier, this);
                else
                    HitNonDamageTarget(collisionEvent, selfStats, collisionStats, damageMultiplier, this);
            }
            else
            {
                HitCollision(collisionEvent, selfStats, collision);
            }
        }

    }

    private void OnDisable()
    {
        // SetStatsCharacter(null);
    }



    private void FixedUpdate()
    {
        if (m_target)
        {
            transform.LookAt(m_target);
        }
    }

    protected virtual bool HitTarget(ParticleCollisionEvent collisionEvent, StatsCharacter self, StatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        //  Debug.Log("GONNA DAMAJE IT " + target.name);
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        AudioManager.PlayAudio(m_damageSound, collisionEvent.intersection);
        target.Damage(m_damage, damageMultiplier, self, this);

        return true;
    }

    protected virtual bool HitNonDamageTarget(ParticleCollisionEvent collisionEvent, StatsCharacter self, StatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        // target.Damage(damage, characterStats);
        return true;
    }

    protected virtual void HitCollision(ParticleCollisionEvent collisionEvent, StatsCharacter self, GameObject other)
    {
        AudioManager.PlayAudio(m_collisionSound, collisionEvent.intersection);
        GameParticles.PlayParticleDust(collisionEvent.intersection, collisionEvent.normal);
    }

    public override bool IsAlive(bool withChildren = true)
    {
        return m_particleSystem.IsAlive(withChildren);
    }

    public GameObject GetGameObject() { return gameObject; }
}
