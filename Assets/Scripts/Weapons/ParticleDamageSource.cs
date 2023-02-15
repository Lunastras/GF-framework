using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParticleDamageSource : ParticleCollision
{
    [SerializeField]
    protected StatsCharacter m_statsCharacter;

    public void SetStatsCharacter(StatsCharacter value, bool setChildren = true)
    {
        m_statsCharacter = value;

        if(setChildren) {
            var subEmitters = m_particleSystem.subEmitters;
            int count = subEmitters.subEmittersCount;
            for(int i = 0; i < count; ++i) {
                ParticleSystem child = subEmitters.GetSubEmitterSystem(i);
                if(child) {
                    ParticleSingleHit system = child.GetComponent<ParticleSingleHit>();
                    if(system) {
                        system.SetStatsCharacter(value);
                    }
                } else {
                    subEmitters.RemoveSubEmitter(i);
                    --count;
                    --i;
                } 
            }
        } 
    }

    public StatsCharacter GetStatsCharacter()
    {
        return m_statsCharacter;
    }
}
