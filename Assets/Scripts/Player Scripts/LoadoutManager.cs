using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Netcode;

using System.Runtime.InteropServices;

public class LoadoutManager : NetworkBehaviour
{
    [SerializeField]
    protected GfMovementGeneric m_parentMovement;

    [SerializeField]
    protected StatsCharacter m_statsCharacter;

    [SerializeField]
    protected WeaponFiring m_weaponFiring;

    protected int[] m_weaponsInInventory;

    [SerializeField]
    protected bool m_infiniteInventory = true;

    [SerializeField]
    protected HudManager m_hudManager = null;

    protected int m_weaponCapacity = 2;

    protected List<List<WeaponData>> m_loadouts;

    protected int m_currentLoadOutIndex = 0;

    protected static int m_countWeaponTypes = -1;

    protected PriorityValue<float> m_weaponCapacityMultiplier = new(1);
    protected PriorityValue<float> m_speedMultiplier = new(1);
    protected PriorityValue<float> m_damageMultiplier = new(1);
    protected PriorityValue<float> m_fireRateMultiplier = new(1);

    protected List<WeaponBasic> m_weapons = null;

    protected static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);

    protected virtual void InternalStart() { }

    // Start is called before the first frame update
    void Start()
    {
        InternalStart();

        if (-1 == m_countWeaponTypes)
            m_countWeaponTypes = Enum.GetValues(typeof(CharacterTypes)).Length;

        m_weapons = new(4);

        if (null == m_weaponFiring)
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
            AddWeaponToInventory(0);

        SetCurrentLoadout(m_currentLoadOutIndex);

        if (IsOwner) m_hudManager = GameManager.GetHudManager();

        Test();
    }

    private void Test()
    {
        SetWeaponCapacity(4);
        AddLoadout(3);

        SetLoadoutWeapon(0, 0, 1);
        SetLoadoutWeapon(0, 1, 1);
        SetLoadoutWeapon(0, 2, 1);

        SetLoadoutWeapon(1, 0, 2);
        SetLoadoutWeapon(1, 1, 2);
        SetLoadoutWeapon(1, 2, 2);

        SetLoadoutWeapon(2, 0, 3);
        SetLoadoutWeapon(2, 1, 3);

        SetCurrentLoadout(0);
    }

    public bool SetLoadoutWeapon(int indexLoadout, int newWeapon, bool fillToCapacity)
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
                ret |= InternalSetLoadoutWeapon(indexLoadout, numWeapons, newWeapon);
            }

        }

        if (ret && indexLoadout == m_currentLoadOutIndex)
            SetCurrentLoadout(m_currentLoadOutIndex);

        return ret;
    }

    public bool SetLoadoutWeapon(int indexLoadout, int indexWeapon, int newWeapon)
    {
        return InternalSetLoadoutWeapon(indexLoadout, indexWeapon, newWeapon, false);
    }

    private unsafe bool InternalSetLoadoutWeapon(int indexLoadout, int indexWeapon, int newWeapon, bool refreshLoadout = true)
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

                        m_loadouts[indexLoadout][indexWeapon] = new(m_loadouts[indexLoadout][indexWeapon]);
                        changedWeapon = true;
                    }
                }

                if (changedWeapon && refreshLoadout && indexLoadout == m_currentLoadOutIndex)
                    SetCurrentLoadout(m_currentLoadOutIndex);
            }
        }

        return changedWeapon;
    }

    private WeaponBasic GetWeapon(GameObject reference)
    {
        WeaponBasic objectToReturn = null;

        List<GameObject> objList = GfPooling.GetPoolList(reference);
        int listCount = 0, index = 0;
        if (null != objList) listCount = objList.Count;
        index = listCount;
        while (0 <= --index)
        {
            GameObject obj = objList[index];
            WeaponBasic wb = obj.GetComponent<WeaponBasic>();
            if (!obj.activeSelf || wb.GetStatsCharacter() == m_statsCharacter) //check if it is inactive or if they have the same character stats
            {
                obj.SetActive(true);
                objectToReturn = wb;

                objList[index] = objList[--listCount];
                objList.RemoveAt(listCount);

                index = 0;
            }
        }

        if (null == objectToReturn)
        {
            objectToReturn = GfPooling.PoolInstantiate(reference).GetComponent<WeaponBasic>();
        }

        return objectToReturn;
    }

    private void DestroyWeapon(WeaponBasic weaponToDestroy)
    {
        weaponToDestroy.StopFiring();
        weaponToDestroy.SetMovementParent(null);
        weaponToDestroy.WasSwitchedOff();

        GameObject obj = weaponToDestroy.gameObject;
        weaponToDestroy.DisableWhenDone = true;
        bool isAlive = weaponToDestroy.IsAlive();
        GfPooling.DestroyInsert(obj, isAlive);
    }

    protected virtual void OnWeaponSet(WeaponBasic weapon) { }

    protected virtual void OnWeaponsCleared() { }


    private void InternalSetCurrentLoadout(int indexLoadout)
    {
        if (null != m_loadouts && 0 < m_loadouts.Count)
        {
            m_currentLoadOutIndex = indexLoadout % m_loadouts.Count;

            int i = m_weapons.Count;
            while (--i >= 0)
            {
                DestroyWeapon(m_weapons[i]);
                m_weapons.RemoveAt(i);
            }

            OnWeaponsCleared();
            m_weaponFiring.ClearWeapons();

            int weaponsCount = m_loadouts[m_currentLoadOutIndex].Count;

            for (i = 0; i < weaponsCount; ++i)
            {
                GameObject desiredWeapon = WeaponMaster.GetWeapon(m_loadouts[m_currentLoadOutIndex][i].weapon);
                m_weapons.Add(GetWeapon(desiredWeapon));

                WeaponBasic weapon = m_weapons[i];
                weapon.SetMovementParent(m_parentMovement);
                weapon.SetSpeedMultiplier(m_speedMultiplier, 0, true);
                weapon.SetFireRateMultiplier(m_fireRateMultiplier, 0, true);
                weapon.SetDamageMultiplier(m_damageMultiplier, 0, true);
                weapon.SetLoadoutCount(weaponsCount);
                weapon.SetLoadoutWeaponIndex(i);
                weapon.SetStatsCharacter(m_weaponFiring.GetStatsCharacter());
                weapon.WasSwitchedOn();
                weapon.DisableWhenDone = false;
                weapon.DestroyWhenDone = false;
                weapon.transform.position = m_parentMovement.transform.position;

                m_weaponFiring.SetWeapon(weapon, i);
                OnWeaponSet(weapon);
            }

            //refresh the levelweapons
            RefreshExpForWeapons();
        }
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

    private void InternalSetWeaponCapacity(int newCapacity, bool repeatWeapons, bool keepSamePoints)
    {
        newCapacity = System.Math.Max(0, newCapacity);

        int numLoadouts = m_loadouts.Count;
        bool updateCurrentWeapon = false;
        while (--numLoadouts >= 0)
        {
            var loadout = m_loadouts[numLoadouts];
            int numWeapon = loadout.Count;
            int weaponDiff = numWeapon - newCapacity;
            updateCurrentWeapon |= m_currentLoadOutIndex == numLoadouts && numWeapon != newCapacity;

            if (weaponDiff > 0) //remove extra weapons
            {
                while (--weaponDiff >= 0)
                {
                    int lastIndex = numWeapon - 1;
                    --numWeapon;

                    if (!m_infiniteInventory) m_weaponsInInventory[loadout[lastIndex].weapon]++;

                    loadout.RemoveAt(lastIndex);
                }
            }
            else if (repeatWeapons && weaponDiff < 0) //add extra weapons 
            {
                bool hasWeapon = numWeapon > 0;
                while (++weaponDiff <= 0 && hasWeapon)
                {
                    int indexOfCopy = hasWeapon ? loadout.Count % numWeapon : 0; //repeat the initial weapons for the refill
                    int weapon = hasWeapon ? loadout[indexOfCopy].weapon : 0;
                    bool isInInventory = 0 < m_weaponsInInventory[weapon];

                    if (m_infiniteInventory || isInInventory)
                        loadout.Add(new WeaponData(loadout[indexOfCopy], keepSamePoints));

                    if (!m_infiniteInventory && isInInventory) m_weaponsInInventory[weapon]--;
                }
            }
        }

        if (updateCurrentWeapon)
        {
            InternalSetCurrentLoadout(m_currentLoadOutIndex);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < m_weapons.Count; ++i)
        {
            DestroyWeapon(m_weapons[i]);
        }
    }

    public int GetWeaponCapacity()
    {
        return m_weaponCapacity;
    }

    public void AddWeaponToInventory(int weapon, int count = 1)
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
            int count = (int)WeaponPointsTypes.NUMBER_OF_TYPES;
            for (int i = 0; i < count; ++i)
            {
                m_weapons[numWeapon].AddPoints((WeaponPointsTypes)i, float.MinValue); //set exp to be 0
                float currentExp = m_weapons[numWeapon].AddPoints((WeaponPointsTypes)i, loadOut[numWeapon].GetPoints(i));
                loadOut[numWeapon].SetPoints(i, currentExp);
            }

        }

        if (m_hudManager) m_hudManager.UpdateSliders(m_weapons);
    }

    public void AddPointsAll(WeaponPointsTypes type, float points)
    {
        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            AddPoints(type, points, numLoadouts);
        }
    }

    /**
    *   Adds exp points to the weapons equipped in this moment
    *   @param points The ammount of points to be added
*/
    public void AddPoints(WeaponPointsTypes type, float points, int loadoutIndex = -1)
    {
        if (loadoutIndex < 0) loadoutIndex = m_currentLoadOutIndex;

        List<WeaponData> loadOut = m_loadouts[m_currentLoadOutIndex];
        int numWeapon = loadOut.Count;
        //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            m_weapons[numWeapon].AddPoints(type, float.MinValue); //set exp to be the lowest value possible
            float currentExp = m_weapons[numWeapon].AddPoints(type, points + loadOut[numWeapon].GetPoints((int)type));
            loadOut[numWeapon].SetPoints((int)type, currentExp);
        }

        if (loadoutIndex == m_currentLoadOutIndex && m_hudManager) m_hudManager.UpdateSlidersValues(m_weapons);
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

    public void RemoveWeapon(int indexLoadout, int indexWeapon)
    {
        SetLoadoutWeapon(-1, indexLoadout, indexWeapon);
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


    public virtual PriorityValue<float> GetSpeedMultiplier() { return m_speedMultiplier; }
    public virtual bool SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
        {
            int weaponCount = m_weapons.Count;
            for (int i = 0; i < weaponCount; ++i)
            {
                m_weapons[i].SetSpeedMultiplier(multiplier);
            }
        }

        return changedValue;
    }

    public virtual PriorityValue<float> GetFireRateMultiplier() { return m_fireRateMultiplier; }
    public virtual bool SetFireRateMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_fireRateMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
        {
            int weaponCount = m_weapons.Count;
            for (int i = 0; i < weaponCount; ++i)
            {
                m_weapons[i].SetFireRateMultiplier(multiplier);
            }
        }

        return changedValue;
    }

    public virtual PriorityValue<float> GetDamageMultiplier() { return m_damageMultiplier; }
    public virtual bool SetDamageMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_damageMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue)
        {
            int weaponCount = m_weapons.Count;
            for (int i = 0; i < weaponCount; ++i)
            {
                m_weapons[i].SetDamageMultiplier(multiplier);
            }
        }

        return changedValue;
    }

    public virtual PriorityValue<float> GetWeaponCapacityMultiplier() { return m_weaponCapacityMultiplier; }
    public virtual bool SetWeaponCapacityMultiplier(float multiplier, uint priority = 0, bool overridePriority = false, bool repeatWeapons = true, bool keepSameExp = true)
    {
        float currentMultiplier = m_weaponCapacityMultiplier;
        bool changedValue = m_weaponCapacityMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue && multiplier != currentMultiplier)
        {
            int effectiveCapacity = (int)System.Math.Round(m_weaponCapacity * m_weaponCapacityMultiplier);
            InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSameExp);
        }

        return changedValue;
    }

    public struct WeaponData
    {
        public int weapon;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)WeaponPointsTypes.NUMBER_OF_TYPES)]
        private float[] weaponPoints; //we use marshalling to avoid creating garbage when removing a struct

        public WeaponData(int weapon = 0)
        {
            weaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            this.weapon = weapon;
        }

        public WeaponData(WeaponData data, bool copyPoints = true)
        {
            weaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            weapon = data.weapon;
            int count = (int)WeaponPointsTypes.NUMBER_OF_TYPES;
            for (int i = 0; i < count & copyPoints; ++i)
                weaponPoints[i] = data.weaponPoints[i];
        }

        public float GetPoints(WeaponPointsTypes type)
        {
            return GetPoints((int)type);
        }

        public float GetPoints(int type)
        {
            return weaponPoints[type];
        }

        public void SetPoints(WeaponPointsTypes type, float value)
        {
            SetPoints((int)type, value);
        }

        public void SetPoints(int type, float value)
        {
            weaponPoints[type] = value;
        }

    }
}


