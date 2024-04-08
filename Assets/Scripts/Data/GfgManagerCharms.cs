using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public unsafe class GfgManagerCharms : MonoBehaviour
{
    protected static GfgManagerCharms Instance = null;

    [SerializeField] protected CharmCategoryColorLink[] m_charmCategoryColorLinks;

    [SerializeField] protected Color[] m_rankColors;

    protected CharmData[] m_charmData = new CharmData[(int)CharmTypes.COUNT_CHARMS];

    protected string[] m_charmEnumStrings = new string[(int)CharmTypes.COUNT_CHARMS];

    protected Color[] m_categoryColors = new Color[(int)CharmCategory.COUNT_CATEGORIES];

    protected const StringTableType CHARM_DESCRIPTIONS = StringTableType.CHARM_DESCRIPTIONS;

    protected const float CHARM_RANK_POINTS_BASELINE = 20;

    protected const float CHARM_HIGHER_RANK_FALLOFF = 1.7f;

    protected const float CHARM_RANK_POINTS_PROGRESSION = 1.5f;

    protected const float CHARM_TIER_POINTS_PROGRESSION = 1.5f;

    protected const float CHARM_RANK_POINTS_SELL_COEF = 0.05f;

    //the termination string concatenated to the entry string of a charm's main description
    protected const string MAIN_DESCRIPTION_ENTRY_TERMINATION = "_MAIN";

    //the termination string concatenated to the entry string of a charm's name
    protected const string NAME_ENTRY_TERMINATION = "_NAME";

    private int m_highestRank = 0;

    public static int HighestRank { get { return Instance.m_highestRank; } }

    // Start is called before the first frame update
    void Awake()
    {
        if (null != Instance)
            Destroy(Instance);

        Instance = this;

        for (int i = 0; i < m_charmCategoryColorLinks.Length; ++i)
        {
            var link = m_charmCategoryColorLinks[i];
            int charmIndex = (int)link.Category;
            if (i != charmIndex)
                Debug.LogError("Found category '" + link.Category + "' at index " + i + ", should be at index " + charmIndex + " in the CharmCategoryColorLinks array");

            m_categoryColors[charmIndex] = link.Color;
        }

        for (int i = 0; i < m_charmData.Length; ++i)
            m_charmEnumStrings[i] = ((CharmTypes)i).ToString();

        m_charmData[(int)CharmTypes.HEALTH_BOOST] = new()
        {
            TiersRequiredForLevels = new int[] { 1, 2, 6, 0 },
            EffectsFunction = new(ApplyHealthBoost),
            CharmCategory = CharmCategory.HEALTH,
            CharmSlots = 2,
            Rank = 0,
        };

        m_charmData[(int)CharmTypes.DIVINE_RECOVERY] = new()
        {
            TiersRequiredForLevels = new int[] { 1, 2, 6, 0 },
            EffectsFunction = new(ApplyDivineRecovery),
            CharmCategory = CharmCategory.HEALTH,
            CharmSlots = 2,
            Rank = 1,
        };

        for (int i = 0; i < m_charmData.Length; ++i)
        {
            if (m_charmData[i].Rank > m_highestRank) m_highestRank = m_charmData[i].Rank;
        }

        int countRanks = m_highestRank + 1;
        if (m_rankColors.Length != countRanks) Debug.LogError("The array's length for rank colors is: " + m_rankColors.Length + ", it should be " + countRanks);
        if (m_charmCategoryColorLinks.Length != m_categoryColors.Length) Debug.LogError("The array's length for charm category color links is: " + m_charmCategoryColorLinks.Length + ", it should be " + m_categoryColors.Length);
    }

    public static float GetCharmPointsSellWorth(int aRank, int aTier) { return CHARM_RANK_POINTS_SELL_COEF * GetCharmPointsWorth(aRank, aTier); }

    public static float GetCharmPointsWorth(int aRank, int aTier) { return CHARM_RANK_POINTS_BASELINE * GetCharmPointsRankMultiplier(aRank) * GetCharmPointsTierMultiplier(aTier); }

    public static float GetCharmPointsRankMultiplier(int aRank) { return System.MathF.Pow(CHARM_RANK_POINTS_PROGRESSION, aRank); }

    public static float GetCharmPointsTierMultiplier(int aTier) { return System.MathF.Pow(CHARM_TIER_POINTS_PROGRESSION, aTier); }

    public static float GetLikelyHoodOfHigherRank(ThreatLevel aThreatLevel) { return 1.0f / System.MathF.Pow(CHARM_HIGHER_RANK_FALLOFF, (int)ThreatLevel.GODDESS - (int)aThreatLevel); }

    public static string GetCharmEnumString(CharmTypes aCharmType) { return Instance.m_charmEnumStrings[(int)aCharmType]; }

    public static string GetCharmName(CharmTypes aCharmType)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(Instance.m_charmEnumStrings[(int)aCharmType]);
        stringBuffer.Concatenate(NAME_ENTRY_TERMINATION);
        string charmName = GfcLocalization.GetString(CHARM_DESCRIPTIONS, stringBuffer);
        stringBuffer.Clear();
        return charmName;
    }

    public static string GetCharmMainDescription(CharmTypes aCharmType)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(Instance.m_charmEnumStrings[(int)aCharmType]);
        stringBuffer.Concatenate(MAIN_DESCRIPTION_ENTRY_TERMINATION);
        string charmDescription = GfcLocalization.GetString(CHARM_DESCRIPTIONS, stringBuffer);
        stringBuffer.Clear();
        return charmDescription;
    }

    public static string GetCharmDescription(CharmTypes aCharmType, int aLevel)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(Instance.m_charmEnumStrings[(int)aCharmType]);
        stringBuffer.Concatenate('_');
        stringBuffer.Concatenate(aLevel);
        string charmDescription = GfcLocalization.GetString(CHARM_DESCRIPTIONS, stringBuffer);
        stringBuffer.Clear();
        return charmDescription;
    }

    public static Color GetCharmCategoryColor(CharmTypes aCharmType) { return Instance.m_categoryColors[(int)GetCharmData(aCharmType).CharmCategory]; }

    public static Color GetCharmRankColor(CharmTypes aCharmType) { return Instance.m_rankColors[(int)GetCharmData(aCharmType).CharmCategory]; }

    public static CharmData GetCharmData(CharmTypes aCharmType) { return Instance.m_charmData[(int)aCharmType]; }

    public static EquipCharmData GetRandomCharmDrop(ItemDrop aItemDrop)
    {
        return GetRandomCharmDrop(aItemDrop.Tier, aItemDrop.ThreatLevel, aItemDrop.Blessed);
    }

    //award for worst rng function ever goes to...
    public static EquipCharmData GetRandomCharmDrop(int aTier, ThreatLevel aThreatLevel, bool aBlessed)
    {
        if (aBlessed)
        {
            aTier += 4;
            aThreatLevel = ThreatLevel.GODDESS;
        }

        int rank = 0;
        int highestRank = Instance.m_highestRank;
        float likelyHoodOfHigherRank = GetLikelyHoodOfHigherRank(aThreatLevel);
        for (; rank < highestRank && likelyHoodOfHigherRank >= UnityEngine.Random.Range(0f, 0.99999f); rank++) ;

        NativeList<int> charmsIndicesOfThisRank = new(4, Allocator.Temp);

        for (int i = 0; i < (int)CharmTypes.COUNT_CHARMS; ++i)
            if (Instance.m_charmData[i].Rank == rank) charmsIndicesOfThisRank.Add(i);

        int charmIndex = 0;
        if (charmsIndicesOfThisRank.Length != 0)
            charmIndex = charmsIndicesOfThisRank[UnityEngine.Random.Range(0, charmsIndicesOfThisRank.Length - 1)];
        else
            Debug.LogWarning("There are no charms of rank " + rank + ", maybe try distributing the ranks better?");

        charmsIndicesOfThisRank.Dispose();
        CharmData droppedCharm = Instance.m_charmData[charmIndex];

        int level = 0;
        int[] tiersRequiredForLevels = droppedCharm.TiersRequiredForLevels;
        if (null != tiersRequiredForLevels)
        {
            int maxLevel = tiersRequiredForLevels.Length - 1;
            while (level <= maxLevel && aTier > tiersRequiredForLevels[level]) level++;
            level = Mathf.Clamp(level, 0, maxLevel);
        }

        return new()
        {
            Level = level,
            CharmType = (CharmTypes)charmIndex,
        };
    }

    public static void ApplyCharms(ref PlayerRuntimeGameData aPlayerData, List<EquipCharmData> someEquipedCharmData)
    {
        int countCharms = someEquipedCharmData.Count;
        for (int i = 0; i < countCharms; ++i)
        {
            CharmData charmData = Instance.m_charmData[(int)someEquipedCharmData[i].CharmType];
            charmData.EffectsFunction(ref aPlayerData, someEquipedCharmData[i].Level);
        }
    }

    protected static void ApplyHealthBoost(ref PlayerRuntimeGameData aPlayerData, int aCharmLevel)
    {

    }

    protected static void ApplyDivineRecovery(ref PlayerRuntimeGameData aPlayerData, int aCharmLevel)
    {

    }
}

public struct EquipCharmData : INetworkSerializable
{
    public CharmTypes CharmType;

    public int Level;

    public string Name { get { return GfgManagerCharms.GetCharmName(CharmType); } }

    public int Rank { get { return GfgManagerCharms.GetCharmData(CharmType).Rank; } }

    public bool Blessed { get { return Rank >= GfgManagerCharms.HighestRank; } }

    public Color CategoryColor { get { return GfgManagerCharms.GetCharmCategoryColor(CharmType); } }

    public Color RankColor { get { return GfgManagerCharms.GetCharmRankColor(CharmType); } }

    public bool IsMaxLevel
    {
        get
        {
            var TiersRequiredForLevels = GfgManagerCharms.GetCharmData(CharmType).TiersRequiredForLevels;
            return TiersRequiredForLevels == null || Level >= GfgManagerCharms.GetCharmData(CharmType).TiersRequiredForLevels.Length;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> aSerializer) where T : IReaderWriter
    {
        aSerializer.SerializeValue(ref CharmType);
        aSerializer.SerializeValue(ref Level);
    }
}

[Serializable]
public struct CharmInventoryData
{
    public int Level;
    public float LevelProgress;
}

public struct CharmData
{
    public delegate void ApplyEffects(ref PlayerRuntimeGameData aPlayerData, int aCharmLevel);

    public int[] TiersRequiredForLevels;

    public ApplyEffects EffectsFunction;

    public CharmCategory CharmCategory;

    public int CharmSlots;

    public int Rank;
}

public enum CharmCategory
{
    HEALTH,
    FIRE_POWER,
    CRITICAL_POWER,
    BOMB_POWER,
    MOVEMENT,
    ITEMS,
    STATUS_RESISTANCE,
    COUNT_CATEGORIES
}

[System.Serializable]
public struct CharmCategoryColorLink
{
    public Color Color;
    public CharmCategory Category;
}

public enum CharmTypes
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
