﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public class GfgManagerCharacters : MonoBehaviour
{
    protected List<GfgStatsCharacter>[] m_instantiatedCharacters;
    protected static GfgManagerCharacters Instance = null;

    [SerializeField]
    protected StructArray<bool>[] m_typesEnemiesWith;
    [SerializeField]
    protected StructArray<float>[] m_typesDamageMultiplier;

    protected List<GfgStatsCharacter> m_allCharacters = new(32);

    protected NativeList<float3>[] m_characterPositions = new NativeList<float3>[NUM_CHARACTER_TYPES];

    protected NativeList<float3> m_allCharactersPositions;

    public static Action<GfgStatsCharacter> OnCharacterRemoved;
    public static Action<GfgStatsCharacter> OnCharacterAdded;

    private const int NUM_CHARACTER_TYPES = (int)CharacterTypes.NUM_CHARACTER_TYPES;

    private void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        m_instantiatedCharacters = new List<GfgStatsCharacter>[NUM_CHARACTER_TYPES];

        Array.Resize<StructArray<bool>>(ref m_typesEnemiesWith, NUM_CHARACTER_TYPES);
        Array.Resize<StructArray<float>>(ref m_typesDamageMultiplier, NUM_CHARACTER_TYPES);

        for (int i = 0; i < NUM_CHARACTER_TYPES; ++i)
        {
            m_instantiatedCharacters[i] = new List<GfgStatsCharacter>(4);
            Array.Resize<bool>(ref m_typesEnemiesWith[i].array, NUM_CHARACTER_TYPES);
            Array.Resize<float>(ref m_typesDamageMultiplier[i].array, NUM_CHARACTER_TYPES);
        }

        for (int i = 0; i < NUM_CHARACTER_TYPES; ++i)
        {
            m_characterPositions[i] = new();
        }

        m_allCharactersPositions = new();
    }
    private void FixedUpdate()
    {
        for (int i = 0; i < NUM_CHARACTER_TYPES; ++i)
        {
            if (m_characterPositions[i].IsCreated)
                CopyPositionsToNativeArray(m_instantiatedCharacters[i], m_characterPositions[i]);
        }

        if (m_allCharactersPositions.IsCreated)
            CopyPositionsToNativeArray(m_allCharacters, m_allCharactersPositions);
    }

    private static void CopyPositionsToNativeArray(List<GfgStatsCharacter> source, NativeList<float3> destination)
    {
        int countCharacters = source.Count;
        destination.ResizeUninitialized(countCharacters);
        for (int j = 0; j < countCharacters; ++j)
        {
            destination[j] = source[j].transform.position;
        }
    }

    public static NativeArray<float3>.ReadOnly GetCharacterPositions(CharacterTypes type)
    {
        int index = (int)type;
        if (!Instance.m_characterPositions[index].IsCreated)
        {
            Instance.m_characterPositions[index] = new(Instance.m_instantiatedCharacters[index].Count, Allocator.Persistent);
            CopyPositionsToNativeArray(Instance.m_instantiatedCharacters[index], Instance.m_characterPositions[index]);
        }

        return Instance.m_characterPositions[index].AsArray().AsReadOnly();
    }

    public static NativeArray<float3>.ReadOnly GetAllCharacterPositions()
    {
        if (!Instance.m_allCharactersPositions.IsCreated)
        {
            Instance.m_allCharactersPositions = new(Instance.m_allCharacters.Count, Allocator.Persistent);
            CopyPositionsToNativeArray(Instance.m_allCharacters, Instance.m_allCharactersPositions);
        }

        return Instance.m_allCharactersPositions.AsArray().AsReadOnly();
    }

    public static bool HasInstance() { return Instance; }

    public static float DamageMultiplier(CharacterTypes a, CharacterTypes b)
    {
        return Instance.m_typesDamageMultiplier[(int)a].array[(int)b];
    }

    public static float DamageMultiplier(GfgStatsCharacter a, GfgStatsCharacter b)
    {
        return DamageMultiplier(a.GetCharacterType(), b.GetCharacterType());
    }

    public static bool EnemyWith(CharacterTypes a, CharacterTypes b)
    {
        return Instance.m_typesEnemiesWith[(int)a].array[(int)b];
    }

    public static bool EnemyWith(GfgStatsCharacter a, GfgStatsCharacter b)
    {
        return EnemyWith(a.GetCharacterType(), b.GetCharacterType());
    }

    public static List<GfgStatsCharacter> GetCharactersOfType(CharacterTypes type)
    {
        return Instance.m_instantiatedCharacters[(int)type];
    }

    public static List<GfgStatsCharacter> GetEnemiesList(GfgStatsCharacter self, GfgStatsCharacter enemy)
    {
        return GetEnemiesList(self.GetCharacterType(), enemy.GetCharacterType());
    }

    public static List<GfgStatsCharacter> GetEnemiesList(CharacterTypes self, CharacterTypes enemy)
    {
        return GetEnemiesList((int)self, (int)enemy);
    }

    public static void EraseAllEnemyBullets(GfgStatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius)
    {
        for (int i = 0; i < (int)CharacterTypes.NUM_CHARACTER_TYPES; ++i)
        {
            if (GfgManagerCharacters.EnemyWith(characterResponsible.GetCharacterType(), (CharacterTypes)i))
            {
                GfgManagerCharacters.EraseAllBullets((CharacterTypes)i, characterResponsible, centerOfErase, speedFromCenter, eraseRadius);
            }
        }
    }

    public static void EraseAllBullets(CharacterTypes type, GfgStatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius)
    {
        var list = Instance.m_instantiatedCharacters[(int)type];
        for (int i = 0; i < list.Count; ++i)
        {
            list[i].EraseAllBullets(characterResponsible, centerOfErase, speedFromCenter, eraseRadius);
        }
    }

    public static List<GfgStatsCharacter> GetEnemiesList(int self, int enemy)
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

        for (int i = startingType; i < NUM_CHARACTER_TYPES; ++i)
        {
            DestroyAllCharacters((CharacterTypes)i);
        }
    }

    public static void DestroyAllCharacters(CharacterTypes characterType)
    {
        List<GfgStatsCharacter> charactersList = Instance.m_instantiatedCharacters[(int)characterType];
        int count = charactersList.Count;
        for (int i = 0; i < count; ++i)
            GfcPooling.Destroy(charactersList[0].gameObject);
    }

    public static void KillAllCharacters(bool killPlayers)
    {
        int startingType = 1;
        if (killPlayers) startingType = 0;

        for (int i = startingType; i < NUM_CHARACTER_TYPES; ++i)
        {
            KillAllCharacters((CharacterTypes)i);
        }
    }

    public static void KillAllCharacters(CharacterTypes characterType)
    {
        List<GfgStatsCharacter> charactersList = Instance.m_instantiatedCharacters[(int)characterType];
        int count = charactersList.Count;
        for (int i = 0; i < count; ++i)
            charactersList[0].Kill();
    }

    public static List<GfgStatsCharacter> GetAllCharacters() { return Instance.m_allCharacters; }

    public static int GetAllCharactersCount() { return Instance.m_allCharacters.Count; }

    public static void AddCharacter(GfgStatsCharacter character)
    {
        if (character.GetCharacterIndex(CharacterIndexType.CHARACTERS_TYPE_LIST) < 0)
        {
            List<GfgStatsCharacter> typeList = Instance.m_instantiatedCharacters[(int)character.GetCharacterType()];
            character.SetCharacterIndex(typeList.Count, CharacterIndexType.CHARACTERS_TYPE_LIST);
            character.SetCharacterIndex(Instance.m_allCharacters.Count, CharacterIndexType.CHARACTERS_ALL_LIST);

            typeList.Add(character);
            Instance.m_allCharacters.Add(character);

            OnCharacterAdded?.Invoke(character);
        }
    }

    protected void OnDestroy()
    {
        DestroyAllCharacters(true);
        Instance = null;
        for (int i = 0; i < NUM_CHARACTER_TYPES; ++i)
        {
            if (m_characterPositions[i].IsCreated)
                m_characterPositions[i].Dispose();
        }

        if (m_allCharactersPositions.IsCreated)
            m_allCharactersPositions.Dispose();
    }

    public static void RemoveCharacter(GfgStatsCharacter character)
    {
        int characterIndex = character.GetCharacterIndex(CharacterIndexType.CHARACTERS_TYPE_LIST);

        if (0 <= characterIndex) //make sure it is in the list
        {
            OnCharacterRemoved?.Invoke(character);

            List<GfgStatsCharacter> typeList = Instance.m_instantiatedCharacters[(int)character.GetCharacterType()];
            List<GfgStatsCharacter> allCharacters = Instance.m_allCharacters;

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

    public static int GetNumTypes() { return NUM_CHARACTER_TYPES; }
}

//Used to visualise matrices in the editor
[System.Serializable]
public struct StructArray<T>
{
    [SerializeField]
    public T[] array;
}


