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

    private void UpdateEnemyListDictionary()
    {
        foreach (int i in Enum.GetValues(typeof(CharacterTypes)))
        {
            CharacterTypes currentType = (CharacterTypes)i;

            Dictionary<CharacterTypes, HashSet<StatsCharacter>> enemiesList = new();

            foreach (CharacterTypes enemyType in enemiesWithDict[currentType])
            {
                enemiesList.Add(enemyType, instantiatedCharacters[enemyType]);
            }

            if (!enemiesListDict.ContainsKey(currentType))
            {
                enemiesListDict.Add(currentType, enemiesList);
            }
            else
            {
                enemiesListDict[currentType] = enemiesList;
            }
        }
    }

    public bool CanDamage(CharacterTypes a, CharacterTypes b)
    {
       // Debug.Log("Asked if " + a + " can damage " + b);
        return canDamageDict[a].Contains(b);
    }

    public bool CanDamage(StatsCharacter a, StatsCharacter b)
    {       
        return CanDamage(a.GetCharacterType(), b.GetCharacterType());
    }

    public Dictionary<CharacterTypes, HashSet<StatsCharacter>> GetEnemiesList(StatsCharacter a)
    {
        return GetEnemiesList(a.GetCharacterType());
    }

    public Dictionary<CharacterTypes, HashSet<StatsCharacter>> GetEnemiesList(CharacterTypes a)
    {
        return enemiesListDict[a];
    }

    public void AddCharacter(StatsCharacter character)
    {
        if (character != null)
            instantiatedCharacters[character.GetCharacterType()].Add(character);
    }

    public void RemoveCharacter(StatsCharacter character)
    {
        if (character != null)
            instantiatedCharacters[character.GetCharacterType()].Remove(character);
    }

    public void AddEnemiesWithAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            enemiesWithDict[parent].Add(charType);
            if (mutual)
            {
                enemiesWithDict[charType].Add(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

    public void AddDamageAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            canDamageDict[parent].Add(charType);
            if (mutual)
            {
                canDamageDict[charType].Add(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

    public void RemoveDamageAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            canDamageDict[parent].Remove(charType);
            if (mutual)
            {
                canDamageDict[charType].Remove(parent);
            }
        }

        UpdateEnemyListDictionary();
    }

    public void RemoveEnemiesWithAssociation(CharacterTypes parent, CharacterTypes[] associations, bool mutual = true)
    {
        foreach (CharacterTypes charType in associations)
        {
            canDamageDict[parent].Remove(charType);
            if (mutual)
            {
                enemiesWithDict[charType].Remove(parent);
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
