using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMaster : MonoBehaviour
{
    [SerializeField]
    public GameObject[] weapons;

    private static WeaponMaster instance = null;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
    }

    public static GameObject GetWeapon(int index)
    {
        if (0 > index || instance.weapons.Length <= index)
            return null;

        return instance.weapons[index];
    }

    public static int NumWeapons()
    {
        return instance.weapons.Length;
    }


}
