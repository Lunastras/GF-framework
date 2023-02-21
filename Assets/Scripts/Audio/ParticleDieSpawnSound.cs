using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ParticleDieSpawnSound : ParticleDieSpawnAction
{
    [SerializeField]
    private Sound m_spawnSound;
    [SerializeField]
    private Sound m_deathSound;

    private AudioSource m_audioSource;

    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
    }

    protected override void OnParticlesDie(int particleCount)
    {
        m_deathSound.Play(m_audioSource);
    }

    protected override void OnParticlesSpawn(int particleCount)
    {
        m_spawnSound.Play(m_audioSource);
    }
}
