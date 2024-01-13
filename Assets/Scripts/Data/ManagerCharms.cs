using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public unsafe class ManagerCharms : MonoBehaviour
{
    protected static ManagerCharms Instance = null;

    protected CharmData[] m_charmData = new CharmData[(int)CharmType.COUNT_CHARMS];

    protected string[] m_charmEnumStrings = new string[(int)CharmType.COUNT_CHARMS];

    protected const StringTableType CHARM_DESCRIPTIONS = StringTableType.CHARM_DESCRIPTIONS;

    protected string m_stringBuffer = new('F', 15);//we do 15 instead of 16 because the nullterminator is added anyway at the end, equating to 16 characters

    //the termination string concatenated to the entry string of a charm's main description
    protected const string MAIN_DESCRIPTION_ENTRY_TERMINATION = "_MAIN";

    //the termination string concatenated to the entry string of a charm's name
    protected const string NAME_ENTRY_TERMINATION = "_NAME";

    // Start is called before the first frame update
    void Start()
    {
        if (null != Instance)
            Destroy(Instance);

        Instance = this;

        for (int i = 0; i < m_charmData.Length; ++i)
            m_charmEnumStrings[i] = ((CharmType)i).ToString();

        m_charmData[(int)CharmType.HEALTH_BOOST] = new()
        {
            Rank = 1,
            CharmSlots = 2,
            EffectsFunction = new(ApplyHealthBoost),
            LevelsTiers = new int[] { 1, 2, 6, 0 },
        };

        m_charmData[(int)CharmType.DIVINE_RECOVERY] = new()
        {
            Rank = 1,
            CharmSlots = 2,
            EffectsFunction = new(ApplyDivineRecovery),
            LevelsTiers = new int[] { 1, 2, 6, 0 },
        };

        Debug.Log(GetCharmName(CharmType.HEALTH_BOOST));
        Debug.Log(GetCharmMainDescription(CharmType.HEALTH_BOOST));
        Debug.Log(GetCharmDescription(CharmType.HEALTH_BOOST, 1));
        Debug.Log(GetCharmDescription(CharmType.HEALTH_BOOST, 1234567));
    }

    public static string GetCharmEnumString(CharmType aCharmType) { return Instance.m_charmEnumStrings[(int)aCharmType]; }

    protected unsafe void ResizeStringBuffer(int requiredLength)
    {
        if (m_stringBuffer.Length <= requiredLength)
            m_stringBuffer = new('F', ((m_stringBuffer.Length + 1) << 1) - 1);

        fixed (char* stringPtr = m_stringBuffer)
        {
            //the length of a string is encoded right before the string is defined in memory
            //I am now wondering if the garbage collector actually checks this length when it moves the string around in memory
            //or when it removes it. If so, then this might cause some memory leaks... YOLO
            int* intPtr = (int*)stringPtr;
            intPtr[-1] = requiredLength; //Thanks Chris Fulstow on StackOverflow
            stringPtr[requiredLength] = '\0';
        }
    }

    protected unsafe string ConcatenateStringToCharmEnum(CharmType aCharmType, string aTermination)
    {
        string charmEnumString = GetCharmEnumString(aCharmType);
        int charmEnumStringLength = charmEnumString.Length;
        int terminationLength = aTermination.Length;

        Instance.ResizeStringBuffer(charmEnumStringLength + terminationLength);

        fixed (char* stringPtr = m_stringBuffer)
        {
            for (int i = 0; i < charmEnumStringLength; ++i)
                stringPtr[i] = charmEnumString[i];

            for (int i = 0; i < terminationLength; ++i)
                stringPtr[i + charmEnumStringLength] = aTermination[i];
        }

        return m_stringBuffer;
    }

    protected unsafe string ConcatenateIntToCharmEnum(CharmType aCharmType, int aNumber)
    {
        string charmEnumString = GetCharmEnumString(aCharmType);
        int charmEnumStringLength = charmEnumString.Length;

        int auxNumber = aNumber;
        int countDigits = auxNumber == 0 ? 1 : 0;
        while (auxNumber != 0)
        {
            countDigits++;
            auxNumber /= 10;
        }

        Instance.ResizeStringBuffer(charmEnumStringLength + countDigits + 1);

        fixed (char* stringPtr = m_stringBuffer)
        {
            for (int i = 0; i < charmEnumStringLength; ++i)
                stringPtr[i] = charmEnumString[i];

            stringPtr[charmEnumStringLength] = '_';

            auxNumber = aNumber;
            int currentCharIndex = charmEnumStringLength + countDigits;
            while (auxNumber != 0)
            {
                stringPtr[currentCharIndex] = (char)(auxNumber % 10 + 48);
                --currentCharIndex;
                auxNumber /= 10;
            }
        }

        return m_stringBuffer;
    }

    public unsafe static string GetCharmName(CharmType aCharmType)
    {
        return GfLocalization.GetString(CHARM_DESCRIPTIONS, Instance.ConcatenateStringToCharmEnum(aCharmType, NAME_ENTRY_TERMINATION));
    }

    public static string GetCharmMainDescription(CharmType aCharmType)
    {
        return GfLocalization.GetString(CHARM_DESCRIPTIONS, Instance.ConcatenateStringToCharmEnum(aCharmType, MAIN_DESCRIPTION_ENTRY_TERMINATION));
    }

    public static string GetCharmDescription(CharmType aCharmType, int aLevel)
    {
        return GfLocalization.GetString(CHARM_DESCRIPTIONS, Instance.ConcatenateIntToCharmEnum(aCharmType, aLevel));
    }

    public static CharmData GetCharmData(CharmType aCharmType) { return Instance.m_charmData[(int)aCharmType]; }

    public static void ApplyCharms(ref PlayerRuntimeGameData aPlayerData, List<CharmInventoryData> someCharmsData)
    {
        int countCharms = someCharmsData.Count;
        for (int i = 0; i < countCharms; ++i)
        {
            CharmData charmData = Instance.m_charmData[(int)someCharmsData[i].CharmType];
            charmData.EffectsFunction(ref aPlayerData, someCharmsData[i].Level);
        }
    }

    protected static void ApplyHealthBoost(ref PlayerRuntimeGameData aPlayerData, int aCharmLevel)
    {

    }

    protected static void ApplyDivineRecovery(ref PlayerRuntimeGameData aPlayerData, int aCharmLevel)
    {

    }
}

public struct CharmInventoryData
{
    public CharmType CharmType;
    public int Level;
}

public struct PlayerRuntimeGameData
{
    public float MaxHp;
}

public struct CharmData
{
    public delegate void ApplyEffects(ref PlayerRuntimeGameData aPlayerData, int aCharmLevel);
    public int Rank;
    public int CharmSlots;
    public ApplyEffects EffectsFunction;
    public int[] LevelsTiers;
}

public enum CharmType
{
    HEALTH_BOOST,
    DIVINE_RECOVERY, //recovery up
    LIFE_SUPPLY_EXPERT, //more hp drops
    HEALTH_GLUTTONY, //more big hp drops

    BOMB_SUPPLIER, //more bomb drops
    HEAVIER_ARTILLERY, //more big bombs drops
    BOMB_SPECIALIST, //extends bombs duration

    DEBT_COLLECTOR, // enemy drops get instantly sent to the player

    WEAPON_HOBBYIST, //more weapon drops
    WEAPON_CONNOISEUR, //better weapon tiers
    BLESSED_ARSENAL, //more blessed weapons

    CHARGE_FREAK, //faster charging
    RELOAD_ADEPT, //faster reload
    POWER_CONSERVATION, //higher capacity for weapons that are not single shot

    ATHLETE, //faster walking
    AERODYNAMIC, //faster flying
    FUEL_EFFIENCY, //more flying fuel
    FLYING_MAINTENANCE, //faster flying fuel recovery

    FIREPOWER, //higher damage
    MARKSMAN, //higher critical rate
    CRITICAL_BOON, //higher critical damage
    WHIRLWIND, //faster fire rate

    JUNKIE, //increases poison resistance
    WARM_HEARTED, //increases freeze resistance
    CLEAR_MIND, //increases confussion resistance
    STEADY_FEET, //increases flinch resistance
    WIND_FLAPS, //increases wind pressure resistance

    POISON_ENJOYER, //increases poison damage
    FROSTBITE, //increases freeze damage
    COUNT_CHARMS
}
