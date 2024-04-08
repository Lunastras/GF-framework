using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgManagerLootDropSpawner : MonoBehaviour
{
    void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;
    }

    protected static GfgManagerLootDropSpawner Instance;

    public GameObject PrefabWeaponDrop;

    public GameObject PrefabCharmDrop;

    public static GameObject SpawnRandomLoot(StatsCharacter aStatsCharacter)
    {
        ItemDrop itemDrop = GfgTiersThreatsBalancingStats.GetItemDrop(aStatsCharacter);

        GameObject lootObject = null;
        switch (itemDrop.ItemDropType)
        {
            case (ItemDropType.BOMB):
                Debug.LogWarning("BOMB drop not implemented");
                break;

            case (ItemDropType.BIG_BOMBS):
                Debug.LogWarning("BIG BOMB drop not implemented");

                break;

            case (ItemDropType.SMALL_HP):
                Debug.LogWarning("SMALL HP drop not implemented");

                break;

            case (ItemDropType.BIG_HP):
                Debug.LogWarning("BIG HP drop not implemented");

                break;

            case (ItemDropType.CHARM):
                EquipCharmData charmData = GfgManagerCharms.GetRandomCharmDrop(itemDrop);
                lootObject = GfcPooling.PoolInstantiate(Instance.PrefabCharmDrop, aStatsCharacter.transform.position, Quaternion.identity);
                lootObject.GetComponent<GfgLootDropCharmReference>().Main.SetEquipCharmData(charmData);
                break;

            case (ItemDropType.WEAPON):
                WeaponData weaponData = GfgManagerWeapons.GetRandomWeaponPickupData(itemDrop);
                lootObject = GfcPooling.PoolInstantiate(Instance.PrefabWeaponDrop, aStatsCharacter.transform.position, Quaternion.identity);
                lootObject.GetComponent<GfgLootDropWeaponReference>().Main.SetWeaponData(weaponData);
                break;
        }

        return lootObject;
    }
}
