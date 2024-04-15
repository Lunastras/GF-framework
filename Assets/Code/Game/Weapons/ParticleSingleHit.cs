using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleHit : WeaponParticle
{
    [SerializeField]
    private bool m_canDamageSelf = false;
    [SerializeField]
    private GfcSound m_damageSound = null;

    [SerializeField]
    private GfcSound m_collisionSound = null;

    public static List<ParticleCollisionEvent> m_collisionEvents;

    protected new void Awake()
    {
        base.Awake();
        m_collisionEvents = new(8);
    }

    private void OnEnable()
    {
        //SetStatsCharacter(null);
        m_target = null;
    }

    private void OnParticleCollision(GameObject collision)
    {
        GfgStatsCharacter selfStats = GetGfgStatsCharacter();
        int collisionNum = m_particleSystem.GetCollisionEvents(collision, m_collisionEvents);
        GfgStatsCharacter collisionStats = collision.GetComponent<GfgStatsCharacter>();

        for (int i = 0; i < collisionNum; ++i)
        {
            ParticleCollisionEvent collisionEvent = m_collisionEvents[i];
            if (collisionStats != null)
            {
                bool hitSelf = selfStats == collisionStats;
                float damageMultiplier = GfgManagerCharacters.DamageMultiplier(selfStats, collisionStats);

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

    private void Update()
    {
        if (m_target)
        {
            transform.LookAt(m_target);
        }
    }

    protected virtual bool HitTarget(ParticleCollisionEvent collisionEvent, GfgStatsCharacter self, GfgStatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        //  Debug.Log("GONNA DAMAJE IT " + target.name);
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        GfcManagerAudio.PlayAudio(m_damageSound, collisionEvent.intersection);
        target.Damage(new(m_damage * damageMultiplier, collisionEvent.intersection, collisionEvent.normal, m_damageType, true, self.NetworkObjectId, m_loadoutIndex, m_loadoutWeaponIndex));

        return true;
    }

    protected virtual bool HitNonDamageTarget(ParticleCollisionEvent collisionEvent, GfgStatsCharacter self, GfgStatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        // target.Damage(damage, characterStats);
        return true;
    }

    protected virtual void HitCollision(ParticleCollisionEvent collisionEvent, GfgStatsCharacter self, GameObject other)
    {
        GfcManagerAudio.PlayAudio(m_collisionSound, collisionEvent.intersection);
        //GameParticles.PlayParticleDust(collisionEvent.intersection, collisionEvent.normal);
    }

    public GameObject GetGameObject() { return gameObject; }
}