using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This class is used to keep track of every single ReusableParticleWeapon instance there is
*/
public class ReusableParticleSystemManager : MonoBehaviour
{
    //List of all the faux particle weapons
    private static List<ReusableParticleWeapon> m_fakeWeapons = null;
    // Start is called before the first frame update
    void Awake()
    {
        m_fakeWeapons = new(8);
    }

    public static void AddWeapon(ReusableParticleWeapon obj)
    {
        if (-1 == obj.GetReusableParticleSystemIndex()) // make sure they aren't in the list
        {
            m_fakeWeapons.Add(obj);
            obj.SetReusableParticleSystemIndex(m_fakeWeapons.Count - 1);
        }
    }

    public static ReusableParticleWeapon GetWeapon(int index)
    {
        return m_fakeWeapons[index];
    }

    public static void RemoveWeapon(ReusableParticleWeapon obj)
    {
        RemoveWeapon(obj.GetReusableParticleSystemIndex());
    }

    public static void RemoveWeapon(int index)
    {
        int last = m_fakeWeapons.Count - 1;

        if (-1 != index)
        {
            m_fakeWeapons[index].SetReusableParticleSystemIndex(-1);
            m_fakeWeapons[index] = m_fakeWeapons[last];
            m_fakeWeapons[index].SetReusableParticleSystemIndex(index);
            m_fakeWeapons.RemoveAt(last);
        }
    }
}
