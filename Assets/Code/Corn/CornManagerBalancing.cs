using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornManagerBalancing : MonoBehaviour
{
    private static CornManagerBalancing Instance = null;

    [SerializeField] private CornEventCostAndRewardsSetter[] m_cornEventCostAndRewards = null;

    public const int LATEST_WAKE_UP_HOUR = 11;

    public const int EARLIEST_SLEEP_HOUR = 22;

    public const int MAX_CORN_ACTION_IN_ROW = 2;

    public const int DICE_ROLL_NUM_FACES = 20;

    private CornEventCostAndRewards m_sleepRewardsTemp;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);
        Instance = this;

        for (int i = 0; i < m_cornEventCostAndRewards.Length; ++i)
        {
            m_cornEventCostAndRewards[i].CostAndRewards.ConsumablesMultiplier = 1;
            m_cornEventCostAndRewards[i].CostAndRewards.ResourcesMultiplier = 1;

            if ((int)m_cornEventCostAndRewards[i].EventType != i)
                Debug.LogError("The values inside CornEventCostAndRewards are out of order. Element at index" + i + " should be at index " + (int)m_cornEventCostAndRewards[i].EventType + '.');
        }

        m_sleepRewardsTemp = m_cornEventCostAndRewards[(int)CornEventType.SLEEP].CostAndRewards;

        if (m_sleepRewardsTemp.ConsumablesModifier != null && m_sleepRewardsTemp.ConsumablesModifier.Length > 0)
            m_sleepRewardsTemp.ConsumablesModifier = new PlayerConsumablesModifier[m_sleepRewardsTemp.ConsumablesModifier.Length];
        if (m_sleepRewardsTemp.ResourcesModifier != null && m_sleepRewardsTemp.ResourcesModifier.Length > 0)
            m_sleepRewardsTemp.ResourcesModifier = new PlayerResourcesModifier[m_sleepRewardsTemp.ResourcesModifier.Length];
    }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, uint aTypeSub = 0) { return GetEventCostAndRewards(aType, out _, aTypeSub); }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, out string aMessage, uint aTypeSub = 0)
    {
        aMessage = null;
        CornEventCostAndRewards eventRewardsAndCost = Instance.m_cornEventCostAndRewards[(int)aType].CostAndRewards;
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        switch (aType)
        {
            case CornEventType.SLEEP:
                int currentHour = cornSaveData.CurrentHour;
                int realHourSlept = eventRewardsAndCost.HoursDuration - (((eventRewardsAndCost.HoursDuration + currentHour) % 24) - CornManagerBalancing.LATEST_WAKE_UP_HOUR).Max(0);
                if (realHourSlept != eventRewardsAndCost.HoursDuration)
                {
                    aMessage = "You slept less tonight, try going to sleep earlier to get well rested.";

                    for (int i = 0; eventRewardsAndCost.ConsumablesModifier != null && i < eventRewardsAndCost.ConsumablesModifier.Length; i++)
                    {
                        Instance.m_sleepRewardsTemp.ConsumablesModifier[i].Type = eventRewardsAndCost.ConsumablesModifier[i].Type;
                        Instance.m_sleepRewardsTemp.ConsumablesModifier[i].Value = 0.1f *
                                (eventRewardsAndCost.ConsumablesModifier[i].Value * 10.0f).Log(eventRewardsAndCost.HoursDuration).Pow(realHourSlept);
                    }

                    for (int i = 0; eventRewardsAndCost.ResourcesModifier != null && i < eventRewardsAndCost.ResourcesModifier.Length; i++)
                    {
                        Instance.m_sleepRewardsTemp.ResourcesModifier[i].Type = eventRewardsAndCost.ResourcesModifier[i].Type;
                        Instance.m_sleepRewardsTemp.ResourcesModifier[i].Value = 0.1f *
                                  (eventRewardsAndCost.ResourcesModifier[i].Value * 10.0f).Log(eventRewardsAndCost.HoursDuration).Pow(realHourSlept);
                    }
                }

                Instance.m_sleepRewardsTemp.HoursDuration = realHourSlept;
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
    public PlayerConsumablesModifier[] ConsumablesModifier;
    public PlayerResourcesModifier[] ResourcesModifier;
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

    public void Preview(float aBonusMultiplier = 0)
    {
        foreach (PlayerConsumablesModifier modifier in ConsumablesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));

        foreach (PlayerResourcesModifier modifier in ResourcesModifier)
            CornMenuApartment.Instance.PreviewChange(modifier, ConsumablesMultiplier * (aBonusMultiplier * modifier.BonusPercent * modifier.Value + modifier.Value));
    }
}

[System.Serializable]
internal struct CornEventCostAndRewardsSetter
{
    public CornEventType EventType;
    public CornEventCostAndRewards CostAndRewards;
}