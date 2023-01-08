using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HostilityManager : MonoBehaviour
{
    [System.Serializable]
    protected struct BoolArray
    {
        [SerializeField]
        public bool[] boolArray;
    }

    protected List<StatsCharacter>[] m_instantiatedCharacters;
    private static HostilityManager hostilityManager = null;
    protected int m_numTypes = 0;

    [SerializeField]
    protected BoolArray[] m_enemiesWith;
    [SerializeField]
    protected BoolArray[] m_canDamage;

    private void Awake()
    {
        hostilityManager = this;

        foreach (int i in Enum.GetValues(typeof(CharacterTypes)))
            ++m_numTypes;

        Debug.Log("I have types: " + m_numTypes);

        m_instantiatedCharacters = new List<StatsCharacter>[m_numTypes];

        CorrectSizeBoolArray(m_enemiesWith, m_numTypes);
        CorrectSizeBoolArray(m_canDamage, m_numTypes);

        for (int i = 0; i < m_numTypes; ++i)
        {
            m_instantiatedCharacters[i] = new List<StatsCharacter>(1);

            for (int j = 0; j < m_numTypes; ++j)
                CorrectSizeBool(m_enemiesWith[i].boolArray, m_numTypes);
        }
    }

    private static void CorrectSizeBoolArray(BoolArray[] array, int size)
    {
        if (null == array)
        {
            array = new BoolArray[size];
        }
        else if (array.Length != size)
        {
            BoolArray[] newArray = new BoolArray[size];
            Array.Copy(array, newArray, System.Math.Min(size, array.Length));
            array = newArray;
        }
    }

    private static void CorrectSizeBool(bool[] array, int size)
    {
        if (null == array)
        {
            array = new bool[size];
        }
        else if (array.Length != size)
        {
            bool[] newArray = new bool[size];
            Array.Copy(array, newArray, System.Math.Min(size, array.Length));
            array = newArray;
        }
    }

    public static bool CanDamage(CharacterTypes a, CharacterTypes b)
    {
        return hostilityManager.m_canDamage[(int)a].boolArray[(int)b];
    }

    public static bool CanDamage(StatsCharacter a, StatsCharacter b)
    {
        return CanDamage(a.GetCharacterType(), b.GetCharacterType());
    }

    public static bool EnemyWith(CharacterTypes a, CharacterTypes b)
    {
        return hostilityManager.m_enemiesWith[(int)a].boolArray[(int)b];
    }

    public static bool EnemyWith(StatsCharacter a, StatsCharacter b)
    {
        return EnemyWith(a.GetCharacterType(), b.GetCharacterType());
    }

    public static List<StatsCharacter> GetCharactersOfType(CharacterTypes type)
    {
        return hostilityManager.m_instantiatedCharacters[(int)type];
    }

    public static List<StatsCharacter> GetEnemiesList(StatsCharacter self, StatsCharacter enemy)
    {
        return GetEnemiesList(self.GetCharacterType(), enemy.GetCharacterType());
    }

    public static List<StatsCharacter> GetEnemiesList(CharacterTypes self, CharacterTypes enemy)
    {
        int enemyIndex = (int)enemy;
        if (hostilityManager.m_enemiesWith[(int)self].boolArray[enemyIndex])
            return hostilityManager.m_instantiatedCharacters[enemyIndex];
        else
            return null;
    }

    public static void AddCharacter(StatsCharacter character)
    {
        int characterIndex = character.GetCharacterIndex();

        if (character != null && characterIndex < 0)
        {
            List<StatsCharacter> list = hostilityManager.m_instantiatedCharacters[(int)character.GetCharacterType()];
            int index = list.Capacity;
            list.Add(character);
            character.SetCharacterIndex(index);
        }
    }

    public static void RemoveCharacter(StatsCharacter character)
    {
        int characterIndex = character.GetCharacterIndex();
        List<StatsCharacter> list = hostilityManager.m_instantiatedCharacters[(int)character.GetCharacterType()];
        int lastCharacterIndex = list.Capacity - 1;

        if (-1 < characterIndex && characterIndex <= lastCharacterIndex)
        {
            StatsCharacter lastCharacter = list[lastCharacterIndex];
            lastCharacter.SetCharacterIndex(characterIndex);
            list[characterIndex] = lastCharacter;
            list.RemoveAt(lastCharacterIndex);
        }
    }

    public static void SetCanDamage(CharacterTypes a, CharacterTypes b, bool value, bool mutual = false)
    {
        hostilityManager.m_canDamage[(int)a].boolArray[(int)b] = value;
        if (mutual) hostilityManager.m_canDamage[(int)b].boolArray[(int)a] = value;

    }

    public static void SetEnemyWith(CharacterTypes a, CharacterTypes b, bool value, bool mutual = false)
    {
        hostilityManager.m_enemiesWith[(int)a].boolArray[(int)b] = value;
        if (mutual) hostilityManager.m_enemiesWith[(int)b].boolArray[(int)a] = value;
    }
}


