using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTriggerDamageManager : MonoBehaviour
{
    private static ParticleTriggerDamageManager m_instance;

    private static List<WeaponParticle> m_particleWeapons;

    // Start is called before the first frame update
    void Awake()
    {
        if (m_instance) Destroy(m_instance);
        m_instance = this;
        m_particleWeapons = new(8);
        HostilityManager.OnCharacterAdded += AddCharacter;
        HostilityManager.OnCharacterRemoved += RemoveCharacter;
    }

    public WeaponParticle GetWeaponParticle(int index)
    {
        return m_particleWeapons[index];
    }

    public static void AddCharacter(StatsCharacter obj)
    {
        int numParticleWeapons = m_particleWeapons.Count;
        for (int i = 0; i < numParticleWeapons; ++i)
        {
            m_particleWeapons[i].GetParticleSystem().trigger.AddCollider(obj);
        }
    }

    public static void RemoveCharacter(StatsCharacter characterToRemove)
    {
        int indexToRemove = characterToRemove.GetCharacterIndex(CharacterIndexType.CHARACTERS_ALL_LIST);
        int numParticleWeapons = m_particleWeapons.Count;
        int lastIndex = HostilityManager.GetAllCharactersCount() - 1;

        ParticleSystem ps;
        for (int i = 0; i < numParticleWeapons; ++i)
        {
            ps = m_particleWeapons[i].GetParticleSystem();
            ps.trigger.SetCollider(indexToRemove, ps.trigger.GetCollider(lastIndex));
            ps.trigger.RemoveCollider(lastIndex);
        }
    }


    public static int AddParticleSystem(WeaponParticle weapon)
    {
        int index = m_particleWeapons.Count;
        m_particleWeapons.Add(weapon);
        weapon.SetParticleTriggerDamageIndex(index);

        ParticleSystem ps = weapon.GetParticleSystem();
        int countColliders = ps.trigger.colliderCount;
        while (0 <= --countColliders) //remove previous colliders
            ps.trigger.RemoveCollider(countColliders);

        var characters = HostilityManager.GetAllCharacters();
        countColliders = characters.Count;

        for (int i = 0; i < countColliders; ++i)
            ps.trigger.AddCollider(characters[i]);

        return index;
    }

    public static void RemoveParticleSystem(WeaponParticle psToRemove)
    {
        int indexToRemove = psToRemove.GetParticleTriggerDamageIndex();
        if (0 <= indexToRemove)
        {
            int lastIndex = m_particleWeapons.Count - 1;
            m_particleWeapons[indexToRemove] = m_particleWeapons[lastIndex];
            m_particleWeapons[indexToRemove].SetParticleTriggerDamageIndex(indexToRemove);
            m_particleWeapons.RemoveAt(lastIndex);
            psToRemove.SetParticleTriggerDamageIndex(-1);
        }
    }
}
