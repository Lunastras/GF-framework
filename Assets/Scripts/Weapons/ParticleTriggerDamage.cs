using System.Collections.Generic;
using UnityEngine;

public class ParticleTriggerDamage : WeaponParticle
{
    public bool m_canDamageSelf;

    //whether or not it reads the damage source and stats character from the red index of a particle's start colour. 
    //The value will be the index of the character found in ReuableParticleSystemManager's list
    public bool m_getDamageSourceFromColor = false;

    private List<ParticleSystem.Particle> m_particlesList = null;

    protected static readonly Vector3 DESTROY_POS = new(100000000, -100000000, 100000000);

    protected void Start()
    {
        m_particlesList = new(2);

        if (GetStatsCharacter() == null)
            SetStatsCharacter(GetComponent<StatsCharacter>());

        m_particleSystem = GetComponent<ParticleSystem>();

        ParticleTriggerDamageManager.AddParticleSystem(this);
    }

    void OnParticleTrigger()
    {
        int numEnter = m_particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, m_particlesList, out var colliderData);

        if (null != m_particlesList && m_particlesList.Count >= numEnter)
        {
            for (int i = 0; i < numEnter; ++i)
            {
                ParticleSystem.Particle particle = m_particlesList[i];
                int colliderCount = colliderData.GetColliderCount(i);
                for (int j = 0; j < colliderCount; ++j)
                {
                    GameObject hitObject = colliderData.GetCollider(i, j).gameObject;
                    CollisionBehaviour(ref particle, hitObject);
                    m_particlesList[i] = particle;
                }
            }
            m_particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, m_particlesList);
        }


    }

    protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject)
    {
        DamageSource damageSource = this;
        if (m_getDamageSourceFromColor)
        {
            Color32 index4Bytes = particle.startColor;

            int index =
              ((int)index4Bytes.r << 24)
            | ((int)index4Bytes.g << 16)
            | ((int)index4Bytes.b << 8)
            | ((int)index4Bytes.a);

            // Debug.Log("The fetched index is: " + index);

            damageSource = ReusableParticleSystemManager.GetWeapon(index);
        }

        StatsCharacter selfStats = damageSource.GetStatsCharacter();

        StatsCharacter collisionStats = hitObject.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = selfStats == collisionStats;
            float damageMultiplier = HostilityManager.DamageMultiplier(selfStats, collisionStats);

            //check if it can damage target
            if (!hitSelf || (hitSelf && m_canDamageSelf))
                HitTarget(ref particle, selfStats, collisionStats, damageMultiplier, damageSource);
        }
        else
        {
            HitCollision(ref particle, selfStats, hitObject, damageSource);
        }
    }

    protected virtual void HitTarget(ref ParticleSystem.Particle particle, StatsCharacter self, StatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(GetDamage(), damageMultiplier, self, damageSource);
        particle.remainingLifetime = 0;
    }

    protected virtual void HitCollision(ref ParticleSystem.Particle particle, StatsCharacter self, GameObject hitObject, DamageSource damageSource)
    {
    }

    private void OnDestroy()
    {
        ParticleTriggerDamageManager.RemoveParticleSystem(this);
    }
}