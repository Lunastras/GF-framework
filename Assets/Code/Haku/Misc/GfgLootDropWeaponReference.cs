using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgLootDropWeaponReference : MonoBehaviour
{
    [SerializeField]
    private GfgLootDropWeapon m_main = null;

    public GfgLootDropWeapon Main { get { return m_main; } }
}
