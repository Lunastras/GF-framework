using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LoadoutManager : MonoBehaviour
{
    [SerializeField]
    private GfMovementGeneric m_parentMovement;

    [SerializeField]
    private WeaponFiring m_weaponFiring;

    [SerializeField]
    public HudManager m_hudManager = null;

    private int[] m_weaponsInInventory;

    private int m_odamaCapacity = 2;

    private List<WeaponLoadOut> m_loadOuts;

    private int m_currentLoadOutIndex = 0;


    //if -1, all loadouts are empty
    private int m_indexOfLastLoadout = -1;

   // public static readonly int MAX_ODAMA = 4;
    //public static readonly int MAX_LOADOUTS = 6;

    private List<WeaponBasic> m_weapons = null;

    private int m_numWeapons = 0;

    private Dictionary<String, List<GameObject>> m_inactiveOdamas;

    private static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);


    // Start is called before the first frame update
    void Start()
    {
        m_hudManager.SetMaxNumSliders(4); //delme

        m_weapons = new(4);

        if (m_weaponFiring == null)
        {
            m_weaponFiring = GetComponent<WeaponFiring>();
        }

        if (null == m_parentMovement)
        {
            m_parentMovement = m_weaponFiring.GetComponent<GfMovementGeneric>();
        }

        m_weaponsInInventory = new int[WeaponMaster.NumWeapons()];

        m_inactiveOdamas = new(10);

        m_loadOuts = new(4);

        if (WeaponMaster.NumWeapons() > 0 && m_weaponsInInventory[0] == 0)
            AddOdamaToInventory(0);

        if (-1 == m_indexOfLastLoadout)
        {
            m_currentLoadOutIndex = 0;

            SetCurrentLoadout(m_currentLoadOutIndex);
            ChangeLoadOutWeapon(0, 0, 0);
        }

        SetCurrentLoadout(m_currentLoadOutIndex);

        Test();
    }

    private void Test()
    {
        IncreaseOdamaCapacity(3);

        AddOdamaToInventory(1);
        AddOdamaToInventory(1);
        AddOdamaToInventory(1);
        AddOdamaToInventory(0);
        AddOdamaToInventory(0);
        AddOdamaToInventory(0);

        ChangeLoadOutWeapon(1, 1, 1);
        ChangeLoadOutWeapon(1, 1, 2);
        ChangeLoadOutWeapon(1, 1, 3);
        ChangeLoadOutWeapon(0, 1, 3);
        ChangeLoadOutWeapon(0, 1, 1);

        ChangeLoadOutWeapon(0, 0, 2);
        ChangeLoadOutWeapon(0, 0, 3);

        ChangeLoadOutWeapon(1, 2, 3);


        //SetCurrentLoadout(1);
        //  Debug.Log(" ");  

        // ChangeLoadOutWeapon(null, 0, 2);
    }



    private void InternalChangeLoadOutWeapon(int newWeapon, int loadOutIndex, int weaponIndex)
    {
        //Debug.Log("I got here! weapon chaneg loadout");

        bool loadoutWasEmpty = m_loadOuts[loadOutIndex].NoWeapons();
        bool loadoutIsEmpty;

        m_loadOuts[loadOutIndex].SetWeapon(newWeapon, weaponIndex);

        loadoutIsEmpty = m_loadOuts[loadOutIndex].NoWeapons();

        if (loadoutIsEmpty != loadoutWasEmpty)
        {
            if (loadoutWasEmpty)
            {
                WeaponLoadOut aux = m_loadOuts[++m_indexOfLastLoadout];
                m_loadOuts[m_indexOfLastLoadout] = m_loadOuts[loadOutIndex];
                m_loadOuts[loadOutIndex] = aux;
            }
            else
            {
                WeaponLoadOut aux = m_loadOuts[loadOutIndex];
                m_loadOuts[loadOutIndex] = m_loadOuts[m_indexOfLastLoadout];
                m_loadOuts[--m_indexOfLastLoadout] = aux;

            }
        }

        if (loadOutIndex == m_currentLoadOutIndex)
            SetCurrentLoadout(m_currentLoadOutIndex);
    }

    public void ChangeLoadOutWeapon(int newWeapon, int loadOutIndex, int weaponIndex)
    {
        bool canAddOdama = !(-1 == newWeapon && m_weaponsInInventory[newWeapon] > 0);

        loadOutIndex %= m_loadOuts.Count;

        int currentWeapon = m_loadOuts[loadOutIndex].weapons[weaponIndex];

        if (canAddOdama && newWeapon != currentWeapon)
        {
            InternalChangeLoadOutWeapon(newWeapon, loadOutIndex, weaponIndex);
        }
    }

    public void NextLoadout()
    {
        SetCurrentLoadout(m_currentLoadOutIndex + 1);
    }

    public void PreviousLoadout()
    {
        SetCurrentLoadout(m_currentLoadOutIndex + 1);
    }

    private GameObject GetOdama(GameObject reference)
    {
        GameObject objectToReturn = null;
        if (m_inactiveOdamas.TryGetValue(reference.name, out List<GameObject> list) && list.Count > 0)
        {
            int count = list.Count;
            objectToReturn = list[count];
            list.RemoveAt(count);
        }

        if (null == objectToReturn)
            objectToReturn = GfPooling.PoolInstantiate(reference);

        return objectToReturn;
    }

    private void DestroyOdama(WeaponBasic weaponToDestroy)
    {
        if (weaponToDestroy.IsAlive(true))
        {
            weaponToDestroy.destroyWhenDone = true;
            m_inactiveOdamas.TryGetValue(weaponToDestroy.name, out List<GameObject> list);
            if (null == list)
                m_inactiveOdamas.Add(weaponToDestroy.name, new(4));

            weaponToDestroy.transform.parent = null;
            weaponToDestroy.transform.position = DESTROY_POSITION;
        }
        else
        {
            GfPooling.DestroyInsert(weaponToDestroy.gameObject);
        }
    }

    public void InternalSetCurrentLoadout(int index)
    {
        m_currentLoadOutIndex = index % Math.Max(1, m_indexOfLastLoadout + 1);

        int i = 0;
        for (; i < m_numWeapons; ++i)
        {
            m_weapons[i].StopFiring();
            OdamaBehaviour ob = m_weapons[i].GetComponent<OdamaBehaviour>();

            if (ob)
                ob.enabled = false;

            DestroyOdama(m_weapons[i]);
        }

        m_numWeapons = m_loadOuts[m_currentLoadOutIndex].NumWeapons();
        float angleBetweenOdamas = 360.0f / m_numWeapons;

        for (i = 0; i < m_numWeapons; ++i)
        {
            GameObject desiredWeapon = WeaponMaster.GetWeapon(m_loadOuts[m_currentLoadOutIndex].weapons[i]);
            m_weapons[i] = GetOdama(desiredWeapon).GetComponent<WeaponBasic>();
            m_weapons[i].destroyWhenDone = false;

            OdamaBehaviour ob = m_weapons[i].GetComponent<OdamaBehaviour>();
            ob.SetAngle(i * angleBetweenOdamas);
            ob.enabled = true;
            ob.SetParent(m_parentMovement);

            m_weapons[i].SetStatsCharacter(m_weaponFiring.GetStatsCharacter());
            m_weapons[i].transform.position = m_weaponFiring.transform.position;
        }

        //refresh the levelweapons
        RefreshExpForWeapons();
        m_weaponFiring.SetWeaponArray(m_weapons);
    }

    public void SetCurrentLoadout(int index)
    {
        InternalSetCurrentLoadout(index);
    }

    public void IncreaseOdamaCapacity(int count = 1)
    {
        m_odamaCapacity++;
    }

    public void AddOdamaToInventory(int weapon, int count = 1)
    {
        if (m_weaponsInInventory != null && null != WeaponMaster.GetWeapon(weapon))
        {
            m_weaponsInInventory[weapon] = count + m_weaponsInInventory[weapon];
        }
    }

    private void RefreshExpForWeapons()
    {
        WeaponLoadOut loadOut = m_loadOuts[m_currentLoadOutIndex];

        int numWeapon = loadOut.NumWeapons();
        //Debug.Log("Switched weapon, the weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            m_weapons[numWeapon].AddExpPoint(float.MinValue); //set exp to be 0
            loadOut.expWeapons[numWeapon] = m_weapons[numWeapon].AddExpPoint(loadOut.expWeapons[numWeapon]);
        }

        m_hudManager.UpdateWeaponSliders(m_weapons, m_loadOuts[m_currentLoadOutIndex].NumWeapons());
    }
    /**
    *   Adds exp points to the weapons equipped in this moment
    *   @param points The ammount of points to be added
    */
    public void AddExpPoints(float points)
    {
        WeaponLoadOut loadOut = m_loadOuts[m_currentLoadOutIndex];
        points /= Mathf.Max(loadOut.numOfExpWeapons, 1.0f);

        int numWeapon = loadOut.NumWeapons();
        //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            loadOut.expWeapons[numWeapon] = m_weapons[numWeapon].AddExpPoint(points);
        }

        m_hudManager.UpdateLevelWeaponSliders(m_weapons, m_loadOuts[m_currentLoadOutIndex].NumWeapons());
    }

    /** Adds a fixed percentage of progress relative to the 
   * exp required for the current and next level to all of the equipped weapons
   * @param percent The percentage of progress to add
   */
    public void AddExpPercent(float percent)
    {
        WeaponLoadOut loadOut = m_loadOuts[m_currentLoadOutIndex];
        int numWeapon = loadOut.NumWeapons();

        while (--numWeapon >= 0)
        {
            loadOut.expWeapons[numWeapon] = m_weapons[numWeapon].AddExpPercent(percent);
        }

        m_hudManager.UpdateLevelWeaponSliders(m_weapons, m_loadOuts[m_currentLoadOutIndex].NumWeapons());
    }

    public void UpdateWeaponHud()
    {
    }
}

public class WeaponLoadOut
{
    public List<int> weapons { get; private set; }

    //the numbers of weapons that use exp in the loadout
    public int numOfExpWeapons { get; private set; } = 0;

    public List<float> expWeapons;

    public WeaponLoadOut(int maxWeapons)
    {
        expWeapons = new(4);
        weapons = new(4);

        for (int i = 0; i < maxWeapons; ++i)
        {
            weapons[i] = -1;
            expWeapons[i] = 0;
        }
    }

    public void SetWeapon(int weapon, int index)
    {
        if (weapon != -1)
        {
            WeanponType weanponType = WeaponMaster.GetWeapon(weapon).GetComponent<WeaponBasic>().GetWeaponType();

            if (weapons[index] != -1)
            {
                WeanponType currentType = WeaponMaster.GetWeapon(weapons[index]).GetComponent<WeaponBasic>().GetWeaponType();
                weapons[index] = weapon;

                if (weanponType == WeanponType.EXPERIENCE && currentType != WeanponType.EXPERIENCE)
                    ++numOfExpWeapons;
                else if (weanponType != WeanponType.EXPERIENCE && currentType == WeanponType.EXPERIENCE)
                    --numOfExpWeapons;
            }
            else
            {
                weapons[++indexOfLastWeapon] = weapon;

                if (weanponType == WeanponType.EXPERIENCE)
                    ++numOfExpWeapons;
            }
        }
        else //if weapon is null
        {
            if (weapons[index] != -1)
            {
                WeanponType currentType = WeaponMaster.GetWeapon(weapons[index]).GetComponent<WeaponBasic>().GetWeaponType();

                if (currentType == WeanponType.EXPERIENCE)
                    --numOfExpWeapons;

                expWeapons[index] = expWeapons[indexOfLastWeapon];
                expWeapons[indexOfLastWeapon] = 0;

                weapons[index] = weapons[indexOfLastWeapon];
                weapons[indexOfLastWeapon] = -1;

                --indexOfLastWeapon;
            }
        }
    }

    public int NumWeapons()
    {
        return weapons.Count;
    }
}
