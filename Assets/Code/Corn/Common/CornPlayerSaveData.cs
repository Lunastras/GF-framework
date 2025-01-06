using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Codice.ThemeImages;
using System.Data;
using Unity.Collections;

[Serializable]
public class GfgPlayerSaveData
{
    [SerializeField] private string m_name;

    public double SecondsPlayed;

    private double m_unixTimeOfCreation;

    public class CornSaveData
    {
        public HashSet<string> FinishedNonSpecificScenes = new(8);

        public int MentalSanity;
        public int MaxMentalSanity;


        public float[] Consumables;

        public float[] Resources;
        public float[] SkillStats;

        public int CurrentStoryPhase;

        public int[] CurrentStoryPhaseProgress;

        public int DayOfTheWeek;

        public int CurrentHour { get; private set; } = FIRST_WAKEUP_TIME;

        public int CurrentDay { get; private set; } = 0;

        public int CurrentMonth { get; private set; } = 0;

        public int CurrentWakeUpTime = FIRST_WAKEUP_TIME;

        public int CurrentSleepTime = 22;

        public bool CurrentStoryPhaseGameLevelFinished;

        public int CurrentStoryPhaseGameTriviasFinished;

        public int WorkCountSinceLastTrivia;

        public bool HadCornEventSinceWakeUp;
        public int CornActionsInARow;

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
            }

            int weeksPassed = (DayOfTheWeek + daysPassed) / 7;
            DayOfTheWeek = (DayOfTheWeek + daysPassed) % 7;

            return new(daysPassed, weeksPassed, monthsPassed);
        }

        //aYear = 1 because we do not want leap year by default
        protected static int GetDaysInMonth(int aMonth, int aYear = 1)
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

        public float GetValue(CornPlayerResources aType) { return Resources[(int)aType]; }
        public float GetValue(CornPlayerConsumables aType) { return Consumables[(int)aType]; }
        public float GetValue(CornSkillsStats aType) { return SkillStats[(int)aType]; }

        public void ApplyModifier(CornSkillsStats aType, float aValue)
        {
            SkillStats[(int)aType] = (SkillStats[(int)aType] + aValue).Clamp(0, 1);
        }

        public void ApplyModifier(CornPlayerResources aType, float aValue)
        {
            Resources[(int)aType] = (Resources[(int)aType] + aValue).Clamp(0, 1);
        }

        public void ApplyModifier(CornPlayerConsumables aType, float aValue)
        {
            Consumables[(int)aType] += aValue;
            if (aType < CornPlayerConsumables.MONEY)
                Consumables[(int)aType] = Consumables[(int)aType].Clamp(0, 1);
        }

        public bool CanAfford(CornPlayerConsumables aType, float aValue)
        {
            return -0.0001 <= Consumables[(int)aType] + aValue; //-0.001 to account for errors
        }

        public bool CanAfford(CornPlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0)
        {
            return CanAfford(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value));
        }

        public bool CanAfford<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerConsumablesModifier>
        {
            bool canAfford = true;
            if (someModifiers != null)
                foreach (CornPlayerConsumablesModifier modifier in someModifiers)
                    canAfford &= CanAfford(modifier, aMultiplier, aBonusMultiplier);

            return canAfford;
        }

        public bool CanAffordMoney<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerConsumablesModifier>
        {
            bool canAfford = true;
            if (someModifiers != null)
                foreach (CornPlayerConsumablesModifier modifier in someModifiers)
                    if (modifier.Type == CornPlayerConsumables.MONEY)
                    {
                        canAfford &= CanAfford(modifier, aMultiplier, aBonusMultiplier);
                        break;
                    }

            return canAfford;
        }

        public void ApplyModifier(CornPlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { ApplyModifier(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value)); }
        public void ApplyModifier(CornPlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { ApplyModifier(aModifier.Type, aMultiplier * (aBonusMultiplier * aModifier.BonusPercent * aModifier.Value + aModifier.Value)); }

        public void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerResourcesModifier> { if (someModifiers != null) foreach (CornPlayerResourcesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
        public void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerConsumablesModifier> { if (someModifiers != null) foreach (CornPlayerConsumablesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
    };

    public CornSaveData Data = new();

    private readonly float m_originalMaxSumMoney = INITIAL_SUM_OF_MONEY;

    private const int INITIAL_SUM_OF_MONEY = 10000;

    private const int FIRST_WAKEUP_TIME = 8;

    private const float START_RESOURCE_VALUE = 0.5f;

    public const int COUNT_RESOURCES = (int)CornPlayerResources.COUNT;

    public const int COUNT_CONSUMABLES = (int)CornPlayerConsumables.COUNT;

    public const int COUNT_0_TO_100_CONSUMABLES = (int)CornPlayerConsumables.MONEY; //Money is not between 0 to 100

    public const int COUNT_NON_0_TO_100_CONSUMABLES = COUNT_CONSUMABLES - COUNT_0_TO_100_CONSUMABLES; //Money is not between 0 to 100

    public GfgPlayerSaveData(string aName, double aCurrentUnixTime)
    {
        m_name = aName;
        m_unixTimeOfCreation = aCurrentUnixTime;

        ValidateSaveFile();

        Data.MaxMentalSanity = CornManagerBalancing.DICE_ROLL_NUM_FACES;
        Data.MentalSanity = Data.MaxMentalSanity;
        Data.Consumables[(int)CornPlayerConsumables.MONEY] = INITIAL_SUM_OF_MONEY;
    }

    public string GetName() { return m_name; }

    public double GetUnixTimeOfCreation() { return m_unixTimeOfCreation; }

    private bool ValidateArrayValues<T>(ref T[] anArray, int aCount, T aDefaultValue = default, Func<T, bool> aValidator = null)
    {
        bool validArray = true;
        if (anArray == null)
        {
            validArray = false;
            anArray = new T[aCount];
            for (int i = 0; i < aCount; ++i)
                anArray[i] = aDefaultValue;
        }
        else if (anArray.Length != aCount)
        {
            Debug.LogWarning("The length of the array'" + anArray.Length + "' is invalid, replacing it with the desired length of: " + aCount);

            validArray = false;
            T[] newArray = new T[aCount];
            for (int i = 0; i < aCount; ++i)
                newArray[i] = i < anArray.Length ? anArray[i] : aDefaultValue;

            anArray = newArray;
        }

        if (aValidator != null)
        {
            for (int i = 0; i < aCount; ++i)
            {
                if (!aValidator(anArray[i]))
                {
                    Debug.LogWarning("The value in the array '" + anArray[i] + "' is invalid, replacing it with the default: " + aDefaultValue);
                    validArray = false;
                    anArray[i] = aDefaultValue;
                }
            }
        }

        return validArray;
    }

    public bool ValidateSaveFile()
    {
        bool validData = true;
        bool createdNewSave = false;

        if (Data == null)
        {
            Debug.LogWarning("The game data was null, save file might be corrupted or there was no save found.");
            Data = new();
            validData = false;
            createdNewSave = true;
        }

        validData &= ValidateArrayValues(ref Data.SkillStats, (int)CornSkillsStats.COUNT, 0);
        validData &= ValidateArrayValues(ref Data.CurrentStoryPhaseProgress, (int)GfcStoryCharacter.COUNT, 0);
        validData &= ValidateArrayValues(ref Data.Resources, (int)CornPlayerResources.COUNT, START_RESOURCE_VALUE);
        validData &= ValidateArrayValues(ref Data.Consumables, (int)CornPlayerConsumables.COUNT, START_RESOURCE_VALUE);

        Data.MentalSanity.ClampSelf(0, CornManagerBalancing.DICE_ROLL_NUM_FACES);

        //used to take into account any changes made to the initial sum of money
        Data.Consumables[(int)CornPlayerConsumables.MONEY] = Mathf.Max(0, Data.Consumables[(int)CornPlayerConsumables.MONEY] + INITIAL_SUM_OF_MONEY - m_originalMaxSumMoney);

        return validData || createdNewSave;
    }
}

//ORDER MUST RESPECT CornPlayerAction
public enum CornEventType
{
    WORK,
    CHORES,
    PERSONAL_TIME,
    SOCIAL,
    SLEEP,

    CORN,
    RANDOM,
    PERSONAL_GIFT,
    NEW_DAY,
    NEW_WEEK,
    STUDY,
    COUNT
}

public enum CornEventTypeWork
{
    WORK_ON_GAME,
    STREAM_WORK,
    CONTRACT_WORK,
    COUNT
}

public enum CornEventTypeSocial
{
    COOL_FRIEND,
    SHADY_FRIEND,
    NO_IDEA_FRIEND,
    COUNT
}

[Serializable]
public enum CornPlayerConsumables
{
    ENERGY,
    WILLPOWER,
    SOCIAL_BATTERIES,
    //values past this point are in a range outside of 0 to 100
    MONEY,
    COUNT
}

[Serializable]
public struct CornPlayerConsumablesModifier
{
    public CornPlayerConsumables Type;
    public float Value;
    public float BonusPercent;
}

[Serializable]
public enum CornSkillsStats
{
    HANDICRAFT,
    PROGRAMMING,
    ART,
    ASTRONOMY,
    COMFORT,
    COUNT
}

[Serializable]
public enum CornPlayerResources
{
    //THE ORDER MUST BE THE SAME AS CornPlayerAction
    WORK,
    CHORES,
    PERSONAL_NEEDS,
    SOCIAL_LIFE,
    COUNT
}

[Serializable]
public enum CornPlayerAction
{
    //THE ORDER MUST BE THE SAME AS CornPlayerResources
    WORK,
    CHORES,
    PERSONAL_NEEDS,
    SOCIAL_LIFE,
    SLEEP,
    COUNT
}

[Serializable]
public struct CornPlayerResourcesModifier
{
    public CornPlayerResources Type;
    public float Value;
    public float BonusPercent;
}

public struct TimeSpan
{
    public TimeSpan(int aDays, int aWeeks, int aMonths)
    {
        Days = aDays;
        Weeks = aWeeks;
        Months = aMonths;
    }

    public int Days, Weeks, Months;
}