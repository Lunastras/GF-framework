using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ParticleDieSpawnSound : ParticleDieSpawnAction
{
    [SerializeField]
    protected GfSound m_spawnSound = null;
    [SerializeField]
    protected GfSound m_deathSound = null;

    protected AudioSource m_audioSource;

    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_deathSound.LoadAudioClip();
        m_spawnSound.LoadAudioClip();
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
