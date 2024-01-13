using System.Collections.Generic;
using UnityEngine;

public class WeaponParticleTrigger : WeaponParticle
{
    [SerializeField]
    protected GfSound m_damageSound = null;

    [SerializeField]
    protected GfSound m_collisionSound = null;
    [SerializeField]
    protected bool m_canDamageSelf;


    //whether or not it reads the damage source and stats character from the red index of a particle's start colour. 
    //The value will be the index of the character found in ReuableParticleSystemManager's list
    public bool m_getDamageSourceFromColor = false;

    private List<ParticleSystem.Particle> m_particlesList = new(16);

    protected static readonly Vector3 DESTROY_POS = new(100000000, -100000000, 100000000);

    protected new void Awake()
    {
        base.Awake();

        var triggerModule = m_particleSystem.trigger;
        triggerModule.enabled = true;
    }

    protected void Start()
    {
        ParticleTriggerDamageManager.AddParticleSystem(this);
        m_damageSound.LoadAudioClip();
        m_collisionSound.LoadAudioClip();
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
            //the index of the damage source is encoded in the 4 bytes values in the startColor of the particle
            Color32 index4Bytes = particle.startColor;

            //we bitshift the byte values to fetch back the index of the damage source
            int index =
              ((int)index4Bytes.r << 24)
            | ((int)index4Bytes.g << 16)
            | ((int)index4Bytes.b << 8)
            | ((int)index4Bytes.a);

            damageSource = ReusableParticleSystemManager.GetWeapon(index);
        }

        if (damageSource)
        {
            StatsCharacter selfStats = damageSource.GetStatsCharacter();
            StatsCharacter collisionStats = hitObject.GetComponent<StatsCharacter>();
            if (collisionStats != null)
            {
                bool hitSelf = selfStats == collisionStats;
                if ((null == selfStats))
                    Debug.LogWarning("ParticleTriggerDamageSelf: stats of self are null");

                float damageMultiplier = HostilityManager.DamageMultiplier(selfStats, collisionStats);
                Debug.Log("Damage multiplier is: " + damageMultiplier);

                //check if it can damage target
                if (!hitSelf || (hitSelf && m_canDamageSelf))
                    HitTarget(ref particle, selfStats, collisionStats, damageMultiplier, damageSource);
            }
            else
            {
                HitCollision(ref particle, selfStats, hitObject, damageSource);
            }
        }
    }

    protected virtual void HitTarget(ref ParticleSystem.Particle particle, StatsCharacter self, StatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        m_damageSound.Play(particle.position);
        Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(new(GetDamage() * damageMultiplier, particle.position, particle.velocity.normalized, m_damageType, true, self.NetworkObjectId, m_loadoutIndex, m_loadoutWeaponIndex));
        particle.remainingLifetime = 0;
    }

    protected virtual void HitCollision(ref ParticleSystem.Particle particle, StatsCharacter self, GameObject hitObject, DamageSource damageSource)
    {
        m_collisionSound.Play(particle.position);
    }

    private void OnDestroy()
    {
        ParticleTriggerDamageManager.RemoveParticleSystem(this);
    }
}
