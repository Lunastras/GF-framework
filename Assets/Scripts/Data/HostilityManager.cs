using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HostilityManager : MonoBehaviour
{
    protected List<StatsCharacter>[] m_instantiatedCharacters;
    protected static HostilityManager Instance = null;
    protected int m_numTypes = 0;

    [SerializeField]
    protected StructArray<bool>[] m_typesEnemiesWith;
    [SerializeField]
    protected StructArray<float>[] m_typesDamageMultiplier;

    protected List<StatsCharacter> m_allCharacters = new(32);

    public static Action<StatsCharacter> OnCharacterRemoved;
    public static Action<StatsCharacter> OnCharacterAdded;

    private void Awake()
    {
        if (Instance)
            Destroy(Instance);

        Instance = this;
        m_numTypes = Enum.GetValues(typeof(CharacterTypes)).Length;

        m_instantiatedCharacters = new List<StatsCharacter>[m_numTypes];

        Array.Resize<StructArray<bool>>(ref m_typesEnemiesWith, m_numTypes);
        Array.Resize<StructArray<float>>(ref m_typesDamageMultiplier, m_numTypes);

        for (int i = 0; i < m_numTypes; ++i)
        {
            m_instantiatedCharacters[i] = new List<StatsCharacter>(4);
            Array.Resize<bool>(ref m_typesEnemiesWith[i].array, m_numTypes);
            Array.Resize<float>(ref m_typesDamageMultiplier[i].array, m_numTypes);
        }
    }

    public static float DamageMultiplier(CharacterTypes a, CharacterTypes b)
    {
        return Instance.m_typesDamageMultiplier[(int)a].array[(int)b];
    }

    public static float DamageMultiplier(StatsCharacter a, StatsCharacter b)
    {
        return DamageMultiplier(a.GetCharacterType(), b.GetCharacterType());
    }

    public static bool EnemyWith(CharacterTypes a, CharacterTypes b)
    {
        return Instance.m_typesEnemiesWith[(int)a].array[(int)b];
    }

    public static bool EnemyWith(StatsCharacter a, StatsCharacter b)
    {
        return EnemyWith(a.GetCharacterType(), b.GetCharacterType());
    }

    public static List<StatsCharacter> GetCharactersOfType(CharacterTypes type)
    {
        return Instance.m_instantiatedCharacters[(int)type];
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
        if (Instance.m_typesEnemiesWith[self].array[enemy])
            return Instance.m_instantiatedCharacters[enemy];
        else
            return null;
    }

    public static void DestroyAllCharacters(bool destroyPlayers)
    {
        int startingType = 1;
        if (destroyPlayers) startingType = 0;

        for (int i = startingType; i < Instance.m_numTypes; ++i)
        {
            DestroyAllCharacters((CharacterTypes)i);
        }
    }

    public static void DestroyAllCharacters(CharacterTypes characterType)
    {
        List<StatsCharacter> charactersList = Instance.m_instantiatedCharacters[(int)characterType];
        int count = charactersList.Count;
        for (int i = 0; i < count; ++i)
            GfPooling.DestroyInsert(charactersList[0].gameObject);
    }

    public static void KillAllCharacters(bool killPlayers)
    {
        int startingType = 1;
        if (killPlayers) startingType = 0;

        for (int i = startingType; i < Instance.m_numTypes; ++i)
        {
            KillAllCharacters((CharacterTypes)i);
        }
    }

    public static void KillAllCharacters(CharacterTypes characterType)
    {
        List<StatsCharacter> charactersList = Instance.m_instantiatedCharacters[(int)characterType];
        int count = charactersList.Count;
        for (int i = 0; i < count; ++i)
            charactersList[0].Kill();
    }

    public static List<StatsCharacter> GetAllCharacters() { return Instance.m_allCharacters; }

    public static int GetAllCharactersCount() { return Instance.m_allCharacters.Count; }

    public static void AddCharacter(StatsCharacter character)
    {
        if (character.GetCharacterIndex(CharacterIndexType.CHARACTERS_TYPE_LIST) < 0)
        {
            List<StatsCharacter> typeList = Instance.m_instantiatedCharacters[(int)character.GetCharacterType()];
            character.SetCharacterIndex(typeList.Count, CharacterIndexType.CHARACTERS_TYPE_LIST);
            character.SetCharacterIndex(Instance.m_allCharacters.Count, CharacterIndexType.CHARACTERS_ALL_LIST);

            typeList.Add(character);
            Instance.m_allCharacters.Add(character);

            if (null != OnCharacterAdded) OnCharacterAdded(character);
        }
    }

    public static void RemoveCharacter(StatsCharacter character)
    {
        int characterIndex = character.GetCharacterIndex(CharacterIndexType.CHARACTERS_TYPE_LIST);

        if (0 <= characterIndex) //make sure it is in the list
        {
            if (null != OnCharacterRemoved) OnCharacterRemoved(character);

            List<StatsCharacter> typeList = Instance.m_instantiatedCharacters[(int)character.GetCharacterType()];
            List<StatsCharacter> allCharacters = Instance.m_allCharacters;

            int lastCharacterIndex = typeList.Count - 1;

            typeList[characterIndex] = typeList[lastCharacterIndex];
            typeList[characterIndex].SetCharacterIndex(characterIndex, CharacterIndexType.CHARACTERS_TYPE_LIST);
            typeList.RemoveAt(lastCharacterIndex);
            character.SetCharacterIndex(-1, CharacterIndexType.CHARACTERS_TYPE_LIST);

            int allCharactersIndex = character.GetCharacterIndex(CharacterIndexType.CHARACTERS_ALL_LIST);

            lastCharacterIndex = allCharacters.Count - 1;

            allCharacters[allCharactersIndex] = allCharacters[lastCharacterIndex];
            allCharacters[allCharactersIndex].SetCharacterIndex(allCharactersIndex, CharacterIndexType.CHARACTERS_ALL_LIST);
            allCharacters.RemoveAt(lastCharacterIndex);
            character.SetCharacterIndex(-1, CharacterIndexType.CHARACTERS_ALL_LIST);

            Debug.Log("enemies left in list: " + allCharacters.Count);
        }
    }

    public static void SetDamageMultiplier(CharacterTypes a, CharacterTypes b, float value, bool mutual = false)
    {
        Instance.m_typesDamageMultiplier[(int)a].array[(int)b] = value;
        if (mutual) Instance.m_typesDamageMultiplier[(int)b].array[(int)a] = value;

    }

    public static void SetEnemyWith(CharacterTypes a, CharacterTypes b, bool value, bool mutual = false)
    {
        Instance.m_typesEnemiesWith[(int)a].array[(int)b] = value;
        if (mutual) Instance.m_typesEnemiesWith[(int)b].array[(int)a] = value;
    }

    public static int GetNumTypes() { return Instance.m_numTypes; }
}

//Used to visualise matrices in the editor
[System.Serializable]
public struct StructArray<T>
{
    [SerializeField]
    public T[] array;
}


