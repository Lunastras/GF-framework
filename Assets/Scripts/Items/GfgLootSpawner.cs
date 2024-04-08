using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgLootSpawner : MonoBehaviour
{
    [SerializeField]
    protected StatsCharacter m_statsCharacter;

    void Awake()
    {
        if (null == m_statsCharacter) m_statsCharacter = GetComponent<StatsCharacter>();
    }

    public virtual GameObject SpawnLoot()
    {
        return GfgManagerLootDropSpawner.SpawnRandomLoot(m_statsCharacter);
    }
}
