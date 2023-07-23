using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
*This class exists in order to prevent multiple weapons from making sounds when fired. This is only an issue when a loadout has around 8+ weapons
*and they all spam sounds when fired, which can be jaring to players. This class only plays sounds if the weapon index of the weapon is lower or equal to m_highestIndexAllowed;
*/
public class WeaponParticleDieSpawnSound : ParticleDieSpawnSound
{
    [SerializeField]
    protected WeaponGeneric m_weapon;

    [SerializeField]
    protected int m_highestIndexAllowed = 8;

    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
        if (null == m_weapon) m_weapon = GetComponent<WeaponGeneric>();
    }

    protected override void OnParticlesDie(int particleCount)
    {
        if (null == m_weapon || m_weapon.GetLoadoutWeaponIndex() <= m_highestIndexAllowed)
            m_deathSound.Play(m_audioSource);
    }

    protected override void OnParticlesSpawn(int particleCount)
    {
        if (null == m_weapon || m_weapon.GetLoadoutWeaponIndex() <= m_highestIndexAllowed)
            m_spawnSound.Play(m_audioSource);
    }
}
