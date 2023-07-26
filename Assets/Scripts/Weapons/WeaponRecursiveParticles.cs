using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRecursiveParticles : WeaponParticleTrigger
{
    [SerializeField]
   protected ParticleSystem m_subEmitter = null;
    
   protected override void HitTarget(ref ParticleSystem.Particle particle, StatsCharacter self, StatsCharacter target, float damageMultiplier, DamageSource damageSource)
    {
        m_damageSound.Play(particle.position);
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(GetDamage() * damageMultiplier, self.NetworkObjectId, m_loadoutIndex, m_loadoutWeaponIndex);
        particle.remainingLifetime = 0;
        if(target.IsDead()) {
            Debug.Log("I killed someone yay");
        }
    }

    protected override void HitCollision(ref ParticleSystem.Particle particle, StatsCharacter self, GameObject hitObject, DamageSource damageSource)
    {
        m_collisionSound.Play(particle.position);
        Debug.Log("I hit a collision");
    }
}
