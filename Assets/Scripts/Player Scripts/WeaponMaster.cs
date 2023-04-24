using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMaster : MonoBehaviour
{
    [SerializeField]
    public GameObject[] m_weapons;

    private static WeaponMaster m_instance = null;

    void Awake()
    {
        if (m_instance != null)
        {
            Destroy(m_instance);
        }

        m_instance = this;
    }

    public static GameObject GetWeapon(int index)
    {
        if (0 > index || m_instance.m_weapons.Length <= index)
            return m_instance.m_weapons[0];

        return m_instance.m_weapons[index];
    }

    public static int NumWeapons()
    {
        return m_instance.m_weapons.Length;
    }
}
