using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTriggerDamageManager : MonoBehaviour
{
    private static ParticleTriggerDamageManager Instance;

    private static List<WeaponParticle> ParticleWeapons = new(32);

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance) Destroy(Instance);
        Instance = this;
        ParticleWeapons.Clear();
        GfcManagerCharacters.OnCharacterAdded += AddCharacter;
        GfcManagerCharacters.OnCharacterRemoved += RemoveCharacter;
    }

    protected void OnDestroy()
    {
        GfcManagerCharacters.OnCharacterAdded -= AddCharacter;
        GfcManagerCharacters.OnCharacterRemoved -= RemoveCharacter;
        Instance = null;


        int weaponsCount = ParticleWeapons.Count;
        for (int i = 0; i < weaponsCount; ++i)
        {
            ParticleWeapons[i].SetParticleTriggerDamageIndex(-1);
        }

        ParticleWeapons.Clear();
    }

    public WeaponParticle GetWeaponParticle(int index)
    {
        return ParticleWeapons[index];
    }

    public static void AddCharacter(StatsCharacter obj)
    {
        int numParticleWeapons = ParticleWeapons.Count;
        for (int i = 0; i < numParticleWeapons; ++i)
        {
            ParticleWeapons[i].GetParticleSystem().trigger.AddCollider(obj);
        }
    }

    public static void RemoveCharacter(StatsCharacter characterToRemove)
    {
        int indexToRemove = characterToRemove.GetCharacterIndex(CharacterIndexType.CHARACTERS_ALL_LIST);
        int numParticleWeapons = ParticleWeapons.Count;
        int lastIndex = GfcManagerCharacters.GetAllCharactersCount() - 1;
        StatsCharacter lastCharacter = GfcManagerCharacters.GetAllCharacters()[lastIndex];

        ParticleSystem ps;
        for (int i = 0; i < numParticleWeapons; ++i)
        {
            ps = ParticleWeapons[i].GetParticleSystem();
            ps.trigger.SetCollider(indexToRemove, lastCharacter);
            ps.trigger.RemoveCollider(lastIndex);

            // if (ps.trigger.colliderCount != lastIndex)
            //{
            //Debug.Log("FUCK THESE VALUES ARE DIFFERENT, i have " + ps.trigger.colliderCount + " they have " + lastIndex);
            // }
        }
    }


    public static int AddParticleSystem(WeaponParticle weapon)
    {
        int index = ParticleWeapons.Count;
        ParticleWeapons.Add(weapon);
        weapon.SetParticleTriggerDamageIndex(index);

        ParticleSystem ps = weapon.GetParticleSystem();
        int countColliders = ps.trigger.colliderCount;
        while (0 <= --countColliders) //remove previous colliders
            ps.trigger.RemoveCollider(countColliders);

        var characters = GfcManagerCharacters.GetAllCharacters();
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
            int lastIndex = ParticleWeapons.Count - 1;
            ParticleWeapons[indexToRemove] = ParticleWeapons[lastIndex];
            ParticleWeapons[indexToRemove].SetParticleTriggerDamageIndex(indexToRemove);
            ParticleWeapons.RemoveAt(lastIndex);
            psToRemove.SetParticleTriggerDamageIndex(-1);
        }
    }
}
