using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Codice.ThemeImages;

[Serializable]
public class PlayerSaveData
{
    [SerializeField] private string m_name;

    public double SecondsPlayed;

    private double m_unixTimeOfCreation;

    public CornPlayerConsumables Consumables;

    public CornPlayerResources Resources;

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

        Resources.MentalSanity = MaxMentalSanity;
        Resources.Productivity = START_RESOURCE_VALUE;
        Resources.Chores = START_RESOURCE_VALUE;
        Resources.Groceries = START_RESOURCE_VALUE;
        Resources.PersonalNeeds = START_RESOURCE_VALUE;
        Resources.PhysicalHealth = START_RESOURCE_VALUE;
        Resources.Relantionship = START_RESOURCE_VALUE;
        Resources.SocialLife = START_RESOURCE_VALUE;

        ValidateSaveFile();
    }

    public string GetName() { return m_name; }

    public double GetUnixTimeOfCreation() { return m_unixTimeOfCreation; }

    public bool ValidateSaveFile()
    {
        //used to take into account any changes made to the initial sum of money
        Consumables.Money += INITIAL_SUM_OF_MONEY - m_originalMaxSumMoney;
        return true;
    }
}

public struct CornPlayerConsumables
{
    public float Money;

    //all of these are between 0-1
    public float Willpower;
    public float Energy;
    public float HealthPoints;
    public float SocialBatteries;

    public static CornPlayerConsumables operator +(CornPlayerConsumables aLeft, CornPlayerConsumables aRight)
    {
        aLeft.Money = Mathf.Max(aLeft.Money + aRight.Money, 0, 1);

        aLeft.Willpower = Mathf.Clamp(aLeft.Willpower + aRight.Willpower, 0, 1);
        aLeft.Energy = Mathf.Clamp(aLeft.Energy + aRight.Energy, 0, 1);
        aLeft.HealthPoints = Mathf.Clamp(aLeft.HealthPoints + aRight.HealthPoints, 0, 1);
        aLeft.SocialBatteries = Mathf.Clamp(aLeft.SocialBatteries + aRight.SocialBatteries, 0, 1);

        return aLeft;
    }

    public static CornPlayerConsumables operator -(CornPlayerConsumables aLeft, CornPlayerConsumables aRight)
    {
        aLeft.Money = Mathf.Max(aLeft.Money - aRight.Money, 0, 1);

        aLeft.Willpower = Mathf.Clamp(aLeft.Willpower - aRight.Willpower, 0, 1);
        aLeft.Energy = Mathf.Clamp(aLeft.Energy - aRight.Energy, 0, 1);
        aLeft.HealthPoints = Mathf.Clamp(aLeft.HealthPoints - aRight.HealthPoints, 0, 1);
        aLeft.SocialBatteries = Mathf.Clamp(aLeft.SocialBatteries - aRight.SocialBatteries, 0, 1);

        return aLeft;
    }
}

public struct CornPlayerResources
{
    public float GameProgress;

    public int MentalSanity; //scored between 0 and DICE_ROLL_NUM_FACES

    // the following stats have values between 0 and 1
    public float Productivity;
    public float Groceries;
    public float Chores;
    public float SocialLife;
    public float Relantionship;
    public float PhysicalHealth;
    public float PersonalNeeds;

    public static CornPlayerResources operator +(CornPlayerResources aLeft, CornPlayerResources aRight)
    {
        aLeft.GameProgress = Mathf.Clamp(aLeft.GameProgress + aRight.GameProgress, 0, 1);
        aLeft.MentalSanity = Mathf.Clamp(aLeft.MentalSanity + aRight.MentalSanity, 0, 1);
        aLeft.Productivity = Mathf.Clamp(aLeft.Productivity + aRight.Productivity, 0, 1);
        aLeft.Groceries = Mathf.Clamp(aLeft.Groceries + aRight.Groceries, 0, 1);
        aLeft.Chores = Mathf.Clamp(aLeft.Chores + aRight.Chores, 0, 1);
        aLeft.SocialLife = Mathf.Clamp(aLeft.SocialLife + aRight.SocialLife, 0, 1);
        aLeft.Relantionship = Mathf.Clamp(aLeft.Relantionship + aRight.Relantionship, 0, 1);
        aLeft.PhysicalHealth = Mathf.Clamp(aLeft.PhysicalHealth + aRight.PhysicalHealth, 0, 1);
        aLeft.PersonalNeeds = Mathf.Clamp(aLeft.PersonalNeeds + aRight.PersonalNeeds, 0, 1);

        return aLeft;
    }

    public static CornPlayerResources operator -(CornPlayerResources aLeft, CornPlayerResources aRight)
    {
        aLeft.GameProgress = Mathf.Clamp(aLeft.GameProgress - aRight.GameProgress, 0, 1);
        aLeft.MentalSanity = Mathf.Clamp(aLeft.MentalSanity - aRight.MentalSanity, 0, CornManagerEvents.DICE_ROLL_NUM_FACES);
        aLeft.Productivity = Mathf.Clamp(aLeft.Productivity - aRight.Productivity, 0, 1);
        aLeft.Groceries = Mathf.Clamp(aLeft.Groceries - aRight.Groceries, 0, 1);
        aLeft.Chores = Mathf.Clamp(aLeft.Chores - aRight.Chores, 0, 1);
        aLeft.SocialLife = Mathf.Clamp(aLeft.SocialLife - aRight.SocialLife, 0, 1);
        aLeft.Relantionship = Mathf.Clamp(aLeft.Relantionship - aRight.Relantionship, 0, 1);
        aLeft.PhysicalHealth = Mathf.Clamp(aLeft.PhysicalHealth - aRight.PhysicalHealth, 0, 1);
        aLeft.PersonalNeeds = Mathf.Clamp(aLeft.PersonalNeeds - aRight.PersonalNeeds, 0, 1);

        return aLeft;
    }
}