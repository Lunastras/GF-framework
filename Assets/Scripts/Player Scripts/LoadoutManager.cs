using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LoadoutManager : MonoBehaviour
{
    [SerializeField]
    private GfMovementGeneric m_parentMovement;

    [SerializeField]
    private StatsCharacter m_statsCharacter;

    [SerializeField]
    private WeaponFiring m_weaponFiring;

    [SerializeField]
    public HudManager m_hudManager = null;

    private int[] m_weaponsInInventory;

    [SerializeField]
    private bool m_infiniteInventory = true;

    private int m_weaponCapacity = 2;

    private List<List<WeaponData>> m_loadouts;

    private int m_currentLoadOutIndex = 0;

    private float m_weaponCapacityMultiplier = 1;
    private float m_speedMultiplier = 1;
    private float m_damageMultiplier = 1;
    private float m_fireRateMultiplier = 1;

    // public static readonly int MAX_ODAMA = 4;
    //public static readonly int MAX_LOADOUTS = 6;

    private List<WeaponLevels> m_weapons = null;

    private static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);


    // Start is called before the first frame update
    void Start()
    {
        m_hudManager.SetMaxNumSliders(1); //delme

        m_weapons = new(4);

        if (m_weaponFiring == null)
        {
            m_weaponFiring = GetComponent<WeaponFiring>();
        }

        if (null == m_parentMovement)
        {
            m_parentMovement = GetComponent<GfMovementGeneric>();
        }

        if (null == m_statsCharacter)
        {
            m_statsCharacter = GetComponent<StatsCharacter>();
        }

        m_weaponsInInventory = new int[WeaponMaster.NumWeapons()];

        m_loadouts = new(4);

        if (WeaponMaster.NumWeapons() > 0 && m_weaponsInInventory[0] == 0)
            AddOdamaToInventory(0);

        SetCurrentLoadout(m_currentLoadOutIndex);

        Test();
    }

    private void Test()
    {
        SetWeaponCapacity(3);
        AddLoadout(3);

        ChangeLoadOutWeapon(0, 0, 1);
        ChangeLoadOutWeapon(0, 1, 1);
        ChangeLoadOutWeapon(0, 2, 1);

        ChangeLoadOutWeapon(1, 0, 2);
        ChangeLoadOutWeapon(1, 1, 0);
        ChangeLoadOutWeapon(1, 2, 2);
        //ChangeLoadOutWeapon(0, 2, 2);

        SetCurrentLoadout(0);
    }

    public bool ChangeLoadoutWeapon(int indexLoadout, int newWeapon, bool fillToCapacity)
    {
        bool ret = false;
        if (indexLoadout < m_loadouts.Count)
        {
            int numWeapons = fillToCapacity ? m_weaponCapacity : m_loadouts[indexLoadout].Count;
            bool nullWeapon = newWeapon < 0;
            int weaponIndex;

            while (--numWeapons >= 0)
            {
                weaponIndex = (nullWeapon || m_infiniteInventory || 0 < m_weaponsInInventory[newWeapon]) ? newWeapon : -1;
                ret |= InternalChangeLoadOutWeapon(indexLoadout, numWeapons, newWeapon);
            }

        }

        if (ret && indexLoadout == m_currentLoadOutIndex)
            SetCurrentLoadout(m_currentLoadOutIndex);

        return ret;
    }

    public bool ChangeLoadOutWeapon(int indexLoadout, int indexWeapon, int newWeapon)
    {
        return InternalChangeLoadOutWeapon(indexLoadout, indexWeapon, newWeapon, false);
    }

    private bool InternalChangeLoadOutWeapon(int indexLoadout, int indexWeapon, int newWeapon, bool refreshLoadout = true)
    {
        bool changedWeapon = false;
        if (indexLoadout < m_loadouts.Count)
        {
            int currentWeapon = -1;
            if (indexWeapon < m_loadouts[indexLoadout].Count)
                currentWeapon = m_loadouts[indexLoadout][indexWeapon].weapon;

            bool nullWeapon = newWeapon < 0;
            bool hasWeapon = nullWeapon || (newWeapon < WeaponMaster.NumWeapons() && (m_infiniteInventory || 0 < m_weaponsInInventory[newWeapon]));

            if (hasWeapon && newWeapon != currentWeapon) //if negative, remove
            {
                if (newWeapon < 0) // if we need to remove weapon
                {
                    if (currentWeapon != -1) // if weapon exists
                    {
                        if (!m_infiniteInventory) m_weaponsInInventory[m_loadouts[indexLoadout][indexWeapon].weapon]++; //put back in inventory
                        m_loadouts[indexLoadout].RemoveAt(indexWeapon);
                        if (m_loadouts.Count == 0)
                            m_loadouts.RemoveAt(indexLoadout);

                        changedWeapon = true;
                    }
                }
                else //add weapon to loadout
                {
                    if (!m_infiniteInventory) m_weaponsInInventory[newWeapon]--;

                    if (currentWeapon == -1) // no weapon found
                    {
                        if (m_loadouts[indexLoadout].Count < m_weaponCapacity)
                        {
                            m_loadouts[indexLoadout].Add(new(newWeapon));
                            changedWeapon = true;
                        }
                    }
                    else
                    {
                        if (!m_infiniteInventory) m_weaponsInInventory[m_loadouts[indexLoadout][indexWeapon].weapon]++; //put back in inventory
                        m_loadouts[indexLoadout][indexWeapon] = new(newWeapon, m_loadouts[indexLoadout][indexWeapon].weaponExp);
                        changedWeapon = true;
                    }
                }

                if (changedWeapon && refreshLoadout && indexLoadout == m_currentLoadOutIndex)
                    SetCurrentLoadout(m_currentLoadOutIndex);
            }
        }

        return changedWeapon;
    }

    private WeaponLevels GetOdama(GameObject reference)
    {
        WeaponLevels objectToReturn = null;

        List<GameObject> objList = GfPooling.GetPoolList(reference);
        int listCount = 0, index = 0;
        if (null != objList) listCount = objList.Count;
        index = listCount;
        while (0 <= --index)
        {
            GameObject obj = objList[index];
            WeaponLevels wb = obj.GetComponent<WeaponLevels>();
            if (!obj.activeSelf || wb.GetStatsCharacter() == m_statsCharacter) //check if it is inactive or if they have the same character stats
            {
                obj.SetActive(true);
                objectToReturn = wb;

                objList[index] = objList[--listCount];
                objList.RemoveAt(listCount);

                if (wb.GetStatsCharacter() != m_statsCharacter) wb.SetStatsCharacter(m_statsCharacter);
                index = 0;
            }
        }

        if (null == objectToReturn)
        {
            objectToReturn = GfPooling.PoolInstantiate(reference).GetComponent<WeaponLevels>();
        }

        return objectToReturn;
    }

    private void DestroyOdama(WeaponLevels weaponToDestroy)
    {
        weaponToDestroy.StopFiring();
        OdamaBehaviour ob = weaponToDestroy.GetComponent<OdamaBehaviour>();

        if (ob) ob.enabled = false;

        GameObject obj = weaponToDestroy.gameObject;
        weaponToDestroy.disableWhenDone = true;
        GfPooling.DestroyInsert(obj, weaponToDestroy.IsAlive(true));
    }

    public void InternalSetCurrentLoadout(int indexLoadout)
    {
        m_currentLoadOutIndex = indexLoadout % m_loadouts.Count;

        int i = m_weapons.Count;
        while (--i >= 0)
        {
            m_weapons[i].SetSpeedMultiplier(1);
            m_weapons[i].SetFireRateMultiplier(1);
            m_weapons[i].SetDamageMultiplier(1);
            m_weapons[i].SetLoadoutCount(1);
            m_weapons[i].SetLoadoutWeaponIndex(0);

            DestroyOdama(m_weapons[i]);
            m_weapons.RemoveAt(i);
        }

        int weaponsCount = m_loadouts[m_currentLoadOutIndex].Count;
        float angleBetweenOdamas = 360.0f / weaponsCount;

        m_weaponFiring.ClearWeapons();

        for (i = 0; i < weaponsCount; ++i)
        {
            GameObject desiredWeapon = WeaponMaster.GetWeapon(m_loadouts[m_currentLoadOutIndex][i].weapon);
            m_weapons.Add(GetOdama(desiredWeapon));
            m_weapons[i].destroyWhenDone = false;

            OdamaBehaviour ob = m_weapons[i].GetComponent<OdamaBehaviour>();
            ob.SetAngle(i * angleBetweenOdamas);
            ob.enabled = true;
            ob.SetParent(m_parentMovement);

            m_weapons[i].SetSpeedMultiplier(m_speedMultiplier);
            m_weapons[i].SetFireRateMultiplier(m_fireRateMultiplier);
            m_weapons[i].SetDamageMultiplier(m_damageMultiplier);
            m_weapons[i].SetLoadoutCount(weaponsCount);
            m_weapons[i].SetLoadoutWeaponIndex(i);


            m_weapons[i].SetStatsCharacter(m_weaponFiring.GetStatsCharacter());
            ob.transform.position = transform.position;
            m_weaponFiring.SetWeapon(m_weapons[i], i);
        }

        //refresh the levelweapons
        RefreshExpForWeapons();
    }

    public void SetCurrentLoadout(int index)
    {
        InternalSetCurrentLoadout(index);
    }

    public void SetWeaponCapacity(int newCapacity, bool repeatWeapons = true, bool keepSameExp = true)
    {
        if (newCapacity != m_weaponCapacity)
        {
            m_weaponCapacity = newCapacity;
            int effectiveCapacity = (int)System.Math.Round(newCapacity * m_weaponCapacityMultiplier);
            InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSameExp);
        }
    }

    private void InternalSetWeaponCapacity(int newCapacity, bool repeatWeapons, bool keepSameExp)
    {
        newCapacity = System.Math.Max(0, newCapacity);

        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            var loadout = m_loadouts[numLoadouts];
            int numWeapon = loadout.Count;

            int weaponDiff = numWeapon - newCapacity;
            if (weaponDiff > 0) //remove extra weapons
            {
                //loadout.RemoveRange(newCapacity - 1, weaponDiff);
                while (--weaponDiff >= 0)
                {
                    int lastIndex = numWeapon - weaponDiff - 1;
                    if (!m_infiniteInventory) m_weaponsInInventory[lastIndex]++;
                    loadout.RemoveAt(lastIndex);
                }
            }
            else if (repeatWeapons && weaponDiff < 0) //add extra weapons 
            {
                bool hasWeapon = numWeapon > 0;
                while (++weaponDiff <= 0)
                {
                    int indexOfCopy = hasWeapon ? loadout.Count % numWeapon : 0; //repeat the initial weapons for the refill
                    float exp = hasWeapon && keepSameExp ? loadout[indexOfCopy].weaponExp : 0;
                    int weapon = hasWeapon ? loadout[indexOfCopy].weapon : 0;
                    bool isInInventory = 0 < m_weaponsInInventory[weapon];

                    if (m_infiniteInventory || isInInventory)
                        loadout.Add(new(weapon, exp));

                    if (!m_infiniteInventory && isInInventory) m_weaponsInInventory[weapon]--;
                }
            }
        }

        if (m_weaponCapacity != newCapacity)
        {
            InternalSetCurrentLoadout(m_currentLoadOutIndex);
        }
    }

    public int GetWeaponCapacity()
    {
        return m_weaponCapacity;
    }

    public void AddOdamaToInventory(int weapon, int count = 1)
    {
        if (weapon >= 0 && m_weaponsInInventory != null && null != WeaponMaster.GetWeapon(weapon))
        {
            m_weaponsInInventory[weapon] = count + m_weaponsInInventory[weapon];
        }
    }

    private void RefreshExpForWeapons()
    {
        List<WeaponData> loadOut = m_loadouts[m_currentLoadOutIndex];

        int numWeapon = loadOut.Count;
        //Debug.Log("Switched weapon, the weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            m_weapons[numWeapon].AddExpPoint(float.MinValue); //set exp to be 0
            loadOut[numWeapon] = new(loadOut[numWeapon].weapon, m_weapons[numWeapon].AddExpPoint(loadOut[numWeapon].weaponExp));
        }

        m_hudManager.UpdateWeaponSliders(m_weapons);
    }

    public void AddExpPointsAll(float points)
    {
        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            AddExpPoints(points, numLoadouts);
        }
    }

    public void AddExpPercentAll(float points)
    {
        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            AddExpPercent(points, numLoadouts);
        }
    }
    /**
    *   Adds exp points to the weapons equipped in this moment
    *   @param points The ammount of points to be added
    */
    public void AddExpPoints(float points, int loadoutIndex = -1)
    {
        if (loadoutIndex < 0) loadoutIndex = m_currentLoadOutIndex;

        List<WeaponData> loadOut = m_loadouts[m_currentLoadOutIndex];
        int numWeapon = loadOut.Count;
        //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            loadOut[numWeapon] = new(loadOut[numWeapon].weapon, m_weapons[numWeapon].AddExpPoint(points));
        }

        m_hudManager.UpdateLevelWeaponSliders(m_weapons);
    }

    /** Adds a fixed percentage of progress relative to the 
   * exp required for the current and next level to all of the equipped weapons
   * @param percent The percentage of progress to add
   */
    public void AddExpPercent(float percent, int loadoutIndex = -1)
    {
        if (loadoutIndex < 0) loadoutIndex = m_currentLoadOutIndex;

        List<WeaponData> loadOut = m_loadouts[m_currentLoadOutIndex];
        int numWeapon = loadOut.Count;

        while (--numWeapon >= 0)
        {
            loadOut[numWeapon] = new(loadOut[numWeapon].weapon, m_weapons[numWeapon].AddExpPercent(percent));
        }

        m_hudManager.UpdateLevelWeaponSliders(m_weapons);
    }

    public void AddLoadout(int count = 1)
    {
        while (--count >= 0)
            m_loadouts.Add(new(m_weaponCapacity));
    }

    public void RemoveLoadout(int index)
    {
        m_loadouts.RemoveAt(index);
    }

    public List<WeaponData> GetCurrentLoadout()
    {
        return m_loadouts[m_currentLoadOutIndex];
    }

    public List<WeaponData> GetLoadout(int indexLoadout)
    {
        return m_loadouts[indexLoadout];
    }

    public List<List<WeaponData>> GetLoadoutsList()
    {
        return m_loadouts;
    }

    public int GetCurrentLoadoutIndex()
    {
        return m_currentLoadOutIndex;
    }

    public int GetCountLoadouts()
    {
        return m_loadouts.Count;
    }

    public int GetCountWeapons(int indexLoadout)
    {
        return m_loadouts[indexLoadout].Count;
    }

    public int GetWeaponId(int indexLoadout, int indexWeapon)
    {
        return m_loadouts[indexLoadout][indexWeapon].weapon;
    }

    public float GetWeaponExp(int indexLoadout, int indexWeapon)
    {
        return m_loadouts[indexLoadout][indexWeapon].weaponExp;
    }

    public void RemoveWeapon(int indexLoadout, int indexWeapon)
    {
        ChangeLoadOutWeapon(-1, indexLoadout, indexWeapon);
    }

    public void NextLoadout()
    {
        SetCurrentLoadout(m_currentLoadOutIndex + 1);
    }

    public void PreviousLoadout()
    {
        SetCurrentLoadout(m_currentLoadOutIndex + 1);
    }

    public List<WeaponLevels> GetWeapons()
    {
        return m_weapons;
    }


    public virtual float GetSpeedMultiplier() { return m_speedMultiplier; }
    public virtual void SetSpeedMultiplier(float multiplier)
    {
        m_speedMultiplier = multiplier; //todo
        int weaponCount = m_weapons.Count;
        for (int i = 0; i < weaponCount; ++i)
        {
            m_weapons[i].SetSpeedMultiplier(multiplier);
        }
    }

    public virtual float GetFireRateMultiplier() { return m_fireRateMultiplier; }
    public virtual void SetFireRateMultiplier(float multiplier)
    {
        m_fireRateMultiplier = multiplier; //todo
        int weaponCount = m_weapons.Count;
        for (int i = 0; i < weaponCount; ++i)
        {
            m_weapons[i].SetFireRateMultiplier(multiplier);
        }
    }

    public virtual float GetDamageMultiplier() { return m_damageMultiplier; }
    public virtual void SetDamageMultiplier(float multiplier)
    {
        m_damageMultiplier = multiplier; //todo
        int weaponCount = m_weapons.Count;
        for (int i = 0; i < weaponCount; ++i)
        {
            m_weapons[i].SetDamageMultiplier(multiplier);
        }
    }

    public virtual float GetLoadoutSizeMultiplier() { return m_weaponCapacityMultiplier; }
    public virtual void SetLoadoutSizeMultiplier(float multiplier, bool repeatWeapons = true, bool keepSameExp = true)
    {
        if (multiplier != m_weaponCapacityMultiplier)
        {
            int effectiveCapacity = (int)System.Math.Round(m_weaponCapacity * m_weaponCapacityMultiplier);
            InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSameExp);
        }
    }
}

public struct WeaponData
{
    public int weapon;
    public float weaponExp;

    public WeaponData(int weapon = 0, float weaponExp = 0)
    {
        this.weapon = weapon;
        this.weaponExp = weaponExp;
    }
}


