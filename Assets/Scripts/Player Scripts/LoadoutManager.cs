using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Netcode;
using Unity.Collections;

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

    protected List<List<WeaponData>> m_loadouts = new(6);

    protected int m_currentLoadOutIndex = 0; // = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    protected PriorityValue<float> m_weaponCapacityMultiplier = new(1);
    protected PriorityValue<float> m_speedMultiplier = new(1);
    protected PriorityValue<float> m_damageMultiplier = new(1);
    protected PriorityValue<float> m_fireRateMultiplier = new(1);

    protected List<WeaponBasic> m_weapons = new(4);

    protected const int MAX_WEAPONS = 128;
    protected const int MAX_LOADOUTS = 16;

    protected static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);

    protected bool HasAuthority
    {
        get
        {
            bool ret = false;
            if (NetworkManager.Singleton) ret = NetworkManager.Singleton.IsServer;
            return ret;
        }
    }

    protected virtual void InternalAwake() { }

    public override void OnNetworkSpawn()
    {
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
    }

    private void OnDisable()
    {
        for (int i = 0; i < m_weapons.Count; ++i)
        {
            DestroyWeapon(m_weapons[i]);
        }
    }


    private void OnEnable()
    {
        SetCurrentLoadout(m_currentLoadOutIndex);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            m_hudManager = GameManager.GetHudManager();
        }

        InternalAwake();

        SetCurrentLoadout(m_currentLoadOutIndex);

        if (HasAuthority)
        {
            Test();
        }

        if (!IsOwner && IsClient && !HasAuthority)
        {
            RequestLoadoutDataServerRpc();
        }
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestLoadoutDataServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        int effectiveCapacity = (int)System.Math.Round(m_weaponCapacity * m_weaponCapacityMultiplier);
        WeaponData[] weaponsData = new WeaponData[m_loadouts.Count * effectiveCapacity];
        for (int i = 0; i < m_loadouts.Count; ++i)
        {
            List<WeaponData> loadout = m_loadouts[i];
            for (int j = 0; j < effectiveCapacity; ++j)
            {
                bool hasWeapon = null != loadout && j < loadout.Count;
                int index = i * effectiveCapacity + j;
                if (hasWeapon)
                {
                    weaponsData[index] = loadout[j];
                }
                else
                {
                    weaponsData[index] = new(-1);
                }
            }
        }

        ClientRpcParams clientParams = default;
        NativeArray<ulong> clientIdsNativeArray = new(1, Allocator.Temp);
        clientIdsNativeArray[0] = clientId;
        clientParams.Send.TargetClientIdsNativeArray = clientIdsNativeArray;
        ReceiveLoadoutDataClientRpc(weaponsData, m_loadouts.Count, effectiveCapacity, m_currentLoadOutIndex, clientParams);
        clientIdsNativeArray.Dispose();
    }

    [ClientRpc]
    public void ReceiveLoadoutDataClientRpc(WeaponData[] weaponsData, int rows, int columns, int currentLoadoutIndex, ClientRpcParams clientParams)
    {
        Debug.Log("I received some data huehuehuehuehuehuhehuehuehueheduehuehueh");
        if (null == m_loadouts) m_loadouts = new(rows);
        while (m_loadouts.Count > rows) m_loadouts.RemoveAt(m_loadouts.Count);
        while (m_loadouts.Count < rows) m_loadouts.Add(new(columns));

        for (int i = 0; i < rows; ++i)
        {
            if (m_loadouts[i] == null) m_loadouts[i] = new(columns);
            List<WeaponData> loadout = m_loadouts[i];
            loadout.Clear();

            int j = 0;
            WeaponData weaponData = weaponsData[i * columns];
            while (-1 != weaponData.Weapon) //while it has a weapon
            {
                j++;
                loadout.Add(weaponData);
                weaponData = weaponsData[i * columns + j];
            }
        }

        InternalSetCurrentLoadout(currentLoadoutIndex);
    }

    [ClientRpc]
    public void SetLoadoutAllWeaponsClientRpc(int indexLoadout, int newWeapon, bool fillToCapacity)
    {
        InternalSetLoadoutAllWeapons(indexLoadout, newWeapon, false);
    }

    [ServerRpc]
    public void SetLoadoutAllWeaponsServerRpc(int indexLoadout, int newWeapon, bool fillToCapacity)
    {
        SetLoadoutAllWeaponsClientRpc(indexLoadout, newWeapon, fillToCapacity);
    }

    public void SetLoadoutAllWeapons(int indexLoadout, int newWeapon, bool fillToCapacity)
    {
        if (HasAuthority)
        {
            SetLoadoutAllWeaponsClientRpc(indexLoadout, newWeapon, fillToCapacity);
        }
        else if (IsOwner)
        {
            SetLoadoutAllWeaponsServerRpc(indexLoadout, newWeapon, fillToCapacity);
        }
    }

    private bool InternalSetLoadoutAllWeapons(int indexLoadout, int newWeapon, bool fillToCapacity)
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

    [ClientRpc]
    public void SetLoadoutWeaponClientRpc(int indexLoadout, int indexWeapon, int newWeapon)
    {
        InternalSetLoadoutWeapon(indexLoadout, indexWeapon, newWeapon, false);
    }

    [ServerRpc]
    public void SetLoadoutWeaponServerRpc(int indexLoadout, int indexWeapon, int newWeapon)
    {
        SetLoadoutWeaponClientRpc(indexLoadout, indexWeapon, newWeapon);
    }

    public bool SetLoadoutWeapon(int indexLoadout, int indexWeapon, int newWeapon)
    {
        bool changedWeapon = false;
        if (HasAuthority)
        {
            SetLoadoutWeaponClientRpc(indexLoadout, indexWeapon, newWeapon);
        }
        else if (IsOwner)
        {
            SetLoadoutWeaponServerRpc(indexLoadout, indexWeapon, newWeapon);
        }

        return changedWeapon;
    }

    private unsafe bool InternalSetLoadoutWeapon(int indexLoadout, int indexWeapon, int newWeapon, bool refreshLoadout = true)
    {
        bool changedWeapon = false;
        if (indexLoadout < m_loadouts.Count)
        {
            int currentWeapon = -1;
            if (indexWeapon < m_loadouts[indexLoadout].Count)
                currentWeapon = m_loadouts[indexLoadout][indexWeapon].Weapon;

            bool nullWeapon = newWeapon < 0;
            bool hasWeapon = nullWeapon || (newWeapon < WeaponMaster.NumWeapons() && (m_infiniteInventory || 0 < m_weaponsInInventory[newWeapon]));

            if (hasWeapon && newWeapon != currentWeapon) //if negative, remove
            {
                if (newWeapon < 0) // if we need to remove weapon
                {
                    if (currentWeapon != -1) // if weapon exists
                    {
                        if (!m_infiniteInventory) m_weaponsInInventory[m_loadouts[indexLoadout][indexWeapon].Weapon]++; //put back in inventory
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
                        if (!m_infiniteInventory) m_weaponsInInventory[m_loadouts[indexLoadout][indexWeapon].Weapon]++; //put back in inventory

                        m_loadouts[indexLoadout][indexWeapon] = new(m_loadouts[indexLoadout][indexWeapon]);
                        changedWeapon = true;
                    }
                }

                if (changedWeapon && refreshLoadout && indexLoadout == m_currentLoadOutIndex)
                    InternalSetCurrentLoadout(m_currentLoadOutIndex);
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
        weaponToDestroy.StopFiring(false);
        weaponToDestroy.SetMovementParent(null);
        weaponToDestroy.WasSwitchedOff();

        GameObject obj = weaponToDestroy.gameObject;
        weaponToDestroy.DisableWhenDone = true;
        bool isAlive = weaponToDestroy.IsAlive();
        GfPooling.DestroyInsert(obj, isAlive);
    }

    protected virtual void OnWeaponSet(WeaponBasic weapon) { }

    protected virtual void OnWeaponsCleared() { }

    #region SET_LOADOUT_REGION

    [ServerRpc]
    protected void SetCurrentLoadoutServerRpc(int index)
    {
        SetCurrentLoadoutClientRpc(index);
    }

    [ClientRpc]
    protected void SetCurrentLoadoutClientRpc(int index)
    {
        InternalSetCurrentLoadout(index);
    }

    public void SetCurrentLoadout(int index)
    {
        if (HasAuthority)
        {
            SetCurrentLoadoutClientRpc(index);
        }
        else if (IsOwner)
        {
            SetCurrentLoadoutServerRpc(index);
        }
    }

    private void InternalSetCurrentLoadout(int indexLoadout)
    {
        if (null != m_loadouts && 0 < m_loadouts.Count)
        {
            m_currentLoadOutIndex = GfTools.Mod(indexLoadout, m_loadouts.Count);

            int i = m_weapons.Count;
            while (--i >= 0) // destroy all current weapons
            {
                DestroyWeapon(m_weapons[i]);
                m_weapons.RemoveAt(i);
            }

            OnWeaponsCleared();
            m_weaponFiring.ClearWeapons();

            int weaponsCount = m_loadouts[m_currentLoadOutIndex].Count;

            for (i = 0; i < weaponsCount; ++i)
            {
                GameObject desiredWeapon = WeaponMaster.GetWeapon(m_loadouts[m_currentLoadOutIndex][i].Weapon);
                m_weapons.Add(GetWeapon(desiredWeapon));

                WeaponBasic weapon = m_weapons[i];
                weapon.SetMovementParent(m_parentMovement);
                weapon.SetSpeedMultiplier(m_speedMultiplier, 0, true);
                weapon.SetFireRateMultiplier(m_fireRateMultiplier, 0, true);
                weapon.SetDamageMultiplier(m_damageMultiplier, 0, true);
                weapon.SetLoadoutCount(weaponsCount);
                weapon.SetLoadoutWeaponIndex(i);
                weapon.SetLoadoutIndex(m_currentLoadOutIndex);
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

    #endregion //SET_LOADOUT_REGION

    #region SET_WEAPON_CAPACITY

    [ClientRpc]
    protected void SetWeaponCapacityClientRpc(int newCapacity, bool repeatWeapons = true, bool keepSamePoints = true)
    {
        m_weaponCapacity = newCapacity;
        int effectiveCapacity = (int)System.Math.Round(newCapacity * m_weaponCapacityMultiplier);
        InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSamePoints);
    }

    public void SetWeaponCapacity(int newCapacity, bool repeatWeapons = true, bool keepSamePoints = true)
    {
        if (HasAuthority)
        {
            if (newCapacity != m_weaponCapacity)
            {
                m_weaponCapacity = newCapacity;
                int effectiveCapacity = (int)System.Math.Round(newCapacity * m_weaponCapacityMultiplier);
                InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSamePoints);
                SetWeaponCapacityClientRpc(newCapacity, repeatWeapons, keepSamePoints);
            }
        }
    }

    protected void InternalSetWeaponCapacity(int newCapacity, bool repeatWeapons, bool keepSamePoints)
    {
        newCapacity = System.Math.Min(MAX_WEAPONS, System.Math.Max(0, newCapacity));

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

                    if (!m_infiniteInventory) m_weaponsInInventory[loadout[lastIndex].Weapon]++;

                    loadout.RemoveAt(lastIndex);
                }
            }
            else if (repeatWeapons && weaponDiff < 0) //add extra weapons 
            {
                bool hasWeapon = numWeapon > 0;
                while (++weaponDiff <= 0 && hasWeapon)
                {
                    int indexOfCopy = hasWeapon ? loadout.Count % numWeapon : 0; //repeat the initial weapons for the refill
                    int weapon = hasWeapon ? loadout[indexOfCopy].Weapon : 0;
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
    #endregion //SET_WEAPON_CAPACITY

    public int GetWeaponCapacity()
    {
        return m_weaponCapacity;
    }

    /* TODO has to work with network behaviour
    public void AddWeaponToInventory(int weapon, int count = 1)
    {
        if (weapon >= 0 && m_weaponsInInventory != null && null != WeaponMaster.GetWeapon(weapon))
        {
            m_weaponsInInventory[weapon] = count + m_weaponsInInventory[weapon];
        }
    }*/

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

        if (m_hudManager) m_hudManager.UpdateWeaponLevelSlidersNumber(m_weapons);
    }

    public void AddPointsAll(WeaponPointsTypes type, float points)
    {
        if (HasAuthority)
            AddPointAllClientRpc(type, points);
        else if (IsOwner)
            AddPointAllServerRpc(type, points);
    }

    [ServerRpc]
    private void AddPointAllServerRpc(WeaponPointsTypes type, float points)
    {
        AddPointAllClientRpc(type, points);
    }

    [ClientRpc]
    private void AddPointAllClientRpc(WeaponPointsTypes type, float points)
    {
        InternalAddPointsAll(type, points);
    }

    protected void InternalAddPointsAll(WeaponPointsTypes type, float points)
    {
        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            InternalAddPoints(type, points, numLoadouts);
        }
    }

    public void AddPoints(WeaponPointsTypes type, float points, int loadoutIndex = -1)
    {
        if (HasAuthority)
            AddPointsClientRpc(type, points, loadoutIndex);
        else if (IsOwner)
            AddPointsServerRpc(type, points, loadoutIndex);
    }

    [ServerRpc]
    private void AddPointsServerRpc(WeaponPointsTypes type, float points, int loadoutIndex)
    {
        AddPointsClientRpc(type, points, loadoutIndex);
    }

    [ClientRpc]
    private void AddPointsClientRpc(WeaponPointsTypes type, float points, int loadoutIndex)
    {
        InternalAddPoints(type, points, loadoutIndex);
    }

    /**
    *   Adds exp points to the weapons equipped in this moment
    *   @param points The ammount of points to be added
    */
    private void InternalAddPoints(WeaponPointsTypes type, float points, int loadoutIndex = -1)
    {
        if (loadoutIndex < 0) loadoutIndex = m_currentLoadOutIndex;

        if (null != m_loadouts && 0 < m_loadouts.Count && m_loadouts.Count >= loadoutIndex)
        {
            List<WeaponData> loadOut = m_loadouts[m_currentLoadOutIndex];
            int numWeapon = loadOut.Count;
            //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);

            while (--numWeapon >= 0)
            {
                m_weapons[numWeapon].AddPoints(type, float.MinValue); //set exp to be the lowest value possible
                float currentExp = m_weapons[numWeapon].AddPoints(type, points + loadOut[numWeapon].GetPoints((int)type));
                loadOut[numWeapon].SetPoints((int)type, currentExp);
            }

            if (loadoutIndex == m_currentLoadOutIndex && m_hudManager) m_hudManager.UpdateWeaponLevelSlidersValues(m_weapons);
        }
    }

    [ClientRpc]
    protected void AddLoadoutClientRpc(int count)
    {
        InternalAddLoadout(count);
    }

    [ServerRpc]
    protected void AddLoadoutServerRpc(int count)
    {
        AddLoadoutClientRpc(count);
    }

    public void AddLoadout(int count = 1)
    {
        if (HasAuthority)
        {
            AddLoadoutClientRpc(count);
        }
        else if (IsOwner)
        {
            AddLoadoutServerRpc(count);
        }
    }

    protected void InternalAddLoadout(int count = 1)
    {
        while (--count >= 0 && m_loadouts.Count < MAX_LOADOUTS)
            m_loadouts.Add(new(m_weaponCapacity)); //todo
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
        return m_loadouts[indexLoadout][indexWeapon].Weapon;
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
        SetCurrentLoadout(m_currentLoadOutIndex - 1);
    }

    public List<WeaponBasic> GetWeapons()
    {
        return m_weapons;
    }

    #region SPEED_MULTIPLIER_REGION

    public virtual PriorityValue<float> GetSpeedMultiplier() { return m_speedMultiplier; }

    protected virtual bool InternalSetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        float oldMultiplier = m_fireRateMultiplier.Value;
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority) && multiplier != oldMultiplier;
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

    public virtual bool SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = false;
        if (HasAuthority)
        {
            changedValue = InternalSetSpeedMultiplier(multiplier, priority, overridePriority);
            if (changedValue) SetSpeedMultiplierClientRpc(multiplier, priority);
        }

        return changedValue;
    }

    [ClientRpc]
    protected virtual void SetSpeedMultiplierClientRpc(float multiplier, uint priority = 0)
    {
        InternalSetSpeedMultiplier(multiplier, priority, true); //override regardless of priority
    }

    #endregion //SPEED_MULTIPLIER_REGION

    #region FIRE_RATE_MULTIPLIER_REGION

    public virtual PriorityValue<float> GetFireRateMultiplier() { return m_fireRateMultiplier; }

    protected virtual bool InternalSetFireRateMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        float oldMultiplier = m_fireRateMultiplier.Value;
        bool changedValue = m_fireRateMultiplier.SetValue(multiplier, priority, overridePriority) && oldMultiplier != multiplier;
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

    public virtual bool SetFireRateMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = false;
        if (HasAuthority)
        {
            changedValue = InternalSetFireRateMultiplier(multiplier, priority, overridePriority);
            if (changedValue) SetFireRateMultiplierClientRpc(multiplier, priority);
        }

        return changedValue;
    }

    [ClientRpc]
    protected virtual void SetFireRateMultiplierClientRpc(float multiplier, uint priority = 0)
    {
        InternalSetFireRateMultiplier(multiplier, priority, true); //override regardless of priority
    }

    #endregion //FIRE_RATE_MULTIPLIER_REGION

    #region DAMAGE_MULTIPLIER_REGION
    public virtual PriorityValue<float> GetDamageMultiplier() { return m_damageMultiplier; }

    protected virtual bool InternalSetDamageMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        float oldMultiplier = m_damageMultiplier.Value;
        bool changedValue = m_damageMultiplier.SetValue(multiplier, priority, overridePriority) && oldMultiplier != multiplier;
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

    public virtual bool SetDamageMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = false;
        if (HasAuthority)
        {
            changedValue = InternalSetDamageMultiplier(multiplier, priority, overridePriority);
            if (changedValue) SetDamageMultiplierClientRpc(multiplier, priority);
        }

        return changedValue;
    }

    [ClientRpc]
    protected virtual void SetDamageMultiplierClientRpc(float multiplier, uint priority = 0)
    {
        InternalSetDamageMultiplier(multiplier, priority, true); //override regardless of priority
    }

    #endregion //DAMAGE_MULTIPLIER_REGION

    #region WEAPON_CAPACITY_MULTIPLIER
    public virtual PriorityValue<float> GetWeaponCapacityMultiplier() { return m_weaponCapacityMultiplier; }

    protected virtual bool InternalSetWeaponCapacityMultiplier(float multiplier, uint priority = 0, bool overridePriority = false, bool repeatWeapons = true, bool keepSameExp = true)
    {
        float oldMultiplier = m_weaponCapacityMultiplier;
        bool changedValue = m_weaponCapacityMultiplier.SetValue(multiplier, priority, overridePriority) && multiplier != oldMultiplier;
        if (changedValue)
        {
            int effectiveCapacity = (int)System.Math.Round(m_weaponCapacity * m_weaponCapacityMultiplier);
            InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSameExp);
        }
        return changedValue;
    }

    public virtual bool SetWeaponCapacityMultiplier(float multiplier, uint priority = 0, bool overridePriority = false, bool repeatWeapons = true, bool keepSameExp = true)
    {
        bool changedValue = false;
        if (HasAuthority)
        {
            changedValue = InternalSetWeaponCapacityMultiplier(multiplier, priority, overridePriority);
            if (changedValue) SetWeaponCapacityMultiplierClientRpc(multiplier, priority);
        }

        return changedValue;
    }

    [ClientRpc]
    protected virtual void SetWeaponCapacityMultiplierClientRpc(float multiplier, uint priority = 0, bool repeatWeapons = true, bool keepSameExp = true)
    {
        InternalSetWeaponCapacityMultiplier(multiplier, priority, true); //override regardless of priority
    }

    #endregion //WEAPON_CAPACITY_MULTIPLIER

    [System.Serializable]
    public struct WeaponData : INetworkSerializable
    {
        public int Weapon;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)WeaponPointsTypes.NUMBER_OF_TYPES)]
        private float[] WeaponPoints; //we use marshalling to avoid creating garbage when removing a struct

        public WeaponData(int weapon = -1)
        {
            WeaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            this.Weapon = weapon;
        }

        public WeaponData(WeaponData data, bool copyPoints = true)
        {
            WeaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            Weapon = data.Weapon;
            int count = (int)WeaponPointsTypes.NUMBER_OF_TYPES;
            for (int i = 0; i < count & copyPoints; ++i)
                WeaponPoints[i] = data.WeaponPoints[i];
        }

        public float GetPoints(WeaponPointsTypes type)
        {
            return GetPoints((int)type);
        }

        public float GetPoints(int type)
        {
            return WeaponPoints[type];
        }

        public void SetPoints(WeaponPointsTypes type, float value)
        {
            SetPoints((int)type, value);
        }

        public void SetPoints(int type, float value)
        {
            WeaponPoints[type] = value;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Length
            int length = 0;
            if (!serializer.IsReader)
            {
                length = WeaponPoints.Length;
            }

            serializer.SerializeValue(ref length);
            serializer.SerializeValue(ref Weapon);

            // Array
            if (serializer.IsReader)
            {
                WeaponPoints = new float[length];
            }

            for (int n = 0; n < length; ++n)
            {
                serializer.SerializeValue(ref WeaponPoints[n]);
            }
        }
    }
}


