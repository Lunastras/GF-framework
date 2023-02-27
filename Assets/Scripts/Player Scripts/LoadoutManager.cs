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

    // public static readonly int MAX_ODAMA = 4;
    //public static readonly int MAX_LOADOUTS = 6;

    private List<WeaponBasic> m_weapons = null;

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
        AddLoadout(3);
        Debug.Log("The loadout count is: " + m_loadouts.Count);
        SetWeaponCapacity(4);

        ChangeLoadOutWeapon(0, 0, 1);
        ChangeLoadOutWeapon(0, 1, 2);
        ChangeLoadOutWeapon(0, 2, 1);
        ChangeLoadOutWeapon(0, 3, 2);




        SetCurrentLoadout(m_currentLoadOutIndex);
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
        return InternalChangeLoadOutWeapon(newWeapon, indexLoadout, indexWeapon, false);
    }

    private bool InternalChangeLoadOutWeapon(int indexLoadout, int indexWeapon, int newWeapon, bool refreshLoadout = true)
    {
        bool ret = false;
        if (indexLoadout < m_loadouts.Count)
        {
            int currentWeapon = -1;
            if (indexWeapon < m_loadouts[indexLoadout].Count)
                currentWeapon = m_loadouts[indexLoadout][indexWeapon].weapon;

            bool nullWeapon = newWeapon < 0;
            bool hasWeapon = nullWeapon || (m_infiniteInventory || 0 < m_weaponsInInventory[newWeapon]);

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

                        ret = true;
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
                            ret = true;
                        }
                    }
                    else
                    {
                        if (!m_infiniteInventory) m_weaponsInInventory[m_loadouts[indexLoadout][indexWeapon].weapon]++; //put back in inventory
                        m_loadouts[indexLoadout][indexWeapon] = new(newWeapon, m_loadouts[indexLoadout][indexWeapon].weaponExp);
                        ret = true;
                    }
                }

                if (ret && refreshLoadout && indexLoadout == m_currentLoadOutIndex)
                    SetCurrentLoadout(m_currentLoadOutIndex);
            }
        }

        return ret;
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

    public List<WeaponBasic> GetWeapons()
    {
        return m_weapons;
    }

    private WeaponBasic GetOdama(GameObject reference)
    {
        WeaponBasic objectToReturn = null;
        List<GameObject> objList = GfPooling.GetPoolList(reference);
        if (null != objList && 0 < objList.Count)
        {
            int listCount = objList.Count;
            while (--listCount >= 0)
            {
                GameObject obj = objList[listCount];
                WeaponBasic wb = obj.GetComponent<WeaponBasic>();
                if (!obj.activeSelf || wb.GetStatsCharacter() == m_statsCharacter) //check if it is inactive or if they have the same character stats
                {
                    obj.SetActive(true);
                    objectToReturn = wb;
                    objList.RemoveAt(listCount);
                    listCount = 0;
                }
            }
        }

        if (null == objectToReturn)
            objectToReturn = GfPooling.PoolInstantiate(reference).GetComponent<WeaponBasic>();

        return objectToReturn;
    }

    private void DestroyOdama(WeaponBasic weaponToDestroy)
    {
        weaponToDestroy.StopFiring();
        OdamaBehaviour ob = weaponToDestroy.GetComponent<OdamaBehaviour>();

        if (ob) ob.enabled = false;

        GameObject obj = weaponToDestroy.gameObject;
        obj.transform.position = DESTROY_POSITION;
        weaponToDestroy.disableWhenDone = true;
        GfPooling.DestroyInsert(obj, false, weaponToDestroy.IsAlive(true));
    }

    public void InternalSetCurrentLoadout(int indexLoadout)
    {
        if (m_loadouts.Count > 0)
        {
            m_currentLoadOutIndex = indexLoadout % m_loadouts.Count;

            int i = m_weapons.Count;
            while (--i >= 0)
            {
                DestroyOdama(m_weapons[i]);
                m_weapons.RemoveAt(i);
            }

            int weaponsCount = m_loadouts[m_currentLoadOutIndex].Count;
            float angleBetweenOdamas = 360.0f / weaponsCount;

            for (i = 0; i < weaponsCount; ++i)
            {
                GameObject desiredWeapon = WeaponMaster.GetWeapon(m_loadouts[m_currentLoadOutIndex][i].weapon);
                m_weapons.Add(GetOdama(desiredWeapon));
                m_weapons[i].destroyWhenDone = false;

                OdamaBehaviour ob = m_weapons[i].GetComponent<OdamaBehaviour>();
                ob.SetAngle(i * angleBetweenOdamas);
                ob.enabled = true;
                ob.SetParent(m_parentMovement);

                m_weapons[i].SetStatsCharacter(m_weaponFiring.GetStatsCharacter());
                ob.transform.position = transform.position;
            }

            //refresh the levelweapons
            RefreshExpForWeapons();
            m_weaponFiring.SetWeaponArray(m_weapons);
        }

    }

    public void SetCurrentLoadout(int index)
    {
        InternalSetCurrentLoadout(index);
    }

    public void SetWeaponCapacity(int newCapacity, bool fillEmptyWeapons = true, bool keepSameExp = true)
    {
        newCapacity = System.Math.Max(0, newCapacity);

        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            var loadout = m_loadouts[numLoadouts];
            int numWeapon = loadout.Count;
            //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);
            int weaponDiff = numWeapon - newCapacity;
            if (weaponDiff > 0) //remove extra weapons
            {
                loadout.RemoveRange(newCapacity - 1, weaponDiff);
            }
            else if (fillEmptyWeapons && weaponDiff < 0) //add extra weapons 
            {
                bool hasWeapon = numWeapon > 0;
                while (++weaponDiff <= 0)
                {
                    int indexOfCopy = hasWeapon ? loadout.Count % numWeapon : 0; //repeat the initial weapons for the refill
                    float exp = hasWeapon && keepSameExp ? loadout[indexOfCopy].weaponExp : 0;
                    int weapon = hasWeapon ? loadout[indexOfCopy].weapon : 0;
                    loadout.Add(new(weapon, exp));
                }
            }
        }

        if (m_weaponCapacity != newCapacity)
        {
            InternalSetCurrentLoadout(m_currentLoadOutIndex);
        }

        m_weaponCapacity = newCapacity;
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


