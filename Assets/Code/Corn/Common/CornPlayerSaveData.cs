using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Codice.ThemeImages;
using System.Data;
using Unity.Collections;

[Serializable]
public class PlayerSaveData
{
    [SerializeField] private string m_name;

    public double SecondsPlayed;

    private double m_unixTimeOfCreation;

    public int MentalSanity;

    public float[] Consumables;

    public float[] Resources;

    public uint CurrentTime = FIRST_WAKEUP_TIME;

    public uint CurrentDay = 0;

    public int CurrentWakeUpTime = FIRST_WAKEUP_TIME;

    public int CurrentSleepTime = 22;

    private float m_originalMaxSumMoney = INITIAL_SUM_OF_MONEY;

    private const int INITIAL_SUM_OF_MONEY = 10000;

    private const int FIRST_WAKEUP_TIME = 8;

    private const float START_RESOURCE_VALUE = 0.5f;

    public int MaxMentalSanity = CornManagerEvents.DICE_ROLL_NUM_FACES;

    public PlayerSaveData(string aName, double aCurrentUnixTime)
    {
        m_name = aName;
        m_unixTimeOfCreation = aCurrentUnixTime;

        ValidateSaveFile();

        MentalSanity = MaxMentalSanity;
        Consumables[(int)PlayerConsumables.MONEY] = INITIAL_SUM_OF_MONEY;
    }

    public string GetName() { return m_name; }

    public double GetUnixTimeOfCreation() { return m_unixTimeOfCreation; }

    private bool ValidateArrayValues<T>(ref T[] aArray, int aCount, T aDefaultValue = default)
    {
        bool changedSomething = false;
        if (aArray == null)
        {
            aArray = new T[aCount];
            for (int i = 0; i < aCount; ++i)
                aArray[i] = aDefaultValue;
        }
        else if (aArray.Length != aCount)
        {
            T[] newArray = new T[aCount];
            for (int i = 0; i < aCount; ++i)
                newArray[i] = i < aArray.Length ? aArray[i] : aDefaultValue;

            aArray = newArray;
        }

        return changedSomething;
    }

    public uint ProgressTime(uint aHoursSpan)
    {
        CurrentTime += aHoursSpan;
        uint daysPassed = CurrentTime / 24;
        CurrentTime %= 24;
        CurrentDay += daysPassed;

        return daysPassed;
    }

    public bool ValidateSaveFile()
    {
        ValidateArrayValues(ref Resources, (int)PlayerResources.COUNT, START_RESOURCE_VALUE);
        ValidateArrayValues(ref Consumables, (int)PlayerConsumables.COUNT);

        MentalSanity.ClampSelf(0, MaxMentalSanity);

        //used to take into account any changes made to the initial sum of money
        Consumables[(int)PlayerConsumables.MONEY] = Mathf.Max(0, Consumables[(int)PlayerConsumables.MONEY] + INITIAL_SUM_OF_MONEY - m_originalMaxSumMoney);
        return true;
    }

    public void ApplyModifier(PlayerResources aType, float aValue)
    {
        Resources[(int)aType] += aValue;
        Resources[(int)aType].ClampSelf(0, 1);
    }

    public void ApplyModifier(PlayerConsumables aType, float aValue)
    {
        Consumables[(int)aType] += aValue;
        Consumables[(int)aType].ClampSelf(0, 1);
    }

    public void ApplyModifier(PlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { ApplyModifier(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value)); }
    public void ApplyModifier(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { ApplyModifier(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value)); }

    public void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerResourcesModifier> { if (someModifiers != null) foreach (PlayerResourcesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
    public void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { if (someModifiers != null) foreach (PlayerConsumablesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
}

[Serializable]
public enum PlayerConsumables
{
    MONEY,
    ENERGY,
    WILLPOWER,
    SOCIAL_BATTERIES,
    COUNT
}

[Serializable]
public struct PlayerConsumablesModifier
{
    public PlayerConsumables Type;
    public float Value;
    public float BonusPercent;
}

[Serializable]
public enum PlayerResources
{
    PRODUCTIVITY,
    HOUSE_CHORES,
    PHYSICAL_HEALTH,
    PERSONAL_NEEDS,
    SOCIAL_LIFE,
    COUNT
}

[Serializable]
public struct PlayerResourcesModifier
{
    public PlayerResources Type;
    public float Value;
    public float BonusPercent;
}