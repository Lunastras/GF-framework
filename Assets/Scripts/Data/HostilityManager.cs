using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HostilityManager : MonoBehaviour
{
    protected List<StatsCharacter>[] m_instantiatedCharacters;
    private static HostilityManager m_hostilityManager = null;
    protected int m_numTypes = 0;

    [SerializeField]
    protected StructArray<bool>[] m_typesEnemiesWith;
    [SerializeField]
    protected StructArray<float>[] m_typesDamageMultiplier;

    private void Awake()
    {
        m_hostilityManager = this;

        foreach (int i in Enum.GetValues(typeof(CharacterTypes)))
            ++m_numTypes;

        m_instantiatedCharacters = new List<StatsCharacter>[m_numTypes];

        Array.Resize<StructArray<bool>>(ref m_typesEnemiesWith, m_numTypes);
        Array.Resize<StructArray<float>>(ref m_typesDamageMultiplier, m_numTypes);

        for (int i = 0; i < m_numTypes; ++i)
        {
            m_instantiatedCharacters[i] = new List<StatsCharacter>(1);
            Array.Resize<bool>(ref m_typesEnemiesWith[i].array, m_numTypes);
            Array.Resize<float>(ref m_typesDamageMultiplier[i].array, m_numTypes);
        }
    }

    public static float DamageMultiplier(CharacterTypes a, CharacterTypes b)
    {
        return m_hostilityManager.m_typesDamageMultiplier[(int)a].array[(int)b];
    }

    public static float DamageMultiplier(StatsCharacter a, StatsCharacter b)
    {
        return DamageMultiplier(a.GetCharacterType(), b.GetCharacterType());
    }

    public static bool EnemyWith(CharacterTypes a, CharacterTypes b)
    {
        return m_hostilityManager.m_typesEnemiesWith[(int)a].array[(int)b];
    }

    public static bool EnemyWith(StatsCharacter a, StatsCharacter b)
    {
        return EnemyWith(a.GetCharacterType(), b.GetCharacterType());
    }

    public static List<StatsCharacter> GetCharactersOfType(CharacterTypes type)
    {
        return m_hostilityManager.m_instantiatedCharacters[(int)type];
    }

    public static List<StatsCharacter> GetEnemiesList(StatsCharacter self, StatsCharacter enemy)
    {
        return GetEnemiesList(self.GetCharacterType(), enemy.GetCharacterType());
    }

    public static List<StatsCharacter> GetEnemiesList(CharacterTypes self, CharacterTypes enemy)
    {
        return GetEnemiesList((int)self, (int)enemy);
    }

    public static List<StatsCharacter> GetEnemiesList(int self, int enemy)
    {
        if (m_hostilityManager.m_typesEnemiesWith[self].array[enemy])
            return m_hostilityManager.m_instantiatedCharacters[enemy];
        else
            return null;
    }

    public static void AddCharacter(StatsCharacter character)
    {
        int characterIndex = character.GetCharacterIndex();

        if (character != null && characterIndex < 0)
        {
            ParticleTriggerDamageManager.AddCharacter(character);

            List<StatsCharacter> list = m_hostilityManager.m_instantiatedCharacters[(int)character.GetCharacterType()];
            int index = list.Count;
            list.Add(character);
            character.SetCharacterIndex(index);
        }
    }

    public static void RemoveCharacter(StatsCharacter character)
    {
        int characterIndex = character.GetCharacterIndex();
        List<StatsCharacter> list = m_hostilityManager.m_instantiatedCharacters[(int)character.GetCharacterType()];
        int lastCharacterIndex = list.Count - 1;

        if (-1 < characterIndex && characterIndex <= lastCharacterIndex)
        {
            list[characterIndex] = list[lastCharacterIndex];
            list[characterIndex].SetCharacterIndex(characterIndex);
            list.RemoveAt(lastCharacterIndex);
            character.SetCharacterIndex(-1);

            ParticleTriggerDamageManager.RemoveCharacter(character);
        }
    }

    public static void SetDamageMultiplier(CharacterTypes a, CharacterTypes b, float value, bool mutual = false)
    {
        m_hostilityManager.m_typesDamageMultiplier[(int)a].array[(int)b] = value;
        if (mutual) m_hostilityManager.m_typesDamageMultiplier[(int)b].array[(int)a] = value;

    }

    public static void SetEnemyWith(CharacterTypes a, CharacterTypes b, bool value, bool mutual = false)
    {
        m_hostilityManager.m_typesEnemiesWith[(int)a].array[(int)b] = value;
        if (mutual) m_hostilityManager.m_typesEnemiesWith[(int)b].array[(int)a] = value;
    }

    public static int GetNumTypes() { return m_hostilityManager.m_numTypes; }
}

//Used to visualise matrices in the editor
[System.Serializable]
public struct StructArray<T>
{
    [SerializeField]
    public T[] array;
}


