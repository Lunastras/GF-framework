using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornManagerBalancing : MonoBehaviour
{
    private static CornManagerBalancing Instance = null;

    [SerializeField] private float m_baseChoresMiniGameExtraPoints = 0.5f;
    [SerializeField] private CornEventCostAndRewardsSetter[] m_cornEventCostAndRewards = null;

    public const int MORNING_START_HOUR = 6;
    public const int EARLIEST_SLEEP_HOUR = 22;
    public const float DAY_WAKE_UP_RATIO_PER_HOUR = 0.3f;
    public const int MAX_CORN_ACTION_IN_ROW = 2;
    public const int DICE_ROLL_NUM_FACES = 20;

    private CornEventCostAndRewards m_sleepRewardsTemp;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);
        Instance = this;

        Debug.Assert(m_baseChoresMiniGameExtraPoints <= 1, "The value for m_baseChoresMiniGameExtraPoints should be between 0 and 1");

        for (int i = 0; i < m_cornEventCostAndRewards.Length; ++i)
        {
            m_cornEventCostAndRewards[i].CostAndRewards.ConsumablesMultiplier = 1;
            m_cornEventCostAndRewards[i].CostAndRewards.ResourcesMultiplier = 1;

            if ((int)m_cornEventCostAndRewards[i].EventType != i)
                Debug.LogError("The values inside CornEventCostAndRewards are out of order. Element at index" + i + " should be at index " + (int)m_cornEventCostAndRewards[i].EventType + '.');
        }

        m_sleepRewardsTemp = m_cornEventCostAndRewards[(int)CornEventType.SLEEP].CostAndRewards;

        if (m_sleepRewardsTemp.ConsumablesModifier != null && m_sleepRewardsTemp.ConsumablesModifier.Length > 0)
            m_sleepRewardsTemp.ConsumablesModifier = new CornPlayerConsumablesModifier[m_sleepRewardsTemp.ConsumablesModifier.Length];
        if (m_sleepRewardsTemp.ResourcesModifier != null && m_sleepRewardsTemp.ResourcesModifier.Length > 0)
            m_sleepRewardsTemp.ResourcesModifier = new CornPlayerResourcesModifier[m_sleepRewardsTemp.ResourcesModifier.Length];
    }

    public static float GetBaseChoresMiniGameExtraPoints() { return Instance.m_baseChoresMiniGameExtraPoints; }
    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, uint aTypeSub = 0) { return GetEventCostAndRewards(aType, out _, aTypeSub); }
    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, out string aMessage, uint aTypeSub = 0)
    {
        aMessage = null;
        CornEventCostAndRewards eventRewardsAndCost = Instance.m_cornEventCostAndRewards[(int)aType].CostAndRewards;
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        switch (aType)
        {
            case CornEventType.SLEEP:

                Debug.Assert(eventRewardsAndCost.ConsumablesModifier.Length == 1 && eventRewardsAndCost.ConsumablesModifier[0].Type == CornPlayerConsumables.ENERGY, " Because of the new design, the only consumable sleep should replenish is energy.");
                int desiredSleepHours = eventRewardsAndCost.HoursDuration - 1; //-1 because one hour has no benefits to the stats
                Debug.Assert(desiredSleepHours > 0);

                float energyPerHour = eventRewardsAndCost.ConsumablesModifier[0].Value / desiredSleepHours;
                int currentHour = cornSaveData.CurrentHour;
                int realHourSlept = 0;
                bool caughtNight = false;
                for (; realHourSlept < desiredSleepHours; realHourSlept++)
                {
                    currentHour = ++currentHour % 24;
                    bool nightTime = currentHour >= EARLIEST_SLEEP_HOUR || currentHour < MORNING_START_HOUR;
                    caughtNight |= nightTime;
                    if ((!nightTime && Random.Range(0.0f, 1.0f) < DAY_WAKE_UP_RATIO_PER_HOUR && aTypeSub == (int)CornSleepType.INTERRUPTED)
                    || 1 <= cornSaveData.GetValue(CornPlayerConsumables.ENERGY) + realHourSlept * energyPerHour)
                        break;
                }

                Instance.m_sleepRewardsTemp.ConsumablesModifier[0].Value = realHourSlept * energyPerHour;

                if (realHourSlept == desiredSleepHours || 1 <= cornSaveData.GetValue(CornPlayerConsumables.ENERGY) + realHourSlept * energyPerHour)
                    aMessage = "You woke up feeling well rested!";
                else if (caughtNight)
                    aMessage = "You woke up early because of the sunlight, might as well start your day early, right?";
                else
                    aMessage = "You couldn't sleep that well because of the sunlight. Go to sleep at night for a good night's rest.";

                /*
                for (int i = 0; eventRewardsAndCost.ConsumablesModifier != null && i < eventRewardsAndCost.ConsumablesModifier.Length; i++)
                {
                    Instance.m_sleepRewardsTemp.ConsumablesModifier[i].Type = eventRewardsAndCost.ConsumablesModifier[i].Type;
                    Instance.m_sleepRewardsTemp.ConsumablesModifier[i].Value = realHourSlept * (eventRewardsAndCost.ConsumablesModifier[i].Value / desiredSleepHours);
                }

                for (int i = 0; eventRewardsAndCost.ResourcesModifier != null && i < eventRewardsAndCost.ResourcesModifier.Length; i++)
                {
                    Instance.m_sleepRewardsTemp.ResourcesModifier[i].Type = eventRewardsAndCost.ResourcesModifier[i].Type;
                    Instance.m_sleepRewardsTemp.ResourcesModifier[i].Value = realHourSlept * (eventRewardsAndCost.ResourcesModifier[i].Value / desiredSleepHours);
                }*/

                Instance.m_sleepRewardsTemp.HoursDuration = realHourSlept + 1;
                eventRewardsAndCost = Instance.m_sleepRewardsTemp;
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

    public void ApplyModifiersToPlayer(float aBonusMultiplier = 0)
    {
        CornManagerEvents.ApplyModifierResourceList(ResourcesModifier, ResourcesMultiplier, aBonusMultiplier);
        CornManagerEvents.ApplyModifierConsumablesList(ConsumablesModifier, ConsumablesMultiplier, aBonusMultiplier);
    }

    public bool CanAfford(float aBonusMultiplier = 0)
    {
        return CornManagerEvents.CanAfford(ConsumablesModifier, ResourcesMultiplier, aBonusMultiplier);
    }

    public bool CanAffordMoney(float aBonusMultiplier = 0)
    {
        return CornManagerEvents.CanAffordMoney(ConsumablesModifier, ResourcesMultiplier, aBonusMultiplier);
    }

    public void Preview(float aBonusMultiplier = 0)
    {
        foreach (CornPlayerConsumablesModifier modifier in ConsumablesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));

        foreach (CornPlayerResourcesModifier modifier in ResourcesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));
    }
}

[System.Serializable]
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