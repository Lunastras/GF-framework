using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CornManagerBalancing : MonoBehaviour
{
    public static CornManagerBalancing Instance { get { return InstanceInternal; } }
    private static CornManagerBalancing InstanceInternal = null;

    [SerializeField] private float m_baseChoresMiniGameExtraPoints = 0.5f;
    [SerializeField] private CornEventCostAndRewardsSetter[] m_cornEventCostAndRewards = null;
    [SerializeField] private EnumSingletons<CornShopItemsData, CornShopItem> m_cornShopItems;

    public const int MORNING_START_HOUR = 6;
    public const int EARLIEST_SLEEP_HOUR = 23;
    public const float DAY_WAKE_UP_RATIO_PER_HOUR = 0.3f;
    public const int MAX_CORN_ACTION_IN_ROW = 2;
    public const int DICE_ROLL_NUM_FACES = 10;

    [NonSerialized] public CornEventCostAndRewards[] AuxCostAndRewardsCopy;

    // Start is called before the first frame update
    void Awake()
    {
        if (InstanceInternal != this)
            Destroy(InstanceInternal);
        InstanceInternal = this;

        m_cornShopItems.Initialize(CornShopItem.COUNT);
        Debug.Assert(m_baseChoresMiniGameExtraPoints <= 1, "The value for m_baseChoresMiniGameExtraPoints should be between 0 and 1");

        AuxCostAndRewardsCopy = new CornEventCostAndRewards[(int)CornEventType.COUNT];

        for (int i = 0; i < m_cornEventCostAndRewards.Length; ++i)
        {
            m_cornEventCostAndRewards[i].CostAndRewards.ConsumablesMultiplier = 1;
            m_cornEventCostAndRewards[i].CostAndRewards.ResourcesMultiplier = 1;
            AuxCostAndRewardsCopy[i] = m_cornEventCostAndRewards[i].CostAndRewards.GetDeepCopy();

            if ((int)m_cornEventCostAndRewards[i].EventType != i)
                Debug.LogError("The values inside CornEventCostAndRewards are out of order. Element at index" + i + " should be at index " + (int)m_cornEventCostAndRewards[i].EventType + '.');
        }
    }

    public static CornShopItemsData GetShopItemData(CornShopItem anItem) { return GetShopItemData((int)anItem); }
    public static CornShopItemsData GetShopItemData(int anItemIndex) { return Instance.m_cornShopItems[anItemIndex]; }

    public static int GetCornPlayerSkillsStatsForEvent(CornEventType aType)
    {
        int skillIndex = -1;
        switch (aType)
        {
            case CornEventType.CHORES:
                skillIndex = (int)CornPlayerSkillsStats.HANDICRAFT;
                break;

            case CornEventType.WORK:
                skillIndex = (int)CornPlayerSkillsStats.PROGRAMMING;
                break;

            case CornEventType.PERSONAL_TIME:
                skillIndex = (int)CornPlayerSkillsStats.COMFORT;
                break;
        }

        return skillIndex;
    }

    public static float GetCornPlayerSkillsStatsValueForEvent(CornEventType aType)
    {
        int skillIndex = GetCornPlayerSkillsStatsForEvent(aType);
        return skillIndex >= 0 ? GfgManagerSaveData.GetActivePlayerSaveData().Data.GetValue((CornPlayerSkillsStats)skillIndex) : 0;
    }

    public static bool IsSleepHour(int anHour) { return anHour >= EARLIEST_SLEEP_HOUR || anHour < MORNING_START_HOUR; }
    public static float GetBaseChoresMiniGameExtraPoints() { return InstanceInternal.m_baseChoresMiniGameExtraPoints; }

    public static CornEventCostAndRewards GetEventCostAndRewardsRaw(CornEventType aType)
    {
        CornEventCostAndRewards eventRewardsAndCost = InstanceInternal.m_cornEventCostAndRewards[(int)aType].CostAndRewards;

        if (eventRewardsAndCost.ResourcesModifier != null && eventRewardsAndCost.ResourcesModifier.Length == 1)
        {
            CornEventCostAndRewards auxRewardsAndCost = InstanceInternal.AuxCostAndRewardsCopy[(int)aType];
            Debug.Assert(auxRewardsAndCost.ResourcesModifier[0].Type == eventRewardsAndCost.ResourcesModifier[0].Type, "The types between the two are different, none should ever change during runtime.");
            auxRewardsAndCost.ResourcesModifier[0].Value = GetCornPlayerSkillsStatsValueForEvent(aType) + eventRewardsAndCost.ResourcesModifier[0].Value;
            return auxRewardsAndCost;
        }

        return eventRewardsAndCost;
    }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, uint aTypeSub = 0) { return GetEventCostAndRewards(aType, out _, aTypeSub); }
    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, out string aMessage, uint aTypeSub = 0)
    {
        aMessage = null;
        CornEventCostAndRewards eventRewardsAndCost = GetEventCostAndRewardsRaw(aType);

        switch (aType)
        {
            case CornEventType.SLEEP:

                CornHoursSleepData sleepData = new();
                sleepData.Initialize();
                eventRewardsAndCost = sleepData.GetFinalRewards();
                break;
        }

        return eventRewardsAndCost;
    }

    public static int GetShopItemsNotOwnedCount() { return (int)CornShopItem.COUNT - GfgManagerSaveData.GetActivePlayerSaveData().Data.PurchasedItems.Count; }

    public static void GetShopItemsNotOwned(ref Span<CornShopItem> someItems)
    {
        Span<bool> itemPurchased = stackalloc bool[(int)CornShopItem.COUNT];
        var purchasedItems = GfgManagerSaveData.GetActivePlayerSaveData().Data.PurchasedItems;
        foreach (var purchaseData in purchasedItems)
            itemPurchased[(int)purchaseData.Item] = true;

        int itemIndex = 0;
        for (int i = 0; i < (int)CornShopItem.COUNT; i++)
        {
            CornShopItem item = (CornShopItem)i;
            if (!itemPurchased[(int)item])
                someItems[itemIndex++] = item;
        }

        Debug.Assert(itemIndex == someItems.Length, "The length of the items list is incorrect.");
    }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEvent aEvent) { return GetEventCostAndRewards(aEvent.EventType, aEvent.EventTypeSub); }
}

[Serializable]
public struct CornEventCostAndRewards
{
    public CornPlayerConsumablesModifier[] ConsumablesModifier;
    public CornPlayerResourcesModifier[] ResourcesModifier;
    public int HoursDuration;
    public bool EventHasCornRoll;

    [HideInInspector] public float ConsumablesMultiplier;
    [HideInInspector] public float ResourcesMultiplier;

    public readonly void ApplyModifiersToPlayer(float aBonusMultiplier = 0)
    {
        CornManagerEvents.ApplyModifierResourceList(ResourcesModifier, ResourcesMultiplier, aBonusMultiplier);
        CornManagerEvents.ApplyModifierConsumablesList(ConsumablesModifier, ConsumablesMultiplier, aBonusMultiplier);
    }

    public readonly bool CanAfford(float aBonusMultiplier = 0)
    {
        return CornManagerEvents.CanAfford(ConsumablesModifier, ResourcesMultiplier, aBonusMultiplier);
    }

    public readonly bool CanAffordMoney(float aBonusMultiplier = 0)
    {
        return CornManagerEvents.CanAffordMoney(ConsumablesModifier, ResourcesMultiplier, aBonusMultiplier);
    }

    public readonly void Preview(float aBonusMultiplier = 0)
    {
        foreach (CornPlayerConsumablesModifier modifier in ConsumablesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier.Type, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));

        foreach (CornPlayerResourcesModifier modifier in ResourcesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier.Type, ResourcesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));
    }
}

[Serializable]
internal struct CornEventCostAndRewardsSetter
{
    public CornEventType EventType;
    public CornEventCostAndRewards CostAndRewards;
}

[Serializable]
public struct CornShopItemsData
{
    public string Name;
    public string Description;
    public int Price;
    public int DeliveryDays;
    public float PersonalNeedsPoints;
    public float PreviewScale;
    public Sprite Icon;
    public GameObject Prefab;
    public CornPlayerSkillStatsModifier[] Modifiers;
}

public enum CornShopItem
{
    COUCH,
    BED,
    PC,
    FLOWERS,
    POSTER,
    COUNT
}