using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornManagerBalancing : MonoBehaviour
{
    public static CornManagerBalancing Instance { get { return InstanceInternal; } }
    private static CornManagerBalancing InstanceInternal = null;

    [SerializeField] private float m_baseChoresMiniGameExtraPoints = 0.5f;
    [SerializeField] private CornEventCostAndRewardsSetter[] m_cornEventCostAndRewards = null;
    [SerializeField] private CornShopItemsData[] m_cornShopItems;

    public const int MORNING_START_HOUR = 6;
    public const int EARLIEST_SLEEP_HOUR = 23;
    public const float DAY_WAKE_UP_RATIO_PER_HOUR = 0.3f;
    public const int MAX_CORN_ACTION_IN_ROW = 2;
    public const int DICE_ROLL_NUM_FACES = 20;

    [NonSerialized] public CornEventCostAndRewards SleepRewardsTemp;

    // Start is called before the first frame update
    void Awake()
    {
        if (InstanceInternal != this)
            Destroy(InstanceInternal);
        InstanceInternal = this;

        Debug.Assert(m_baseChoresMiniGameExtraPoints <= 1, "The value for m_baseChoresMiniGameExtraPoints should be between 0 and 1");

        for (int i = 0; i < m_cornEventCostAndRewards.Length; ++i)
        {
            m_cornEventCostAndRewards[i].CostAndRewards.ConsumablesMultiplier = 1;
            m_cornEventCostAndRewards[i].CostAndRewards.ResourcesMultiplier = 1;

            if ((int)m_cornEventCostAndRewards[i].EventType != i)
                Debug.LogError("The values inside CornEventCostAndRewards are out of order. Element at index" + i + " should be at index " + (int)m_cornEventCostAndRewards[i].EventType + '.');
        }

        SleepRewardsTemp = m_cornEventCostAndRewards[(int)CornEventType.SLEEP].CostAndRewards;

        if (SleepRewardsTemp.ConsumablesModifier != null && SleepRewardsTemp.ConsumablesModifier.Length > 0)
            SleepRewardsTemp.ConsumablesModifier = new CornPlayerConsumablesModifier[SleepRewardsTemp.ConsumablesModifier.Length];
        if (SleepRewardsTemp.ResourcesModifier != null && SleepRewardsTemp.ResourcesModifier.Length > 0)
            SleepRewardsTemp.ResourcesModifier = new CornPlayerResourcesModifier[SleepRewardsTemp.ResourcesModifier.Length];
    }

    public static bool IsSleepHour(int anHour) { return anHour >= EARLIEST_SLEEP_HOUR || anHour < MORNING_START_HOUR; }
    public static float GetBaseChoresMiniGameExtraPoints() { return InstanceInternal.m_baseChoresMiniGameExtraPoints; }
    public static CornEventCostAndRewards GetEventCostAndRewardsRaw(CornEventType aType) { return InstanceInternal.m_cornEventCostAndRewards[(int)aType].CostAndRewards; }
    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, uint aTypeSub = 0) { return GetEventCostAndRewards(aType, out _, aTypeSub); }
    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, out string aMessage, uint aTypeSub = 0)
    {
        aMessage = null;
        CornEventCostAndRewards eventRewardsAndCost = GetEventCostAndRewardsRaw(aType);
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        switch (aType)
        {
            case CornEventType.SLEEP:

                CornHoursSleepData sleepData = new();
                sleepData.Initialize();
                eventRewardsAndCost = sleepData.GetFinalRewards();

                /*
                for (; realHourSlept < desiredSleepHours; realHourSlept++)
                {
                    currentHour = ++currentHour % 24;
                    bool nightTime = IsSleepHour(currentHour);
                    caughtNight |= nightTime;
                    if ((!nightTime && Random.Range(0.0f, 1.0f) < DAY_WAKE_UP_RATIO_PER_HOUR && aTypeSub == (int)CornSleepType.INTERRUPTED)
                    || 1 <= cornSaveData.GetValue(CornPlayerConsumables.ENERGY) + realHourSlept * energyPerHour)
                        break;
                }*/
                break;
        }

        return eventRewardsAndCost;
    }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEvent aEvent) { return GetEventCostAndRewards(aEvent.EventType, aEvent.EventTypeSub); }
}

[System.Serializable]
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
            CornMenuApartment.Instance.PreviewChange(modifier, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));

        foreach (CornPlayerResourcesModifier modifier in ResourcesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));
    }
}

[Serializable]
internal struct CornEventCostAndRewardsSetter
{
    public CornEventType EventType;
    public CornEventCostAndRewards CostAndRewards;
}

public enum CornSleepType
{
    UNINTERRUPTED,
    INTERRUPTED
}

[Serializable]
public struct CornShopItemsData
{
    public CornShopItem ItemType;
    public string Name;
    public int Price;
    public CornSkillStatsModifier[] Modifiers;
}

public enum CornShopItem
{
    COUCH,
    BED,
    PC,
    FLOWERS,
    POSTER,
}

[Serializable]
public struct CornSkillStatsModifier
{
    public CornSkillsStats Type;
    public float Value;
}