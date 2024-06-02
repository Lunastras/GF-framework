using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornManagerBalancing : MonoBehaviour
{
    private static CornManagerBalancing Instance = null;
    [SerializeField] private CornEventCostAndRewardsSetter[] CornEventCostAndRewards = null;

    [SerializeField] private float m_gameProgressOnWork = 1.0f;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);
        Instance = this;

        for (int i = 0; i < CornEventCostAndRewards.Length; ++i)
        {
            CornEventCostAndRewards[i].CostAndRewards.ConsumablesMultiplier = 1;
            CornEventCostAndRewards[i].CostAndRewards.ResourcesMultiplier = 1;

            if ((int)CornEventCostAndRewards[i].EventType != i)
                Debug.LogError("The values inside CornEventCostAndRewards are out of order. Element at index" + i + " should be at index " + (int)CornEventCostAndRewards[i].EventType + '.');
        }
    }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEventType aType, uint aTypeSub = 0)
    {
        return Instance.CornEventCostAndRewards[(int)aType].CostAndRewards;
    }

    public static float GetGameProgressOnWork() { return Instance.m_gameProgressOnWork; }

    public static CornEventCostAndRewards GetEventCostAndRewards(CornEvent aEvent) { return GetEventCostAndRewards(aEvent.EventType, aEvent.EventTypeSub); }
}

[System.Serializable]
public struct CornEventCostAndRewards
{
    public PlayerConsumablesModifier[] ConsumablesModifier;
    public PlayerResourcesModifier[] ResourcesModifier;
    public int HoursDuration;
    public bool EventHasCornRoll;

    public float ConsumablesMultiplier;
    public float ResourcesMultiplier;

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