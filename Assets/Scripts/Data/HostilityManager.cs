using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HostilityManager : MonoBehaviour
{
    [System.Serializable]
    private class CharacterTypesAssociation
    {
        public CharacterTypes parent;
        public CharacterTypes[] associations;

        public bool mutual = false;
    }

    [SerializeField]
    private CharacterTypesAssociation[] associationsEnemiesWith;

    [SerializeField]
    private CharacterTypesAssociation[] associationsCanDamage;

    private Dictionary<CharacterTypes, HashSet<StatsCharacter>> instantiatedCharacters;

    //Contains the list of hostile types of the given character type.
    private Dictionary<CharacterTypes, HashSet<CharacterTypes>> enemiesWithDict { get; set; }
    private Dictionary<CharacterTypes, HashSet<CharacterTypes>> canDamageDict { get; set; }

    private Dictionary<CharacterTypes, Dictionary<CharacterTypes, HashSet<StatsCharacter>>> enemiesListDict { get; set; }
    public static HostilityManager hostilityManager;

    private void Awake()
    {
        hostilityManager = this;

        instantiatedCharacters = new();
        enemiesWithDict = new();
        canDamageDict = new();
        enemiesListDict = new();

        foreach (int i in Enum.GetValues(typeof(CharacterTypes)))
        {
            CharacterTypes currentType = (CharacterTypes)i;
            enemiesWithDict.Add(currentType, new HashSet<CharacterTypes>());
            canDamageDict.Add(currentType, new HashSet<CharacterTypes>());
            instantiatedCharacters.Add(currentType, new HashSet<StatsCharacter>());

            Dictionary<CharacterTypes, HashSet<StatsCharacter>> enemiesList = new();

            foreach (CharacterTypes enemyType in enemiesWithDict[currentType])
            {
                enemiesList.Add(enemyType, instantiatedCharacters[enemyType]);
            }
        }

        foreach (CharacterTypesAssociation association in associationsEnemiesWith)
        {
            AddEnemiesWithAssociation(association.parent, association.associations, association.mutual);
        }

        foreach (CharacterTypesAssociation association in associationsCanDamage)
        {
            AddDamageAssociation(association.parent, association.associations, association.mutual);
        }

        UpdateEnemyListDictionary();

        //deinitialize the values
        associationsCanDamage = associationsEnemiesWith = null;
    }

    private static void UpdateEnemyListDictionary()
    {
        foreach (int i in Enum.GetValues(typeof(CharacterTypes)))
        {
            CharacterTypes currentType = (CharacterTypes)i;

            Dictionary<CharacterTypes, HashSet<StatsCharacter>> enemiesList = new();

            foreach (CharacterTypes enemyType in hostilityManager.enemiesWithDict[currentType])
            {
                enemiesList.Add(enemyType, hostilityManager.instantiatedCharacters[enemyType]);
            }

            if (!hostilityManager.enemiesListDict.ContainsKey(currentType))
            {
                hostilityManager.enemiesListDict.Add(currentType, enemiesList);
            }
            else
            {
                hostilityManager.enemiesListDict[currentType] = enemiesList;
            }
        }
    }

    public static bool CanDamage(CharacterTypes a, CharacterTypes b)
    {
       // Debug.Log("Asked if " + a + " can damage " + b);
        return hostilityManager.canDamageDict[a].Contains(b);
    }

    public static bool CanDamage(StatsCharacter a, StatsCharacter b)
    {       
        return CanDamage(a.GetCharacterType(), b.GetCharacterType());
    }

    public static Dictionary<CharacterTypes, HashSet<StatsCharacter>> GetEnemiesList(StatsCharacter a)
    {
        return GetEnemiesList(a.GetCharacterType());
    }

    public static Dictionary<CharacterTypes, HashSet<StatsCharacter>> GetEnemiesList(CharacterTypes a)
    {
        return hostilityManager.enemiesListDict[a];
    }

    public static void AddCharacter(StatsCharacter character)
    {
        if (character != null && null != hostilityManager)
            hostilityManager.instantiatedCharacters[character.GetCharacterType()].Add(character);
    }

    public static void RemoveCharacter(StatsCharacter character)
    {
        if (character != null && null != hostilityManager)
            hostilityManager.instantiatedCharacters[character.GetCharacterType()].Remove(character);
    }

    public static void AddEnemiesWithAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            hostilityManager.enemiesWithDict[parent].Add(charType);
            if (mutual)
            {
                hostilityManager.enemiesWithDict[charType].Add(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

    public static void AddDamageAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            hostilityManager.canDamageDict[parent].Add(charType);
            if (mutual)
            {
                hostilityManager.canDamageDict[charType].Add(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

    public static void RemoveDamageAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            hostilityManager.canDamageDict[parent].Remove(charType);
            if (mutual)
            {
                hostilityManager.canDamageDict[charType].Remove(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

    public static void RemoveEnemiesWithAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            hostilityManager.canDamageDict[parent].Remove(charType);
            if (mutual)
            {
                hostilityManager.enemiesWithDict[charType].Remove(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

}

internal class IntPairMap<T>
{

    private Dictionary<float, T> dictionary;

    public IntPairMap()
    {
        dictionary = new Dictionary<float, T>();
    }

    public void Add(int a, int b, T value)
    {
        dictionary.Add(Hash(a, b), value);
    }

    public T Get(int a, int b)
    {
        return dictionary[Hash(a, b)];
    }

    private float Hash(int a, int b)
    {
        //swap numbers
        if (a > b)
        {
            a += b;
            b = a - b;
            a -= b;
        }

        return ((a << 19) | (b << 7));
    }


}
