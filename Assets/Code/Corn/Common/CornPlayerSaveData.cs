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

    public int CurrentHour { get; private set; } = FIRST_WAKEUP_TIME;

    public int CurrentDay { get; private set; } = 0;

    public int CurrentMonth { get; private set; } = 0;

    public int CurrentWakeUpTime = FIRST_WAKEUP_TIME;

    public int CurrentSleepTime = 22;

    public float GameProgress = 0;
    public int CurrentMilestone { get { return (int)Mathf.Floor(GameProgress / ((float)GAME_PROGRESS_MAX / GAME_PROGRESS_MILESTONES)); } }

    private readonly float m_originalMaxSumMoney = INITIAL_SUM_OF_MONEY;

    private const int INITIAL_SUM_OF_MONEY = 10000;

    private const int FIRST_WAKEUP_TIME = 8;

    private const float START_RESOURCE_VALUE = 0.5f;

    public const int COUNT_RESOURCES = (int)PlayerResources.COUNT;

    public const int COUNT_CONSUMABLES = (int)PlayerConsumables.COUNT;

    public const int COUNT_0_TO_100_CONSUMABLES = (int)PlayerConsumables.MONEY; //Money is not between 0 to 100

    public const int COUNT_NON_0_TO_100_CONSUMABLES = COUNT_CONSUMABLES - COUNT_0_TO_100_CONSUMABLES; //Money is not between 0 to 100

    public const int GAME_PROGRESS_MILESTONES = 20;

    public const int GAME_PROGRESS_MAX = 100;

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

    public TimeSpan ProgressTime(int aHoursSpan)
    {
        CurrentHour += aHoursSpan;
        int daysPassed = CurrentHour / 24;
        CurrentHour %= 24;
        CurrentDay += daysPassed;

        int daysInMonth = GetDaysInMonth(CurrentMonth);

        int monthsPassed = 0;
        while (daysInMonth <= CurrentDay)
        {
            monthsPassed++;
            CurrentDay -= daysInMonth;
            CurrentMonth++;
            CurrentMonth %= 12;

            daysInMonth = GetDaysInMonth(CurrentMonth);
        };

        return new(daysPassed, monthsPassed);
    }

    //aYear = 1 because we do not want leap year by default
    protected int GetDaysInMonth(int aMonth, int aYear = 1)
    {
        int numDays = 30;

        if (aMonth == 1)
        {
            bool leapYear = (aYear % 400 == 0) || (aYear % 100 != 0) && (aYear % 4 == 0);
            numDays = leapYear ? 29 : 28;
        }
        else if (aMonth % 2 == 0 || aMonth == 7) //august also has 31 days because Augustus could not accept Julius has more days
            numDays++;

        return numDays;
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

    public float GetValue(PlayerResources aType) { return Resources[(int)aType]; }

    public float GetValue(PlayerConsumables aType) { return Consumables[(int)aType]; }

    public void ApplyModifier(PlayerResources aType, float aValue)
    {
        Resources[(int)aType] += aValue;
        Resources[(int)aType].ClampSelf(0, 1);
    }

    public void ApplyModifier(PlayerConsumables aType, float aValue)
    {
        Consumables[(int)aType] += aValue;
        if (aType < PlayerConsumables.MONEY)
            Consumables[(int)aType].ClampSelf(0, 1);
    }

    public bool CanAfford(PlayerConsumables aType, float aValue)
    {
        return -0.001 <= Consumables[(int)aType] + aValue; //-0.001 to account for errors
    }

    public bool CanAfford(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0)
    {
        return CanAfford(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value));
    }

    public bool CanAfford<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier>
    {
        bool canAfford = true;
        if (someModifiers != null)
            foreach (PlayerConsumablesModifier modifier in someModifiers)
                canAfford &= CanAfford(modifier, aMultiplier, aBonusMultiplier);

        return canAfford;
    }

    public void ApplyModifier(PlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { ApplyModifier(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value)); }
    public void ApplyModifier(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { ApplyModifier(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value)); }

    public void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerResourcesModifier> { if (someModifiers != null) foreach (PlayerResourcesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
    public void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { if (someModifiers != null) foreach (PlayerConsumablesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
}

[Serializable]
public enum PlayerConsumables
{
    ENERGY,
    WILLPOWER,
    SOCIAL_BATTERIES,
    //values past this point are in a range outside of 0 to 100
    MONEY,
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
    WORK,
    CHORES,
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

public struct TimeSpan
{
    public TimeSpan(int aDays, int aMonths)
    {
        Days = aDays;
        Months = aMonths;
    }

    public int Days;
    public int Months;
}