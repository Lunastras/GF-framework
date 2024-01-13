using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerWeapons : MonoBehaviour
{
    [SerializeField]
    public GameObject[] m_weapons;

    [SerializeField]
    public WeaponTier[] m_weaponTiers;

    private static ManagerWeapons OurInstance = null;

    void Awake()
    {
        if (OurInstance != null)
        {
            Destroy(OurInstance);
        }

        OurInstance = this;
    }

    public static GameObject GetWeapon(int index)
    {
        if (0 > index || OurInstance.m_weapons.Length <= index)
            return OurInstance.m_weapons[0];

        return OurInstance.m_weapons[index];
    }

    public static int NumWeapons()
    {
        return OurInstance.m_weapons.Length;
    }

    public static WeaponInventoryData GetRandomWeaponInventoryData(int tier, bool blessed)
    {
        int countWeapons = blessed ? OurInstance.m_weaponTiers[tier].BlessedWeaponPrefabs.Length : OurInstance.m_weaponTiers[tier].WeaponPrefabs.Length;
        return new()
        {
            Index = Random.Range(0, countWeapons - 1),
            Tier = tier,
            Blessed = blessed
        };
    }

    static public GameObject GetWeapon(WeaponInventoryData data)
    {
        return data.Blessed ? OurInstance.m_weaponTiers[data.Tier].BlessedWeaponPrefabs[data.Index] : OurInstance.m_weaponTiers[data.Tier].WeaponPrefabs[data.Index];
    }

    public static string GetWeaponName(WeaponInventoryData data)
    {
        return GetWeapon(data).name;
    }

    public static Color GetTierColor(uint tier)
    {
        return OurInstance.m_weaponTiers[tier].TierColor;
    }
}

[System.Serializable]
public struct WeaponTier
{
    public GameObject[] WeaponPrefabs;
    public GameObject[] BlessedWeaponPrefabs;
    public Color TierColor;
}

[System.Serializable]
public struct WeaponInventoryData
{
    public int Index;
    public int Tier;
    public bool Blessed;
}
