using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTriggerDamageManager : MonoBehaviour
{
    private static ParticleTriggerDamageManager m_instance;

    private static List<WeaponParticle> m_particleWeapons;

    private static List<StatsCharacter> m_characters;

    // Start is called before the first frame update
    void Awake()
    {
        if (m_instance) Destroy(m_instance);
        m_instance = this;

        m_particleWeapons = new(8);
        m_characters = new(8);
    }

    public WeaponParticle GetWeaponParticle(int index)
    {
        return m_particleWeapons[index];
    }

    public static void AddCharacter(StatsCharacter obj)
    {
        int numParticleWeapons = m_particleWeapons.Count;

        if (-1 == obj.GetParticleTriggerDamageIndex()) // make sure they aren't in the list
        {
            m_characters.Add(obj);
            obj.SetParticleTriggerDamageIndex(m_characters.Count - 1);
            for (int i = 0; i < numParticleWeapons; ++i)
            {
                m_particleWeapons[i].GetParticleSystem().trigger.AddCollider(obj);
            }
        }
    }

    public static void RemoveCharacter(StatsCharacter obj)
    {
        RemoveCharacter(obj.GetParticleTriggerDamageIndex());
    }

    public static void RemoveCharacter(int index)
    {
        int numParticleWeapons = m_particleWeapons.Count;
        int numCharactersMinusOne = m_characters.Count - 1;

        if (-1 != index)
        {
            m_characters[index].SetParticleTriggerDamageIndex(-1);
            m_characters[index] = m_characters[numCharactersMinusOne];
            m_characters.RemoveAt(numCharactersMinusOne);
            if (index < numCharactersMinusOne) m_characters[index].SetParticleTriggerDamageIndex(index);

            ParticleSystem ps;
            for (int i = 0; i < numParticleWeapons; ++i)
            {
                ps = m_particleWeapons[i].GetParticleSystem();
                if (ps)
                {
                    ps.trigger.SetCollider(index, ps.trigger.GetCollider(numCharactersMinusOne));
                    ps.trigger.RemoveCollider(numCharactersMinusOne);
                }
            }
        }
    }


    public static int AddParticleSystem(WeaponParticle weapon)
    {
        m_particleWeapons.Add(weapon);
        int index = m_particleWeapons.Count;
        weapon.SetParticleTriggerDamageIndex(--index);

        ParticleSystem ps = weapon.GetParticleSystem();
        int countCollider = ps.trigger.colliderCount;
        while (0 <= --countCollider) ps.trigger.RemoveCollider(countCollider);

        countCollider = m_characters.Count;

        for (int i = 0; i < countCollider; ++i)
            ps.trigger.AddCollider(m_characters[i]);

        return index;
    }

    public static void RemoveParticleSystem(WeaponParticle ps)
    {
        RemoveParticleSystem(ps.GetParticleTriggerDamageIndex());
    }

    public static void RemoveParticleSystem(int index)
    {
        if (index != -1)
        {
            int count = m_particleWeapons.Count;
            --count;
            m_particleWeapons[index] = m_particleWeapons[count];
            m_particleWeapons.RemoveAt(count);
            if (index < count) m_particleWeapons[index].SetParticleTriggerDamageIndex(index);
        }
    }
}
