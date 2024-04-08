using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgLootDropCharmReference : MonoBehaviour
{
    [SerializeField]
    private GfgLootDropCharm m_main;

    public GfgLootDropCharm Main { get { return m_main; } }
}
