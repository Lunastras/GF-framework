using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public abstract class ParticleDieSpawnAction : MonoBehaviour
{
    protected ParticleSystem m_particleSystem;

    protected int m_lastCountParticles;
    // Start is called before the first frame update
    void Awake()
    {
        m_particleSystem = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        int currentNumParticles = m_particleSystem.particleCount;
        int diff = m_lastCountParticles - currentNumParticles;

        if (0 < diff)
            OnParticlesDie(diff);
        else if (0 > diff)
            OnParticlesSpawn(-diff);

        m_lastCountParticles = currentNumParticles;
    }

    protected abstract void OnParticlesDie(int countParticles);

    protected abstract void OnParticlesSpawn(int countParticles);
}
