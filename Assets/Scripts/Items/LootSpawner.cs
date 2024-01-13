using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    [SerializeField]
    protected StatsCharacter m_statsCharacter;

    void Awake()
    {
        if (null == m_statsCharacter) m_statsCharacter = GetComponent<StatsCharacter>();
    }

    public bool SpawnLoot()
    {
        ItemDrop itemDrop = TiersThreatsBalancingStats.GetItemDrop(m_statsCharacter);

        return itemDrop.ItemDropType != ItemDropType.NONE;
    }
}
